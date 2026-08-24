using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    }
}
