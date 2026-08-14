using System.Text.Json;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenAdvertiserClient(RakutenAuthenticatedClient client) : IRakutenAdvertiserClient
{
    public async Task<IReadOnlyList<RakutenAdvertiserRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<RakutenAdvertiserRecord>();
        for (var page = 1; page <= 100; page++)
        {
            using var response = await client.SendAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"v2/advertisers?page={page}&limit=100"), cancellationToken);
            RakutenProviderResponse.EnsureSuccess(response, "RAKUTEN_ADVERTISERS");
            JsonDocument document;
            try { document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); }
            catch (JsonException) { throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_ADVERTISERS_RESPONSE_MALFORMED"); }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("advertisers", out var advertisers) || advertisers.ValueKind != JsonValueKind.Array)
                    throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_ADVERTISERS_RESPONSE_INCOMPLETE");
                foreach (var advertiser in advertisers.EnumerateArray())
                {
                    if (!TryStringOrNumber(advertiser, "id", out var mid) ||
                        !advertiser.TryGetProperty("name", out var nameNode) || nameNode.ValueKind != JsonValueKind.String ||
                        string.IsNullOrWhiteSpace(nameNode.GetString())) continue;
                    var shipsTo = ReadStrings(advertiser, "policies", "international_capabilities", "ships_to");
                    var features = advertiser.TryGetProperty("features", out var featureNode) ? featureNode : default;
                    results.Add(new RakutenAdvertiserRecord(
                        mid!, nameNode.GetString()!, String(advertiser, "url"), Boolean(advertiser, "can_partner"), shipsTo,
                        Boolean(features, "product_feed"), Boolean(features, "deep_links")));
                }

                var total = ReadInt(document.RootElement, "_metadata", "page", "total");
                if (total <= results.Count || advertisers.GetArrayLength() == 0) break;
            }
        }
        return results;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var part in path)
            if (!current.TryGetProperty(part, out current)) return [];
        return current.ValueKind == JsonValueKind.Array
            ? current.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
            : [];
    }

    private static bool Boolean(JsonElement node, string name) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True;

    private static string? String(JsonElement node, string name) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool TryStringOrNumber(JsonElement node, string name, out string? value)
    {
        value = null;
        if (!node.TryGetProperty(name, out var property)) return false;
        value = property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null
        };
        return !string.IsNullOrWhiteSpace(value);
    }

    private static int ReadInt(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var part in path)
            if (!current.TryGetProperty(part, out current)) return 0;
        return current.TryGetInt32(out var value) ? value : 0;
    }
}
