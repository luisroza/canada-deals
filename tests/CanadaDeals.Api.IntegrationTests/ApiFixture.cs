using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CanadaDeals.Api.IntegrationTests;

public sealed class ApiFixture : WebApplicationFactory<Program>
{
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("TEST_DATABASE_CONNECTION") ?? "Host=localhost;Port=5432;Database=canadadeals;Username=canadadeals;Password=canadadeals";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
        builder.UseSetting("Database:ApplyMigrations", "true");
        builder.UseSetting("Database:SeedDemoData", "true");
        builder.UseSetting("AffiliateHandoff:Enabled", "true");
        builder.UseSetting("AuthenticationRateLimit:PermitLimit", "1000");
    }
}
