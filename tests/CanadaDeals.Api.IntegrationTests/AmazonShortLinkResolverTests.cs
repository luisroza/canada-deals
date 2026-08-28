using System.Net;
using CanadaDeals.Api.Services;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class AmazonShortLinkResolverTests
{
    [Fact]
    public async Task Resolver_accepts_only_an_https_Amazon_Canada_destination()
    {
        using var client = new HttpClient(new RedirectHandler(_ => Redirect("https://www.amazon.ca/Useful-Product/dp/B0DMNJNFW8?tag=canadadeal-20")));
        var resolver = new AmazonShortLinkResolver(client);

        var result = await resolver.ResolveAsync("https://amzn.to/example", CancellationToken.None);

        Assert.Equal("www.amazon.ca", result.IdnHost);
        Assert.Equal("B0DMNJNFW8", OwnerProvidedAffiliateLinkInspector.AmazonAsin(result.AbsoluteUri));
    }

    [Fact]
    public async Task Resolver_rejects_a_destination_outside_Amazon_Canada()
    {
        using var client = new HttpClient(new RedirectHandler(_ => Redirect("https://www.amazon.com/dp/B0DMNJNFW8?tag=other-20")));
        var resolver = new AmazonShortLinkResolver(client);

        var exception = await Assert.ThrowsAsync<AmazonShortLinkResolutionException>(() =>
            resolver.ResolveAsync("https://amzn.to/example", CancellationToken.None));

        Assert.Contains("not Amazon Canada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolver_never_requests_the_final_Amazon_Product_page()
    {
        var requests = new List<Uri>();
        using var client = new HttpClient(new RedirectHandler(request =>
        {
            requests.Add(request.RequestUri!);
            return Redirect("https://www.amazon.ca/Useful-Product/dp/B0DMNJNFW8?tag=canadadeal-20");
        }));
        var resolver = new AmazonShortLinkResolver(client);

        await resolver.ResolveAsync("https://amzn.to/example", CancellationToken.None);

        Assert.Single(requests);
        Assert.Equal("amzn.to", requests[0].IdnHost);
    }

    private static HttpResponseMessage Redirect(string location) => new(HttpStatusCode.MovedPermanently)
    {
        Headers = { Location = new Uri(location) }
    };

    private sealed class RedirectHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response(request));
    }
}
