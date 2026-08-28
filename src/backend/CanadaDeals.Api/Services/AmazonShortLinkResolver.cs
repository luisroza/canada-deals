using System.Net;
using CanadaDeals.Infrastructure.Affiliates;

namespace CanadaDeals.Api.Services;

public interface IAmazonShortLinkResolver
{
    Task<Uri> ResolveAsync(string shortLink, CancellationToken cancellationToken);
}

public sealed class AmazonShortLinkResolutionException(string message) : Exception(message);

public sealed class AmazonShortLinkResolver(HttpClient httpClient) : IAmazonShortLinkResolver
{
    private const int MaximumRedirects = 3;

    public async Task<Uri> ResolveAsync(string shortLink, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(shortLink, UriKind.Absolute, out var current) ||
            current.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(current.IdnHost, "amzn.to", StringComparison.OrdinalIgnoreCase) ||
            current.Port != 443 ||
            !string.IsNullOrEmpty(current.UserInfo) ||
            !string.IsNullOrEmpty(current.Fragment))
            throw new AmazonShortLinkResolutionException("Use a complete HTTPS amzn.to link without credentials or a fragment.");

        for (var redirect = 0; redirect < MaximumRedirects; redirect++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, current);
            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new AmazonShortLinkResolutionException("Amazon did not respond before link validation timed out. Try again or paste the full Amazon.ca link.");
            }
            catch (HttpRequestException)
            {
                throw new AmazonShortLinkResolutionException("The Amazon short link could not be reached. Try again or paste the full Amazon.ca link.");
            }
            using (response)
            {
                if (!IsRedirect(response.StatusCode) || response.Headers.Location is null)
                    throw new AmazonShortLinkResolutionException("Amazon did not return a usable destination for this short link. Create a new Canada Associates link and try again.");

                var next = response.Headers.Location.IsAbsoluteUri
                    ? response.Headers.Location
                    : new Uri(current, response.Headers.Location);
                if (string.Equals(next.IdnHost, "amzn.to", StringComparison.OrdinalIgnoreCase))
                {
                    if (next.Scheme != Uri.UriSchemeHttps || next.Port != 443 || !string.IsNullOrEmpty(next.UserInfo) || !string.IsNullOrEmpty(next.Fragment))
                        throw new AmazonShortLinkResolutionException("The Amazon short link returned an unsafe intermediate destination.");
                    current = next;
                    continue;
                }

                if (!AffiliateUrlPolicy.HostMatches(next.IdnHost, "amazon.ca"))
                    throw new AmazonShortLinkResolutionException($"This short link resolves to {next.IdnHost}, not Amazon Canada. Create the link from the Amazon.ca Associates account.");
                if (next.Scheme != Uri.UriSchemeHttps || next.Port != 443 || !string.IsNullOrEmpty(next.UserInfo) || !string.IsNullOrEmpty(next.Fragment))
                    throw new AmazonShortLinkResolutionException("The Amazon Canada destination must use HTTPS without credentials, a custom port, or a fragment.");

                return next;
            }
        }

        throw new AmazonShortLinkResolutionException("The Amazon short link used too many redirects and was not accepted.");
    }

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Found or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;
}
