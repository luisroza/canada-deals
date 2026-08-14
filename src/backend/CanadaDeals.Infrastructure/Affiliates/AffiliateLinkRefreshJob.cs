using Hangfire;

namespace CanadaDeals.Infrastructure.Affiliates;

public sealed class AffiliateLinkRefreshJob(AffiliateLinkRefreshService refreshService)
{
    [JobDisplayName("Refresh approved affiliate tracking links")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(600)]
    public Task<AffiliateRefreshSummary> RunAsync(CancellationToken cancellationToken = default) =>
        refreshService.RefreshDueAsync(null, cancellationToken);
}
