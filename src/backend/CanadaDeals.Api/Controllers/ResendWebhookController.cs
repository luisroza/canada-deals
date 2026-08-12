using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CanadaDeals.Domain.Notifications;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/email/resend")]
public sealed class ResendWebhookController(
    DealsDbContext db,
    IOptions<TransactionalEmailOptions> options,
    TimeProvider clock,
    ILogger<ResendWebhookController> logger) : ControllerBase
{
    private static readonly HashSet<string> SupportedEvents = ["email.sent", "email.delivered", "email.failed", "email.bounced", "email.complained", "email.suppressed"];

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(262_144)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var eventId = Request.Headers["svix-id"].ToString();
        var timestamp = Request.Headers["svix-timestamp"].ToString();
        var signature = Request.Headers["svix-signature"].ToString();
        if (!SvixSignatureVerifier.Verify(rawBody, eventId, timestamp, signature, options.Value.WebhookSigningSecret, clock.GetUtcNow()))
            return Unauthorized();

        ResendWebhookEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ResendWebhookEnvelope>(rawBody, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                ?? throw new JsonException();
        }
        catch (JsonException)
        {
            return BadRequest();
        }
        if (!SupportedEvents.Contains(envelope.Type) || string.IsNullOrWhiteSpace(envelope.Data?.EmailId)) return NoContent();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({eventId}, 0))", cancellationToken);
        if (await db.ProcessedEmailWebhooks.AnyAsync(x => x.Provider == "RESEND" && x.EventId == eventId, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return NoContent();
        }

        var eventAt = envelope.CreatedAt;
        var alertDelivery = await db.NotificationDeliveries.SingleOrDefaultAsync(x => x.ProviderMessageId == envelope.Data.EmailId, cancellationToken);
        var accountDelivery = alertDelivery is null
            ? await db.AccountConfirmationDeliveries.SingleOrDefaultAsync(x => x.ProviderMessageId == envelope.Data.EmailId, cancellationToken)
            : null;
        alertDelivery?.ApplyProviderEvent(envelope.Type, eventAt);
        accountDelivery?.ApplyProviderEvent(envelope.Type, eventAt);

        if ((alertDelivery is not null || accountDelivery is not null) && envelope.Type is "email.bounced" or "email.complained" or "email.suppressed")
        {
            var address = alertDelivery?.DestinationAddress ?? accountDelivery?.DestinationAddress;
            if (!string.IsNullOrWhiteSpace(address))
                await EmailSuppressionStore.SuppressAsync(db, address, envelope.Type, eventAt, cancellationToken);
        }

        db.ProcessedEmailWebhooks.Add(ProcessedEmailWebhook.Create("RESEND", eventId, envelope.Type, envelope.Data.EmailId, eventAt, clock.GetUtcNow()));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Resend webhook {EventId} ({EventType}) processed.", eventId, envelope.Type);
        return NoContent();
    }

    private sealed record ResendWebhookEnvelope(string Type, [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt, ResendWebhookData? Data);
    private sealed record ResendWebhookData([property: JsonPropertyName("email_id")] string EmailId, string[]? To);
}

internal static class SvixSignatureVerifier
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromMinutes(5);

    public static bool Verify(string body, string eventId, string timestamp, string signatureHeader, string? secret, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(secret)) return false;
        if (!long.TryParse(timestamp, out var seconds)) return false;
        var signedAt = DateTimeOffset.FromUnixTimeSeconds(seconds);
        if ((now - signedAt).Duration() > Tolerance) return false;
        try
        {
            var encodedSecret = secret.StartsWith("whsec_", StringComparison.Ordinal) ? secret[6..] : secret;
            var key = Convert.FromBase64String(encodedSecret);
            var payload = Encoding.UTF8.GetBytes($"{eventId}.{timestamp}.{body}");
            var expected = HMACSHA256.HashData(key, payload);
            foreach (var part in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = part.IndexOf(',');
                if (separator < 0 || part[..separator] != "v1") continue;
                var supplied = Convert.FromBase64String(part[(separator + 1)..]);
                if (CryptographicOperations.FixedTimeEquals(expected, supplied)) return true;
            }
        }
        catch (FormatException) { }
        return false;
    }
}
