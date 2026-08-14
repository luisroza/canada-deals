# Vertical Slice 9 test report

Date: 2026-08-14

## Verdict

- Vertical Slice: `Vertical Slice 9 — Affiliate Provider Boundary + Rakuten Advertising Affiliate/Catalog Connector`
- Technical status: `IMPLEMENTED AND VALIDATED AFTER QA REMEDIATION`
- Rakuten live status: `BLOCKED — SECURE CREDENTIAL, MERCHANT APPROVAL, AND DATA-RIGHTS CHECKPOINT REQUIRED`
- Existing Impact/CJ boundary: retained and regression-tested; neither provider is live.
- Scraping: none.

## Automated evidence

| Gate | Result |
| --- | --- |
| Release backend build | passed, 0 warnings, 0 errors |
| Domain tests | 72 passed, 0 failed, 0 skipped |
| PostgreSQL/provider integrations | 138 passed, 0 failed, 0 skipped |
| Frontend component tests | 50 passed, 0 failed |
| Release frontend build | passed |
| Full-stack Playwright | 27 passed, 0 failed; includes controlled Rakuten search → Product Page → `/go` → persisted tracking link |
| App Platform App Spec | provider schema valid; Rakuten and catalog import disabled by default |
| NuGet vulnerability audit | no vulnerable direct or transitive package reported |
| pnpm audit | no known vulnerabilities |
| Diff whitespace check | passed |

## Post-QA remediation

The release-blocking review findings were corrected and verified on a newly created isolated PostgreSQL 17 database:

- Rakuten `/go` now requires the persisted advertiser capability, retailer/policy mapping, operator enablement, active advertiser/partnership, and deep-link permission to remain eligible. Discovery reconciliation suspends affected programs and disables active links when a partnership becomes inactive or disappears from a complete provider snapshot.
- Existing source mappings revalidate a present, exact, unique UPC against both the canonical Product and the stored source identifier before changing current price or history. A missing or changed UPC is routed to review without catalog mutation.
- Current-price storage remains governed by `AllowPriceStorage`; `PriceObservation` history is created only when `AllowPriceHistory=ALLOWED`. `UNKNOWN` and `DENIED` produce no historical observation.
- Observation idempotency compares the current listing state, so unchanged repeats remain idempotent while a real `A -> B -> A` price reversion creates the third observation.
- Partial multi-page failure rolls back catalog mutations and a controlled retry succeeds once. Cancellation records terminal `FAILED / RAKUTEN_IMPORT_CANCELLED` state before the cancellation is propagated, so no durable run remains `RUNNING`.

No migration was required for these corrections. Live Rakuten validation remains blocked by the existing credential, merchant-approval, and data-rights checkpoint.

## Database and migration evidence

A separate empty PostgreSQL 17 database applied all ten migrations through `20260814173414_AddRakutenConnector`. A second `--migrate-only` run reported that the database was current. `RakutenAdvertiserCapabilities`, `RakutenSourceMappings`, `RakutenImportRuns`, `AffiliatePrograms`, `AffiliateLinks`, `ClickEvents`, and `pg_trgm` were present. No prior migration was edited.

The integration suite ran against a separate freshly created PostgreSQL database through `TEST_DATABASE_CONNECTION`. The migration advisory lock prevented parallel application hosts from racing first-time schema creation.

## Rakuten provider contract evidence

Controlled, network-free contracts verify:

- OAuth token-key construction, required Publisher Account ID `scope`, 3,600-second response handling, pre-expiry renewal, refresh-token use, one cached token across concurrent callers, one retry after `401`, and no token/secret logging;
- Advertisers v2 and Partnerships pagination/parsing, exact MID correlation, capability/status mapping, and exclusion of contact PII;
- Product Search MID/page/max request parameters, safe XML parsing with DTD disabled, current price versus retail price, source identifiers and dates, CAD normalization, and bounded pagination;
- Deep Links JSON request/response, exact advertiser MID/destination, privacy-safe `u1`, approved tracking host, and fail-closed mapping for access, relationship, deep-link, template, auth, rate-limit, and provider failures.

## Persistence, matching, and policy evidence

PostgreSQL tests prove dry-run creates only an import audit and does not mutate Product, listing, or observation records. Live-mode controlled tests require an ACTIVE advertiser/partnership, Canada relevance, explicit retailer/MerchantPolicy mapping, operator catalog enablement, and `ALLOWED` metadata/price permissions.

Exact unique UPC can attach a source record to an existing canonical Product. A missing UPC/title-only candidate or conflicting identifier is routed to review and cannot create a new canonical Product. USD is skipped. New listing fields retain unknown seller, marketplace, condition, and availability when the source does not prove them. Images are not cached. Repeated input is idempotent; a changed permitted CAD price creates a new observation while the source mapping remains stable.

## Affiliate handoff evidence

The previous Impact/CJ controlled contracts and persisted `/go/{listingId}` tests remain green. The Rakuten controlled fixture adds a real full-stack path from exact model search through product evidence and a persisted Rakuten AffiliateLink to a validated tracking redirect. No browser-supplied destination controls the redirect and no provider call occurs in the shopper request.

## Security result

- The credential pasted into chat was not used, logged, persisted, or copied into source/configuration. It must be rotated again before live testing.
- Rakuten is disabled by default in API, Worker, and DigitalOcean App Spec. Enabled startup fails closed without Account ID, Client ID, Client Secret, HTTPS API origin, and sane limits.
- Tokens are memory-only; refresh is synchronized; responses/logs use safe error codes and omit bodies/credentials.
- Provider URLs require HTTPS, no userinfo, exact merchant destination, and configured destination/tracking hosts.
- XML DTD resolution is disabled and document size is bounded.
- `u1` and ClickEvent fields contain only non-PII classification/placement data.
- Operator scripts print missing configuration names only and exit `2` when secrets are absent.

## Live validation matrix

| Operation | Result | Reason |
| --- | --- | --- |
| OAuth token | `BLOCKED` | no securely supplied rotated secret or Publisher Account ID |
| Partnerships | `BLOCKED` | OAuth unavailable; no approved live read |
| Advertisers | `BLOCKED` | OAuth unavailable; no approved live read |
| Product Search | `BLOCKED` | no approved merchant MID/data rights; dry-run not authorized live |
| Deep Link | `BLOCKED` | no verified ACTIVE merchant partnership/deep-link rights |
| Catalog persistence | `NOT AUTHORIZED` | no merchant mapping/policy approval or controlled live evidence |

No advertiser counts, merchant candidates, permissions, or Canada coverage are invented. No purchase, artificial conversion, provider write, cloud mutation, or production resource change occurred.

## Recommended checkpoint

`Rakuten Merchant Approval + Data Rights Checkpoint`.

Required inputs: rotate the exposed secret; configure Client ID, Client Secret, and Publisher Account ID through an approved secret store; run read-only discovery; select a merchant only from returned ACTIVE evidence; document affiliate/deep-link/product-feed/field/retention/image/price-history rights; approve MerchantPolicy and retailer mapping; then run bounded Product Search dry-run before any live persistence.
