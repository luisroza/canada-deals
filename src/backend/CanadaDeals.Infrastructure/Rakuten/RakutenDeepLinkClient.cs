using System.Text;
using System.Text.Json;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenDeepLinkClient(RakutenAuthenticatedClient client) : IRakutenDeepLinkClient
{
    public async Task<RakutenDeepLinkResult> CreateAsync(
        string advertiserMid,
        string destinationUrl,
        string? opaqueAttribution,
        CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(advertiserMid, out var numericMid) || numericMid <= 0)
            throw new RakutenProviderException(RakutenFailureKind.ConfigurationError, "RAKUTEN_ADVERTISER_MID_INVALID");
        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out var destination) || destination.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(destination.UserInfo))
            throw new RakutenProviderException(RakutenFailureKind.InvalidDestination, "RAKUTEN_DESTINATION_INVALID");
        if (!string.IsNullOrWhiteSpace(opaqueAttribution) && (opaqueAttribution.Length > 64 || opaqueAttribution.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_')))
            throw new RakutenProviderException(RakutenFailureKind.ConfigurationError, "RAKUTEN_U1_INVALID");

        using var response = await client.SendAsync(() =>
        {
            var payload = new Dictionary<string, object> { ["url"] = destination.ToString(), ["advertiser_id"] = numericMid };
            if (!string.IsNullOrWhiteSpace(opaqueAttribution)) payload["u1"] = opaqueAttribution;
            return new HttpRequestMessage(HttpMethod.Post, "v1/links/deep_links")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
        }, cancellationToken);
        RakutenProviderResponse.EnsureSuccess(response, "RAKUTEN_DEEP_LINK");

        JsonDocument document;
        try { document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); }
        catch (JsonException) { throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_DEEP_LINK_RESPONSE_MALFORMED"); }
        using (document)
        {
            var root = document.RootElement;
            if (TryProviderError(root, out var error)) throw MapProviderError(error!);
            if (!root.TryGetProperty("advertiser", out var advertiser) ||
                !advertiser.TryGetProperty("deep_link", out var deepLink) ||
                !String(deepLink, "deep_link_url", out var trackingUrl) ||
                !String(deepLink, "url", out var returnedDestination))
                throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, "RAKUTEN_DEEP_LINK_RESPONSE_INCOMPLETE");
            return new RakutenDeepLinkResult(advertiserMid, trackingUrl!, returnedDestination!, StringValue(deepLink, "u1"));
        }
    }

    private static bool TryProviderError(JsonElement root, out string? error)
    {
        error = StringValue(root, "error") ?? StringValue(root, "message") ?? StringValue(root, "error_code");
        return !string.IsNullOrWhiteSpace(error);
    }

    private static RakutenProviderException MapProviderError(string error) => error.Trim().ToUpperInvariant() switch
    {
        "ACCESS_DENIED" => new(RakutenFailureKind.PartnershipDenied, "RAKUTEN_ACCESS_DENIED"),
        "DEEP_LINK_DENIED" => new(RakutenFailureKind.DeepLinkDisabled, "RAKUTEN_DEEP_LINK_DENIED"),
        "CANNOT_RESOLVE_ADVERTISER" => new(RakutenFailureKind.AdvertiserInactive, "RAKUTEN_CANNOT_RESOLVE_ADVERTISER"),
        "DEEP_LINKING_NOT_ENABLED" => new(RakutenFailureKind.DeepLinkDisabled, "RAKUTEN_DEEP_LINKING_NOT_ENABLED"),
        "URL_TEMPLATE_MISMATCH" => new(RakutenFailureKind.InvalidDestination, "RAKUTEN_URL_TEMPLATE_MISMATCH"),
        _ => new(RakutenFailureKind.MalformedResponse, "RAKUTEN_DEEP_LINK_PROVIDER_ERROR")
    };

    private static bool String(JsonElement node, string name, out string? value)
    {
        value = StringValue(node, name);
        return !string.IsNullOrWhiteSpace(value);
    }
    private static string? StringValue(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
