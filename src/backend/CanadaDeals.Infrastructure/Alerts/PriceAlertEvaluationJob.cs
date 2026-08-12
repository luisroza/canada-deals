using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Alerts;

public sealed class PriceAlertEvaluationJob(
    DealsDbContext db,
    ITransactionalEmailSender emailSender,
    IOptions<TransactionalEmailOptions> emailOptions,
    IBackgroundJobClient backgroundJobs,
    TimeProvider clock,
    ILogger<PriceAlertEvaluationJob> logger)
{
    public async Task RunAsync()
    {
        var cancellationToken = CancellationToken.None;
        var alertIds = await db.PriceAlerts
            .AsNoTracking()
            .Where(x => x.Status == PriceAlertStatus.Active)
            .OrderBy(x => x.LastEvaluatedAt)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var alertId in alertIds)
            await EvaluateAlertAsync(alertId, cancellationToken);

        await ProcessPendingDeliveriesAsync(cancellationToken);
    }

    private async Task EvaluateAlertAsync(Guid alertId, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({alertId.ToString()}, 0))",
            cancellationToken);

        var alert = await db.PriceAlerts.SingleOrDefaultAsync(x => x.Id == alertId, cancellationToken);
        if (alert is null || alert.Status != PriceAlertStatus.Active)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var user = await db.Users
            .AsNoTracking()
            .Where(x => x.Id == alert.UserId)
            .Select(x => new { x.Email, x.EmailConfirmed })
            .SingleAsync(cancellationToken);
        var now = clock.GetUtcNow();
        if (!user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
        {
            alert.RecordEvaluation(now);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Price alert {AlertId} evaluation skipped because the account email is not confirmed.", alert.Id);
            return;
        }

        var candidates = await LoadCandidatesAsync(alert.ProductId, cancellationToken);
        var result = PriceAlertEvaluator.Evaluate(alert, candidates, now);

        switch (result.Outcome)
        {
            case PriceAlertEvaluationOutcome.Eligible:
                var candidate = result.QualifyingCandidate!;
                var alreadyExists = await db.NotificationDeliveries.AnyAsync(
                    x => x.PriceAlertId == alert.Id &&
                         x.TargetVersion == alert.TargetVersion &&
                         x.PriceObservationId == candidate.PriceObservationId,
                    cancellationToken);
                if (!alreadyExists)
                {
                    db.NotificationDeliveries.Add(NotificationDelivery.Create(
                        alert.Id,
                        candidate.PriceObservationId,
                        alert.TargetVersion,
                        alert.TargetPrice,
                        candidate.Amount,
                        candidate.Currency,
                        user.Email,
                        now));
                    alert.RecordTriggered(now);
                }
                else
                {
                    alert.RecordEvaluation(now);
                }
                break;
            case PriceAlertEvaluationOutcome.AboveTarget:
                alert.RecordAboveTarget(now);
                break;
            default:
                alert.RecordEvaluation(now);
                break;
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation(
            "Price alert {AlertId} for product {ProductId} evaluated as {Outcome}.",
            alert.Id,
            alert.ProductId,
            result.Outcome);
    }

    private async Task<IReadOnlyList<AlertPriceCandidate>> LoadCandidatesAsync(Guid productId, CancellationToken cancellationToken)
    {
        var listings = await db.RetailerListings
            .AsNoTracking()
            .Include(x => x.MerchantPolicy)
            .Where(x => x.ProductId == productId && x.CurrentPriceAmount != null && x.CurrentPriceCurrency != null)
            .ToListAsync(cancellationToken);
        if (listings.Count == 0) return [];

        var listingIds = listings.Select(x => x.Id).ToArray();
        var observations = await db.PriceObservations
            .AsNoTracking()
            .Where(x => listingIds.Contains(x.RetailerListingId))
            .OrderByDescending(x => x.ObservedAt)
            .ToListAsync(cancellationToken);

        var candidates = new List<AlertPriceCandidate>();
        foreach (var listing in listings)
        {
            var observation = observations.FirstOrDefault(x =>
                x.RetailerListingId == listing.Id &&
                x.Amount == listing.CurrentPriceAmount &&
                x.Currency == listing.CurrentPriceCurrency);
            if (observation is null) continue;

            candidates.Add(new AlertPriceCandidate(
                observation.Id,
                observation.Amount,
                observation.Currency,
                observation.ObservedAt,
                observation.IsPermitted,
                listing.MerchantPolicy.AllowPriceStorage,
                listing.MatchState,
                listing.OnlineAvailability,
                listing.MerchantPolicy.PriceMaxAgeHours));
        }

        return candidates;
    }

    private async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        var eligibleAt = clock.GetUtcNow();
        var deliveryIds = await db.NotificationDeliveries
            .AsNoTracking()
            .Where(x => (x.Status == NotificationDeliveryStatus.Pending || (x.Status == NotificationDeliveryStatus.TransientFailure && (x.NextAttemptAt == null || x.NextAttemptAt <= eligibleAt))) && x.AttemptCount < emailOptions.Value.MaxDeliveryAttempts)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        foreach (var deliveryId in deliveryIds)
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({deliveryId.ToString()}, 0))",
                cancellationToken);
            var delivery = await db.NotificationDeliveries
                .Include(x => x.PriceAlert)
                .ThenInclude(x => x.Product)
                .SingleOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);
            if (delivery is null || delivery.Status is not (NotificationDeliveryStatus.Pending or NotificationDeliveryStatus.TransientFailure))
            {
                await transaction.CommitAsync(cancellationToken);
                continue;
            }

            var now = clock.GetUtcNow();
            if (delivery.LastAttemptedAt.HasValue && now - delivery.LastAttemptedAt.Value >= TimeSpan.FromHours(23))
            {
                delivery.Suppress("IDEMPOTENCY_WINDOW_EXPIRED_AFTER_AMBIGUOUS_ATTEMPT", now);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                continue;
            }
            var normalizedAddress = delivery.DestinationAddress.Trim().ToUpperInvariant();
            if (await db.EmailSuppressions.AnyAsync(x => x.NormalizedAddress == normalizedAddress, cancellationToken))
            {
                delivery.Suppress("ADDRESS_SUPPRESSED", now);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                continue;
            }

            delivery.RecordAttempt(now);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var message = TransactionalEmailTemplates.PriceAlert(
                delivery.IdempotencyKey,
                delivery.DestinationAddress,
                delivery.PriceAlert.Product.Title,
                delivery.PriceAlert.Product.Slug,
                delivery.TargetPrice,
                delivery.QualifyingPrice,
                delivery.Currency,
                emailOptions.Value.PublicOrigin ?? "http://localhost:3000");
            var result = await emailSender.SendAsync(message, cancellationToken);

            db.ChangeTracker.Clear();
            await using var resultTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({deliveryId.ToString()}, 0))",
                cancellationToken);
            var persisted = await db.NotificationDeliveries.SingleAsync(x => x.Id == deliveryId, cancellationToken);
            ApplyDeliveryResult(persisted, result, clock.GetUtcNow(), emailOptions.Value.MaxDeliveryAttempts);
            if (!string.IsNullOrWhiteSpace(persisted.ProviderMessageId))
            {
                var priorEvent = await db.ProcessedEmailWebhooks
                    .Where(x => x.Provider == "RESEND" && x.ProviderMessageId == persisted.ProviderMessageId)
                    .OrderByDescending(x => x.ProviderCreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (priorEvent is not null)
                {
                    persisted.ApplyProviderEvent(priorEvent.EventType, priorEvent.ProviderCreatedAt);
                    if (priorEvent.EventType is "email.bounced" or "email.complained" or "email.suppressed")
                        await EmailSuppressionStore.SuppressAsync(db, persisted.DestinationAddress, priorEvent.EventType, priorEvent.ProviderCreatedAt, cancellationToken);
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            await resultTransaction.CommitAsync(cancellationToken);
            if (persisted.Status == NotificationDeliveryStatus.TransientFailure && persisted.NextAttemptAt.HasValue)
            {
                var delay = persisted.NextAttemptAt.Value - clock.GetUtcNow();
                var retryJobId = backgroundJobs.Schedule<PriceAlertEvaluationJob>(job => job.RunAsync(), delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1));
                logger.LogWarning("Notification delivery {DeliveryId} scheduled bounded retry job {RetryJobId}.", persisted.Id, retryJobId);
            }
            logger.LogInformation("Notification delivery {DeliveryId} processed as {Status}.", persisted.Id, persisted.Status);
        }
    }

    private static void ApplyDeliveryResult(NotificationDelivery delivery, EmailSendResult result, DateTimeOffset now, int maxAttempts)
    {
        switch (result.Outcome)
        {
            case EmailSendOutcome.DevelopmentCaptured:
                delivery.CaptureForDevelopment(now);
                break;
            case EmailSendOutcome.ProviderAccepted:
                delivery.Accept(result.ProviderMessageId!, now);
                break;
            case EmailSendOutcome.TransientFailure when delivery.AttemptCount < maxAttempts:
                var retryDelay = result.RetryAfter ?? TimeSpan.FromMinutes(Math.Min(30, Math.Pow(2, delivery.AttemptCount)));
                delivery.FailTransient(result.Reason ?? "TRANSIENT_PROVIDER_FAILURE", now.Add(retryDelay));
                break;
            case EmailSendOutcome.TransientFailure:
                delivery.FailPermanent("MAX_DELIVERY_ATTEMPTS_EXHAUSTED", now);
                break;
            case EmailSendOutcome.PermanentFailure:
                delivery.FailPermanent(result.Reason ?? "PERMANENT_PROVIDER_FAILURE", now);
                break;
            default:
                delivery.Suppress(result.Reason ?? "DELIVERY_SUPPRESSED", now);
                break;
        }
    }
}
