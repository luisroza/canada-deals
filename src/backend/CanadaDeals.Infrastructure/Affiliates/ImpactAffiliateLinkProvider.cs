using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CanadaDeals.Domain.Common;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Affiliates;

public sealed class ImpactAffiliateLinkProvider(HttpClient httpClient, IOptions<AffiliateOptions> options, TimeProvider clock) : IAffiliateLinkProvider
{
    private readonly ImpactAffiliateOptions _options = options.Value.Impact;
    public AffiliateProviderType Provider => AffiliateProviderType.Impact;

    public async Task<AffiliateLinkResolution> ResolveAsync(AffiliateLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.AccountSid) || string.IsNullOrWhiteSpace(_options.AuthToken))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.ConfigurationIncomplete, "IMPACT_CONFIGURATION_INCOMPLETE");
        if (!request.Program.CanGenerateLinks() || string.IsNullOrWhiteSpace(request.Program.ProviderProgramId) || string.IsNullOrWhiteSpace(request.Program.MediaPropertyId))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.ConfigurationIncomplete, "PROGRAM_CONFIGURATION_INCOMPLETE");
        if (!AffiliateUrlPolicy.TryValidateHttps(request.Listing.ApprovedAffiliateDestinationReference, request.Program.DestinationDomains, out var destination))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidDestination, "DESTINATION_NOT_APPROVED");

        using var relationshipRequest = CreateRequest(HttpMethod.Get,
            $"Mediapartners/{Uri.EscapeDataString(_options.AccountSid)}/Campaigns/{Uri.EscapeDataString(request.Program.ProviderProgramId)}");
        using var relationshipResponse = await httpClient.SendAsync(relationshipRequest, cancellationToken);
        if (!relationshipResponse.IsSuccessStatusCode) return FailureFromResponse(relationshipResponse, "IMPACT_RELATIONSHIP_LOOKUP");

        JsonDocument relationship;
        try { relationship = JsonDocument.Parse(await relationshipResponse.Content.ReadAsStringAsync(cancellationToken)); }
        catch (JsonException) { return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.MalformedResponse, "IMPACT_RELATIONSHIP_MALFORMED"); }
        using (relationship)
        {
            var root = relationship.RootElement;
            if (!TryGetString(root, "ContractStatus", out var contractStatus) || !string.Equals(contractStatus, "Active", StringComparison.OrdinalIgnoreCase))
                return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.RelationshipInactive, "IMPACT_RELATIONSHIP_NOT_ACTIVE");
            if (!TryGetBoolean(root, "AllowsDeeplinking", out var allowsDeepLinking) || !allowsDeepLinking)
                return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.DeepLinkForbidden, "IMPACT_DEEPLINK_FORBIDDEN");

            var providerDomains = ReadDeepLinkDomains(root);
            if (providerDomains.Count == 0 || !providerDomains.Any(domain => AffiliateUrlPolicy.HostMatches(destination!.IdnHost, domain)))
                return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidDestination, "IMPACT_DESTINATION_DOMAIN_NOT_RETURNED_BY_PROGRAM");
        }

        var query = new Dictionary<string, string>
        {
            ["Type"] = "Regular",
            ["DeepLink"] = destination!.ToString(),
            ["MediaPartnerPropertyId"] = request.Program.MediaPropertyId,
            ["subId1"] = request.Placement,
            ["subId2"] = request.OpaqueClassification
        };
        var queryString = string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        using var trackingRequest = CreateRequest(HttpMethod.Post,
            $"Mediapartners/{Uri.EscapeDataString(_options.AccountSid)}/Programs/{Uri.EscapeDataString(request.Program.ProviderProgramId)}/TrackingLinks?{queryString}");
        using var trackingResponse = await httpClient.SendAsync(trackingRequest, cancellationToken);
        if (!trackingResponse.IsSuccessStatusCode) return FailureFromResponse(trackingResponse, "IMPACT_TRACKING_LINK_CREATE");

        try
        {
            using var payload = JsonDocument.Parse(await trackingResponse.Content.ReadAsStringAsync(cancellationToken));
            if (!TryGetString(payload.RootElement, "TrackingURL", out var trackingUrl) ||
                !AffiliateUrlPolicy.TryValidateHttps(trackingUrl, request.Program.TrackingDomains, out _))
                return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidTrackingUrl, "IMPACT_TRACKING_URL_INVALID");

            var now = clock.GetUtcNow();
            return new AffiliateLinkResolution(AffiliateResolutionStatus.Success, Provider, trackingUrl,
                request.Program.ProviderProgramId, destination.ToString(), now, null, now.AddDays(7),
                request.Program.ProviderProgramId);
        }
        catch (JsonException)
        {
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.MalformedResponse, "IMPACT_TRACKING_RESPONSE_MALFORMED");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + '/'), relativePath));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.AccountSid}:{_options.AuthToken}")));
        return request;
    }

    private AffiliateLinkResolution FailureFromResponse(HttpResponseMessage response, string operation)
    {
        var status = response.StatusCode;
        var resolution = status switch
        {
            HttpStatusCode.Unauthorized => AffiliateResolutionStatus.AuthenticationFailed,
            HttpStatusCode.Forbidden => AffiliateResolutionStatus.RelationshipInactive,
            (HttpStatusCode)429 => AffiliateResolutionStatus.RateLimited,
            >= HttpStatusCode.InternalServerError => AffiliateResolutionStatus.TemporaryFailure,
            _ => AffiliateResolutionStatus.TemporaryFailure
        };
        return new AffiliateLinkResolution(resolution, Provider, RevalidateAt: RetryAt(response), FailureReason: $"{operation}_{(int)status}");
    }

    private DateTimeOffset RetryAt(HttpResponseMessage response)
    {
        var now = clock.GetUtcNow();
        return response.Headers.RetryAfter?.Delta is { } delta ? now.Add(delta) :
            response.Headers.RetryAfter?.Date is { } date && date > now ? date : now.AddHours(1);
    }

    private static bool TryGetString(JsonElement root, string name, out string value)
    {
        value = string.Empty;
        if (!root.TryGetProperty(name, out var property)) return false;
        value = property.ValueKind == JsonValueKind.String ? property.GetString() ?? string.Empty : property.ToString();
        return value.Length > 0;
    }

    private static bool TryGetBoolean(JsonElement root, string name, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(name, out var property)) return false;
        return property.ValueKind switch
        {
            JsonValueKind.True => value = true,
            JsonValueKind.False => true,
            JsonValueKind.String => bool.TryParse(property.GetString(), out value),
            _ => false
        };
    }

    private static IReadOnlyList<string> ReadDeepLinkDomains(JsonElement root)
    {
        if (!root.TryGetProperty("DeeplinkDomains", out var element)) return [];
        var values = new List<string>();
        CollectStrings(element, values);
        return values;
    }

    private static void CollectStrings(JsonElement element, List<string> values)
    {
        if (element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString())) values.Add(element.GetString()!);
        else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) CollectStrings(item, values);
        else if (element.ValueKind == JsonValueKind.Object) foreach (var property in element.EnumerateObject()) CollectStrings(property.Value, values);
    }
}
