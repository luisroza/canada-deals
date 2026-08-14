using System.Net;

namespace CanadaDeals.Infrastructure.Rakuten;

internal static class RakutenProviderResponse
{
    public static void EnsureSuccess(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new RakutenProviderException(RakutenFailureKind.Authentication, $"{operation}_AUTHENTICATION_FAILED");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new RakutenProviderException(RakutenFailureKind.Authorization, $"{operation}_AUTHORIZATION_FAILED");
        if ((int)response.StatusCode == 429)
            throw new RakutenProviderException(RakutenFailureKind.RateLimited, $"{operation}_RATE_LIMITED", response.Headers.RetryAfter?.Delta);
        if ((int)response.StatusCode >= 500)
            throw new RakutenProviderException(RakutenFailureKind.ProviderUnavailable, $"{operation}_PROVIDER_UNAVAILABLE");
        throw new RakutenProviderException(RakutenFailureKind.MalformedResponse, $"{operation}_REQUEST_REJECTED");
    }
}
