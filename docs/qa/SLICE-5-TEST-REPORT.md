# Vertical Slice 5 Test Report

**Slice:** Search + Filters
**Status:** IMPLEMENTED AND VALIDATED
**Validation date:** 2026-08-11

## Scope and boundaries

Implemented public Product discovery only: PostgreSQL full-text search, controlled `pg_trgm` typo fallback, deterministic ranking, P0 filters, sorting, bounded pagination, URL-driven responsive controls, and honest result states. All sources remain controlled synthetic fixtures. No retailer connector, scraping, affiliate credential, email/provider change, account expansion, personalization, AI ranking, or external search was added.

## Database and query evidence

A separate empty PostgreSQL 17 database named `canadadeals_slice5_validation` applied, in order:

1. `20260811180731_InitialCreate`
2. `20260811185543_AddListingIssueReports`
3. `20260811192055_AddIdentityAndSavedProducts`
4. `20260811202709_AddPriceAlertsAndNotificationDeliveries`
5. `20260811205846_AddPostgresProductSearch`

A second update was already current. `pg_trgm` and the generated Product `SearchVector`, `SearchDocument` trigram, normalized model, and normalized MPN indexes were verified. The migration backfills current Products from canonical product/Brand/Category attributes without changing an earlier migration.

`EXPLAIN (ANALYZE, BUFFERS)` with sequential scans disabled showed bitmap index scans through `IX_Products_SearchVector` for `websearch_to_tsquery('wireless headphones')` (0.622 ms fixture execution) and `IX_Products_SearchDocument` for the indexable `<%` typo operator (`cordles dril`, 0.217 ms fixture execution). These are local fixture timings, not a production throughput claim.

## Automated results

| Suite | Result |
|---|---:|
| Domain | 46 passed, 0 failed, 0 skipped |
| PostgreSQL API integration | 69 passed, 0 failed, 0 skipped |
| Frontend component/library | 33 passed, 0 failed, 0 skipped |
| Playwright full stack | 18 passed, 0 failed, 0 skipped |
| Backend build | passed, 0 warnings, 0 errors |
| Next.js production build | passed |

## Coverage evidence

- Exact normalized model/MPN/GTIN matching precedes exact title, prefix, FTS, `pg_trgm` word similarity, recency, and Product-ID tie break; query-free feeds default recent and queries default relevance.
- Search covers title, Brand, Category, identifiers, typo fallback, no-result metadata, bounded pages, and deterministic repeated sort results.
- Category, retailer, minimum/maximum price, reference availability, freshness, match group, and availability are exercised individually and in combination; comma values are OR within a dimension and dimensions are ANDed.
- Price filtering requires safe online same-product offers. Supported savings only appears when a permitted higher earlier observation exists.
- Unknown/denied policy listings are excluded before public search, ranking, cards, and facets. Ranking has no affiliate commission, click, saved-product, alert, or user input.
- Browser flows exercise exact model search, combined filters, browser Back state restoration, mobile dialog focus/application, zero-result recovery, and prior trust/account/alert regressions against real services.

## Defects found and fixed

- EF Core could not translate the initial grouping shape used to select one representative offer per Product. It was replaced with an equivalent correlated, ordered representative-listing query that executes in PostgreSQL.
- Whole-document trigram similarity was too weak for a multiword typo. The fallback now uses PostgreSQL word similarity (`<%`) with its indexable default threshold and retains word-similarity score as an explainable relevance signal.
- The general-feed sort control accidentally submitted `recent` with a new search, bypassing the approved query-default relevance behavior. The UI now submits no sort unless the shopper explicitly selects one.
- The new mobile controls initially permitted horizontal overflow. Grid minimum widths and long-title wrapping were tightened, and the mobile viewport regression passes.

## Production limitations

No live retailer data or affiliate connectors were introduced. Search quality is verified only against controlled fixtures; evaluate relevance, latency, multilingual behavior, abuse limits, and data-retention implications again when a permitted production source is approved. `PRODUCTION EMAIL DELIVERY NOT YET CONFIGURED` remains unchanged.
