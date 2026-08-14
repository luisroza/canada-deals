using CanadaDeals.Infrastructure.Affiliates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Rakuten;

public static class RakutenServiceCollectionExtensions
{
    public static IServiceCollection AddCanadaDealsRakuten(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RakutenOptions>()
            .Bind(configuration.GetSection(RakutenOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RakutenOptions>, RakutenOptionsValidator>();
        services.AddHttpClient("Rakuten", (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<RakutenOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + '/');
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CanadaDeals/1.0 (+https://canadadeals.ca)");
        });
        services.AddSingleton<IRakutenAccessTokenProvider>(provider => new RakutenAccessTokenProvider(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("Rakuten"),
            provider.GetRequiredService<IOptions<RakutenOptions>>(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<RakutenRequestGate>();
        services.AddScoped(provider => new RakutenAuthenticatedClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("Rakuten"),
            provider.GetRequiredService<IRakutenAccessTokenProvider>(),
            provider.GetRequiredService<RakutenRequestGate>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RakutenAuthenticatedClient>>()));
        services.AddScoped<IRakutenAdvertiserClient, RakutenAdvertiserClient>();
        services.AddScoped<IRakutenPartnershipClient, RakutenPartnershipClient>();
        services.AddScoped<IRakutenProductSearchClient, RakutenProductSearchClient>();
        services.AddScoped<IRakutenDeepLinkClient, RakutenDeepLinkClient>();
        services.AddScoped<IRakutenCapabilityGate, RakutenCapabilityGate>();
        services.AddScoped<RakutenDiscoveryService>();
        services.AddScoped<RakutenCatalogImportService>();
        services.AddScoped<RakutenCatalogImportJob>();
        services.AddScoped<RakutenAffiliateLinkProvider>();
        services.AddScoped<IAffiliateLinkProvider>(provider => provider.GetRequiredService<RakutenAffiliateLinkProvider>());
        return services;
    }
}
