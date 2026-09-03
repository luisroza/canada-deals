[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("rakuten", "ebay", "impact", "awin", "cj")]
    [string]$Provider,
    [Parameter(Mandatory = $true)]
    [ValidateLength(1, 160)]
    [string]$AdvertiserId,
    [ValidateLength(1, 160)]
    [string]$CatalogId,
    [ValidateLength(1, 200)]
    [string]$Query
)

$ErrorActionPreference = "Stop"
$requirements = @{
    rakuten = @("Rakuten__AccountId", "Rakuten__ClientId", "Rakuten__ClientSecret")
    ebay = @("CatalogProviders__Ebay__ClientId", "CatalogProviders__Ebay__ClientSecret")
    impact = @("Affiliate__Impact__AccountSid", "Affiliate__Impact__AuthToken")
    awin = @("CatalogProviders__Awin__DataFeedApiKey")
    cj = @("Affiliate__Cj__PersonalAccessToken", "CatalogProviders__Cj__WebsiteId")
}
$missing = @($requirements[$Provider] | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
if ($missing.Count -gt 0) {
    [Console]::Error.WriteLine("$Provider live dry-run blocked; missing configuration names: " + ($missing -join ", "))
    exit 2
}

switch ($Provider) {
    "rakuten" { $env:Rakuten__Enabled = "true"; $env:Rakuten__LiveDiscoveryEnabled = "true" }
    "ebay" { $env:CatalogProviders__Ebay__Enabled = "true" }
    "impact" { $env:Affiliate__Impact__Enabled = "true"; $env:CatalogProviders__Impact__Enabled = "true" }
    "awin" { $env:CatalogProviders__Awin__Enabled = "true" }
    "cj" { $env:Affiliate__Cj__Enabled = "true"; $env:CatalogProviders__Cj__Enabled = "true" }
}
$env:CatalogIngestion__PersistenceEnabled = "false"
$arguments = @("run", "--project", "src/backend/CanadaDeals.Worker", "--", "--catalog-dry-run", $Provider, "--advertiser", $AdvertiserId)
if (-not [string]::IsNullOrWhiteSpace($CatalogId)) { $arguments += @("--catalog", $CatalogId) }
if (-not [string]::IsNullOrWhiteSpace($Query)) { $arguments += @("--query", $Query) }

Write-Host "Starting LIVE bounded catalog dry-run for $Provider. No Product, RetailerListing, or PriceObservation will be written."
& dotnet @arguments
exit $LASTEXITCODE
