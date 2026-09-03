# Backend testing

Validated toolchain (2026-09-03): .NET SDK 10.0.300, Release configuration, PostgreSQL 17 in Docker Compose, and no test skips when the database is available. The multi-network catalog increment passed the Release solution build with 0 warnings/errors, 99 domain tests, and 185 integration/provider tests against a clean dedicated PostgreSQL database.

Run the solution tests:

```powershell
dotnet test CanadaDeals.slnx --configuration Release
```

The 99-test domain suite covers all prior behavior plus individual-offer savings rules, listing-keyed Wishlist intent, affiliate lifecycle, provider-neutral catalog-source activation rules, case-preserving provider advertiser identity, Rakuten capability/policy fail-closed behavior, original and merchant-approved StoreBannerProfile rights validation, and StoreAffiliateDestination lifecycle. API integration tests use `Microsoft.AspNetCore.Mvc.Testing` and real PostgreSQL migrations/persistence. They never fall back to an in-memory provider.

Before running API integration tests, start `docker compose up -d postgres` and set `TEST_DATABASE_CONNECTION` to a dedicated test database. The fixture fails closed when the variable is absent or points to the application database named `canadadeals`; integration tests must never create users, roles, offers, or audit records in the local application database.

Example using a dedicated database that has already been created:

```powershell
$env:TEST_DATABASE_CONNECTION = "Host=localhost;Port=5432;Database=canadadeals_integration;Username=canadadeals;Password=canadadeals"
dotnet test tests/CanadaDeals.Api.IntegrationTests/CanadaDeals.Api.IntegrationTests.csproj
```

The 185-test integration/provider suite preserves prior slices and validates independent listing discovery, exact-offer detail/Wishlist contracts, PostgreSQL migration/persistence, safe API projection, protected redirect/click persistence, fail-closed relationship and URL states including redirect rejection, uniqueness, restricted deletion, provider contract parsing, discovery identity validation, dry-run/import idempotency, rollback, policy enforcement, CAD-only writes, strong-identity Product matching, and Awin post-gzip decompressed-size enforcement. Provider tests use controlled fixtures and credentials only; no live provider call is part of the deterministic gate. Frontend validation is 92 component tests plus a production build, and 26 Playwright flows exercise real Next.js/API/PostgreSQL/Worker behavior including multiple offers for one internal Product and exact-offer Wishlist persistence.
