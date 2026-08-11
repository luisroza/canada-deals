using System.Net;
using System.Text.Json;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class ApiContractTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient CreateClient() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    [RequiresPostgresFact]
    public async Task Discovery_returns_fixture_evidence_and_freshness_states()
    {
        using var response = await CreateClient().GetAsync("/api/v1/deals");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("items");

        Assert.True(items.GetArrayLength() >= 6);
        Assert.Contains(items.EnumerateArray(), item => item.GetProperty("evidenceState").GetString() == "STRONG");
        Assert.Contains(items.EnumerateArray(), item => item.GetProperty("freshnessState").GetString() == "STALE");
    }

    [RequiresPostgresFact]
    public async Task Product_detail_separates_possible_variant_from_safe_comparison()
    {
        using var response = await CreateClient().GetAsync("/api/v1/products/mapleforge-20v-drill-kit");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal("PARTIAL", json.RootElement.GetProperty("primaryOffer").GetProperty("historyState").GetString());
        Assert.Empty(json.RootElement.GetProperty("safeComparisons").EnumerateArray());
        Assert.NotEmpty(json.RootElement.GetProperty("relatedListingsForReview").EnumerateArray());
        Assert.Equal("Review before comparing", json.RootElement.GetProperty("relatedListingsForReview")[0].GetProperty("matchState").GetString());
    }

    [RequiresPostgresFact]
    public async Task Handoff_resolves_server_side_and_ignores_arbitrary_query_destination()
    {
        var client = CreateClient();
        using var deals = await client.GetAsync("/api/v1/deals");
        deals.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await deals.Content.ReadAsStringAsync());
        var listingId = json.RootElement.GetProperty("items")[0].GetProperty("listingId").GetGuid();

        using var response = await client.GetAsync($"/go/{listingId}?url=https://attacker.example");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://demo.local/", response.Headers.Location?.ToString());
    }

    [RequiresPostgresFact]
    public async Task Health_reports_postgres_reachability()
    {
        using var response = await CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
