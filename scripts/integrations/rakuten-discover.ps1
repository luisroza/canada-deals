[CmdletBinding()]
param(
    [switch]$PersistCapabilities
)

$ErrorActionPreference = "Stop"
$required = @("Rakuten__AccountId", "Rakuten__ClientId", "Rakuten__ClientSecret")
$missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
if ($missing.Count -gt 0) {
    [Console]::Error.WriteLine("Rakuten live discovery blocked; missing secret configuration names: " + ($missing -join ", "))
    exit 2
}

$env:Rakuten__Enabled = "true"
$env:Rakuten__LiveDiscoveryEnabled = "true"
$arguments = @("run", "--project", "src/backend/CanadaDeals.Worker", "--", "--rakuten-discover")
if ($PersistCapabilities) { $arguments += "--persist-capabilities" }

Write-Host "Starting read-only Rakuten Partnerships + Advertisers discovery. Credential and token values will not be printed."
& dotnet @arguments
exit $LASTEXITCODE
