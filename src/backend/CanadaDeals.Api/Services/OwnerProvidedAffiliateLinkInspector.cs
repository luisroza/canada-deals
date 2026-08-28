using System.Text.RegularExpressions;
using CanadaDeals.Domain.Common;
using CanadaDeals.Infrastructure.Affiliates;

namespace CanadaDeals.Api.Services;

public sealed record OwnerProvidedAffiliateLinkInspection(
    AffiliateProviderType Provider,
    AffiliateHandoffMode HandoffMode,
    string Status,
    string TrackingHost,
    string? DestinationHost,
    string? ResolvedProductUrl,
    string? ExternalProductId,
    string? CanonicalProductUrl,
    string? PartnerTag,
    IReadOnlyList<string> Warnings);

public sealed partial class OwnerProvidedAffiliateLinkInspector
{
    [GeneratedRegex(@"/(?:dp|gp/product|gp/aw/d)/([A-Z0-9]{10})(?:[/?]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmazonProductPath();

    public OwnerProvidedAffiliateLinkInspection Inspect(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 2000 || value != value.Trim())
            throw new ArgumentException("Paste one complete link without leading or trailing spaces.", nameof(value));
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.Port != 443 || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("Use an absolute HTTPS link on port 443 without credentials or a fragment.", nameof(value));

        if (string.Equals(uri.IdnHost, "amzn.to", StringComparison.OrdinalIgnoreCase))
        {
            return new OwnerProvidedAffiliateLinkInspection(
                AffiliateProviderType.AmazonCreators,
                AffiliateHandoffMode.DirectProvider,
                "NEEDS_REVIEW",
                uri.IdnHost,
                null,
                null,
                null,
                null,
                null,
                [
                    "Amazon short link recognized. Its destination must be validated before Product details can be filled."
                ]);
        }

        if (!AffiliateUrlPolicy.HostMatches(uri.IdnHost, "amazon.ca"))
            throw new ArgumentException("This first version accepts only Amazon.ca or amzn.to owner-provided links.", nameof(value));

        var match = AmazonProductPath().Match(uri.AbsolutePath);
        var asin = match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
        var tag = QueryValue(uri.Query, "tag");
        var warnings = new List<string>();
        if (asin is null) warnings.Add("No ASIN was found in the Amazon Product path.");
        if (string.IsNullOrWhiteSpace(tag)) warnings.Add("No Partner Tag was found. This is a Product page URL, not a finished affiliate link; generate the link in Amazon Associates/SiteStripe before publishing.");

        return new OwnerProvidedAffiliateLinkInspection(
            AffiliateProviderType.AmazonCreators,
            AffiliateHandoffMode.DirectProvider,
            asin is not null && !string.IsNullOrWhiteSpace(tag) ? "READY" : "NEEDS_REVIEW",
            uri.IdnHost,
            uri.IdnHost,
            value,
            asin,
            asin is null ? null : $"https://www.amazon.ca/dp/{asin}",
            tag,
            warnings);
    }

    public OwnerProvidedAffiliateLinkInspection InspectResolvedShortLink(string shortLink, Uri destination)
    {
        var shortInspection = Inspect(shortLink);
        if (!string.Equals(shortInspection.TrackingHost, "amzn.to", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only amzn.to links can use short-link resolution.", nameof(shortLink));

        var destinationInspection = Inspect(destination.AbsoluteUri);
        return destinationInspection with
        {
            TrackingHost = "amzn.to",
            ResolvedProductUrl = destination.AbsoluteUri,
            Warnings =
            [
                "Amazon short link resolved to a validated Amazon.ca destination. The original short link remains the public handoff URL.",
                .. destinationInspection.Warnings
            ]
        };
    }

    public static string? AmazonAsin(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !AffiliateUrlPolicy.HostMatches(uri.IdnHost, "amazon.ca")) return null;
        var match = AmazonProductPath().Match(uri.AbsolutePath);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    private static string? QueryValue(string query, string key)
    {
        foreach (var item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split('=', 2);
            if (!string.Equals(Uri.UnescapeDataString(parts[0]), key, StringComparison.OrdinalIgnoreCase)) continue;
            return parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
        }
        return null;
    }
}
