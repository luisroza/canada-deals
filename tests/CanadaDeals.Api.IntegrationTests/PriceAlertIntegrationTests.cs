using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CanadaDeals.Domain.Alerts;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Alerts;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class PriceAlertIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Password = "SecurePass42";

    private HttpClient CreateClient() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/account/antiforgery");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> MutateAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        var token = await GetAntiforgeryTokenAsync(client);
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        using var response = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/register", new { email, password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> GetProductIdAsync(HttpClient client, string slug)
    {
        using var response = await client.GetAsync($"/api/v1/products/{slug}");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("productId").GetGuid();
    }

    [RequiresPostgresFact]
    public async Task Alert_mutations_require_authentication_and_antiforgery()
    {
        var anonymous = CreateClient();
        var productId = await GetProductIdAsync(anonymous, "northstar-55-qled-tv");
        using var anonymousPut = await MutateAsync(anonymous, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 1000m, consentToEmail = true });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousPut.StatusCode);

        var client = CreateClient();
        await RegisterAsync(client, $"alert-csrf-{Guid.NewGuid():N}@example.test");
        using var missingCsrf = await client.PutAsJsonAsync($"/api/v1/price-alerts/{productId}", new { targetPrice = 1000m, consentToEmail = true });
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Alert_is_persisted_idempotent_versioned_listed_and_also_saves_the_product()
    {
        var client = CreateClient();
        var email = $"alert-persist-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client, "northstar-55-qled-tv");

        using var first = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 1000m, consentToEmail = true });
        using var same = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 1000m, consentToEmail = true });
        using var changed = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 950m, consentToEmail = true });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        using var list = await client.GetAsync("/api/v1/price-alerts");
        using var alerts = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        var alert = Assert.Single(alerts.RootElement.EnumerateArray());
        Assert.Equal(950m, alert.GetProperty("targetPrice").GetDecimal());
        Assert.Equal("CAD", alert.GetProperty("currency").GetString());
        Assert.Equal("ACTIVE", alert.GetProperty("status").GetString());
        Assert.Equal(2, alert.GetProperty("targetVersion").GetInt32());
        Assert.Equal(PriceAlert.CurrentConsentVersion, alert.GetProperty("consentVersion").GetString());

        using var saved = await client.GetAsync("/api/v1/saved-products");
        using var savedJson = JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
        Assert.Contains(savedJson.RootElement.EnumerateArray(), item => item.GetProperty("productId").GetGuid() == productId);
    }

    [RequiresPostgresFact]
    public async Task Alert_rejects_missing_consent_invalid_targets_and_unknown_products()
    {
        var client = CreateClient();
        await RegisterAsync(client, $"alert-invalid-{Guid.NewGuid():N}@example.test");
        var productId = await GetProductIdAsync(client, "northstar-55-qled-tv");

        using var noConsent = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 100m, consentToEmail = false });
        using var tooPrecise = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 100.001m, consentToEmail = true });
        using var tooHigh = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 1000000.01m, consentToEmail = true });
        using var unknown = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{Guid.NewGuid()}", new { targetPrice = 100m, consentToEmail = true });

        Assert.Equal(HttpStatusCode.BadRequest, noConsent.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooPrecise.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooHigh.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Unconfirmed_email_cannot_activate_an_alert()
    {
        var client = CreateClient();
        var email = $"alert-unconfirmed-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client, "northstar-55-qled-tv");
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == email);
            user.EmailConfirmed = false;
            await db.SaveChangesAsync();
        }

        using var response = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 1000m, consentToEmail = true });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("Confirm your email", await response.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task User_ownership_prevents_cross_user_read_or_delete()
    {
        var userA = CreateClient();
        var userB = CreateClient();
        await RegisterAsync(userA, $"alert-owner-a-{Guid.NewGuid():N}@example.test");
        await RegisterAsync(userB, $"alert-owner-b-{Guid.NewGuid():N}@example.test");
        var productId = await GetProductIdAsync(userA, "northstar-55-qled-tv");
        using (var created = await MutateAsync(userA, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 1000m, consentToEmail = true }))
            created.EnsureSuccessStatusCode();

        using var listB = await userB.GetAsync("/api/v1/price-alerts");
        Assert.Equal("[]", await listB.Content.ReadAsStringAsync());
        using var deleteB = await MutateAsync(userB, HttpMethod.Delete, $"/api/v1/price-alerts/{productId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteB.StatusCode);
        using var listA = await userA.GetAsync("/api/v1/price-alerts");
        Assert.Contains("\"status\":\"ACTIVE\"", await listA.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task Worker_evaluates_history_unavailable_product_and_deduplicates_a_continuous_condition()
    {
        var client = CreateClient();
        var email = $"alert-worker-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client, "northstar-quiet-headphones");
        using (var created = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 250m, consentToEmail = true }))
            created.EnsureSuccessStatusCode();

        await RunJobAsync(fixture.Services);
        await RunJobAsync(fixture.Services);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var userId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
        var delivery = Assert.Single(await db.NotificationDeliveries.Where(x => x.PriceAlert.UserId == userId && x.PriceAlert.ProductId == productId).ToListAsync());
        Assert.Equal(NotificationDeliveryStatus.DevelopmentCaptured, delivery.Status);
        Assert.Equal("CONTROLLED_DEVELOPMENT_TEST_CAPTURE", delivery.StatusReason);
        Assert.True(delivery.QualifyingPrice <= 250m);
        var capture = await db.ControlledEmailCaptures.SingleAsync(x => x.IdempotencyKey == delivery.IdempotencyKey);
        Assert.Contains("Northstar Quiet Wireless Headphones", capture.Subject);
        Assert.Contains($"Qualifying observed price: CAD {delivery.QualifyingPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}", capture.TextBody);
    }

    [RequiresPostgresFact]
    public async Task Worker_rejects_stale_and_unsafe_prices_even_when_they_are_below_target()
    {
        var client = CreateClient();
        var email = $"alert-safety-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var staleProduct = await GetProductIdAsync(client, "northstar-65-oled-tv");
        var unsafeProduct = await GetProductIdAsync(client, "ridgeway-20v-drill-tool-only");
        using (var stale = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{staleProduct}", new { targetPrice = 1500m, consentToEmail = true })) stale.EnsureSuccessStatusCode();
        using (var unsafeAlert = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{unsafeProduct}", new { targetPrice = 100m, consentToEmail = true })) unsafeAlert.EnsureSuccessStatusCode();

        await RunJobAsync(fixture.Services);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var userId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
        Assert.Empty(await db.NotificationDeliveries.Where(x => x.PriceAlert.UserId == userId).ToListAsync());
    }

    [RequiresPostgresFact]
    public async Task Worker_accepts_equality_but_skips_above_target_policy_denied_and_no_current_price()
    {
        var client = CreateClient();
        var email = $"alert-evaluation-matrix-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var equalProduct = await CreateEvaluationProductAsync(fixture.Services, 100m, PolicyPermission.Allowed, true);
        var aboveProduct = await CreateEvaluationProductAsync(fixture.Services, 101m, PolicyPermission.Allowed, true);
        var deniedProduct = await CreateEvaluationProductAsync(fixture.Services, 90m, PolicyPermission.Denied, false);
        var noPriceProduct = await CreateEvaluationProductAsync(fixture.Services, null, PolicyPermission.Allowed, true);
        foreach (var productId in new[] { equalProduct, aboveProduct, deniedProduct, noPriceProduct })
        {
            using var created = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 100m, consentToEmail = true });
            created.EnsureSuccessStatusCode();
        }

        await RunJobAsync(fixture.Services);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var userId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
        var deliveries = await db.NotificationDeliveries.Where(x => x.PriceAlert.UserId == userId).ToListAsync();
        var delivery = Assert.Single(deliveries);
        Assert.Equal(equalProduct, (await db.PriceAlerts.FindAsync(delivery.PriceAlertId))!.ProductId);
        Assert.Equal(100m, delivery.QualifyingPrice);
    }

    [RequiresPostgresFact]
    public async Task Target_change_creates_a_new_deduplication_condition()
    {
        var client = CreateClient();
        var email = $"alert-version-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client, "northstar-quiet-headphones");
        using (var first = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 250m, consentToEmail = true })) first.EnsureSuccessStatusCode();
        await RunJobAsync(fixture.Services);
        using (var update = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 260m, consentToEmail = true })) update.EnsureSuccessStatusCode();
        await RunJobAsync(fixture.Services);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var userId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
        var deliveries = await db.NotificationDeliveries.Where(x => x.PriceAlert.UserId == userId && x.PriceAlert.ProductId == productId).OrderBy(x => x.TargetVersion).ToListAsync();
        Assert.Equal(2, deliveries.Count);
        Assert.Equal([1, 2], deliveries.Select(x => x.TargetVersion));
    }

    [RequiresPostgresFact]
    public async Task Disabled_alert_persists_but_is_not_evaluated()
    {
        var client = CreateClient();
        var email = $"alert-disabled-{Guid.NewGuid():N}@example.test";
        await RegisterAsync(client, email);
        var productId = await GetProductIdAsync(client, "northstar-quiet-headphones");
        using (var created = await MutateAsync(client, HttpMethod.Put, $"/api/v1/price-alerts/{productId}", new { targetPrice = 300m, consentToEmail = true })) created.EnsureSuccessStatusCode();
        using (var removed = await MutateAsync(client, HttpMethod.Delete, $"/api/v1/price-alerts/{productId}")) Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);

        await RunJobAsync(fixture.Services);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var userId = await db.Users.Where(x => x.Email == email).Select(x => x.Id).SingleAsync();
        var alert = await db.PriceAlerts.SingleAsync(x => x.UserId == userId && x.ProductId == productId);
        Assert.Equal(PriceAlertStatus.Disabled, alert.Status);
        Assert.False(await db.NotificationDeliveries.AnyAsync(x => x.PriceAlertId == alert.Id));
    }

    [RequiresPostgresFact]
    public void Production_startup_fails_closed_when_email_provider_is_not_configured()
    {
        using var productionFactory = fixture.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var error = Assert.ThrowsAny<Exception>(() => productionFactory.CreateClient());
        Assert.Contains("Production email delivery must be enabled", error.ToString());
    }

    private static async Task RunJobAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PriceAlertEvaluationJob>().RunAsync();
    }

    private static async Task<Guid> CreateEvaluationProductAsync(
        IServiceProvider services,
        decimal? currentPrice,
        PolicyPermission pricePermission,
        bool observationPermitted)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;
        var brand = Brand.Create($"Alert fixture {suffix}", $"alert-brand-{suffix}");
        var category = Category.Create($"Alert category {suffix}", $"alert-category-{suffix}");
        var retailer = Retailer.Create($"alert-retailer-{suffix}", $"Alert retailer {suffix}");
        var policy = MerchantPolicy.Create($"alert-policy-{suffix}", pricePermission, PolicyPermission.Unknown, PolicyPermission.Denied, PolicyPermission.Allowed, 24, "SAME_PRODUCT_ONLY", "TEST_ONLY", "Synthetic alert evaluation fixture.", 1, "Local synthetic data only", now);
        var product = Product.Create($"alert-product-{suffix}", $"Alert product {suffix}", brand, category);
        db.AddRange(brand, category, retailer, policy, product);
        await db.SaveChangesAsync();
        if (currentPrice.HasValue)
        {
            var listing = RetailerListing.Create(product.Id, retailer.Id, $"ALERT-{suffix}", product.Title, $"https://demo.local/alert/{suffix}", policy.Id, MatchState.Confirmed, now, now, currentPrice, "CAD", FreshnessState.Recent, EvidenceState.Partial, HistoryAvailability.Unavailable);
            db.RetailerListings.Add(listing);
            await db.SaveChangesAsync();
            db.PriceObservations.Add(PriceObservation.Create(listing.Id, currentPrice.Value, "CAD", now, now, observationPermitted, $"alert-{suffix}"));
            await db.SaveChangesAsync();
        }
        return product.Id;
    }
}
