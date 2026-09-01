# Backend testing

Validated toolchain (2026-08-31): .NET SDK 10.0.300, Release configuration, PostgreSQL 17 in Docker Compose, and no test skips when the database is available. The current status refresh passed the Release solution build with 0 warnings/errors, 94 domain tests, and 161 integration/provider tests against the dedicated `canadadeals_status_20260831` database; that temporary database was removed after the run.

Run the solution tests:

```powershell
dotnet test CanadaDeals.slnx --configuration Release
```

The 94-test domain suite covers all prior behavior plus individual-offer savings rules, listing-keyed Wishlist intent, affiliate lifecycle, Rakuten capability/policy fail-closed behavior, original and merchant-approved StoreBannerProfile rights validation, and StoreAffiliateDestination lifecycle. API integration tests use `Microsoft.AspNetCore.Mvc.Testing` and real PostgreSQL migrations/persistence. They never fall back to an in-memory provider.

Before running API integration tests, start `docker compose up -d postgres` and set `TEST_DATABASE_CONNECTION` to a dedicated test database. The fixture fails closed when the variable is absent or points to the application database named `canadadeals`; integration tests must never create users, roles, offers, or audit records in the local application database.

Example using a dedicated database that has already been created:

```powershell
$env:TEST_DATABASE_CONNECTION = "Host=localhost;Port=5432;Database=canadadeals_integration;Username=canadadeals;Password=canadadeals"
dotnet test tests/CanadaDeals.Api.IntegrationTests/CanadaDeals.Api.IntegrationTests.csproj
```

The 161-test integration/provider suite preserves prior slices and validates independent listing discovery, exact-offer detail/Wishlist contracts, PostgreSQL migration/persistence, safe API projection, protected redirect/click persistence, fail-closed relationship and URL states, uniqueness, and restricted deletion. It uses controlled credentials/tokens only. Frontend validation is 91 component tests plus a production build, and 26 Playwright flows exercise real Next.js/API/PostgreSQL/Worker behavior including multiple offers for one internal Product and exact-offer Wishlist persistence.
