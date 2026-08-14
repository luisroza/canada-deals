using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CanadaDeals.Domain.Reporting;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class ApiContractTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient CreateClient() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    private async Task<Guid> GetListingIdAsync(string search)
    {
        using var response = await CreateClient().GetAsync($"/api/v1/deals?search={Uri.EscapeDataString(search)}");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("items")[0].GetProperty("listingId").GetGuid();
    }

    [RequiresPostgresFact]
    public async Task API_responses_include_the_approved_security_headers()
    {
        using var response = await CreateClient().GetAsync("/api/v1/deals");
        response.EnsureSuccessStatusCode();

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [RequiresPostgresFact]
    public async Task Discovery_returns_fixture_evidence_and_freshness_states()
    {
        using var strongResponse = await CreateClient().GetAsync("/api/v1/deals?search=NS55QLED-2026");
        using var staleResponse = await CreateClient().GetAsync("/api/v1/deals?search=NS65OLED-2025");
        strongResponse.EnsureSuccessStatusCode();
        staleResponse.EnsureSuccessStatusCode();
        using var strongJson = JsonDocument.Parse(await strongResponse.Content.ReadAsStringAsync());
        using var staleJson = JsonDocument.Parse(await staleResponse.Content.ReadAsStringAsync());

        Assert.Contains(strongJson.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("evidenceState").GetString() == "STRONG");
        Assert.Contains(staleJson.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("freshnessState").GetString() == "STALE");
    }

    [RequiresPostgresFact]
    public async Task Product_detail_separates_possible_variant_from_safe_comparison()
    {
        using var response = await CreateClient().GetAsync("/api/v1/products/mapleforge-20v-drill-kit");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("PARTIAL", json.RootElement.GetProperty("primaryOffer").GetProperty("historyState").GetString());
        Assert.Equal("AVAILABLE", json.RootElement.GetProperty("primaryOffer").GetProperty("availabilityState").GetString());
        Assert.Equal("NEW", json.RootElement.GetProperty("primaryOffer").GetProperty("conditionState").GetString());
        Assert.Equal("Canada", json.RootElement.GetProperty("primaryOffer").GetProperty("regionAvailabilityContext").GetString());
        Assert.True(json.RootElement.GetProperty("primaryOffer").TryGetProperty("seller", out _));
        Assert.True(json.RootElement.GetProperty("primaryOffer").TryGetProperty("shippingContext", out _));
        Assert.Empty(json.RootElement.GetProperty("safeComparisons").EnumerateArray());
        Assert.NotEmpty(json.RootElement.GetProperty("relatedListingsForReview").EnumerateArray());
        Assert.Equal("Review before comparing", json.RootElement.GetProperty("relatedListingsForReview")[0].GetProperty("matchState").GetString());
    }

    [RequiresPostgresFact]
    public async Task Product_detail_exposes_unavailable_history_without_a_strong_claim()
    {
        using var response = await CreateClient().GetAsync("/api/v1/products/northstar-quiet-headphones");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("UNAVAILABLE", json.RootElement.GetProperty("primaryOffer").GetProperty("historyState").GetString());
        Assert.Contains("unavailable", json.RootElement.GetProperty("historySummary").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("all-time-low", json.RootElement.GetProperty("historySummary").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [RequiresPostgresFact]
    public async Task Product_detail_returns_not_found_for_an_unknown_slug()
    {
        using var response = await CreateClient().GetAsync("/api/v1/products/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Handoff_resolves_server_side_and_ignores_arbitrary_query_destination()
    {
        var client = CreateClient();
        using var deals = await client.GetAsync("/api/v1/deals?search=55-inch");
        deals.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await deals.Content.ReadAsStringAsync());
        var listingId = json.RootElement.GetProperty("items")[0].GetProperty("listingId").GetGuid();

        using var response = await client.GetAsync($"/go/{listingId}?url=https://attacker.example");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://demo.local/", response.Headers.Location?.ToString());
    }

    [RequiresPostgresFact]
    public async Task Handoff_does_not_redirect_for_a_listing_without_an_approved_destination()
    {
        var client = CreateClient();
        using var deals = await client.GetAsync("/api/v1/deals?search=compact%20impact%20driver");
        deals.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await deals.Content.ReadAsStringAsync());
        var item = json.RootElement.GetProperty("items")[0];

        Assert.Equal(JsonValueKind.Null, item.GetProperty("handoffPath").ValueKind);
        var listingId = item.GetProperty("listingId").GetGuid();

        using var response = await client.GetAsync($"/go/{listingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Handoff_rejects_malformed_and_unknown_listing_ids()
    {
        var client = CreateClient();

        using var malformed = await client.GetAsync("/go/not-a-guid");
        using var unknown = await client.GetAsync($"/go/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Health_reports_postgres_reachability()
    {
        using var response = await CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Valid_listing_report_is_persisted_open_and_reviewable_without_changing_listing_truth()
    {
        var listingId = await GetListingIdAsync("55-inch");
        var note = $"integration-price-change-{Guid.NewGuid():N}";
        decimal? priceBefore;

        await using (var beforeScope = fixture.Services.CreateAsyncScope())
        {
            var beforeDb = beforeScope.ServiceProvider.GetRequiredService<DealsDbContext>();
            priceBefore = await beforeDb.RetailerListings
                .Where(x => x.Id == listingId)
                .Select(x => x.CurrentPriceAmount)
                .SingleAsync();
        }

        using var response = await CreateClient().PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/reports",
            new { reason = "PRICE_CHANGED", note });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var reportId = json.RootElement.GetProperty("reportId").GetGuid();
        Assert.Equal("OPEN", json.RootElement.GetProperty("status").GetString());

        await using (var afterScope = fixture.Services.CreateAsyncScope())
        {
            var afterDb = afterScope.ServiceProvider.GetRequiredService<DealsDbContext>();
            var persisted = await afterDb.ListingIssueReports.SingleAsync(x => x.Id == reportId);
            var priceAfter = await afterDb.RetailerListings
                .Where(x => x.Id == listingId)
                .Select(x => x.CurrentPriceAmount)
                .SingleAsync();

            Assert.Equal(ListingIssueStatus.Open, persisted.Status);
            Assert.Equal(ListingIssueReason.PriceChanged, persisted.Reason);
            Assert.Equal(note, persisted.Note);
            Assert.Equal(priceBefore, priceAfter);
        }

        using var review = await CreateClient().GetAsync("/api/internal/listing-issue-reports?status=OPEN");
        review.EnsureSuccessStatusCode();
        using var reviewJson = JsonDocument.Parse(await review.Content.ReadAsStringAsync());
        Assert.Contains(reviewJson.RootElement.EnumerateArray(), item => item.GetProperty("reportId").GetGuid() == reportId);
    }

    [RequiresPostgresFact]
    public async Task Unknown_listing_report_is_rejected_without_an_orphan()
    {
        var unknownListingId = Guid.NewGuid();
        int countBefore;

        await using (var beforeScope = fixture.Services.CreateAsyncScope())
        {
            var db = beforeScope.ServiceProvider.GetRequiredService<DealsDbContext>();
            countBefore = await db.ListingIssueReports.CountAsync();
        }

        using var response = await CreateClient().PostAsJsonAsync(
            $"/api/v1/listings/{unknownListingId}/reports",
            new { reason = "WRONG_PRODUCT" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        await using var afterScope = fixture.Services.CreateAsyncScope();
        var afterDb = afterScope.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.Equal(countBefore, await afterDb.ListingIssueReports.CountAsync());
    }

    [RequiresPostgresFact]
    public async Task Invalid_report_reason_is_rejected()
    {
        var listingId = await GetListingIdAsync("55-inch");

        using var response = await CreateClient().PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/reports",
            new { reason = "MAKE_IT_CHEAPER" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Excessive_report_note_is_rejected()
    {
        var listingId = await GetListingIdAsync("55-inch");

        using var response = await CreateClient().PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/reports",
            new { reason = "OTHER", note = new string('x', ListingIssueReport.MaxNoteLength + 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Same_listing_can_receive_multiple_reports()
    {
        var listingId = await GetListingIdAsync("55-inch");
        var firstNote = $"duplicate-valid-a-{Guid.NewGuid():N}";
        var secondNote = $"duplicate-valid-b-{Guid.NewGuid():N}";
        var client = CreateClient();

        using var first = await client.PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/reports",
            new { reason = "OFFER_EXPIRED", note = firstNote });
        using var second = await client.PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/reports",
            new { reason = "RETAILER_PAGE_UNAVAILABLE", note = secondNote });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        Assert.Equal(2, await db.ListingIssueReports.CountAsync(x => x.Note == firstNote || x.Note == secondNote));
    }

    [RequiresPostgresFact]
    public async Task Already_stale_listing_can_be_reported_for_review()
    {
        var listingId = await GetListingIdAsync("65-inch");

        using var response = await CreateClient().PostAsJsonAsync(
            $"/api/v1/listings/{listingId}/reports",
            new { reason = "PRICE_CHANGED" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Internal_report_review_endpoint_is_not_exposed_outside_development()
    {
        using var nonDevelopmentFactory = fixture.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
        using var response = await nonDevelopmentFactory.CreateClient().GetAsync("/api/internal/listing-issue-reports?status=OPEN");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
