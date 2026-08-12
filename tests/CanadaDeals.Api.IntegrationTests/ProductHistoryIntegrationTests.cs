using System.Net;
using System.Text.Json;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class ProductHistoryIntegrationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private HttpClient Client() => fixture.CreateClient(new() { AllowAutoRedirect = false });

    private async Task<JsonDocument> GetHistoryAsync(string slug, string window)
    {
        using var response = await Client().GetAsync($"/api/v1/products/{slug}/history?window={window}");
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    [RequiresPostgresFact]
    public async Task Reliable_history_returns_real_daily_lowest_points_and_bounded_summary()
    {
        using var json = await GetHistoryAsync("northstar-55-qled-tv", "90d");
        var root = json.RootElement;

        Assert.Equal("90d", root.GetProperty("window").GetString());
        Assert.Equal("RELIABLE", root.GetProperty("state").GetString());
        Assert.True(root.GetProperty("observationCount").GetInt32() > root.GetProperty("observedDayCount").GetInt32());
        Assert.Equal(1049.99m, root.GetProperty("lowestObservedPrice").GetDecimal());
        Assert.Contains(root.GetProperty("points").EnumerateArray(), point => point.GetProperty("lowestPrice").GetDecimal() == 1049.99m && point.GetProperty("observationCount").GetInt32() == 2);
        Assert.DoesNotContain("all-time", json.RootElement.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [RequiresPostgresFact]
    public async Task Thirty_day_window_excludes_older_observations_while_ninety_day_includes_them()
    {
        using var thirty = await GetHistoryAsync("northstar-55-qled-tv", "30d");
        using var ninety = await GetHistoryAsync("northstar-55-qled-tv", "90d");

        Assert.Equal("RELIABLE", thirty.RootElement.GetProperty("state").GetString());
        Assert.True(ninety.RootElement.GetProperty("observationCount").GetInt32() > thirty.RootElement.GetProperty("observationCount").GetInt32());
        Assert.True(ninety.RootElement.GetProperty("observationStart").GetDateTimeOffset() < thirty.RootElement.GetProperty("observationStart").GetDateTimeOffset());
    }

    [RequiresPostgresFact]
    public async Task Partial_history_returns_actual_sparse_points_and_gap_explanation()
    {
        using var json = await GetHistoryAsync("mapleforge-20v-drill-kit", "90d");
        var root = json.RootElement;

        Assert.Equal("PARTIAL", root.GetProperty("state").GetString());
        Assert.Equal(2, root.GetProperty("points").GetArrayLength());
        Assert.Contains("gaps", root.GetProperty("coverageSummary").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [RequiresPostgresFact]
    public async Task Unavailable_history_has_no_fake_series_but_product_current_price_remains_available()
    {
        using var history = await GetHistoryAsync("search-fixture-unavailable-kettle", "90d");
        Assert.Equal("UNAVAILABLE", history.RootElement.GetProperty("state").GetString());
        Assert.Empty(history.RootElement.GetProperty("points").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, history.RootElement.GetProperty("lowestObservedPrice").ValueKind);

        using var productResponse = await Client().GetAsync("/api/v1/products/search-fixture-unavailable-kettle");
        productResponse.EnsureSuccessStatusCode();
        using var product = JsonDocument.Parse(await productResponse.Content.ReadAsStringAsync());
        Assert.True(product.RootElement.GetProperty("primaryOffer").GetProperty("currentPrice").GetDecimal() > 0);
    }

    [RequiresPostgresFact]
    public async Task Policy_unknown_history_is_not_exposed()
    {
        using var json = await GetHistoryAsync("search-fixture-policy-hidden-speaker", "90d");
        Assert.Equal("UNAVAILABLE", json.RootElement.GetProperty("state").GetString());
        Assert.Empty(json.RootElement.GetProperty("points").EnumerateArray());
        Assert.DoesNotContain("9.99", json.RootElement.GetRawText());
        Assert.DoesNotContain("19.99", json.RootElement.GetRawText());
    }

    [RequiresPostgresFact]
    public async Task Unsafe_cheaper_variant_does_not_enter_canonical_product_history()
    {
        using var json = await GetHistoryAsync("mapleforge-20v-drill-kit", "90d");
        Assert.Equal(179.99m, json.RootElement.GetProperty("lowestObservedPrice").GetDecimal());
        Assert.DoesNotContain(json.RootElement.GetProperty("points").EnumerateArray(), point => point.GetProperty("lowestPrice").GetDecimal() is 49.99m or 59.99m or 89.99m);
    }

    [RequiresPostgresFact]
    public async Task Stale_current_price_remains_independent_from_reliable_history()
    {
        using var history = await GetHistoryAsync("northstar-65-oled-tv", "90d");
        Assert.Equal("RELIABLE", history.RootElement.GetProperty("state").GetString());

        using var productResponse = await Client().GetAsync("/api/v1/products/northstar-65-oled-tv");
        productResponse.EnsureSuccessStatusCode();
        using var product = JsonDocument.Parse(await productResponse.Content.ReadAsStringAsync());
        Assert.Equal("STALE", product.RootElement.GetProperty("primaryOffer").GetProperty("freshnessState").GetString());
    }

    [RequiresPostgresTheory]
    [InlineData("all")]
    [InlineData("365d")]
    [InlineData("0d")]
    public async Task Unsupported_windows_return_bad_request(string window)
    {
        using var response = await Client().GetAsync($"/api/v1/products/northstar-55-qled-tv/history?window={window}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresPostgresFact]
    public async Task Unknown_product_history_returns_not_found()
    {
        using var response = await Client().GetAsync("/api/v1/products/not-a-product/history?window=30d");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
