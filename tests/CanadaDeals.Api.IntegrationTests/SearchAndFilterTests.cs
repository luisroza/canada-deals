using System.Net;
using System.Text.Json;
using CanadaDeals.Domain.Catalog;
using CanadaDeals.Domain.Common;
using CanadaDeals.Domain.Policies;
using CanadaDeals.Domain.Retailers;
using CanadaDeals.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class SearchAndFilterTests(ApiFixture fixture) : IClassFixture<ApiFixture>, IAsyncLifetime
{
    private const string UnavailableSlug = "search-fixture-unavailable-kettle";
    private const string HiddenSlug = "search-fixture-policy-hidden-speaker";
    private HttpClient Client() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    public async Task InitializeAsync()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealsDbContext>();
        if (await db.Products.AnyAsync(product => product.Slug == UnavailableSlug)) return;

        var category = await db.Categories.SingleAsync(item => item.Slug == "electronics");
        var brand = await db.Brands.SingleAsync(item => item.Slug == "northstar-demo");
        var retailer = await db.Retailers.SingleAsync(item => item.Key == "demo-north-electronics");
        var allowed = await db.MerchantPolicies.SingleAsync(item => item.SourceKey == "demo-fixture");
        var unknown = await db.MerchantPolicies.SingleAsync(item => item.SourceKey == "unknown-fixture-policy");
        var now = DateTimeOffset.UtcNow;
        var empty = new Dictionary<string, string>();
        var unavailable = Product.Create(UnavailableSlug, "Northstar Search Fixture Kettle", brand, category, "NS-KETTLE-404", "NS-KETTLE-404", "990000000001", empty);
        var hidden = Product.Create(HiddenSlug, "Northstar Policy Hidden Speaker", brand, category, "NS-HIDDEN-404", "NS-HIDDEN-404", "990000000002", empty);
        db.AddRange(unavailable, hidden);
        await db.SaveChangesAsync();
        db.AddRange(
            RetailerListing.Create(unavailable.Id, retailer.Id, "SEARCH-UNAVAILABLE", unavailable.Title, "https://demo.local/search-unavailable", allowed.Id, MatchState.Confirmed, now.AddMinutes(-30), now.AddMinutes(-30), 49.99m, "CAD", FreshnessState.Recent, EvidenceState.Unknown, HistoryAvailability.Unavailable, empty, empty, onlineAvailability: OnlineAvailabilityState.Unavailable),
            RetailerListing.Create(hidden.Id, retailer.Id, "SEARCH-HIDDEN", hidden.Title, "https://demo.local/search-hidden", unknown.Id, MatchState.Confirmed, now, now, 9.99m, "CAD", FreshnessState.Recent, EvidenceState.Unknown, HistoryAvailability.Unavailable, empty, empty, onlineAvailability: OnlineAvailabilityState.Available));
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<JsonDocument> GetJsonAsync(string path)
    {
        using var response = await Client().GetAsync(path);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task Feed_defaults_to_recent_with_bounded_metadata_and_one_card_per_product()
    {
        using var json = await GetJsonAsync("/api/v1/deals?pageSize=3");
        var root = json.RootElement;
        Assert.Equal("recent", root.GetProperty("sort").GetString());
        Assert.Equal(1, root.GetProperty("page").GetInt32());
        Assert.Equal(3, root.GetProperty("pageSize").GetInt32());
        Assert.True(root.GetProperty("totalPages").GetInt32() >= 2);
        var ids = root.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetGuid()).ToArray();
        Assert.Equal(ids.Distinct().Count(), ids.Length);
    }

    [RequiresPostgresFact]
    public async Task Exact_model_identifier_wins_and_search_defaults_to_relevance()
    {
        using var json = await GetJsonAsync("/api/v1/deals?search=NS55QLED-2026");
        Assert.Equal("relevance", json.RootElement.GetProperty("sort").GetString());
        Assert.Equal("northstar-55-qled-tv", json.RootElement.GetProperty("items")[0].GetProperty("productSlug").GetString());
    }

    [RequiresPostgresTheory]
    [InlineData("wireless headphones", "northstar-quiet-headphones")]
    [InlineData("cordles dril", "mapleforge-20v-drill-kit")]
    [InlineData("Home Improvement Tools", "mapleforge-20v-drill-kit")]
    public async Task Full_text_and_controlled_typo_search_find_expected_products(string query, string expectedSlug)
    {
        using var json = await GetJsonAsync($"/api/v1/deals?search={Uri.EscapeDataString(query)}");
        Assert.Contains(json.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("productSlug").GetString() == expectedSlug);
    }

    [RequiresPostgresFact]
    public async Task No_result_query_returns_honest_empty_metadata()
    {
        using var json = await GetJsonAsync("/api/v1/deals?search=definitely-no-such-product-xyz");
        Assert.Equal(0, json.RootElement.GetProperty("count").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Empty(json.RootElement.GetProperty("items").EnumerateArray());
    }

    [RequiresPostgresTheory]
    [InlineData("category=home-improvement-tools", "home-improvement-tools")]
    [InlineData("retailer=demo-home-tool", "demo-home-tool")]
    [InlineData("hasReference=true", "reference")]
    [InlineData("freshness=stale", "stale")]
    [InlineData("match=review", "review")]
    [InlineData("availability=unavailable", UnavailableSlug)]
    public async Task Each_P0_filter_returns_a_supported_subset(string filter, string evidence)
    {
        using var json = await GetJsonAsync($"/api/v1/deals?{filter}");
        var items = json.RootElement.GetProperty("items");
        Assert.NotEmpty(items.EnumerateArray());
        if (evidence == "reference") Assert.All(items.EnumerateArray(), item => Assert.NotEqual(JsonValueKind.Null, item.GetProperty("referencePrice").ValueKind));
        if (evidence == "stale") Assert.All(items.EnumerateArray(), item => Assert.Equal("STALE", item.GetProperty("freshnessState").GetString()));
        if (evidence == "review") Assert.All(items.EnumerateArray(), item => Assert.Equal("Review before comparing", item.GetProperty("matchState").GetString()));
        if (evidence == UnavailableSlug) Assert.Contains(items.EnumerateArray(), item => item.GetProperty("productSlug").GetString() == UnavailableSlug);
    }

    [RequiresPostgresFact]
    public async Task Price_filter_requires_safe_online_comparison_and_combines_with_other_dimensions()
    {
        using var json = await GetJsonAsync("/api/v1/deals?category=home-improvement-tools&minPrice=100&maxPrice=200&match=safe&availability=online");
        var items = json.RootElement.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal("mapleforge-20v-drill-kit", items[0].GetProperty("productSlug").GetString());
        Assert.InRange(items[0].GetProperty("currentPrice").GetDecimal(), 100m, 200m);
    }

    [RequiresPostgresFact]
    public async Task Comma_values_are_OR_within_a_dimension_and_dimensions_are_ANDed()
    {
        using var json = await GetJsonAsync("/api/v1/deals?category=electronics,home-improvement-tools&retailer=demo-home-tool&match=safe");
        var items = json.RootElement.GetProperty("items");
        Assert.NotEmpty(items.EnumerateArray());
        Assert.All(items.EnumerateArray(), item => Assert.Equal("Demo Home & Tool", item.GetProperty("retailer").GetString()));
    }

    [RequiresPostgresTheory]
    [InlineData("price-asc")]
    [InlineData("recent")]
    [InlineData("savings")]
    [InlineData("relevance")]
    public async Task Approved_sorts_are_stable_and_report_the_effective_sort(string sort)
    {
        var search = sort == "relevance" ? "&search=northstar" : string.Empty;
        using var first = await GetJsonAsync($"/api/v1/deals?sort={sort}{search}");
        using var second = await GetJsonAsync($"/api/v1/deals?sort={sort}{search}");
        Assert.Equal(sort, first.RootElement.GetProperty("sort").GetString());
        var firstIds = first.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetGuid()).ToArray();
        var secondIds = second.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetGuid()).ToArray();
        Assert.Equal(firstIds, secondIds);
    }

    [RequiresPostgresFact]
    public async Task Pagination_has_no_overlap_and_reports_next_page()
    {
        using var first = await GetJsonAsync("/api/v1/deals?page=1&pageSize=2");
        using var second = await GetJsonAsync("/api/v1/deals?page=2&pageSize=2");
        var firstIds = first.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetGuid()).ToHashSet();
        var secondIds = second.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("productId").GetGuid()).ToHashSet();
        Assert.True(first.RootElement.GetProperty("hasNext").GetBoolean());
        Assert.Empty(firstIds.Intersect(secondIds));
    }

    [RequiresPostgresTheory]
    [InlineData("page=0")]
    [InlineData("pageSize=49")]
    [InlineData("minPrice=200&maxPrice=100")]
    [InlineData("minPrice=abc")]
    [InlineData("sort=commission")]
    [InlineData("freshness=future")]
    [InlineData("match=maybe")]
    [InlineData("availability=store-only")]
    [InlineData("category=unknown-category")]
    [InlineData("retailer=unknown-retailer")]
    public async Task Invalid_or_unknown_parameters_return_400(string query)
    {
        using var response = await Client().GetAsync($"/api/v1/deals?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Unknown_policy_data_is_excluded_from_search_and_facets()
    {
        using var json = await GetJsonAsync("/api/v1/deals?search=NS-HIDDEN-404");
        Assert.DoesNotContain(json.RootElement.GetProperty("items").EnumerateArray(), item => item.GetProperty("productSlug").GetString() == HiddenSlug);
    }
}
