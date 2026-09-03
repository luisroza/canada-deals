[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("rakuten", "ebay", "impact", "awin", "cj")]
    [string]$Provider,
    [switch]$PersistSnapshot
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
    [Console]::Error.WriteLine("$Provider live discovery blocked; missing configuration names: " + ($missing -join ", "))
    exit 2
}

switch ($Provider) {
    "rakuten" { $env:Rakuten__Enabled = "true"; $env:Rakuten__LiveDiscoveryEnabled = "true" }
    "ebay" { $env:CatalogProviders__Ebay__Enabled = "true" }
    "impact" { $env:Affiliate__Impact__Enabled = "true"; $env:CatalogProviders__Impact__Enabled = "true" }
    "awin" { $env:CatalogProviders__Awin__Enabled = "true" }
    "cj" { $env:Affiliate__Cj__Enabled = "true"; $env:CatalogProviders__Cj__Enabled = "true" }
}

$arguments = @("run", "--project", "src/backend/CanadaDeals.Worker", "--", "--catalog-discover", $Provider)
if ($PersistSnapshot) { $arguments += "--persist-discovery" }
$mode = if ($PersistSnapshot) { "read-only provider discovery with a local capability snapshot" } else { "strictly read-only provider discovery" }
Write-Host "Starting LIVE $mode for $Provider. Secrets and tokens will not be printed. No catalog offer will be written."
& dotnet @arguments
exit $LASTEXITCODE
