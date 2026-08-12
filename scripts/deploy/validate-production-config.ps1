[CmdletBinding()]
param(
    [string]$SpecPath = ".do/app.yaml"
)

$ErrorActionPreference = "Stop"

function Test-Present([string]$Name) {
    $present = -not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name))
    Write-Host ("{0}: {1}" -f $Name, $(if ($present) { "PRESENT" } else { "MISSING" }))
    return $present
}

$required = @(
    "DIGITALOCEAN_ACCESS_TOKEN",
    "RESEND_API_KEY",
    "RESEND_WEBHOOK_SIGNING_SECRET",
    "CANADA_DEALS_PRODUCTION_DOMAIN",
    "CANADA_DEALS_DATA_PROTECTION_PFX_BASE64",
    "CANADA_DEALS_DATA_PROTECTION_PFX_PASSWORD"
)
$allPresent = $true
foreach ($name in $required) { if (-not (Test-Present $name)) { $allPresent = $false } }

$domain = [Environment]::GetEnvironmentVariable("CANADA_DEALS_PRODUCTION_DOMAIN")
if ($domain -and ($domain.Contains("://") -or $domain.Contains("/") -or $domain.EndsWith("example.ca"))) {
    throw "CANADA_DEALS_PRODUCTION_DOMAIN must be a real hostname, not a URL or placeholder."
}

if (-not (Test-Path -LiteralPath $SpecPath)) { throw "App Spec not found: $SpecPath" }
$sourcePublished = $false
try {
    git ls-remote --exit-code origin refs/heads/main *> $null
    $sourcePublished = $LASTEXITCODE -eq 0
} catch { $sourcePublished = $false }
Write-Host ("origin/main published: {0}" -f $(if ($sourcePublished) { "YES" } else { "NO" }))

if (-not $allPresent) { throw "Production configuration is incomplete. No secret values were printed." }
if (-not $sourcePublished) { throw "Validated source is not published at origin/main." }
Write-Host "Production configuration preflight: READY"
