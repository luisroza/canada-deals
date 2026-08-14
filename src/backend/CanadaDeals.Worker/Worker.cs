using Hangfire;
using CanadaDeals.Infrastructure.Alerts;
using CanadaDeals.Infrastructure.Affiliates;
using CanadaDeals.Infrastructure.Rakuten;

namespace CanadaDeals.Worker;

public sealed class Worker(ILogger<Worker> logger, IBackgroundJobClient jobs, IConfiguration configuration) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Canada Deals worker started with Hangfire PostgreSQL storage.");

        if (configuration.GetValue<bool>("Worker:EnqueueSampleJob"))
        {
            jobs.Enqueue<FixtureJob>(job => job.Run());
        }

        if (configuration.GetValue<bool>("Worker:EnqueueAlertEvaluationJob"))
        {
            jobs.Enqueue<PriceAlertEvaluationJob>(job => job.RunAsync());
        }

        if (configuration.GetValue<bool>("Worker:EnqueueAffiliateLinkRefreshJob"))
        {
            jobs.Enqueue<AffiliateLinkRefreshJob>(job => job.RunAsync(CancellationToken.None));
        }

        if (configuration.GetValue<bool>("Worker:EnqueueRakutenCatalogImportJob"))
        {
            var advertiserMid = configuration["Worker:RakutenAdvertiserMid"];
            if (string.IsNullOrWhiteSpace(advertiserMid))
                throw new InvalidOperationException("Worker:RakutenAdvertiserMid is required when Rakuten catalog enqueue is enabled.");
            var dryRun = configuration.GetValue("Worker:RakutenCatalogDryRun", true);
            jobs.Enqueue<RakutenCatalogImportJob>(job => job.RunAsync(advertiserMid, dryRun, CancellationToken.None));
        }

        return Task.CompletedTask;
    }
}

public sealed class FixtureJob(ILogger<FixtureJob> logger)
{
    [JobDisplayName("Fixture-safe sample job")]
    public void Run() => logger.LogInformation("Fixture-safe sample Hangfire job completed; no merchant data was fetched.");
}
