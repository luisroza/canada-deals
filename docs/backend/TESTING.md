# Backend testing

Validated toolchain (2026-08-12): .NET SDK 10.0.300, Release configuration, PostgreSQL 17.10 in Docker Compose, and no test skips when the database is available.

Run the solution tests:

```powershell
dotnet test CanadaDeals.slnx --configuration Release
```

The 64-test domain suite covers all prior behavior plus Product history and delivery idempotency/status/order/retry rules. API integration tests use `Microsoft.AspNetCore.Mvc.Testing` and real PostgreSQL migrations/persistence. They are discovery-skipped only when PostgreSQL is unavailable; they never fall back to an in-memory provider.

Before running API integration tests, start `docker compose up -d postgres`. The 91-test integration suite preserves Slices 1-7 and adds restart-safe persisted Data Protection cookie/token coverage, emergency-stop non-delivery, and API security-header coverage. It continues to cover captured account confirmation/token replay, generic resend, production fail-closed configuration, Resend request/idempotency/429 classification, exact alert content, signed webhook rejection/acceptance/replay/order, webhook-before-acceptance reconciliation, and suppression behavior.
