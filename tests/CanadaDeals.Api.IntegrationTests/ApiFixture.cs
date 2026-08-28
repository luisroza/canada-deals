using CanadaDeals.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class ApiFixture : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = GetDedicatedTestConnectionString();

    private static string GetDedicatedTestConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("TEST_DATABASE_CONNECTION must target a dedicated test database. Integration tests are not allowed to use the application database.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.Equals(builder.Database, "canadadeals", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("TEST_DATABASE_CONNECTION points to the application database 'canadadeals'. Use a dedicated test database.");

        return connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("Database:SeedDemoData", "true");
        builder.UseSetting("AffiliateHandoff:Enabled", "true");
        builder.UseSetting("ProductFeatures:PriceHistoryEnabled", "true");
        builder.UseSetting("ProductFeatures:PriceAlertsEnabled", "true");
        builder.UseSetting("AuthenticationRateLimit:PermitLimit", "1000");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAmazonShortLinkResolver>();
            services.AddSingleton<IAmazonShortLinkResolver, ControlledAmazonShortLinkResolver>();
        });
    }

    private sealed class ControlledAmazonShortLinkResolver : IAmazonShortLinkResolver
    {
        public Task<Uri> ResolveAsync(string shortLink, CancellationToken cancellationToken) => shortLink switch
        {
            "https://amzn.to/example" => Task.FromResult(new Uri("https://www.amazon.ca/Levoit-Smart-Humidifiers-Bedroom-Large/dp/B0DMNJNFW8?tag=canadadeal-20&linkCode=as4")),
            "https://amzn.to/non-canada" => throw new AmazonShortLinkResolutionException("This short link resolves to www.amazon.com, not Amazon Canada. Create the link from the Amazon.ca Associates account."),
            _ => throw new AmazonShortLinkResolutionException("Controlled test short link was not configured.")
        };
    }
}
