[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+$')]
    [string]$AdvertiserMid
)

$ErrorActionPreference = "Stop"
$required = @("Rakuten__AccountId", "Rakuten__ClientId", "Rakuten__ClientSecret")
$missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
if ($missing.Count -gt 0) {
    [Console]::Error.WriteLine("Rakuten Product Search dry-run blocked; missing secret configuration names: " + ($missing -join ", "))
    exit 2
}

$env:Rakuten__Enabled = "true"
$env:Rakuten__LiveDiscoveryEnabled = "true"
$env:Rakuten__CatalogImportEnabled = "false"
Write-Host "Starting bounded Rakuten Product Search dry-run for MID $AdvertiserMid. No Product, listing, or observation will be written."
& dotnet run --project src/backend/CanadaDeals.Worker -- --rakuten-dry-run $AdvertiserMid
exit $LASTEXITCODE
