# Vertical Slice 1 QA evidence

**Scope:** connector-neutral Trusted Product Discovery with synthetic fixtures.
**Date:** 2026-08-11
**Environment:** Windows 11, .NET SDK 10.0.300, Node.js 24.14.0, repository-declared `pnpm@10.15.0`, Docker Desktop Linux engine, PostgreSQL 17, Chromium 151.0.7922.34.

## Database validation

- `docker compose up -d postgres` — PASS; PostgreSQL container healthy and accepting connections.
- `20260811180731_InitialCreate` — PASS against a clean PostgreSQL database.
- Reapplying `dotnet-ef database update` — PASS; no migrations were reapplied.
- `pg_trgm` extension — PASS.
- Development seed — PASS; six controlled products and their listings reached the API.

## Backend

- `dotnet restore src/backend/CanadaDeals.slnx` — PASS.
- `dotnet build src/backend/CanadaDeals.slnx --configuration Release --no-restore` — PASS; 0 warnings, 0 errors.
- Domain tests — PASS: 7 passed, 0 skipped.
- PostgreSQL API integration tests — PASS: 8 passed, 0 skipped.
- API coverage includes `/api/v1/deals`, valid and invalid Product Page slugs, strong/partial/unavailable history, stale discovery state, safe comparison separation, PostgreSQL health, allowlisted handoff, arbitrary destination rejection, malformed IDs, unknown IDs, and listings without approved destinations.

## Frontend

- Component tests — PASS: 3 passed.
- Production build — PASS with Next.js 16.3.0 and TypeScript validation.
- Same-site development contract — PASS: Next.js rewrites `/api/*` and `/go/*` to the local API while browser links remain relative.

## E2E

- Core discovery → Product Page → evidence/freshness/history → safe comparison — PASS.
- Unsafe variant excluded from safe comparison and related card has no retailer CTA — PASS.
- History unavailable is shown without an all-time-low claim — PASS.
- Mobile viewport smoke, meaningful headings/main landmark, keyboard focus, and no horizontal overflow — PASS.
- Playwright total — 4 passed, 0 failed.

The browser path used the real Next.js server, real ASP.NET Core API, and PostgreSQL fixture data. No API interception or frontend-only mock was used.

## Worker and security baseline

- Worker Release build — PASS.
- Worker startup and Hangfire PostgreSQL storage — PASS; 12 Hangfire tables created.
- Opt-in fixture-safe sample Hangfire job — PASS; no merchant fetch occurred.
- Anonymous Hangfire dashboard — PASS; no dashboard is registered/exposed.
- Safe redirect baseline — PASS; only server-resolved, allowlisted fixture destinations redirect.
- Production retailer connectors — none added.

## Defect found and fixed

**Symptom:** the feed could expose a `handoffPath` for a listing with no approved affiliate destination; the redirect then returned `404`, and the integration test was order-dependent.

**Root cause:** the API generated `/go/{listingId}` from the listing ID without checking whether the approved destination reference existed.

**Fix:** `handoffPath` is now nullable and is emitted only for listings with an approved destination. The frontend suppresses the CTA when it is null. The integration suite now covers the no-destination, malformed-ID, unknown-ID, and deterministic approved-handoff paths.

## Skips

No critical tests remained skipped in the validated environment. The existing PostgreSQL skip mechanism remains intentional only for machines where PostgreSQL/Docker is unavailable, with an explicit environment reason and no SQLite fallback.

## Vertical Slice Status

### `IMPLEMENTED AND VALIDATED`

## Recommended next step

Begin Vertical Slice 2 — persisted stale/wrong listing report workflow, after review of this validation evidence. No Slice 2 work was started during this run.
