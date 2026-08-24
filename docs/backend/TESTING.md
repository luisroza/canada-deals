# Backend testing

Validated toolchain (2026-08-14): .NET SDK 10.0.300, Release configuration, PostgreSQL 17 in Docker Compose, and no test skips when the database is available.

Run the solution tests:

```powershell
dotnet test CanadaDeals.slnx --configuration Release
```

The 76-test domain suite covers all prior behavior plus affiliate lifecycle, Rakuten capability/policy fail-closed behavior, original and merchant-approved StoreBannerProfile rights validation, and StoreAffiliateDestination lifecycle. API integration tests use `Microsoft.AspNetCore.Mvc.Testing` and real PostgreSQL migrations/persistence. They are discovery-skipped only when PostgreSQL is unavailable; they never fall back to an in-memory provider.

Before running API integration tests, start `docker compose up -d postgres` and set `TEST_DATABASE_CONNECTION` to a dedicated test database. The fixture fails closed when the variable is absent or points to the application database named `canadadeals`; integration tests must never create users, roles, offers, or audit records in the local application database.

Example using a dedicated database that has already been created:

```powershell
$env:TEST_DATABASE_CONNECTION = "Host=localhost;Port=5432;Database=canadadeals_integration;Username=canadadeals;Password=canadadeals"
dotnet test tests/CanadaDeals.Api.IntegrationTests/CanadaDeals.Api.IntegrationTests.csproj
```

The 143-test integration/provider suite preserves prior slices and adds network-free store-banner contracts for safe API projection, protected redirect/click persistence, fail-closed relationship and URL states, uniqueness, and restricted deletion. It uses controlled credentials/tokens only. Frontend validation is 61 component tests plus a Release build, and 25 Playwright flows exercise real Next.js/API/PostgreSQL/Worker behavior including controlled product and store handoffs.
