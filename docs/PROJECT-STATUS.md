# Project Status

## Current phase

Application Implementation - Vertical Slice 1

## Completed

- Local workspace linked to `luisroza/canada-deals`
- Project and agent governance foundation
- Canadian market research and product definition
- Human Product Checkpoint
- UX / Product Design and Human UX Checkpoint
- Solution / Cloud / FinOps Architecture
- Data / Affiliate Integration Architecture
- Architecture / Data reconciliation
- Human Architecture / Data Integration Checkpoint approved with refinements
- Application monorepo foundation
- PostgreSQL EF Core model and first migration
- Synthetic fixture seed data
- Connector-neutral REST API
- Next.js discovery feed and Product Page
- Domain and frontend automated tests
- Hangfire worker foundation

## Approved product direction

- Positioning: Canadian price-truth layer for planned online purchases
- Initial wedge: electronics plus home improvement/tools
- English-first responsive web MVP
- Evidence before enthusiasm, visible freshness, conservative history, and safe same-product comparison
- Save Product and Target Price Alert remain P1; Weekly Digest remains P2
- Two high-quality approved retailers are sufficient for launch; retailer count is not an MVP KPI

## Current implementation

- Backend: .NET 10 modular monolith projects under `src/backend/` for Domain, Infrastructure, API, and Worker.
- Persistence: PostgreSQL/Npgsql with migration `20260811180731_InitialCreate`; local development uses Docker Compose.
- API: `GET /api/v1/deals`, `GET /api/v1/products/{slug}`, safe fixture-only `GET /go/{listingId}`, and `GET /health`.
- Fixtures: Products A-F cover strong evidence, current-only history unavailable, partial history, stale price, possible variant review, and no safe comparison.
- Frontend: Next.js 16 + React 19 pages `/` and `/products/[slug]`, server-rendered API-driven content, responsive cards, evidence/freshness/match states.
- Tests: 7 domain tests and 3 frontend component tests pass. Four PostgreSQL API integration tests are present and skipped when PostgreSQL is unavailable.
- Worker: Hangfire PostgreSQL storage and opt-in fixture-safe sample job; no merchant ingestion.

## Blocked external integrations

- Best Buy Canada: program exists, but feed/API rights, permitted fields, retention, and cadence are unresolved.
- Home Depot Canada: affiliate program exists, but catalog/API/feed rights are unresolved.
- Amazon.ca: gated; not on the critical implementation path.
- Walmart Canada: fallback / Phase 2 pending Rakuten partnership and data access validation.

No merchant-specific production connector may be added until the verified source evidence required by `docs/integrations/INTEGRATION-BACKLOG.md` is committed.

## Current checkpoint

Human Architecture / Data Integration Checkpoint: approved. Application implementation is authorized within the approved architecture and connector gate.

## Known validation limitation

Docker Desktop's Linux engine was unavailable during this run and no local PostgreSQL service was installed. Therefore migration application, database-backed API assertions, and Playwright E2E were not executed; they are explicitly documented as remaining validation work.

## Next vertical slice

Implement the persisted stale/wrong listing report boundary: report endpoint and migration, minimal user-facing report entry, and a reviewable status without introducing full accounts/admin or merchant connectors.
