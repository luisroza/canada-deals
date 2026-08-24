using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CanadaDeals.Api.Security;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Domain.Reporting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class AdminPanelIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private const string Password = "SecureAdmin42";

    private HttpClient CreateClient() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    private static async Task<string> TokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/v1/account/antiforgery");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("requestToken").GetString()!;
    }

    private static async Task<HttpResponseMessage> MutateAsync(HttpClient client, HttpMethod method, string path, object body)
    {
        using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", await TokenAsync(client));
        return await client.SendAsync(request);
    }

    private async Task<HttpClient> CreateAuthenticatedAsync(bool admin)
    {
        var email = $"admin-panel-{Guid.NewGuid():N}@example.test";
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = email, EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            if (admin)
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                if (!await roles.RoleExistsAsync(AdminAccess.OwnerRole))
                    Assert.True((await roles.CreateAsync(new IdentityRole<Guid>(AdminAccess.OwnerRole))).Succeeded);
                Assert.True((await users.AddToRoleAsync(user, AdminAccess.OwnerRole)).Succeeded);
            }
        }

        var client = CreateClient();
        using var login = await MutateAsync(client, HttpMethod.Post, "/api/v1/account/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private async Task<object> OfferInputAsync(string slug, bool enabled)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var brandId = await db.Brands.OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var categoryId = await db.Categories.OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var retailerId = await db.Retailers.Where(item => item.IsEnabled).OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var policyId = await db.MerchantPolicies.Where(item => item.SourceKey == "demo-fixture").Select(item => item.Id).SingleAsync();
        var now = DateTimeOffset.UtcNow;
        return new
        {
            slug,
            productTitle = "Admin Controlled Offer",
            brandId,
            categoryId,
            modelNumber = "ADMIN-100",
            manufacturerPartNumber = "ADMIN-100",
            gtin = (string?)null,
            variantAttributes = new Dictionary<string, string> { ["colour"] = "Green" },
            retailerId,
            merchantPolicyId = policyId,
            externalListingId = $"ADMIN-{Guid.NewGuid():N}",
            retailerSku = "ADMIN-SKU",
            originalTitle = "Admin Controlled Offer",
            productUrl = "https://demo.local/admin-controlled-offer",
            approvedAffiliateDestinationReference = (string?)null,
            seller = "Controlled seller",
            isMarketplaceSeller = false,
            conditionState = "NEW",
            packQuantity = 1,
            bundleContents = (string?)null,
            regionAvailabilityContext = "Canada",
            availabilityState = "AVAILABLE",
            shippingContext = "Calculated at checkout",
            externalIdentifiers = new Dictionary<string, string> { ["model"] = "ADMIN-100" },
            currentPrice = 99.99m,
            observedAt = now,
            fetchedAt = now,
            matchState = "CONFIRMED",
            isEnabled = enabled,
            changeReason = "Integration test"
        };
    }

    [RequiresPostgresFact]
    public async Task Admin_endpoints_require_authentication_and_owner_role()
    {
        using var anonymous = await CreateClient().GetAsync("/api/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var ordinary = await CreateAuthenticatedAsync(admin: false);
        using var forbidden = await ordinary.GetAsync("/api/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var owner = await CreateAuthenticatedAsync(admin: true);
        using var allowed = await owner.GetAsync("/api/v1/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Owner_can_create_and_reversibly_disable_an_offer_with_audit()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var slug = $"admin-offer-{Guid.NewGuid():N}";
        var input = await OfferInputAsync(slug, enabled: true);
        using var created = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var listingId = createdJson.RootElement.GetProperty("listingId").GetGuid();

        using var publicBefore = await client.GetAsync($"/api/v1/products/{slug}");
        Assert.Equal(HttpStatusCode.OK, publicBefore.StatusCode);

        var updateJson = JsonSerializer.SerializeToNode(input)!.AsObject();
        updateJson["isEnabled"] = false;
        updateJson["changeReason"] = "Offer is no longer current";
        using var disabled = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/offers/{listingId}", updateJson);
        Assert.Equal(HttpStatusCode.NoContent, disabled.StatusCode);

        using var publicAfter = await client.GetAsync($"/api/v1/products/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicAfter.StatusCode);
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.False((await db.RetailerListings.SingleAsync(item => item.Id == listingId)).IsEnabled);
        Assert.Equal(2, await db.AdminAuditEvents.CountAsync(item => item.EntityId == listingId));
    }

    [RequiresPostgresFact]
    public async Task Admin_mutations_require_antiforgery_and_banner_assets_remain_first_party()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var input = await OfferInputAsync($"admin-csrf-{Guid.NewGuid():N}", enabled: false);
        using var missingCsrf = await client.PostAsJsonAsync("/api/v1/admin/offers", input);
        Assert.Equal(HttpStatusCode.BadRequest, missingCsrf.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var retailerId = await db.Retailers.OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var banner = new
        {
            title = "Controlled banner",
            subtitle = "Original GreatDeals artwork",
            assetPath = "/store-banners/electronics-devices.svg",
            assetSource = "CANADADEALSORIGINAL",
            assetProvider = (string?)null,
            assetEvidenceReference = (string?)null,
            allowedPlacement = "store_banner",
            effectiveAt = (DateTimeOffset?)null,
            expiresAt = (DateTimeOffset?)null,
            bannerOrder = 1,
            isEnabled = true,
            changeReason = "Editorial banner update"
        };
        using var updated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/banners/{retailerId}", banner);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var profile = await db.StoreBannerProfiles.AsNoTracking().SingleAsync(item => item.RetailerId == retailerId);
        Assert.Equal("/store-banners/electronics-devices.svg", profile.AssetPath);
        Assert.True(profile.IsEnabled);
    }

    [RequiresPostgresFact]
    public async Task Owner_can_close_the_customer_report_loop_with_an_audited_status_change()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        Guid reportId;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var listingId = await db.RetailerListings.Where(item => item.IsEnabled).Select(item => item.Id).FirstAsync();
            var report = ListingIssueReport.Create(listingId, ListingIssueReason.PriceChanged, "Price differs at the retailer.", DateTimeOffset.UtcNow);
            db.ListingIssueReports.Add(report);
            await db.SaveChangesAsync();
            reportId = report.Id;
        }

        using var updated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/reports/{reportId}/status", new
        {
            status = "RESOLVED",
            resolutionNote = "Offer was reviewed and corrected."
        });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        await using var verification = fixture.Services.CreateAsyncScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.Equal(ListingIssueStatus.Resolved, (await verificationDb.ListingIssueReports.SingleAsync(item => item.Id == reportId)).Status);
        Assert.True(await verificationDb.AdminAuditEvents.AnyAsync(item => item.EntityId == reportId && item.Action == "STATUS_CHANGE"));
    }
}
