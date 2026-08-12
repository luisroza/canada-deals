# Vertical Slice 6 Test Report

**Slice:** Product History Evidence View
**Status:** IMPLEMENTED AND VALIDATED
**Validation date:** 2026-08-12

## Scope and boundaries

Implemented a public, bounded 30/90-day Product Page history view over persisted controlled observations. Product-level points are the lowest safely matched, policy-permitted new-product CAD price observed per UTC day. Missing dates are not generated or interpolated. Current price/freshness remain separate from historical coverage.

No live retailer connector, scraping, merchant data right, new retention policy, background aggregation job, search ranking change, email/provider work, or account expansion was introduced. `PRODUCTION EMAIL DELIVERY NOT YET CONFIGURED` remains unchanged.

## Environment

- Windows local development environment
- .NET SDK 10.0.300, Release configuration
- Node.js 24.14.0 and `pnpm@10.15.0`
- PostgreSQL 17.10 in the repository Docker Compose container
- Chromium through Playwright 1.62.1
- Real Next.js, ASP.NET Core API, Hangfire Worker, and PostgreSQL for E2E; no core history API interception

## Database and migration evidence

A separate empty PostgreSQL database named `canadadeals_slice6_validation` applied the complete unchanged chain:

1. `20260811180731_InitialCreate`
2. `20260811185543_AddListingIssueReports`
3. `20260811192055_AddIdentityAndSavedProducts`
4. `20260811202709_AddPriceAlertsAndNotificationDeliveries`
5. `20260811205846_AddPostgresProductSearch`

A second API startup applied no additional migration and was current. No Slice 6 migration was required. The full integration suite passed after the one-time deployment-style migration step.

The bounded query was reviewed with `EXPLAIN (ANALYZE, BUFFERS)`. With sequential scans disabled on the small fixture database, PostgreSQL used `IX_RetailerListings_ProductId` followed by `IX_PriceObservations_RetailerListingId_ObservedAt_SourceHash` with listing and observed-time index conditions (0.289 ms fixture execution). This is access-path evidence, not a production latency claim.

## Automated results

| Suite | Result |
|---|---:|
| Domain | 58 passed, 0 failed, 0 skipped |
| PostgreSQL API integration | 80 passed, 0 failed, 0 skipped |
| Frontend component/library | 38 passed, 0 failed, 0 skipped |
| Playwright full stack | 23 passed, 0 failed, 0 skipped |
| Backend Release build | passed, 0 warnings, 0 errors |
| Next.js production build | passed |

## History state and safety coverage

- `RELIABLE`: 30-day and 90-day threshold rules, tracking start, lowest observed price, observation/day counts, and same-day daily-low aggregation.
- `PARTIAL`: sparse real points remain visible with explicit gap and non-continuous-monitoring language.
- `UNAVAILABLE`: fewer than two qualifying days returns no points, no low/high, and no fake chart; a dedicated fixture avoids mutation by alert tests.
- Current `STALE`: reliable historical context does not hide or change stale current freshness.
- Policy: denied/`UNKNOWN` history observations and protected prices never enter the public projection.
- Matching: cheaper possible-match variant observations are excluded from canonical Product history.
- Value validity: future, non-positive, unsupported-currency, marketplace, and non-new observations are excluded by rule/query boundaries.
- API: invalid windows return `400`; an unknown Product returns `404`; 30 days excludes older observations that 90 days includes.

## Frontend, accessibility, and mobile

- Summary and current freshness render before the lightweight SVG.
- The 30/90 links expose selection with `aria-current` and preserve it in `?history=` URL state.
- SVG title/description and point details are supplemented by a semantic date/price/observation-count table, so data is not hover-only or color-only.
- Larger gaps use dashed segments and explicit copy. Missing dates are not drawn as generated points.
- `UNAVAILABLE` renders no chart. Technical failure uses a separate announced error while the Product Page/current price remain available.
- The independent history request streams behind a contained `aria-busy`/status fallback, so primary Product content does not wait for history and current evidence remains visible during loading.
- A 390x844 Playwright viewport confirmed Product identity remains above history, controls remain visible, and the page has no horizontal overflow.

## Regression

All automated suites were run in full. Slices 1-5 remained green, including discovery, Product Page, Search + Filters, exact-model relevance, URL restoration, safe comparison/handoff, listing reports, Identity, Saved Products, Target Price Alerts, Hangfire evaluation/deduplication, CSRF, IDOR, no-results recovery, and mobile filter behavior.

## Defects found and fixed

1. **SVG hydration warning:** React received multiple JSX children inside SVG `title`/`desc`, producing a browser hydration mismatch. The accessible strings and point titles now use single template-string children; component, build, and Playwright regressions cover the fix.
2. **History E2E selector ambiguity:** assertions selected nested `strong` nodes or matched both visible copy and SVG descriptions. Assertions now target exact state text and the owning summary/current-price containers.
3. **Unavailable fixture contamination:** the original current-only Product is intentionally mutated by Target Alert scenarios, so repeated E2E runs could make its history partial. The unavailable history journey and integration assertion now use the dedicated unavailable fixture that no alert scenario mutates.
4. **Mobile horizontal overflow:** the SVG's grid item retained its intrinsic minimum width. Product grid children can now shrink and the SVG is bounded/hidden at its own box; the representative mobile regression passes.
5. **Back-navigation fixture aging:** an older Slice 5 E2E used wall-clock freshness in its initial URL and eventually returned zero results. It now verifies restoration with stable category plus safe-match filters, preserving the intended URL/Back coverage without a real-time dependency.
6. **Clean-test startup race during validation:** starting all test-host factories concurrently against a completely empty database caused competing Identity table creation. Validation was corrected to the production-shaped sequence: one migration startup, a second idempotency startup, then the parallel integration suite. No application migration was changed.

## Production limitations

- No production retailer connector or scraping exists.
- Real merchant storage/history/display rights remain unresolved and `MerchantPolicy` stays restrictive for `UNKNOWN`.
- Controlled fixtures are evidence of product capability only, not authorization for any real source.
- Production email confirmation and alert delivery are not configured; `PRODUCTION EMAIL DELIVERY NOT YET CONFIGURED`.
- Password recovery and MFA remain unimplemented.
