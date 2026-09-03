using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Email;
using CanadaDeals.Infrastructure.Persistence;
using CanadaDeals.Infrastructure.Rakuten;
using CanadaDeals.Infrastructure.Catalogs;
using CanadaDeals.Worker;
using Hangfire;
using Hangfire.PostgreSql;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var connectionString = DatabaseServices.GetValidatedConnectionString(builder.Configuration, builder.Environment);

builder.Services.AddCanadaDealsPersistence(builder.Configuration, builder.Environment);
builder.Services.AddCanadaDealsAffiliateLinks(builder.Configuration);
builder.Services.AddCanadaDealsRakuten(builder.Configuration);
builder.Services.AddCanadaDealsCatalogSources(builder.Configuration);
builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer(options => options.WorkerCount = 1);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddCanadaDealsTransactionalEmail(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<FixtureJob>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHealthChecks();

var app = builder.Build();

var catalogDiscoverIndex = Array.IndexOf(args, "--catalog-discover");
if (catalogDiscoverIndex >= 0)
{
    if (catalogDiscoverIndex + 1 >= args.Length)
    {
        Console.Error.WriteLine("CATALOG_PROVIDER_REQUIRED");
        Environment.ExitCode = 2;
        return;
    }
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        var provider = args[catalogDiscoverIndex + 1].Trim().ToLowerInvariant();
        var persist = args.Contains("--persist-discovery", StringComparer.Ordinal);
        var result = await scope.ServiceProvider.GetRequiredService<CatalogDiscoveryService>().DiscoverAsync(provider, persist);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Provider = provider,
            Mode = persist ? "LIVE_READ_ONLY_WITH_CAPABILITY_SNAPSHOT" : "LIVE_READ_ONLY",
            Candidates = result.Select(candidate => new
            {
                candidate.ProviderAdvertiserId,
                candidate.CatalogId,
                candidate.DisplayName,
                Relationship = candidate.RelationshipStatus.ToString(),
                candidate.CatalogAvailable,
                candidate.AffiliateAvailable,
                candidate.CanadaRelevant,
                candidate.Currency,
                candidate.SourceUpdatedAt
            })
        }, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }
    catch (CatalogProviderException exception)
    {
        Console.Error.WriteLine(exception.SafeCode);
        Environment.ExitCode = 1;
        return;
    }
}

var catalogDryRunIndex = Array.IndexOf(args, "--catalog-dry-run");
if (catalogDryRunIndex >= 0)
{
    static string? Argument(string[] values, string name)
    {
        var index = Array.IndexOf(values, name);
        return index >= 0 && index + 1 < values.Length ? values[index + 1] : null;
    }
    if (catalogDryRunIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(Argument(args, "--advertiser")))
    {
        Console.Error.WriteLine("CATALOG_PROVIDER_AND_ADVERTISER_REQUIRED");
        Environment.ExitCode = 2;
        return;
    }
    await using var scope = app.Services.CreateAsyncScope();
    var result = await scope.ServiceProvider.GetRequiredService<CatalogImportService>().RunAsync(
        args[catalogDryRunIndex + 1], Argument(args, "--advertiser")!, Argument(args, "--catalog"), true,
        Argument(args, "--query"));
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    Environment.ExitCode = result.Status == CanadaDeals.Domain.Common.IntegrationRunStatus.Succeeded ? 0 : 1;
    return;
}

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
