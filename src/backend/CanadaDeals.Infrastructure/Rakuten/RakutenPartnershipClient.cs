using System.Text.Json;
using CanadaDeals.Domain.Common;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenPartnershipClient(RakutenAuthenticatedClient client) : IRakutenPartnershipClient
{
    public async Task<IReadOnlyList<RakutenPartnershipRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<RakutenPartnershipRecord>();
        for (var page = 1; page <= 100; page++)
        {
            using var response = await client.SendAsync(
                () => new HttpRequestMessage(HttpMethod.Get, $"v1/partnerships?page={page}&limit=100"), cancellationToken);
            RakutenProviderResponse.EnsureSuccess(response, "RAKUTEN_PARTNERSHIPS");
            JsonDocument document;
            try { document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); }
            catch (JsonException) { throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_PARTNERSHIPS_RESPONSE_MALFORMED"); }

            using (document)
            {
                if (!document.RootElement.TryGetProperty("partnerships", out var partnerships) || partnerships.ValueKind != JsonValueKind.Array)
                    throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_PARTNERSHIPS_RESPONSE_INCOMPLETE");
                foreach (var partnership in partnerships.EnumerateArray())
                {
                    if (!partnership.TryGetProperty("advertiser", out var advertiser) ||
                        !TryStringOrNumber(advertiser, "id", out var mid) || string.IsNullOrWhiteSpace(mid)) continue;
                    results.Add(new RakutenPartnershipRecord(
                        mid!, String(advertiser, "name") ?? $"MID {mid}", ParseAdvertiserStatus(String(advertiser, "status")),
                        ParsePartnershipStatus(String(partnership, "status")), ParseDate(String(partnership, "approve_datetime")),
                        ParseDate(String(partnership, "status_update_datetime"))));
                }

                var total = ReadInt(document.RootElement, "metadata", "total");
                if (total <= results.Count || partnerships.GetArrayLength() == 0) break;
            }
        }
        return results;
    }

    public static IntegrationAdvertiserStatus ParseAdvertiserStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "active" => IntegrationAdvertiserStatus.Active,
        "inactive" => IntegrationAdvertiserStatus.Inactive,
        _ => IntegrationAdvertiserStatus.Unknown
    };

    public static IntegrationPartnershipStatus ParsePartnershipStatus(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "active" => IntegrationPartnershipStatus.Active,
        "pending" => IntegrationPartnershipStatus.Pending,
        "self-removed" => IntegrationPartnershipStatus.SelfRemoved,
        "permanent-decline" => IntegrationPartnershipStatus.PermanentDecline,
        "permanent-remove" => IntegrationPartnershipStatus.PermanentRemove,
        "temp-decline" => IntegrationPartnershipStatus.TemporaryDecline,
        "temp-remove" => IntegrationPartnershipStatus.TemporaryRemove,
        "extended" => IntegrationPartnershipStatus.Extended,
        _ => IntegrationPartnershipStatus.Unknown
    };

    private static DateTimeOffset? ParseDate(string? value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;
    private static string? String(JsonElement node, string name) => node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static bool TryStringOrNumber(JsonElement node, string name, out string? value)
    {
        value = null;
        if (!node.TryGetProperty(name, out var property)) return false;
        value = property.ValueKind == JsonValueKind.String ? property.GetString() : property.ValueKind == JsonValueKind.Number ? property.GetRawText() : null;
        return !string.IsNullOrWhiteSpace(value);
    }
    private static int ReadInt(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var part in path) if (!current.TryGetProperty(part, out current)) return 0;
        return current.TryGetInt32(out var value) ? value : 0;
    }
}
