using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CanadaDeals.Infrastructure.Affiliates;

public static class AffiliateServiceCollectionExtensions
{
    public static IServiceCollection AddCanadaDealsAffiliateLinks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AffiliateOptions>()
            .Bind(configuration.GetSection(AffiliateOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AffiliateOptions>, AffiliateOptionsValidator>();
        services.AddHttpClient<ImpactAffiliateLinkProvider>();
        services.AddHttpClient<CjAffiliateLinkProvider>();
        services.AddScoped<IAffiliateLinkProvider>(provider => provider.GetRequiredService<ImpactAffiliateLinkProvider>());
        services.AddScoped<IAffiliateLinkProvider>(provider => provider.GetRequiredService<CjAffiliateLinkProvider>());
        services.AddScoped<AffiliateLinkRefreshService>();
        services.AddScoped<AffiliateLinkRefreshJob>();
        return services;
    }
}
