# Backend testing

Run the solution tests:

```powershell
dotnet test src/backend/CanadaDeals.slnx
```

The domain suite covers freshness, evidence, policy blocking, deterministic matching, and title-only review states. API integration tests use `Microsoft.AspNetCore.Mvc.Testing` and real PostgreSQL migrations/persistence when PostgreSQL is available. They are discovery-skipped with an explicit reason when Docker/PostgreSQL is unavailable; they do not silently fall back to an in-memory provider.

Before running API integration tests, start `docker compose up -d postgres`. The suite validates the discovery contract, Product Page safe-comparison separation, server-side allowlisted handoff, and PostgreSQL health.
