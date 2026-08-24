using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Infrastructure.Rakuten;
using CanadaDeals.Worker;
using Hangfire;
using Hangfire.PostgreSql;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseServices.GetValidatedConnectionString(builder.Configuration, builder.Environment);

builder.Services.AddCanadaDealsPersistence(builder.Configuration, builder.Environment);
builder.Services.AddCanadaDealsAffiliateLinks(builder.Configuration);
builder.Services.AddCanadaDealsRakuten(builder.Configuration);
builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer(options => options.WorkerCount = 1);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCanadaDealsTransactionalEmail(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<FixtureJob>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (args.Contains("--rakuten-discover", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        var result = await scope.ServiceProvider.GetRequiredService<RakutenDiscoveryService>()
            .DiscoverAsync(args.Contains("--persist-capabilities", StringComparer.Ordinal));
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            result.AdvertisersReturned,
            result.PartnershipsReturned,
            result.ActivePartnerships,
            result.CanadaRelevantCandidates,
            result.ProductFeedCandidates,
            result.DeepLinkCandidates,
            Advertisers = result.Capabilities.Select(capability => new
            {
                capability.Advertiser.Mid,
                capability.Advertiser.Name,
                AdvertiserStatus = capability.Partnership.AdvertiserStatus.ToString(),
                PartnershipStatus = capability.Partnership.PartnershipStatus.ToString(),
                capability.CanadaRelevant,
                capability.Advertiser.ProductFeedAvailable,
                capability.Advertiser.DeepLinksAvailable
            })
        }, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }
    catch (RakutenProviderException exception)
    {
        Console.Error.WriteLine(exception.SafeCode);
        Environment.ExitCode = 1;
        return;
    }
}

var dryRunIndex = Array.IndexOf(args, "--rakuten-dry-run");
if (dryRunIndex >= 0)
{
    if (dryRunIndex + 1 >= args.Length)
    {
        Console.Error.WriteLine("RAKUTEN_ADVERTISER_MID_REQUIRED");
        Environment.ExitCode = 2;
        return;
    }
    await using var scope = app.Services.CreateAsyncScope();
    var result = await scope.ServiceProvider.GetRequiredService<RakutenCatalogImportService>()
        .RunAsync(args[dryRunIndex + 1], true);
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    Environment.ExitCode = result.Status == CanadaDeals.Domain.Common.IntegrationRunStatus.Succeeded ? 0 : 1;
    return;
}

app.MapHealthChecks("/health");
app.Run();
