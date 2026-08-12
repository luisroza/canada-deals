using Hangfire;
using CanadaDeals.Infrastructure.Alerts;

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

        return Task.CompletedTask;
    }
}

public sealed class FixtureJob(ILogger<FixtureJob> logger)
{
    [JobDisplayName("Fixture-safe sample job")]
    public void Run() => logger.LogInformation("Fixture-safe sample Hangfire job completed; no merchant data was fetched.");
}
