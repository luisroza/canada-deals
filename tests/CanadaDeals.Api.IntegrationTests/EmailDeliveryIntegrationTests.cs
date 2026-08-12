using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Notifications;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class EmailDeliveryIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Password = "SecurePass42";
    private static readonly string WebhookSecret = "whsec_" + Convert.ToBase64String(Encoding.UTF8.GetBytes("01234567890123456789012345678901"));

    private HttpClient CreateConfirmationClient(out Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory)
    {
        factory = fixture.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:AutoConfirmDevelopmentAccounts", "false");
            builder.UseSetting("Email:PublicOrigin", "http://localhost:3000");
            builder.UseSetting("Email:WebhookSigningSecret", WebhookSecret);
        });
        return factory.CreateClient(new() { AllowAutoRedirect = false });
    }

    [RequiresPostgresFact]
    public async Task Registration_captures_real_confirmation_content_and_token_confirms_once()
    {
        using var client = CreateConfirmationClient(out var factory);
        using (factory)
        {
            var email = $"confirm-{Guid.NewGuid():N}@example.test";
            using var registration = await MutateAsync(client, "/api/v1/account/register", new { email, password = Password });
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            using var registrationJson = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
            Assert.False(registrationJson.RootElement.GetProperty("isAuthenticated").GetBoolean());

            string textBody;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
                var capture = await db.ControlledEmailCaptures.SingleAsync(x => x.DestinationAddress == email);
                Assert.Contains("Confirm your Canada Deals account", capture.Subject);
                Assert.Contains("/account/confirm-email?", capture.TextBody);
                Assert.DoesNotContain("tracking", capture.HtmlBody, StringComparison.OrdinalIgnoreCase);
                textBody = capture.TextBody;
            }

            var link = textBody.Split('\n').Single(line => line.StartsWith("http://localhost:3000/account/confirm-email?", StringComparison.Ordinal));
            var uri = new Uri(link);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            var payload = new { userId = Guid.Parse(query["userId"]!), code = query["code"].ToString() };
            using var confirmation = await MutateAsync(client, "/api/v1/account/confirm-email", payload);
            Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
            Assert.Contains("CONFIRMED", await confirmation.Content.ReadAsStringAsync());
            using var replay = await MutateAsync(client, "/api/v1/account/confirm-email", payload);
            Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
            Assert.Contains("ALREADY_CONFIRMED", await replay.Content.ReadAsStringAsync());
            using var login = await MutateAsync(client, "/api/v1/account/login", new { email, password = Password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }
    }

    [RequiresPostgresFact]
    public async Task Invalid_confirmation_is_rejected_and_resend_is_generic_and_non_enumerating()
    {
        using var client = CreateConfirmationClient(out var factory);
        using (factory)
        {
            var email = $"resend-{Guid.NewGuid():N}@example.test";
            using (var registration = await MutateAsync(client, "/api/v1/account/register", new { email, password = Password })) registration.EnsureSuccessStatusCode();
            using var invalid = await MutateAsync(client, "/api/v1/account/confirm-email", new { userId = Guid.NewGuid(), code = "invalid" });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

            using var existing = await MutateAsync(client, "/api/v1/account/resend-confirmation", new { email });
            using var missing = await MutateAsync(client, "/api/v1/account/resend-confirmation", new { email = $"missing-{Guid.NewGuid():N}@example.test" });
            Assert.Equal(HttpStatusCode.Accepted, existing.StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, missing.StatusCode);
            Assert.Equal(await existing.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            Assert.Equal(2, await db.ControlledEmailCaptures.CountAsync(x => x.DestinationAddress == email));
        }
    }

    [RequiresPostgresFact]
    public async Task Signed_webhooks_are_replay_safe_order_safe_and_create_suppression()
    {
        using var client = CreateConfirmationClient(out var factory);
        using (factory)
        {
            var email = $"webhook-{Guid.NewGuid():N}@example.test";
            using (var registration = await MutateAsync(client, "/api/v1/account/register", new { email, password = Password })) registration.EnsureSuccessStatusCode();
            var messageId = $"resend-message-{Guid.NewGuid():N}";
            Guid deliveryId;
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
                var delivery = await db.AccountConfirmationDeliveries.SingleAsync(x => x.DestinationAddress == email);
                delivery.Accept(messageId, DateTimeOffset.UtcNow.AddMinutes(-2));
                deliveryId = delivery.Id;
                await db.SaveChangesAsync();
            }

            var deliveredAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            var deliveredEventId = $"evt-{Guid.NewGuid():N}";
            using (var delivered = await SendWebhookAsync(client, deliveredEventId, "email.delivered", messageId, email, deliveredAt))
                Assert.Equal(HttpStatusCode.NoContent, delivered.StatusCode);
            using (var replay = await SendWebhookAsync(client, deliveredEventId, "email.delivered", messageId, email, deliveredAt))
                Assert.Equal(HttpStatusCode.NoContent, replay.StatusCode);
            using (var olderSent = await SendWebhookAsync(client, $"evt-{Guid.NewGuid():N}", "email.sent", messageId, email, deliveredAt.AddMinutes(-1)))
                Assert.Equal(HttpStatusCode.NoContent, olderSent.StatusCode);
            using (var bounced = await SendWebhookAsync(client, $"evt-{Guid.NewGuid():N}", "email.bounced", messageId, email, deliveredAt.AddMinutes(1)))
                Assert.Equal(HttpStatusCode.NoContent, bounced.StatusCode);

            await using var verificationScope = factory.Services.CreateAsyncScope();
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var persisted = await verificationDb.AccountConfirmationDeliveries.FindAsync(deliveryId);
            Assert.Equal(EmailDeliveryStatus.Bounced, persisted!.Status);
            Assert.Equal(3, await verificationDb.ProcessedEmailWebhooks.CountAsync(x => x.ProviderMessageId == messageId));
            Assert.True(await verificationDb.EmailSuppressions.AnyAsync(x => x.NormalizedAddress == email.ToUpperInvariant()));
        }
    }

    [RequiresPostgresFact]
    public async Task Webhook_rejects_invalid_signature()
    {
        using var client = CreateConfirmationClient(out var factory);
        using (factory)
        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/email/resend"))
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            request.Headers.Add("svix-id", "evt-invalid");
            request.Headers.Add("svix-timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            request.Headers.Add("svix-signature", "v1,invalid");
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [RequiresPostgresFact]
    public async Task Provider_acceptance_reconciles_a_webhook_that_arrived_before_its_database_commit()
    {
        var messageId = $"early-{Guid.NewGuid():N}";
        using var factory = fixture.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Email:AutoConfirmDevelopmentAccounts", "false");
            builder.UseSetting("Email:WebhookSigningSecret", WebhookSecret);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITransactionalEmailSender>();
                services.AddSingleton<ITransactionalEmailSender>(new AcceptedSender(messageId));
            });
        });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var email = $"early-webhook-{Guid.NewGuid():N}@example.test";
        using (var early = await SendWebhookAsync(client, $"evt-{Guid.NewGuid():N}", "email.bounced", messageId, email, DateTimeOffset.UtcNow))
            Assert.Equal(HttpStatusCode.NoContent, early.StatusCode);
        await using (var earlyScope = factory.Services.CreateAsyncScope())
        {
            var earlyDb = earlyScope.ServiceProvider.GetRequiredService<DealsDbContext>();
            Assert.False(await earlyDb.EmailSuppressions.AnyAsync(x => x.NormalizedAddress == email.ToUpperInvariant()));
        }
        using (var registration = await MutateAsync(client, "/api/v1/account/register", new { email, password = Password }))
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var delivery = await db.AccountConfirmationDeliveries.SingleAsync(x => x.DestinationAddress == email);
        Assert.Equal(EmailDeliveryStatus.Bounced, delivery.Status);
        Assert.Equal(messageId, delivery.ProviderMessageId);
        Assert.True(await db.EmailSuppressions.AnyAsync(x => x.NormalizedAddress == email.ToUpperInvariant()));
    }

    [Fact]
    public async Task Resend_sender_sets_stable_idempotency_key_and_records_provider_acceptance()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "provider-id" }) });
        var sender = new ResendTransactionalEmailSender(new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") }, Options.Create(ResendOptions()));
        var result = await sender.SendAsync(new("stable-key", "user@example.test", "Subject", "<p>Body</p>", "Body"), CancellationToken.None);
        Assert.Equal(EmailSendOutcome.ProviderAccepted, result.Outcome);
        Assert.Equal("provider-id", result.ProviderMessageId);
        Assert.Equal("stable-key", handler.Request!.Headers.GetValues("Idempotency-Key").Single());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
    }

    [Fact]
    public async Task Resend_sender_classifies_429_as_transient_and_honours_retry_after()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(3));
        var sender = new ResendTransactionalEmailSender(new HttpClient(new RecordingHandler(response)) { BaseAddress = new Uri("https://api.resend.com/") }, Options.Create(ResendOptions()));
        var result = await sender.SendAsync(new("stable-key", "user@example.test", "Subject", "<p>Body</p>", "Body"), CancellationToken.None);
        Assert.Equal(EmailSendOutcome.TransientFailure, result.Outcome);
        Assert.Equal(TimeSpan.FromSeconds(3), result.RetryAfter);
    }

    [Fact]
    public async Task Email_emergency_stop_suppresses_delivery_before_the_provider_request()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = "provider-id" }) });
        var options = ResendOptions();
        options.EmergencyStop = true;
        var sender = new ResendTransactionalEmailSender(new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") }, Options.Create(options));

        var result = await sender.SendAsync(new("stable-key", "user@example.test", "Subject", "<p>Body</p>", "Body"), CancellationToken.None);

        Assert.Equal(EmailSendOutcome.Suppressed, result.Outcome);
        Assert.Equal("EMAIL_EMERGENCY_STOP", result.Reason);
        Assert.Null(handler.Request);
    }

    private static TransactionalEmailOptions ResendOptions() => new() { Enabled = true, Provider = "Resend", ApiKey = "re_test", FromAddress = "alerts@example.test", PublicOrigin = "https://example.test", WebhookSigningSecret = WebhookSecret };

    private static async Task<HttpResponseMessage> MutateAsync(HttpClient client, string path, object body)
    {
        using var tokenResponse = await client.GetAsync("/api/v1/account/antiforgery");
        using var tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", tokenJson.RootElement.GetProperty("requestToken").GetString());
        return await client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> SendWebhookAsync(HttpClient client, string eventId, string type, string messageId, string to, DateTimeOffset eventAt)
    {
        var body = JsonSerializer.Serialize(new { type, created_at = eventAt, data = new { email_id = messageId, to = new[] { to } } });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var key = Convert.FromBase64String(WebhookSecret[6..]);
        var signature = Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{eventId}.{timestamp}.{body}")));
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/email/resend") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("svix-id", eventId);
        request.Headers.Add("svix-timestamp", timestamp);
        request.Headers.Add("svix-signature", $"v1,{signature}");
        return client.SendAsync(request);
    }

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(response);
        }
    }

    private sealed class AcceptedSender(string messageId) : ITransactionalEmailSender
    {
        public Task<EmailSendResult> SendAsync(TransactionalEmailMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(new EmailSendResult(EmailSendOutcome.ProviderAccepted, messageId));
    }
}
