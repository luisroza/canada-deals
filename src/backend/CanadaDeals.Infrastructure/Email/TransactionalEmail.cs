using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using CanadaDeals.Domain.Notifications;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Email;

public sealed class TransactionalEmailOptions
{
    public const string SectionName = "Email";
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Controlled";
    public string? ApiKey { get; set; }
    public string? FromAddress { get; set; }
    public string FromDisplayName { get; set; } = "Canada Deals";
    public string? PublicOrigin { get; set; }
    public string? WebhookSigningSecret { get; set; }
    public bool AutoConfirmDevelopmentAccounts { get; set; } = true;
    public int ConfirmationTokenHours { get; set; } = 24;
    public int MaxDeliveryAttempts { get; set; } = 4;
}

public enum EmailSendOutcome { DevelopmentCaptured, ProviderAccepted, TransientFailure, PermanentFailure, Suppressed }

public sealed record TransactionalEmailMessage(
    string IdempotencyKey,
    string DestinationAddress,
    string Subject,
    string HtmlBody,
    string TextBody);

public sealed record EmailSendResult(EmailSendOutcome Outcome, string? ProviderMessageId = null, string? Reason = null, TimeSpan? RetryAfter = null);

public interface ITransactionalEmailSender
{
    Task<EmailSendResult> SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken);
}

public sealed class ControlledTransactionalEmailSender(DealsDbContext db, TimeProvider clock) : ITransactionalEmailSender
{
    public async Task<EmailSendResult> SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken)
    {
        var exists = await db.ControlledEmailCaptures.AnyAsync(x => x.IdempotencyKey == message.IdempotencyKey, cancellationToken);
        if (!exists)
        {
            db.ControlledEmailCaptures.Add(ControlledEmailCapture.Create(
                message.IdempotencyKey, message.DestinationAddress, message.Subject, message.HtmlBody, message.TextBody, clock.GetUtcNow()));
            await db.SaveChangesAsync(cancellationToken);
        }
        return new EmailSendResult(EmailSendOutcome.DevelopmentCaptured);
    }
}

public sealed class ResendTransactionalEmailSender(HttpClient client, IOptions<TransactionalEmailOptions> options) : ITransactionalEmailSender
{
    private readonly TransactionalEmailOptions settings = options.Value;

    public async Task<EmailSendResult> SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new("Bearer", settings.ApiKey);
        request.Headers.Add("Idempotency-Key", message.IdempotencyKey);
        request.Content = JsonContent.Create(new
        {
            from = $"{settings.FromDisplayName} <{settings.FromAddress}>",
            to = new[] { message.DestinationAddress },
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.TextBody
        });

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                using var body = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
                var id = body.RootElement.TryGetProperty("id", out var value) ? value.GetString() : null;
                return string.IsNullOrWhiteSpace(id)
                    ? new EmailSendResult(EmailSendOutcome.TransientFailure, Reason: "PROVIDER_RESPONSE_MISSING_ID")
                    : new EmailSendResult(EmailSendOutcome.ProviderAccepted, id);
            }

            var retryAfter = response.Headers.RetryAfter?.Delta;
            if (!retryAfter.HasValue && response.Headers.RetryAfter?.Date is { } retryDate)
                retryAfter = retryDate - DateTimeOffset.UtcNow;
            if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.Conflict || (int)response.StatusCode >= 500)
                return new EmailSendResult(EmailSendOutcome.TransientFailure, Reason: $"PROVIDER_HTTP_{(int)response.StatusCode}", RetryAfter: retryAfter);

            return new EmailSendResult(EmailSendOutcome.PermanentFailure, Reason: $"PROVIDER_HTTP_{(int)response.StatusCode}");
        }
        catch (HttpRequestException)
        {
            return new EmailSendResult(EmailSendOutcome.TransientFailure, Reason: "PROVIDER_NETWORK_FAILURE");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new EmailSendResult(EmailSendOutcome.TransientFailure, Reason: "PROVIDER_TIMEOUT");
        }
    }
}

public static class TransactionalEmailTemplates
{
    public static TransactionalEmailMessage AccountConfirmation(string key, string destination, string confirmationUrl)
    {
        var url = HtmlEncoder.Default.Encode(confirmationUrl);
        return new(key, destination, "Confirm your Canada Deals account",
            $"<h1>Confirm your email</h1><p>Confirm this address to activate your Canada Deals account.</p><p><a href=\"{url}\">Confirm email</a></p><p>If you did not create this account, you can ignore this message.</p>",
            $"Confirm your email to activate your Canada Deals account:\n\n{confirmationUrl}\n\nIf you did not create this account, you can ignore this message.");
    }

    public static TransactionalEmailMessage PriceAlert(string key, string destination, string productTitle, string productSlug, decimal targetPrice, decimal qualifyingPrice, string currency, string publicOrigin)
    {
        var title = HtmlEncoder.Default.Encode(productTitle);
        var productUrl = $"{publicOrigin.TrimEnd('/')}/products/{Uri.EscapeDataString(productSlug)}";
        var manageUrl = $"{publicOrigin.TrimEnd('/')}/saved";
        var price = qualifyingPrice.ToString("0.00", CultureInfo.InvariantCulture);
        var target = targetPrice.ToString("0.00", CultureInfo.InvariantCulture);
        return new(key, destination, $"Price alert: {productTitle} is {currency} {price}",
            $"<h1>Your target price was reached</h1><p><strong>{title}</strong> has a qualifying observed price of {currency} {price}, at or below your target of {currency} {target}.</p><p>Prices can change. Review freshness, seller, variant, availability, and shipping before buying.</p><p><a href=\"{HtmlEncoder.Default.Encode(productUrl)}\">Review price evidence</a></p><p><a href=\"{HtmlEncoder.Default.Encode(manageUrl)}\">Manage or disable this alert</a></p>",
            $"Your target price was reached\n\n{productTitle}\nQualifying observed price: {currency} {price}\nYour target: {currency} {target}\n\nPrices can change. Review freshness, seller, variant, availability, and shipping before buying.\n\nReview price evidence: {productUrl}\nManage or disable this alert: {manageUrl}");
    }
}

public static class TransactionalEmailServices
{
    public static IServiceCollection AddCanadaDealsTransactionalEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(TransactionalEmailOptions.SectionName);
        services.Configure<TransactionalEmailOptions>(section);
        var settings = section.Get<TransactionalEmailOptions>() ?? new();
        Validate(settings, environment);

        services.AddScoped<ControlledTransactionalEmailSender>();
        services.AddHttpClient<ResendTransactionalEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CanadaDeals/1.0");
        });
        services.AddScoped<ITransactionalEmailSender>(provider => settings.Enabled
            ? provider.GetRequiredService<ResendTransactionalEmailSender>()
            : provider.GetRequiredService<ControlledTransactionalEmailSender>());
        return services;
    }

    private static void Validate(TransactionalEmailOptions settings, IHostEnvironment environment)
    {
        if (settings.ConfirmationTokenHours is < 1 or > 72) throw new InvalidOperationException("Email:ConfirmationTokenHours must be between 1 and 72.");
        if (settings.MaxDeliveryAttempts is < 1 or > 10) throw new InvalidOperationException("Email:MaxDeliveryAttempts must be between 1 and 10.");
        if (environment.IsProduction() && !settings.Enabled) throw new InvalidOperationException("Production email delivery must be enabled.");
        if (!settings.Enabled) return;
        if (!string.Equals(settings.Provider, "Resend", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Email:Provider must be Resend when delivery is enabled.");
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.FromAddress) || string.IsNullOrWhiteSpace(settings.WebhookSigningSecret))
            throw new InvalidOperationException("Email provider credentials, sender address, and webhook signing secret are required.");
        if (!Uri.TryCreate(settings.PublicOrigin, UriKind.Absolute, out var origin) ||
            origin.Scheme != Uri.UriSchemeHttps ||
            origin.PathAndQuery != "/" ||
            !string.IsNullOrEmpty(origin.Fragment) ||
            !string.IsNullOrEmpty(origin.UserInfo) ||
            !string.Equals(settings.PublicOrigin.TrimEnd('/'), origin.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Email:PublicOrigin must be a canonical HTTPS origin without a path.");
    }
}

public static class EmailSuppressionStore
{
    public static Task SuppressAsync(DealsDbContext db, string address, string reason, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var normalized = address.Trim().ToUpperInvariant();
        return db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""EmailSuppressions"" (""Id"", ""NormalizedAddress"", ""Reason"", ""CreatedAt"", ""UpdatedAt"")
            VALUES ({Guid.NewGuid()}, {normalized}, {reason}, {at}, {at})
            ON CONFLICT (""NormalizedAddress"") DO UPDATE SET ""Reason"" = EXCLUDED.""Reason"", ""UpdatedAt"" = EXCLUDED.""UpdatedAt""", cancellationToken);
    }
}
