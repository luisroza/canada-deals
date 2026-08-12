[CmdletBinding()]
param(
    [string]$SpecPath = ".do/app.yaml",
    [switch]$RequireDeployableValues
)

$ErrorActionPreference = "Stop"
$resolvedSpec = (Resolve-Path -LiteralPath $SpecPath).Path
$content = Get-Content -LiteralPath $resolvedSpec -Raw

$requiredSections = @("name:", "region: tor", "ingress:", "services:", "workers:", "jobs:", "databases:")
foreach ($section in $requiredSections) {
    if (-not $content.Contains($section)) { throw "App Spec is missing required content: $section" }
}

$placeholders = [regex]::Matches($content, "(?m)(REPLACE_[A-Z0-9_]+|example\.ca)") | ForEach-Object Value | Sort-Object -Unique
if ($RequireDeployableValues -and $placeholders.Count -gt 0) {
    throw "App Spec is not deployable: $($placeholders.Count) placeholder value(s) remain."
}

$doctl = Get-Command doctl -ErrorAction SilentlyContinue
if (-not $doctl) {
    $localDoctl = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) ".tools/doctl.exe"
    if (Test-Path -LiteralPath $localDoctl) { $doctl = Get-Item -LiteralPath $localDoctl }
}
if (-not $doctl) { throw "doctl is required for provider schema validation." }

$doctlPath = if ($doctl.Source) { $doctl.Source } else { $doctl.FullName }
& $doctlPath apps spec validate $resolvedSpec --schema-only
if ($LASTEXITCODE -ne 0) { throw "DigitalOcean App Spec schema validation failed." }

Write-Host "App Spec schema: VALID"
Write-Host "Deployment placeholders remaining: $($placeholders.Count)"
if ($placeholders.Count -gt 0) { $placeholders | ForEach-Object { Write-Host " - $_" } }
