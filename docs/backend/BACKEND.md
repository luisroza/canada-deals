# Backend foundation

**Status:** IMPLEMENTED - Vertical Slice 1 foundation
**Scope:** fixture-backed, connector-neutral trusted product discovery.

The backend is an ASP.NET Core REST API in a modular monolith. Domain rules live in `src/backend/CanadaDeals.Domain`; PostgreSQL/EF Core/seed infrastructure lives in `src/backend/CanadaDeals.Infrastructure`; the API host lives in `src/backend/CanadaDeals.Api`; the Hangfire worker host lives in `src/backend/CanadaDeals.Worker`.

Implemented modules for this slice:

- Catalog: Product, Brand, Category.
- Retailers/Listings: Retailer and the expanded RetailerListing contract.
- PriceTruth: permitted current price, evidence state, history availability, and freshness.
- Matching: deterministic-first match states and safe comparison filtering.
- Affiliate boundary: fixture-only `/go/{listingId}` server-side handoff with host allowlist.
- Ingestion foundation: MerchantPolicy and PriceObservation persistence; no live connector.
- Worker foundation: Hangfire PostgreSQL storage and an opt-in fixture-safe sample job.

Accounts, alerts, reporting persistence, admin workflows, real affiliate links, and merchant-specific connectors are not implemented in this slice.
