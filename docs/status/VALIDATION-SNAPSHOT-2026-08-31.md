# Validation snapshot — 2026-08-31

This file separates evidence executed during the status refresh from historical project evidence.

## Executed during this refresh

### Backend build

```powershell
dotnet build CanadaDeals.slnx --configuration Release --no-restore
```

Result: **PASS**, 0 warnings and 0 errors.

### Domain tests

```powershell
dotnet test tests\CanadaDeals.Domain.Tests\CanadaDeals.Domain.Tests.csproj --configuration Release --no-build
```

Result: **PASS**, 94 passed, 0 failed, 0 skipped.

### PostgreSQL integration/provider tests

A dedicated database named `canadadeals_status_20260831` was created in the healthy local PostgreSQL 17 Compose service. `TEST_DATABASE_CONNECTION` pointed only to that database. The normal local application database was not used. The dedicated database was verified by exact name and removed after completion.

```powershell
dotnet test tests\CanadaDeals.Api.IntegrationTests\CanadaDeals.Api.IntegrationTests.csproj --configuration Release --no-build
```

Result: **PASS**, 161 passed, 0 failed, 0 skipped, approximately 28 seconds.

### Frontend default suite

```powershell
pnpm --dir apps/web test
```

Result: **FAIL**, 90 passed and 1 failed.

Failure:

- File: `components/DiscoveryExperience.test.tsx`
- Scenario: `applies and clears filters without navigation while preserving scroll position`
- Assertion: Category should be empty after Clear.
- Observed value: `electronics`.

The complete default suite failed twice during the refresh.

### Frontend diagnostic reruns

Isolated test:

```powershell
pnpm --dir apps/web exec vitest run components/DiscoveryExperience.test.tsx
```

Result: **PASS**, 1/1.

Complete suite with one worker:

```powershell
pnpm --dir apps/web exec vitest run --maxWorkers=1
```

Result: **PASS**, 91/91.

Interpretation: the failure is order/concurrency-sensitive and must remain an open QA gate until the normal command is stable.

### Frontend production build

```powershell
$env:API_BASE_URL = "http://api:8080"
$env:API_ORIGIN = "http://api:8080"
pnpm --dir apps/web build
```

Result: **PASS**. TypeScript and all app routes compiled successfully, including `/`, `/admin_panel`, `/offers/[listingId]`, `/products/[slug]`, `/saved`, `/healthz`, `/icon.svg`, `robots.txt`, and `sitemap.xml`.

## Not rerun during this refresh

- Full-stack Playwright: latest recorded evidence is 26 passed, 0 skipped.
- Lighthouse: latest recorded clean-browser scores are retained as historical evidence.
- DigitalOcean App Spec schema validation.
- Docker image build/runtime validation.
- Vulnerability audits.
- Live email or merchant-provider operational tests; external prerequisites remain absent.

## Release interpretation

Backend and production compilation evidence are current and green. Frontend functional evidence is not fully green under the repository's normal test command. The status is therefore **implemented with a QA stability follow-up**, not a completely validated release candidate.
