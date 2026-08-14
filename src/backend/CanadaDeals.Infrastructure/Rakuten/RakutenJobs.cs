using Hangfire;

namespace CanadaDeals.Infrastructure.Rakuten;

public sealed class RakutenCatalogImportJob(RakutenCatalogImportService importService)
{
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = [60, 300], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [JobDisplayName("Rakuten bounded catalog import: {0}, dry-run={1}")]
    public async Task<RakutenImportSummary> RunAsync(string advertiserMid, bool dryRun, CancellationToken cancellationToken)
    {
        var result = await importService.RunAsync(advertiserMid, dryRun, cancellationToken);
        if (result.Status == CanadaDeals.Domain.Common.IntegrationRunStatus.Failed)
            throw new InvalidOperationException(result.FailureReason ?? "RAKUTEN_IMPORT_FAILED");
        return result;
    }
}
