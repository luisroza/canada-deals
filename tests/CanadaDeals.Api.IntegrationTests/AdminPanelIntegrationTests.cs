using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using CanadaDeals.Api.Security;
using CanadaDeals.Infrastructure.Identity;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Affiliates;
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

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes, string contentType, string fileName)
    {
        using var body = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        body.Add(file, "file", fileName);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/banner-assets") { Content = body };
        request.Headers.Add("X-CSRF-TOKEN", await TokenAsync(client));
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> UploadProductImageAsync(HttpClient client, Guid productId, byte[] bytes, bool activate)
    {
        using var body = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        body.Add(file, "file", "reviewed-product.png");
        body.Add(new StringContent("Owner-created integration test image"), "rightsEvidenceReference");
        body.Add(new StringContent("DEAL_CARD,PRODUCT_PAGE,WISHLIST"), "allowedPlacements");
        body.Add(new StringContent(activate.ToString()), "activate");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/products/{productId}/images") { Content = body };
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
        var brandId = await db.Brands.Where(item => item.IsEnabled).OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var categoryId = await db.Categories.Where(item => item.IsEnabled).OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var retailerId = await db.Retailers.Where(item => item.IsEnabled).OrderBy(item => item.Name).Select(item => item.Id).FirstAsync();
        var policyId = await db.MerchantPolicies.Where(item => item.SourceKey == "demo-fixture").Select(item => item.Id).SingleAsync();
        var now = DateTimeOffset.UtcNow;
        return new
        {
            productId = (Guid?)null,
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
            offerValidUntil = (DateTimeOffset?)null,
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
    public async Task Owner_can_resolve_and_inspect_Amazon_Canada_short_links()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        using var shortLink = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/affiliate-links/inspect", new { url = "https://amzn.to/example" });
        Assert.Equal(HttpStatusCode.OK, shortLink.StatusCode);
        using var shortJson = JsonDocument.Parse(await shortLink.Content.ReadAsStringAsync());
        Assert.Equal("READY", shortJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("DIRECT_PROVIDER", shortJson.RootElement.GetProperty("handoffMode").GetString());
        Assert.Equal("B0DMNJNFW8", shortJson.RootElement.GetProperty("externalProductId").GetString());
        Assert.Equal("https://www.amazon.ca/dp/B0DMNJNFW8", shortJson.RootElement.GetProperty("canonicalProductUrl").GetString());
        Assert.Contains("Levoit-Smart-Humidifiers", shortJson.RootElement.GetProperty("resolvedProductUrl").GetString());
        Assert.Equal("canadadeal-20", shortJson.RootElement.GetProperty("partnerTag").GetString());
        var brandCandidate = shortJson.RootElement.GetProperty("brandCandidate");
        Assert.Equal("Levoit", brandCandidate.GetProperty("name").GetString());
        Assert.Equal("levoit", brandCandidate.GetProperty("normalizedKey").GetString());
        Assert.Equal("URL_PATH", brandCandidate.GetProperty("source").GetString());
        Assert.Equal("LOW", brandCandidate.GetProperty("confidence").GetString());
        Assert.Equal("NEW_CANDIDATE", brandCandidate.GetProperty("matchStatus").GetString());

        using var nonCanadian = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/affiliate-links/inspect", new { url = "https://amzn.to/non-canada" });
        Assert.Equal(HttpStatusCode.BadRequest, nonCanadian.StatusCode);
        Assert.Contains("not Amazon Canada", await nonCanadian.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        using var fullLink = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/affiliate-links/inspect", new { url = "https://www.amazon.ca/example/dp/B0DMNJNFW8?tag=canadadeal-20" });
        Assert.Equal(HttpStatusCode.OK, fullLink.StatusCode);
        using var fullJson = JsonDocument.Parse(await fullLink.Content.ReadAsStringAsync());
        Assert.Equal("READY", fullJson.RootElement.GetProperty("status").GetString());
        Assert.Equal("B0DMNJNFW8", fullJson.RootElement.GetProperty("externalProductId").GetString());
        Assert.Equal("https://www.amazon.ca/dp/B0DMNJNFW8", fullJson.RootElement.GetProperty("canonicalProductUrl").GetString());

        using var productPage = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/affiliate-links/inspect", new { url = "https://www.amazon.ca/example/dp/B0DMNJNFW8" });
        Assert.Equal(HttpStatusCode.OK, productPage.StatusCode);
        using var productPageJson = JsonDocument.Parse(await productPage.Content.ReadAsStringAsync());
        Assert.Equal("NEEDS_REVIEW", productPageJson.RootElement.GetProperty("status").GetString());
        Assert.Contains(productPageJson.RootElement.GetProperty("warnings").EnumerateArray(), warning =>
            warning.GetString()!.Contains("not a finished affiliate link", StringComparison.OrdinalIgnoreCase));
    }

    [RequiresPostgresFact]
    public async Task Owner_provided_Amazon_link_is_persisted_exactly_and_exposed_as_direct_handoff()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        Guid retailerId;
        Guid policyId;
        await using (var setup = fixture.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<DealsDbContext>();
            var retailer = await db.Retailers.SingleOrDefaultAsync(item => item.Key == "amazon-ca");
            if (retailer is null)
            {
                retailer = Retailer.Create("amazon-ca", "Amazon.ca");
                db.Retailers.Add(retailer);
            }
            var policy = await db.MerchantPolicies.SingleOrDefaultAsync(item => item.SourceKey == "amazon-creators-api");
            if (policy is null)
            {
                policy = MerchantPolicy.Create(
                    "amazon-creators-api", PolicyPermission.Allowed, PolicyPermission.Denied,
                    PolicyPermission.Denied, PolicyPermission.Allowed, 24, "NO_CROSS_RETAILER_COMPARISON",
                    "As an Amazon Associate I earn from qualifying purchases.",
                    "Paid link. As an Amazon Associate I earn from qualifying purchases.",
                    0, "Controlled integration fixture", DateTimeOffset.UtcNow, PolicyPermission.Allowed);
                db.MerchantPolicies.Add(policy);
            }
            await db.SaveChangesAsync();
            retailerId = retailer.Id;
            policyId = policy.Id;
        }

        const string trackingUrl = "https://www.amazon.ca/example/dp/B0DMNJNFW8?tag=canadadeal-20";
        var slug = $"amazon-owner-link-{Guid.NewGuid():N}";
        var input = JsonSerializer.SerializeToNode(await OfferInputAsync(slug, enabled: true))!.AsObject();
        input["retailerId"] = retailerId;
        input["merchantPolicyId"] = policyId;
        input["externalListingId"] = "B0DMNJNFW8";
        input["productUrl"] = "https://www.amazon.ca/dp/B0DMNJNFW8";
        input["approvedAffiliateDestinationReference"] = null;
        input["affiliateTrackingUrl"] = trackingUrl;
        input["affiliatePartnerTag"] = "canadadeal-20";
        input["affiliateRelationshipEvidenceReference"] = "OWNER_APPROVED_ACCOUNT_FIXTURE";
        input["affiliateRelationshipConfirmed"] = true;

        var untaggedInput = JsonSerializer.SerializeToNode(input)!.AsObject();
        untaggedInput["affiliateTrackingUrl"] = "https://www.amazon.ca/example/dp/B0DMNJNFW8";
        using var untagged = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", untaggedInput);
        Assert.Equal(HttpStatusCode.BadRequest, untagged.StatusCode);
        Assert.Contains("not tagged as an affiliate link", await untagged.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        using var created = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var listingId = createdJson.RootElement.GetProperty("listingId").GetGuid();

        using var product = await client.GetAsync($"/api/v1/products/{slug}");
        Assert.Equal(HttpStatusCode.OK, product.StatusCode);
        using var productJson = JsonDocument.Parse(await product.Content.ReadAsStringAsync());
        var offer = productJson.RootElement.GetProperty("primaryOffer");
        Assert.Equal(JsonValueKind.Null, offer.GetProperty("handoffPath").ValueKind);
        Assert.Equal(trackingUrl, offer.GetProperty("handoffUrl").GetString());
        Assert.Equal("DIRECT_PROVIDER", offer.GetProperty("handoffMode").GetString());

        using var internalRedirect = await client.GetAsync($"/go/{listingId}");
        Assert.Equal(HttpStatusCode.NotFound, internalRedirect.StatusCode);

        await using var verification = fixture.Services.CreateAsyncScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
        var link = await verificationDb.AffiliateLinks.SingleAsync(item => item.RetailerListingId == listingId);
        Assert.Equal(trackingUrl, link.TrackingUrl);
        Assert.Equal(AffiliateLinkAcquisitionMode.OwnerProvided, link.AcquisitionMode);
        Assert.Equal(AffiliateHandoffMode.DirectProvider, link.HandoffMode);
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
    public async Task Owner_can_manage_categories_without_deleting_linked_catalog_records()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var slug = $"managed-category-{Guid.NewGuid():N}";
        using var created = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/categories", new { name = "Managed Category", slug });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var categoryId = createdJson.RootElement.GetProperty("categoryId").GetGuid();

        await using (var initialScope = fixture.Services.CreateAsyncScope())
        {
            var initialDb = initialScope.ServiceProvider.GetRequiredService<DealsDbContext>();
            Assert.False((await initialDb.Categories.SingleAsync(item => item.Id == categoryId)).IsEnabled);
        }

        using var activated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/categories/{categoryId}", new { name = "Managed Category", isEnabled = true, changeReason = "Ready for editorial use" });
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);

        var productSlug = $"managed-category-product-{Guid.NewGuid():N}";
        var offer = JsonSerializer.SerializeToNode(await OfferInputAsync(productSlug, enabled: true))!.AsObject();
        offer["categoryId"] = categoryId;
        using var offerCreated = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", offer);
        Assert.Equal(HttpStatusCode.Created, offerCreated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/products/{productSlug}")).StatusCode);

        using var reasonRequired = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/categories/{categoryId}", new { name = "Managed Category", isEnabled = false, changeReason = "" });
        Assert.Equal(HttpStatusCode.BadRequest, reasonRequired.StatusCode);
        using var deactivated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/categories/{categoryId}", new { name = "Managed Category", isEnabled = false, changeReason = "Seasonal category retired" });
        Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{productSlug}")).StatusCode);

        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.True(await db.Products.AnyAsync(product => product.CategoryId == categoryId));
        Assert.True(await db.AdminAuditEvents.AnyAsync(item => item.EntityId == categoryId && item.Action == "DEACTIVATE"));
    }

    [RequiresPostgresFact]
    public async Task Owner_can_manage_brands_and_deactivation_closes_public_discovery()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var slug = $"managed-brand-{Guid.NewGuid():N}";
        using var created = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/brands", new { name = "Managed Brand", slug });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var brandId = createdJson.RootElement.GetProperty("brandId").GetGuid();

        using var activated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/brands/{brandId}", new { name = "Managed Brand Canada", isEnabled = true, changeReason = "Identity reviewed" });
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);

        var productSlug = $"managed-brand-product-{Guid.NewGuid():N}";
        var offer = JsonSerializer.SerializeToNode(await OfferInputAsync(productSlug, enabled: true))!.AsObject();
        offer["brandId"] = brandId;
        using var offerCreated = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", offer);
        Assert.Equal(HttpStatusCode.Created, offerCreated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/products/{productSlug}")).StatusCode);

        using var deactivated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/brands/{brandId}", new { name = "Managed Brand Canada", isEnabled = false, changeReason = "Brand hidden pending review" });
        Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{productSlug}")).StatusCode);

        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.True(await db.Products.AnyAsync(product => product.BrandId == brandId));
        Assert.True(await db.AdminAuditEvents.AnyAsync(item => item.EntityId == brandId && item.Action == "DEACTIVATE"));
    }

    [RequiresPostgresFact]
    public async Task Confirmed_new_brand_is_created_with_the_offer_and_normalized_variants_are_reused()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var rejectedName = $"Unconfirmed Brand {Guid.NewGuid():N}";
        var rejected = JsonSerializer.SerializeToNode(await OfferInputAsync($"unconfirmed-brand-{Guid.NewGuid():N}", enabled: false))!.AsObject();
        rejected["brandId"] = null;
        rejected["newBrandName"] = rejectedName;
        rejected["newBrandSlug"] = $"unconfirmed-brand-{Guid.NewGuid():N}";
        rejected["confirmBrandCreation"] = false;
        using var rejectedResponse = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", rejected);
        Assert.Equal(HttpStatusCode.BadRequest, rejectedResponse.StatusCode);

        var suffix = Guid.NewGuid().ToString("N");
        var displayName = $"Atomic Brand {suffix}";
        var normalizedKey = Brand.NormalizeKey(displayName);
        var first = JsonSerializer.SerializeToNode(await OfferInputAsync($"atomic-brand-product-{suffix}", enabled: true))!.AsObject();
        first["brandId"] = null;
        first["newBrandName"] = $"{displayName}®";
        first["newBrandSlug"] = $"atomic-brand-{suffix}";
        first["confirmBrandCreation"] = true;
        using var firstResponse = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", first);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        using var firstJson = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        var brandId = firstJson.RootElement.GetProperty("brandId").GetGuid();

        var second = JsonSerializer.SerializeToNode(await OfferInputAsync($"atomic-brand-second-{suffix}", enabled: false))!.AsObject();
        second["brandId"] = null;
        second["newBrandName"] = displayName.ToUpperInvariant();
        second["newBrandSlug"] = $"duplicate-brand-{suffix}";
        second["confirmBrandCreation"] = true;
        using var secondResponse = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", second);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        using var secondJson = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Equal(brandId, secondJson.RootElement.GetProperty("brandId").GetGuid());

        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
        var brand = await db.Brands.SingleAsync(item => item.NormalizedKey == normalizedKey);
        Assert.Equal(brandId, brand.Id);
        Assert.True(brand.IsEnabled);
        Assert.Equal(2, await db.Products.CountAsync(product => product.BrandId == brandId));
        Assert.True(await db.AdminAuditEvents.AnyAsync(item => item.EntityId == brandId && item.EntityType == "Brand" && item.Action == "CREATE"));
        Assert.False(await db.Brands.AnyAsync(item => item.NormalizedKey == Brand.NormalizeKey(rejectedName)));
    }

    [RequiresPostgresFact]
    public async Task Owner_can_add_a_second_retailer_offer_to_an_existing_product_without_duplicating_it()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var slug = $"multi-offer-{Guid.NewGuid():N}";
        using var first = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", await OfferInputAsync(slug, enabled: true));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        var productId = firstJson.RootElement.GetProperty("productId").GetGuid();
        var firstListingId = firstJson.RootElement.GetProperty("listingId").GetGuid();

        Guid secondRetailerId;
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var retailer = Retailer.Create($"second-{Guid.NewGuid():N}", "Second Controlled Store");
            db.Retailers.Add(retailer);
            await db.SaveChangesAsync();
            secondRetailerId = retailer.Id;
        }

        var secondInput = JsonSerializer.SerializeToNode(await OfferInputAsync(slug, enabled: true))!.AsObject();
        secondInput["productId"] = productId;
        secondInput["retailerId"] = secondRetailerId;
        secondInput["externalListingId"] = $"SECOND-{Guid.NewGuid():N}";
        secondInput["productUrl"] = "https://demo.local/second-controlled-offer";
        secondInput["currentPrice"] = 89.99m;
        using var second = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", secondInput);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var secondListingId = secondJson.RootElement.GetProperty("listingId").GetGuid();

        await using (var verification = fixture.Services.CreateAsyncScope())
        {
            var db = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
            Assert.Equal(1, await db.Products.CountAsync(item => item.Id == productId));
            Assert.Equal(2, await db.RetailerListings.CountAsync(item => item.ProductId == productId));
        }
        using var feed = await client.GetAsync("/api/v1/deals?search=Admin%20Controlled%20Offer&pageSize=48");
        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
        using var feedJson = JsonDocument.Parse(await feed.Content.ReadAsStringAsync());
        var productOffers = feedJson.RootElement.GetProperty("items").EnumerateArray()
            .Where(item => item.GetProperty("productId").GetGuid() == productId)
            .Select(item => item.GetProperty("listingId").GetGuid())
            .ToArray();
        Assert.Equal(2, productOffers.Length);
        Assert.Contains(firstListingId, productOffers);
        Assert.Contains(secondListingId, productOffers);

        using var offerDetail = await client.GetAsync($"/api/v1/offers/{secondListingId}");
        Assert.Equal(HttpStatusCode.OK, offerDetail.StatusCode);
        using var offerJson = JsonDocument.Parse(await offerDetail.Content.ReadAsStringAsync());
        Assert.Equal(secondListingId, offerJson.RootElement.GetProperty("primaryOffer").GetProperty("listingId").GetGuid());
        Assert.False(offerJson.RootElement.TryGetProperty("safeComparisons", out _));
    }

    [RequiresPostgresFact]
    public async Task Product_slug_is_immutable_and_expired_offers_are_automatically_hidden()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var slug = $"immutable-product-{Guid.NewGuid():N}";
        var input = await OfferInputAsync(slug, enabled: true);
        using var created = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var listingId = createdJson.RootElement.GetProperty("listingId").GetGuid();

        var renamed = JsonSerializer.SerializeToNode(input)!.AsObject();
        renamed["slug"] = $"renamed-{Guid.NewGuid():N}";
        using var rejected = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/offers/{listingId}", renamed);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/products/{slug}")).StatusCode);

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"RetailerListings\" SET \"OfferValidUntil\" = {DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE \"Id\" = {listingId}");
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{slug}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/go/{listingId}")).StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Owner_can_manage_stores_and_deactivation_closes_public_discovery()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var key = $"managed-store-{Guid.NewGuid():N}";
        using var created = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/retailers", new { name = "Managed Store", key });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var createdJson = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var retailerId = createdJson.RootElement.GetProperty("retailerId").GetGuid();

        using var activated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/retailers/{retailerId}", new { name = "Managed Store Canada", isEnabled = true, changeReason = "Editorial setup complete" });
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);

        var productSlug = $"managed-store-product-{Guid.NewGuid():N}";
        var offer = JsonSerializer.SerializeToNode(await OfferInputAsync(productSlug, enabled: true))!.AsObject();
        offer["retailerId"] = retailerId;
        using var offerCreated = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", offer);
        Assert.Equal(HttpStatusCode.Created, offerCreated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/v1/products/{productSlug}")).StatusCode);

        using var deactivated = await MutateAsync(client, HttpMethod.Put, $"/api/v1/admin/retailers/{retailerId}", new { name = "Managed Store Canada", isEnabled = false, changeReason = "Store is temporarily unavailable" });
        Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/products/{productSlug}")).StatusCode);

        await using var verification = fixture.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.True(await db.RetailerListings.AnyAsync(listing => listing.RetailerId == retailerId));
        Assert.True(await db.AdminAuditEvents.AnyAsync(item => item.EntityId == retailerId && item.Action == "DEACTIVATE"));
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
        var retailerId = await db.StoreBannerProfiles.Where(item => item.IsEnabled).OrderBy(item => item.BannerOrder).Select(item => item.RetailerId).FirstAsync();
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
    public async Task Owner_can_upload_a_reviewed_raster_asset_and_public_delivery_uses_the_persisted_copy()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        byte[] png = [137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3, 4];
        using var uploaded = await UploadAsync(client, png, "image/png", "homepage-store.png");
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        using var json = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        var assetPath = json.RootElement.GetProperty("assetPath").GetString();
        Assert.StartsWith("/api/v1/store-banner-assets/", assetPath);

        using var publicAsset = await client.GetAsync(assetPath);
        Assert.Equal(HttpStatusCode.OK, publicAsset.StatusCode);
        Assert.Equal("image/png", publicAsset.Content.Headers.ContentType?.MediaType);
        Assert.Equal(png, await publicAsset.Content.ReadAsByteArrayAsync());

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        var assetId = Guid.Parse(assetPath!["/api/v1/store-banner-assets/".Length..]);
        Assert.True(await db.StoreBannerAssets.AnyAsync(asset => asset.Id == assetId));
        Assert.True(await db.AdminAuditEvents.AnyAsync(audit => audit.EntityType == "StoreBannerAsset" && audit.Action == "UPLOAD"));

        using var rejected = await UploadAsync(client, [1, 2, 3, 4], "image/png", "not-really.png");
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Product_images_are_admin_reviewed_and_publication_fails_closed()
    {
        var client = await CreateAuthenticatedAsync(admin: true);
        var slug = $"product-image-{Guid.NewGuid():N}";
        using var offer = await MutateAsync(client, HttpMethod.Post, "/api/v1/admin/offers", await OfferInputAsync(slug, enabled: true));
        Assert.Equal(HttpStatusCode.Created, offer.StatusCode);
        using var offerJson = JsonDocument.Parse(await offer.Content.ReadAsStringAsync());
        var productId = offerJson.RootElement.GetProperty("productId").GetGuid();
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        using var uploaded = await UploadProductImageAsync(client, productId, png, activate: false);
        Assert.Equal(HttpStatusCode.Created, uploaded.StatusCode);
        using var uploadedJson = JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
        var imageId = uploadedJson.RootElement.GetProperty("id").GetGuid();
        var publicPath = uploadedJson.RootElement.GetProperty("publicPath").GetString()!;
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(publicPath)).StatusCode);

        using var before = JsonDocument.Parse(await (await client.GetAsync($"/api/v1/products/{slug}")).Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, before.RootElement.GetProperty("productImage").ValueKind);

        using var activated = await MutateAsync(client, HttpMethod.Post, $"/api/v1/admin/product-images/{imageId}/activate", new { changeReason = "Reviewed for all public product placements" });
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);
        using var publicImage = await client.GetAsync(publicPath);
        Assert.Equal(HttpStatusCode.OK, publicImage.StatusCode);
        Assert.Equal("image/png", publicImage.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(publicImage.Headers.ETag);
        Assert.True(publicImage.Headers.CacheControl?.Public);
        Assert.True(publicImage.Headers.CacheControl?.MustRevalidate);
        Assert.Equal(TimeSpan.Zero, publicImage.Headers.CacheControl?.MaxAge);
        using var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, publicPath);
        conditionalRequest.Headers.IfNoneMatch.Add(publicImage.Headers.ETag!);
        using var notModified = await client.SendAsync(conditionalRequest);
        Assert.Equal(HttpStatusCode.NotModified, notModified.StatusCode);

        using var after = JsonDocument.Parse(await (await client.GetAsync($"/api/v1/products/{slug}")).Content.ReadAsStringAsync());
        Assert.Equal(publicPath, after.RootElement.GetProperty("productImage").GetProperty("url").GetString());

        using var archived = await MutateAsync(client, HttpMethod.Post, $"/api/v1/admin/product-images/{imageId}/archive", new { changeReason = "Asset withdrawn" });
        Assert.Equal(HttpStatusCode.NoContent, archived.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(publicPath)).StatusCode);

        await using var cleanup = fixture.Services.CreateAsyncScope();
        var db = cleanup.ServiceProvider.GetRequiredService<DealsDbContext>();
        var listing = await db.RetailerListings.SingleAsync(item => item.ProductId == productId);
        listing.SetEnabled(false);
        await db.SaveChangesAsync();
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
