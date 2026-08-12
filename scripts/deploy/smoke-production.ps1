[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^https://')]
    [string]$ProductionOrigin,
    [switch]$AllowEmailMutation
)

$ErrorActionPreference = "Stop"
$origin = $ProductionOrigin.TrimEnd("/")

function Assert-Status([string]$Path, [int[]]$Expected) {
    try {
        $response = Invoke-WebRequest -Uri "$origin$Path" -UseBasicParsing -MaximumRedirection 0
        $status = [int]$response.StatusCode
    } catch {
        if (-not $_.Exception.Response) { throw }
        $response = $_.Exception.Response
        $status = [int]$response.StatusCode
    }
    if ($Expected -notcontains $status) { throw "$Path returned $status; expected $($Expected -join ', ')." }
    Write-Host "$Path -> $status"
    return $response
}

$home = Assert-Status "/" @(200)
Assert-Status "/health" @(200) | Out-Null
Assert-Status "/api/v1/deals" @(200) | Out-Null
Assert-Status "/swagger" @(404) | Out-Null
Assert-Status "/hangfire" @(404) | Out-Null

foreach ($header in @("Content-Security-Policy", "Referrer-Policy", "X-Content-Type-Options", "X-Frame-Options")) {
    if (-not $home.Headers[$header]) { throw "Missing security header on /: $header" }
}

if ($AllowEmailMutation) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $tokenResponse = Invoke-RestMethod -Uri "$origin/api/v1/account/antiforgery" -WebSession $session
    $label = [Guid]::NewGuid().ToString("N")
    $body = @{ email = "delivered+$label@resend.dev"; password = "Operational42Smoke" } | ConvertTo-Json
    $registration = Invoke-WebRequest -Uri "$origin/api/v1/account/register" -Method Post -WebSession $session -Headers @{ "X-CSRF-TOKEN" = $tokenResponse.requestToken } -ContentType "application/json" -Body $body -UseBasicParsing
    if ([int]$registration.StatusCode -ne 201) { throw "Registration smoke did not return 201." }
    Write-Host "Registration email queued for the Resend delivered test address. Verify provider acceptance and webhook delivery separately."
}

Write-Host "Production HTTP smoke tests: PASSED"
