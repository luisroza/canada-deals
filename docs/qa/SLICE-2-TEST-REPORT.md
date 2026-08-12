# Vertical Slice 2 QA evidence

**Vertical Slice:** Persisted Stale/Wrong Listing Report
**Date:** 2026-08-11
**Status:** IMPLEMENTED AND VALIDATED

## Environment

- Windows 11
- .NET SDK 10.0.300
- PostgreSQL 17 through Docker Desktop / Compose
- Node.js 24.14.0 and repository-declared `pnpm@10.15.0`
- Next.js 16.3.0, React 19.2.8, Playwright 1.62.1, Chromium 151.0.7922.34

## Database

- Clean temporary database: PASS.
- Full migration chain `20260811180731_InitialCreate` → `20260811185543_AddListingIssueReports`: PASS.
- `ListingIssueReports` table and `(Status, CreatedAt)` review index: PASS.
- Existing schema migration and controlled fixture seed: PASS.
- PostgreSQL-backed report persistence and multiple reports per listing: PASS.
- FK delete behavior is `RESTRICT`; report history does not cascade-delete with a listing.

## Backend

- Release restore/build: PASS, 0 warnings and 0 errors.
- Domain tests: 18 passed, 0 skipped.
- PostgreSQL API integration tests: 15 passed, 0 skipped.
- Valid report creation returns `201`, persists `OPEN`, and is visible through the Development-only review boundary.
- Unknown listing, invalid reason, and excessive note fail safely.
- Reporting a listing already marked stale is accepted as additional review evidence.
- Creating a report does not change listing price, match state, evidence, availability, or public visibility.
- The internal review endpoint returns `404` outside Development.

## Frontend

- Component tests: 6 passed.
- Production Next.js build: PASS.
- Product Page exposes a secondary `Report stale or wrong` action.
- The inline form provides controlled reasons, an optional 500-character plain-text note, pending/disabled behavior, inline validation, retry-preserving errors, success announcement, and focus management.

## E2E and Slice 1 regression

- Playwright: 6 passed, 0 failed.
- Existing discovery, evidence, safe comparison, unavailable history, mobile focus, and overflow paths remain green.
- Price changed submission persists through the real Next.js → ASP.NET Core → PostgreSQL path and is verified through the safe Development review endpoint.
- Wrong variant submission persists as an `OPEN` review signal.
- No API interception or frontend-only mocked response was used for the E2E report paths.
- Worker project and Hangfire foundation remain buildable and unchanged; no report-processing job was added.

## Security and privacy

- Reports are anonymous and require no name, email, address, phone, or full IP storage.
- Request contract accepts only reason and note; status, timestamps, listing ID, and internal fields cannot be mass-assigned.
- Notes are bounded, logged only by report metadata, and treated as untrusted plain text.
- No public report-listing endpoint or fake admin route exists.
- Rate limiting, bot abuse, repeated false reports, and resource-exhaustion controls remain explicit Security Review follow-ups.

## Defects found

No existing Slice 1 product defect was found. During frontend test expansion, test DOM cleanup was made explicit so report-component tests remain isolated and deterministic.

## Merchant integrations

No production retailer connector, live credential, scraping path, or merchant data source was added.

## Recommended next slice

Vertical Slice 3 — Save Product persistence with the approved minimal account boundary. This recommendation was not implemented as part of Slice 2.
