using System.Net;
using CanadaDeals.Infrastructure.Affiliates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Catalogs;

public static class CatalogServiceCollectionExtensions
{
    public static IServiceCollection AddCanadaDealsCatalogSources(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CatalogIngestionOptions>().Bind(configuration.GetSection(CatalogIngestionOptions.SectionName))
            .Validate(options => options.MaximumPagesPerRun is >= 1 and <= 20 && options.PageSize is >= 1 and <= 1000 &&
                options.MaximumRecordsPerRun is >= 1 and <= 5000 && options.MaximumMetadataEntries is >= 1 and <= 32 &&
                options.MaximumMetadataValueLength is >= 40 and <= 1000, "Catalog ingestion bounds are invalid.").ValidateOnStart();
        services.AddOptions<EbayCatalogOptions>().Bind(configuration.GetSection(EbayCatalogOptions.SectionName))
            .Validate(options => !options.Enabled || ValidOfficialOrigin(options.ApiBaseUrl, "api.ebay.com") && options.Marketplace == "EBAY_CA" &&
                !string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret) &&
                options.TimeoutSeconds is >= 5 and <= 120 &&
                PrivacySafeReference(options.AffiliateCampaignId) &&
                PrivacySafeReference(options.AffiliateReferenceId),
                "Enabled eBay catalog configuration is invalid.").ValidateOnStart();
        services.AddOptions<ImpactCatalogOptions>().Bind(configuration.GetSection(ImpactCatalogOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 5 and <= 120, "Impact catalog timeout is invalid.").ValidateOnStart();
        services.AddOptions<AwinCatalogOptions>().Bind(configuration.GetSection(AwinCatalogOptions.SectionName))
            .Validate(options => !options.Enabled || ValidOfficialOrigin(options.FeedListBaseUrl, "productdata.awin.com") && !string.IsNullOrWhiteSpace(options.DataFeedApiKey) &&
                options.TimeoutSeconds is >= 5 and <= 180 && options.MaximumFeedBytes is >= 1_048_576 and <= 268_435_456,
                "Enabled Awin catalog configuration is invalid.").ValidateOnStart();
        services.AddOptions<CjCatalogOptions>().Bind(configuration.GetSection(CjCatalogOptions.SectionName))
            .Validate(options => !options.Enabled || ValidOfficialOrigin(options.ProductSearchBaseUrl, "product-search.api.cj.com") && !string.IsNullOrWhiteSpace(options.WebsiteId) &&
                options.TimeoutSeconds is >= 5 and <= 120, "Enabled CJ catalog configuration is invalid.").ValidateOnStart();

        services.AddHttpClient("EbayCatalog", (provider, client) => Configure(client,
            provider.GetRequiredService<IOptions<EbayCatalogOptions>>().Value.ApiBaseUrl,
            provider.GetRequiredService<IOptions<EbayCatalogOptions>>().Value.TimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => NoRedirectHandler())
            .RemoveAllLoggers();
        services.AddSingleton<IEbayTokenProvider>(provider => new EbayTokenProvider(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("EbayCatalog"),
            provider.GetRequiredService<IOptions<EbayCatalogOptions>>(), provider.GetRequiredService<TimeProvider>()));
        services.AddScoped<EbayCatalogSource>(provider => new EbayCatalogSource(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("EbayCatalog"),
            provider.GetRequiredService<IEbayTokenProvider>(), provider.GetRequiredService<IOptions<EbayCatalogOptions>>(),
            provider.GetRequiredService<TimeProvider>()));

        services.AddHttpClient("ImpactCatalog", (provider, client) => Configure(client,
            provider.GetRequiredService<IOptions<AffiliateOptions>>().Value.Impact.BaseUrl,
            provider.GetRequiredService<IOptions<ImpactCatalogOptions>>().Value.TimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => NoRedirectHandler())
            .RemoveAllLoggers();
        services.AddScoped<ImpactCatalogSource>(provider => new ImpactCatalogSource(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("ImpactCatalog"),
            provider.GetRequiredService<IOptions<AffiliateOptions>>(), provider.GetRequiredService<IOptions<ImpactCatalogOptions>>(),
            provider.GetRequiredService<TimeProvider>()));

        services.AddHttpClient("AwinCatalog", (provider, client) => Configure(client,
                provider.GetRequiredService<IOptions<AwinCatalogOptions>>().Value.FeedListBaseUrl,
                provider.GetRequiredService<IOptions<AwinCatalogOptions>>().Value.TimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .RemoveAllLoggers();
        services.AddScoped<AwinCatalogSource>(provider => new AwinCatalogSource(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("AwinCatalog"),
            provider.GetRequiredService<IOptions<AwinCatalogOptions>>(), provider.GetRequiredService<TimeProvider>()));

        services.AddHttpClient("CjCatalog", (provider, client) => Configure(client,
            provider.GetRequiredService<IOptions<CjCatalogOptions>>().Value.ProductSearchBaseUrl,
            provider.GetRequiredService<IOptions<CjCatalogOptions>>().Value.TimeoutSeconds))
            .ConfigurePrimaryHttpMessageHandler(() => NoRedirectHandler())
            .RemoveAllLoggers();
        services.AddScoped<CjCatalogSource>(provider => new CjCatalogSource(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("CjCatalog"),
            provider.GetRequiredService<IOptions<AffiliateOptions>>(), provider.GetRequiredService<IOptions<CjCatalogOptions>>(),
            provider.GetRequiredService<TimeProvider>()));

        services.AddScoped<RakutenCatalogSource>();
        services.AddScoped<IOfferCatalogSource>(provider => provider.GetRequiredService<RakutenCatalogSource>());
        services.AddScoped<IOfferCatalogSource>(provider => provider.GetRequiredService<EbayCatalogSource>());
        services.AddScoped<IOfferCatalogSource>(provider => provider.GetRequiredService<ImpactCatalogSource>());
        services.AddScoped<IOfferCatalogSource>(provider => provider.GetRequiredService<AwinCatalogSource>());
        services.AddScoped<IOfferCatalogSource>(provider => provider.GetRequiredService<CjCatalogSource>());
        services.AddScoped<CatalogDiscoveryService>();
        services.AddScoped<CatalogImportService>();
        services.AddScoped<CatalogDiscoveryJob>();
        services.AddScoped<CatalogDryRunJob>();
        services.AddScoped<CatalogImportJob>();
        return services;
    }

    private static void Configure(HttpClient client, string origin, int timeoutSeconds)
    {
        client.BaseAddress = new Uri(origin.TrimEnd('/') + '/');
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GreatDeals.ca-Catalog/1.0");
    }

    private static HttpClientHandler NoRedirectHandler() => new() { AllowAutoRedirect = false };

    private static bool ValidOfficialOrigin(string value, string expectedHost) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath == "/" &&
        string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
        string.Equals(uri.IdnHost, expectedHost, StringComparison.OrdinalIgnoreCase);

    private static bool PrivacySafeReference(string? value) => string.IsNullOrWhiteSpace(value) ||
        value.Length <= 128 && !value.Contains('@') && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
}
