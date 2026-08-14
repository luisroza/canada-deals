# Backend testing

Validated toolchain (2026-08-14): .NET SDK 10.0.300, Release configuration, PostgreSQL 17 in Docker Compose, and no test skips when the database is available.

Run the solution tests:

```powershell
dotnet test CanadaDeals.slnx --configuration Release
```

The 72-test domain suite covers all prior behavior plus affiliate lifecycle, missing-provider snapshot handling, and Rakuten capability/policy fail-closed behavior. API integration tests use `Microsoft.AspNetCore.Mvc.Testing` and real PostgreSQL migrations/persistence. They are discovery-skipped only when PostgreSQL is unavailable; they never fall back to an in-memory provider.

Before running API integration tests, start `docker compose up -d postgres`. Set `TEST_DATABASE_CONNECTION` to a dedicated clean database when validating the complete suite. The 138-test integration/provider suite preserves prior slices and adds network-free Rakuten OAuth/Advertisers/Partnerships/Product Search/Deep Links contracts plus real PostgreSQL dry-run, policy, matching, UPC revalidation, transition-safe idempotency, price-history permission, rollback/retry, cancellation-audit, relationship-revocation handoff, failure-audit, and conflict-quarantine tests. It uses controlled credentials/tokens only. Frontend validation is 50 component tests plus a Release build, and 27 Playwright flows exercise real Next.js/API/PostgreSQL/Worker behavior including the controlled Rakuten persisted handoff.
