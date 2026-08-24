# Store Affiliate Banner System test report

Date: 2026-08-20

## Verdict

- Technical status: `IMPLEMENTED AND VALIDATED`
- Live merchant status: `BLOCKED — MERCHANT RELATIONSHIP, STOREFRONT DESTINATION, AND ASSET-RIGHTS EVIDENCE REQUIRED`
- Controlled fixtures: ACTIVE and DISCOVERY_ONLY behavior validated without a real provider credential or merchant activation.
- Scraping and copied retailer creative: none.

## Automated evidence

| Gate | Result |
| --- | --- |
| Release backend build | passed, 0 warnings, 0 errors |
| Domain tests | 76 passed, 0 failed, 0 skipped |
| PostgreSQL/provider integrations | 143 passed, 0 failed, 0 skipped |
| Frontend component tests | 61 passed, 0 failed, 0 skipped |
| Release frontend build | passed |
| Full-stack Playwright | 25 passed, 0 failed, 0 skipped |
| Empty-database migration | all 12 migrations applied through `20260820191750_AddStoreBannerAssetRightsMetadata`; second application current |
| NuGet vulnerability audit | no vulnerable direct or transitive package reported |
| pnpm audit | no known vulnerabilities |
| Diff whitespace check | passed |

## Functional evidence

- `/api/v1/store-banners` returns enabled eligible stores only, in explicit editorial order followed by retailer name.
- The API returns only first-party asset paths and internal application routes; raw destination and tracking URLs are never returned to the browser.
- ACTIVE banners use `/go/store/{retailerKey}`, `target=_blank`, and `rel="noopener noreferrer sponsored"`.
- DISCOVERY_ONLY banners stay inside the store-filtered GreatDeals catalog.
- Missing or invalid assets use a first-party fallback; disabled profiles render nothing.
- The store handoff requires an enabled retailer, affiliate-permitted MerchantPolicy, ACTIVE program, usable destination, provider/retailer consistency, and validated HTTPS destination and tracking domains.
- Rakuten store handoff additionally requires a currently eligible persisted advertiser capability.
- A successful handoff persists one minimal `ClickEvent` with `placement=store_banner`; browser-supplied destination parameters cannot change the redirect.
- Missing, pending, disabled, expired, HTTP, or wrong-domain scenarios fail closed.
- Database constraints enforce one store destination per affiliate program and restrict destructive relationship deletion.

## Visual and accessibility evidence

The real local stack was inspected at 1280 x 900 and 390 x 844. Four banners formed one aligned desktop row; mobile used a two-by-two grid. Both viewports had no horizontal overflow. Retailer names remained accessible HTML text, artwork was decorative, commission and neutral-ordering disclosures were visible, and active links exposed their new-tab behavior in the accessible name. Browser logs contained no warnings or errors.

## Asset and rights result

Eight original Canada Deals SVG compositions are recorded in `docs/ux/STORE-BANNER-ASSETS.md`. They contain no retailer logos, retailer names, copied campaign creative, price, coupon, or promotional claim. Their presence does not activate a merchant. Official assets require explicit evidence, provider, `store_banner` placement, effective/expiry dates, and the separate `MerchantApprovedAffiliateAsset` state; UNKNOWN or expired rights fall back to owned artwork and are not treated as approval.

## Remaining release gate

Before any real store banner becomes ACTIVE, record the verified merchant relationship, storefront destination right, tracking and destination hosts, revalidation/expiry, and approved evidence. If official merchant creative will be used, separately record its display right, placement restrictions, effective date, and expiry. Then run a controlled 302 smoke test without completing a purchase.
