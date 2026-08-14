using System.Net;
using System.Net.Http.Headers;
using System.Xml.Linq;
using CanadaDeals.Domain.Common;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Affiliates;

public sealed class CjAffiliateLinkProvider(HttpClient httpClient, IOptions<AffiliateOptions> options, TimeProvider clock) : IAffiliateLinkProvider
{
    private readonly CjAffiliateOptions _options = options.Value.Cj;
    public AffiliateProviderType Provider => AffiliateProviderType.Cj;

    public async Task<AffiliateLinkResolution> ResolveAsync(AffiliateLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.PersonalAccessToken))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.ConfigurationIncomplete, "CJ_CONFIGURATION_INCOMPLETE");
        if (!request.Program.CanGenerateLinks() || string.IsNullOrWhiteSpace(request.Program.ProviderProgramId) ||
            string.IsNullOrWhiteSpace(request.Program.MediaPropertyId) || string.IsNullOrWhiteSpace(request.Program.ProviderLinkReference))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.ConfigurationIncomplete, "CJ_PROGRAM_OR_LINK_REFERENCE_INCOMPLETE");
        if (!AffiliateUrlPolicy.TryValidateHttps(request.Listing.ApprovedAffiliateDestinationReference, request.Program.DestinationDomains, out var destination))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidDestination, "DESTINATION_NOT_APPROVED");

        var query = new Dictionary<string, string>
        {
            ["website-id"] = request.Program.MediaPropertyId,
            ["advertiser-ids"] = request.Program.ProviderProgramId,
            ["link-id"] = request.Program.ProviderLinkReference,
            ["allow-deep-linking"] = "true",
            ["targeted-country"] = "CA",
            ["records-per-page"] = "1"
        };
        var queryString = string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        using var message = new HttpRequestMessage(HttpMethod.Get,
            new Uri(new Uri(_options.BaseUrl.TrimEnd('/') + '/'), $"v2/link-search?{queryString}"));
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.PersonalAccessToken);
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        using var response = await httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) return FailureFromResponse(response);

        XDocument document;
        try { document = XDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); }
        catch (Exception exception) when (exception is System.Xml.XmlException or InvalidOperationException)
        { return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.MalformedResponse, "CJ_LINK_RESPONSE_MALFORMED"); }

        var link = document.Descendants("link").FirstOrDefault(element => element.Element("advertiser-id") is not null);
        if (link is null) return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.RelationshipInactive, "CJ_LINK_NOT_AVAILABLE");
        if (!string.Equals(link.Element("advertiser-id")?.Value, request.Program.ProviderProgramId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(link.Element("relationship-status")?.Value, "joined", StringComparison.OrdinalIgnoreCase))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.RelationshipInactive, "CJ_RELATIONSHIP_NOT_JOINED");
        if (!bool.TryParse(link.Element("allow-deep-linking")?.Value, out var allowsDeepLinking) || !allowsDeepLinking)
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.DeepLinkForbidden, "CJ_DEEPLINK_FORBIDDEN");
        if (!AffiliateUrlPolicy.TryValidateHttps(link.Element("destination")?.Value, request.Program.DestinationDomains, out var returnedDestination) ||
            !AffiliateUrlPolicy.DestinationsMatch(destination!, returnedDestination!))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidDestination, "CJ_RETURNED_DESTINATION_MISMATCH");

        var trackingUrl = link.Element("clickUrl")?.Value;
        if (!AffiliateUrlPolicy.TryValidateHttps(trackingUrl, request.Program.TrackingDomains, out _))
            return AffiliateLinkResolution.Failure(Provider, AffiliateResolutionStatus.InvalidTrackingUrl, "CJ_TRACKING_URL_INVALID");

        var now = clock.GetUtcNow();
        return new AffiliateLinkResolution(AffiliateResolutionStatus.Success, Provider, trackingUrl,
            request.Program.ProviderProgramId, destination!.ToString(), now, null, now.AddDays(7),
            link.Element("link-id")?.Value);
    }

    private AffiliateLinkResolution FailureFromResponse(HttpResponseMessage response)
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
        var now = clock.GetUtcNow();
        var retryAt = response.Headers.RetryAfter?.Delta is { } delta ? now.Add(delta) :
            response.Headers.RetryAfter?.Date is { } date && date > now ? date : now.AddMinutes(5);
        return new AffiliateLinkResolution(resolution, Provider, RevalidateAt: retryAt, FailureReason: $"CJ_LINK_SEARCH_{(int)status}");
    }
}
