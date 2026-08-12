using System.Text;
using CanadaDeals.Domain.Notifications;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.Services;

public enum AccountConfirmationResult { Confirmed, AlreadyConfirmed, Invalid }

public sealed class AccountConfirmationEmailService(
    UserManager<ApplicationUser> userManager,
    DealsDbContext db,
    ITransactionalEmailSender sender,
    IOptions<TransactionalEmailOptions> options,
    TimeProvider clock,
    ILogger<AccountConfirmationEmailService> logger)
{
    public async Task SendAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email)) return;
        var now = clock.GetUtcNow();
        var delivery = AccountConfirmationDelivery.Create(user.Id, user.Email, now);
        db.AccountConfirmationDeliveries.Add(delivery);
        await db.SaveChangesAsync(cancellationToken);

        var normalized = user.Email.Trim().ToUpperInvariant();
        if (await db.EmailSuppressions.AnyAsync(x => x.NormalizedAddress == normalized, cancellationToken))
        {
            delivery.Suppress("ADDRESS_SUPPRESSED", now);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Account confirmation delivery {DeliveryId} was suppressed.", delivery.Id);
            return;
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var origin = options.Value.PublicOrigin?.TrimEnd('/') ?? "http://localhost:3000";
        var url = $"{origin}/account/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&code={Uri.EscapeDataString(encodedToken)}";
        var message = TransactionalEmailTemplates.AccountConfirmation(delivery.IdempotencyKey, user.Email, url);

        delivery.RecordAttempt(now);
        await db.SaveChangesAsync(cancellationToken);
        var result = await sender.SendAsync(message, cancellationToken);
        Apply(delivery, result, clock.GetUtcNow());
        if (!string.IsNullOrWhiteSpace(delivery.ProviderMessageId))
        {
            var priorEvent = await db.ProcessedEmailWebhooks
                .Where(x => x.Provider == "RESEND" && x.ProviderMessageId == delivery.ProviderMessageId)
                .OrderByDescending(x => x.ProviderCreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (priorEvent is not null)
            {
                delivery.ApplyProviderEvent(priorEvent.EventType, priorEvent.ProviderCreatedAt);
                if (priorEvent.EventType is "email.bounced" or "email.complained" or "email.suppressed")
                    await EmailSuppressionStore.SuppressAsync(db, delivery.DestinationAddress, priorEvent.EventType, priorEvent.ProviderCreatedAt, cancellationToken);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Account confirmation delivery {DeliveryId} processed as {Status}.", delivery.Id, delivery.Status);
    }

    public async Task<AccountConfirmationResult> ConfirmAsync(Guid userId, string encodedCode)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || string.IsNullOrWhiteSpace(encodedCode)) return AccountConfirmationResult.Invalid;
        if (user.EmailConfirmed) return AccountConfirmationResult.AlreadyConfirmed;
        try
        {
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCode));
            var result = await userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded ? AccountConfirmationResult.Confirmed : AccountConfirmationResult.Invalid;
        }
        catch (FormatException)
        {
            return AccountConfirmationResult.Invalid;
        }
    }

    private static void Apply(AccountConfirmationDelivery delivery, EmailSendResult result, DateTimeOffset now)
    {
        switch (result.Outcome)
        {
            case EmailSendOutcome.DevelopmentCaptured: delivery.CaptureForDevelopment(now); break;
            case EmailSendOutcome.ProviderAccepted: delivery.Accept(result.ProviderMessageId!, now); break;
            case EmailSendOutcome.TransientFailure: delivery.FailTransient(result.Reason ?? "TRANSIENT_PROVIDER_FAILURE"); break;
            case EmailSendOutcome.PermanentFailure: delivery.FailPermanent(result.Reason ?? "PERMANENT_PROVIDER_FAILURE", now); break;
            default: delivery.Suppress(result.Reason ?? "DELIVERY_SUPPRESSED", now); break;
        }
    }
}
