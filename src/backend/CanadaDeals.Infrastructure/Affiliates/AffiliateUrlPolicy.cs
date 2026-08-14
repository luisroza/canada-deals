namespace CanadaDeals.Infrastructure.Affiliates;

public static class AffiliateUrlPolicy
{
    public static bool TryValidateHttps(string? value, IReadOnlyCollection<string> allowedDomains, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(value) || allowedDomains.Count == 0 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            candidate.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(candidate.UserInfo) ||
            string.IsNullOrWhiteSpace(candidate.IdnHost) ||
            !allowedDomains.Any(domain => HostMatches(candidate.IdnHost, domain)))
        {
            return false;
        }

        uri = candidate;
        return true;
    }

    public static bool HostMatches(string host, string configuredDomain)
    {
        var normalizedHost = host.Trim().TrimEnd('.').ToLowerInvariant();
        var normalizedDomain = configuredDomain.Trim().TrimEnd('.').ToLowerInvariant();
        return normalizedDomain.Length > 0 &&
               !normalizedDomain.Contains('/') &&
               !normalizedDomain.Contains('*') &&
               (normalizedHost == normalizedDomain || normalizedHost.EndsWith('.' + normalizedDomain, StringComparison.Ordinal));
    }

    public static bool DestinationsMatch(Uri first, Uri second) =>
        string.Equals(first.Scheme, second.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(first.IdnHost, second.IdnHost, StringComparison.OrdinalIgnoreCase) &&
        first.Port == second.Port &&
        string.Equals(first.PathAndQuery, second.PathAndQuery, StringComparison.Ordinal);
}
