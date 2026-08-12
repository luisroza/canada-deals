[CmdletBinding(SupportsShouldProcess, ConfirmImpact = "High")]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^https://')]
    [string]$ProductionOrigin
)

$ErrorActionPreference = "Stop"
if (-not $PSCmdlet.ShouldProcess($ProductionOrigin, "Create four test accounts and send confirmation emails through Resend")) { return }

$origin = $ProductionOrigin.TrimEnd("/")
$label = [Guid]::NewGuid().ToString("N")
$addresses = @(
    "delivered+$label@resend.dev",
    "bounced+$label@resend.dev",
    "complained+$label@resend.dev",
    "suppressed@resend.dev"
)

foreach ($address in $addresses) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $token = Invoke-RestMethod -Uri "$origin/api/v1/account/antiforgery" -WebSession $session
    $body = @{ email = $address; password = "Operational42Events" } | ConvertTo-Json
    try {
        $response = Invoke-WebRequest -Uri "$origin/api/v1/account/register" -Method Post -WebSession $session -Headers @{ "X-CSRF-TOKEN" = $token.requestToken } -ContentType "application/json" -Body $body -UseBasicParsing
        Write-Host "$address -> registration $([int]$response.StatusCode)"
    } catch {
        Write-Warning "$address -> registration request failed; inspect API/worker logs without exposing message content."
    }
}

Write-Host "Requests submitted. Confirm delivered/bounced/complained/suppressed webhook state and idempotency in PostgreSQL before declaring operational validation complete."
