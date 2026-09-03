using Hangfire;

namespace CanadaDeals.Infrastructure.Catalogs;

public sealed class CatalogDiscoveryJob(CatalogDiscoveryService discovery)
{
    [AutomaticRetry(Attempts = 1, DelaysInSeconds = [300], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [JobDisplayName("Catalog discovery: {0}")]
    public Task<IReadOnlyList<CatalogCandidate>> RunAsync(string provider, CancellationToken cancellationToken) =>
        discovery.DiscoverAsync(provider, true, cancellationToken: cancellationToken);
}

public sealed class CatalogDryRunJob(CatalogImportService imports)
{
    [AutomaticRetry(Attempts = 1, DelaysInSeconds = [300], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [JobDisplayName("Catalog dry-run: {0}/{1}")]
    public Task<CatalogImportSummary> RunAsync(string provider, string advertiserId, string? catalogId, string? query, CancellationToken cancellationToken) =>
        imports.RunAsync(provider, advertiserId, catalogId, true, query, cancellationToken: cancellationToken);
}

public sealed class CatalogImportJob(CatalogImportService imports)
{
    [AutomaticRetry(Attempts = 1, DelaysInSeconds = [300], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [JobDisplayName("Bounded catalog import: {0}/{1}")]
    public async Task<CatalogImportSummary> RunAsync(string provider, string advertiserId, string? catalogId, string? query, CancellationToken cancellationToken)
    {
        var result = await imports.RunAsync(provider, advertiserId, catalogId, false, query, cancellationToken: cancellationToken);
        if (result.Status == CanadaDeals.Domain.Common.IntegrationRunStatus.Failed)
            throw new InvalidOperationException(result.FailureReason ?? "CATALOG_IMPORT_FAILED");
        return result;
    }
}
