# Backend local development

Validated environment (2026-08-11): Windows 11, .NET SDK 10.0.300, PostgreSQL 17 via Docker Compose, Node.js 24.14.0, and the repository-declared `pnpm@10.15.0`.

Prerequisites: .NET 10 SDK, Docker Desktop with the PostgreSQL engine running, and the repository checkout.

```powershell
dotnet restore CanadaDeals.slnx
docker compose up -d postgres
& .tools\dotnet-ef.exe database update --project src/backend/CanadaDeals.Infrastructure --startup-project src/backend/CanadaDeals.Api
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/backend/CanadaDeals.Api --urls http://localhost:5099
```

Development settings apply the migration and seed synthetic data. The API health endpoint is `http://localhost:5099/health`. The explicit migration command is idempotent and is useful for validating a clean database before starting the API.

Browser account calls should use the Next.js same-site `/api/*` path, not a separate origin. Obtain `GET /api/v1/account/antiforgery` immediately before each state-changing request and send its `requestToken` in `X-CSRF-TOKEN`; the browser retains both the anti-forgery and Identity cookies as HttpOnly cookies.

Development defaults to `Email:AutoConfirmDevelopmentAccounts=true`. To exercise the real confirmation UI without network email, set `Email__AutoConfirmDevelopmentAccounts=false`; registration then writes the exact message to `ControlledEmailCaptures`, and the Development-only `GET /api/internal/email-captures/latest?to=...` endpoint exposes it for deterministic tests. See `docs/backend/EMAIL.md` for production configuration and the external provider gate.

The worker can be started separately:

```powershell
dotnet run --project src/backend/CanadaDeals.Worker --urls http://localhost:5100
```

Worker health is `http://localhost:5100/health`. Set `Worker__EnqueueSampleJob=true` only to exercise the existing fixture-safe sample, or `Worker__EnqueueAlertEvaluationJob=true` for one explicit alert-evaluation enqueue. Alert creation also enqueues evaluation. The job reads persisted fixture observations and performs no merchant fetch. Hangfire creates its PostgreSQL storage schema on worker startup.

Development/Test notifications are persisted as `DevelopmentCaptured` with reason `CONTROLLED_DEVELOPMENT_TEST_CAPTURE`; no email leaves the machine. Production fails closed unless the complete email-provider configuration is supplied through the deployment secret/configuration boundary. No email or retailer credentials belong in committed local configuration.

Production-shaped images can be built locally with the Dockerfiles under `apps/web/`, `src/backend/CanadaDeals.Api/`, and `src/backend/CanadaDeals.Worker/`. See `docs/operations/DEPLOYMENT.md`; local Development key-ring rows are intentionally unencrypted, while Production requires the persisted PFX-protected configuration.
