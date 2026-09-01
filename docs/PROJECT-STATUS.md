# Project Status

## Current phase

Application Implementation — Individual Offer + Wishlist Revision — IMPLEMENTED; FRONTEND DEFAULT-SUITE STABILITY FOLLOW-UP REQUIRED; OWNER SECURITY AND LIVE MERCHANT ACTIVATION BLOCKED

## Status refresh — 2026-08-31

- Repository: `main` and `origin/main` both point to `feba527ea2e7ecf2fe536c544a02484306de168a`, but the current implementation is not committed. Before this status-document refresh, the working tree contained 70 modified files, 2 tracked deletions, and 7 untracked implementation files; the tracked diff covered 72 files with 867 insertions and 821 deletions.
- Current validation: the Release solution build passes with 0 warnings/errors; all 94 domain tests and all 161 isolated PostgreSQL integration/provider tests pass with zero skips; the optimized Next.js production build passes.
- Frontend test-runner issue: the default `pnpm --dir apps/web test` run currently reports 90 passed and 1 failed in `DiscoveryExperience.test.tsx` because Category sometimes remains `electronics` after Clear. The isolated test passes, and the complete suite passes 91/91 with `--maxWorkers=1`, so the implementation or test has an order/concurrency-sensitive state leak. The normal repository command is not fully green until this is stabilized.
- The 26 full-stack Playwright journeys are the most recent recorded E2E evidence, but they were not rerun during this status refresh.
- Requirement discrepancy: the current admin code and UX documentation still expose **Catalog > Brands**, while the latest owner direction was to remove the standalone Brands tab and retain Brand detection/creation inside Offer entry. This must be reconciled before calling the admin workflow final.

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
- Vertical Slice 1 validation and stabilization
- Vertical Slice 2 persisted stale/wrong listing report workflow
- Vertical Slice 3 Save Product persistence with minimal account boundary
- Vertical Slice 4 Target-Price Alert persistence and evaluation boundary
- Vertical Slice 5 Search + Filters
- Vertical Slice 6 Product History Evidence View
- Vertical Slice 7 Production Email Delivery Boundary
- Vertical Slice 8 Production Deployment + Email Operational Validation preparation
- Vertical Slice 9 Affiliate Link Provider Activation and Rakuten Advertising Affiliate + Catalog Connector
- Human Product/UX revision: Promobit-informed store-led discovery, category/store-only filtering, compact cards, and Wishlist-only retention
- Provider-neutral Store Affiliate Banner System with original first-party artwork, discovery-only fallback, protected backend handoff, and minimal click attribution
- Owner-only administration boundary with unlinked `/admin_panel`, role-protected offer/banner/report operations, reversible publication state, and audit trail
- Common-account Wishlist usability refinement with card-level save, synchronized counts, local organization controls, and accessible retry states
- Owner Category and Store management with inactive-by-default creation, immutable identifiers, audited reversible state, impact counts, and fail-closed public visibility
- Owner-reviewed Product Image publishing with secure upload, rights evidence, reversible activation/archive, fail-closed delivery, and card/Product Page/Wishlist presentation
- Offer-time Brand candidate matching/creation, canonical Product reuse for additional store offers, immutable Product slugs, optional automatic offer expiry, progressive offer entry, and single-source carousel selection
- Individual retailer-offer identity, same-listing regular/deal pricing, canonical Offer Pages, and listing-keyed Wishlist persistence

## Approved product direction

- Positioning: fast Canadian deal discovery with visible current-offer trust cues
- Initial wedge: electronics plus home improvement/tools
- English-first responsive web MVP
- Promobit-informed information architecture with an independent GreatDeals.ca identity: Search, Categories, Stores, Wishlist, compact cards, and clear deal CTAs
- Category and Store are the only public discovery filters; current check time and same-listing deal/regular price evidence remain visible
- Wishlist is the only retention feature; public price history/tracking, Target Price Alerts, and Weekly Digest are removed
- Two high-quality approved retailers are sufficient for launch; retailer count is not an MVP KPI

## Current implementation

- Backend: .NET 10 modular monolith projects under `src/backend/` for Domain, Infrastructure, API, and Worker.
- Persistence: PostgreSQL/Npgsql through `20260831135128_IndividualOfferPricing`; local development uses Docker Compose.
- API: public individual-offer discovery/detail/health/report/handoff, eligible store-banner discovery, PostgreSQL FTS + `pg_trgm` typo fallback, policy-safe filtering/sorting/pagination, Identity accounts, and listing-keyed current-user Wishlist persistence. Legacy Product/history/price-alert compatibility boundaries do not create public comparisons or active alert UI.
- Fixtures: Products A-H cover reliable/partial/unavailable/policy-hidden Product history, stale current price with valid history, unsafe cheaper history, discovery states, and alerts without live retailer sources.
- Frontend: Next.js 16 + React 19 with sticky global search, adjacent Categories/Stores menus, an API-backed one-row store carousel exposing at most four banners per viewport, category/store-only quick filters, responsive four/two/one-density individual-offer cards, canonical Offer Pages, account routes, and `/saved` as Wishlist. Cards save exact listing IDs; one shared client load synchronizes saved state and counts. Public comparisons, price history, and Target Price controls are absent.
- UX refinement: store-led discovery and compact card scanning adopt the useful structural concepts observed on Promobit without copying its identity or importing community, voting, urgency, coupon, or engagement-ranking behavior. Deal cards and Product pages reserve a stable square media area, use contained owner-reviewed product imagery when eligible, and fall back to neutral category artwork. Retailer logos remain excluded until display rights are verified.
- Reports: controlled reasons, optional bounded plain-text note, `OPEN` default status, non-cascading listing FK, no required PII, and no automatic mutation/suppression of listing truth.
- Identity: normalized email identifier, Identity hashing/token support, confirmed-email policy, explicit 24-hour email-confirmation token provider, base64url confirmation links from a configured canonical origin, secure same-site cookie session, anti-forgery on mutations, generic resend/login behavior, lockout, and register/login/resend rate limit.
- Saved Offers: composite `(UserId, RetailerListingId)` persistence, current-session ownership, exact listing identity, user cascade/listing restrict delete behavior, card/Offer Page save and remove, synchronized navigation count, local search/category/store/sort, and no influence on Price Truth, Deal Quality, evidence, freshness, commercial economics, or organic ranking.
- Legacy Target Price Alerts: persistence/domain/migrations remain for safe rollback, but product routes are disabled by default, frontend controls are removed, and the worker can no longer enqueue alert evaluation.
- Delivery: provider-neutral transactional email remains for account confirmation. Historical alert-delivery code and records are retained but are not an active product capability.
- Deployment preparation: Docker multi-stage images for web/API/worker, non-root runtime users, health endpoints, production security headers, component-scoped secrets, DigitalOcean App Spec with Toronto services/worker/PRE_DEPLOY migration job/managed PostgreSQL binding, and operations scripts. No cloud resource has been provisioned.
- Data Protection: PostgreSQL-backed shared key ring with explicit application name and PFX encryption required in Production; API restart preserves valid cookies and confirmation tokens.
- Product history: historical storage/projection code remains policy-gated for compatibility, but the public endpoint and UI are disabled by default.
- Store banners: only eligible stores with an explicitly enabled persisted profile participate in the admin-ordered carousel; no-profile stores are never auto-published. One audited admin selection controls membership, Carousel position controls sequence, and responsive presentation shows no more than four banners at once. ACTIVE controlled fixtures use `/go/store/{retailerKey}` in a protected sponsored new tab; stores without an approved usable destination remain DISCOVERY_ONLY and open the filtered GreatDeals catalog. The API never exposes raw tracking URLs.
- Store artwork: eight responsive SVG compositions are owned by Canada Deals and contain no merchant logo, trade dress, retailer name, price, coupon, or promotional claim. A bounded owner-only PNG/JPEG/WebP library persists immutable reviewed images in PostgreSQL and serves opaque same-origin asset IDs. Upload does not bypass provenance or merchant-rights gates.
- Product images: one active primary image may be published per Product from the owner-reviewed library. Uploads are restricted to signature-verified PNG/JPEG/WebP files up to 1 MB and 2400 x 2400, record SHA-256, placements, rights evidence, effective/expiry dates, actor and audit, and are served through a same-origin ETag endpoint only while active and current. Replacement archives the previous active asset; missing, pending, expired, archived, or failed images render a neutral fallback. Connector image URLs remain unpersisted and blocked.
- Owner administration: separate responsive shell for Overview, Offers, Catalog, Stores, Store Banners, customer Reports, and Audit. Catalog opens on Categories while Brand lifecycle management remains a secondary advanced tab. New offers no longer inherit the first enabled Brand: link validation selects a matching Brand when available, and the owner can create, explicitly activate, and select a Brand inline. Brand/Category/Store operations preserve immutable slug/key identity, expose database-wide impact counts, require reasons for deactivation, and never delete linked records. Offer entry follows Validate and fill → Review card → Publish, starts with safe unknown/manual-review defaults, keeps Save draft separate from Publish, can create or reuse a canonical Product, and resolves only bounded `amzn.to` redirect headers to an HTTPS Amazon.ca destination without downloading the Product page. Amazon relationship/tag/evidence confirmation is explicit; Product slugs remain immutable. The manual editor omits advanced matching/composition fields that are unnecessary for independent-offer publishing while preserving their backend values for existing records and approved connectors. An optional validity cutoff removes expired offers from public reads and handoff without a job. Banner membership is controlled only by the audited selection workflow; the banner editor manages content, order, artwork, and rights. All APIs require the `OwnerAdmin` role; writes require CSRF, rate limiting, bounded validation, and transactional audit. Bootstrap is interactive and no credential is committed.
- Tests: the 2026-08-31 refresh passes 94 domain tests and 161 isolated PostgreSQL integration/provider tests with zero skips, plus the Release backend build and optimized Next.js build. The frontend suite passes 91/91 with one worker, but the normal parallel command currently fails the Clear-filters case once (90/91); this is an open test-stability gate. The latest recorded full-stack Playwright evidence remains 26/26 and was not rerun in this refresh. PostgreSQL integrations run serially because they intentionally share one isolated database fixture.
- Browser validation: the store carousel was inspected at 1280 x 900 and 390 x 844 during its earlier rollout. The subsequent mobile correction now reserves one complete banner per phone page, two per tablet page, and four per desktop page; unit coverage verifies page counts and final-page behavior without counting inter-card gaps as extra pages.
- LAN mobile validation: development CSP no longer upgrades private-network HTTP assets to unavailable HTTPS. `http://<LAN-IP>:3000` loaded the stylesheet and responsive two-column banner grid at 390 x 844; production retains `upgrade-insecure-requests`.
- Worker: Hangfire PostgreSQL storage, health endpoint, affiliate-link refresh, and gated Rakuten jobs; Price Alert evaluation enqueue has been removed.
- Affiliate handoff: provider-neutral Impact/CJ/Rakuten generated-link flow retains persisted lifecycle, refresh, HTTPS/domain/deeplink/relationship gates, `/go/{listingId}` Product handoff, and `/go/store/{retailerKey}` store handoff. ADR-010 adds an owner-provided Amazon direct-link mode that stores the exact URL and bypasses `/go`; ADR-011 permits bounded short-link redirect-header resolution only during owner validation. Explicit relationship/tag/evidence and an Amazon-specific data policy remain required before publication. No live credentials or merchant approval is inferred.
- Rakuten connector: opt-in OAuth token-key + Publisher Account ID scope, memory-only token cache/refresh with anti-stampede, bounded Advertisers/Partnerships discovery, fail-closed partnership-removal reconciliation, provider capability snapshots, one-link deep-link generation, Product Search XML normalization, dry-run, MID-scoped import audit, UPC revalidation for existing mappings, transition-safe price observations, and MerchantPolicy-gated current-price/history persistence. Cancelled imports close their durable audit before propagating cancellation. It is disabled by default.

## Blocked external integrations

- Best Buy Canada: Impact adapter implemented; `IMPLEMENTED — AWAITING PUBLISHER APPROVAL`. Account, active contract, ProgramId, MediaPartnerPropertyId, credentials, deeplink domains, and controlled live link evidence are absent. Catalog/feed rights remain separate and unresolved.
- Home Depot Canada: CJ adapter implemented; `IMPLEMENTED — AWAITING PUBLISHER APPROVAL`. Publisher account/PAT/PID, joined advertiser relationship, CID/Link ID, and controlled live evidence are absent. Catalog/feed rights remain separate and unresolved.
- Amazon.ca: owner-provided direct-link boundary implemented; commercial activation remains gated until the owner records approved Canada Associates/GreatDeals.ca evidence and Partner Tag. Creators API, Product data, price, availability, image, comparison, cache, and history rights remain gated; no API adapter exists.
- Walmart Canada: `GATED / UNVERIFIED FOR CANADA DEALS`; official Walmart.ca application path exists through Rakuten, but Canada Deals approval/link/data rights are absent.

No merchant-specific production connector may be added until the verified source evidence required by `docs/integrations/INTEGRATION-BACKLOG.md` is committed.

## Current checkpoint

Human Architecture / Data Integration Checkpoint: approved. Application implementation is authorized within the approved architecture and connector gate.

## Validation evidence

- Clean PostgreSQL 17 Compose instance started successfully.
- EF migration `20260811180731_InitialCreate` applied successfully; a second application reported the database already up to date.
- `pg_trgm` extension and relational schema were created successfully.
- Development API seeded six controlled synthetic products and their listings without live retailer data.
- Release backend restore/build/tests passed with 0 warnings and 0 errors.
- Worker startup created 12 Hangfire PostgreSQL tables and completed the opt-in fixture-safe sample job.
- Same-site Next.js rewrites for `/api/*` and `/go/*` were exercised by the Playwright path.
- A separate empty PostgreSQL 17 database successfully applied InitialCreate → AddListingIssueReports → AddIdentityAndSavedProducts → AddPriceAlertsAndNotificationDeliveries; reapplication reported current before all 39 integration tests passed.
- The report table, review index, `OPEN` lifecycle, anonymous persistence, retry-tolerant duplicate behavior, and Development-only operator review path were validated.
- Identity confirmation boundaries, production Secure/HttpOnly/SameSite cookie flags, CSRF rejection, rate limiting, normalized duplicates, generic login errors, logout, IDOR isolation, database uniqueness, and user/Product delete semantics were validated.
- Alert equality/above/stale/unsafe/policy/no-price/history-unavailable cases, target version re-trigger, CSRF, confirmed-email gate, IDOR, durable deduplication, Development capture, and Production suppression were validated.
- Release backend build passed with 0 warnings and 0 errors. Release frontend build and 13 real Playwright journeys passed without core API interception.
- Concurrent clean-database startup exposed and fixed a fixture-seed race; a PostgreSQL transaction advisory lock now serializes Development seed initialization.
- Worker regression created 12 Hangfire tables and the fixture-safe job reached `Succeeded`; no Save Product background job was added.
- A separate empty PostgreSQL 17 database applied all five migrations including `AddPostgresProductSearch`; a second update was current, `pg_trgm` and the generated FTS/trigram/identifier indexes were present, and all 69 integration tests passed without skips.
- `EXPLAIN (ANALYZE, BUFFERS)` verified bitmap index scans through the Product FTS GIN index and the `pg_trgm` word-similarity GIN index on the controlled fixture database.
- Release frontend build and all 18 real Playwright journeys passed against the isolated PostgreSQL validation database.
- A separate empty PostgreSQL 17 database applied the unchanged five-migration chain for Slice 6; a second startup was current and all 80 PostgreSQL integrations passed without skips.
- Slice 6 `EXPLAIN (ANALYZE, BUFFERS)` used `IX_RetailerListings_ProductId` and `IX_PriceObservations_RetailerListingId_ObservedAt_SourceHash` for the bounded Product/listing/time query, so no migration or speculative index was added.
- Release backend build passed with 0 warnings and 0 errors; 58 domain, 80 integration, 38 frontend, and 23 real full-stack Playwright tests passed with zero skips.
- A separate empty PostgreSQL 17 database applied the complete seven-migration chain through `AddProductionEmailDelivery` and `AddEmailRetrySchedule`; a second update reported current, all 87 integration tests passed, and the exact temporary database was removed.
- Release backend build passed with 0 warnings and 0 errors; 64 domain, 87 PostgreSQL integration, 43 frontend, and 24 real full-stack Playwright tests passed with zero skips.
- The E2E account journey read the exact persisted confirmation email, followed its real Identity token, confirmed, signed in, created an alert, ran Hangfire, and asserted the exact captured HTML/text alert content without API interception or external email.
- Signed webhook tests covered invalid signatures, provider-event replay, out-of-order delivery, provider acceptance versus delivery, and application suppression. Resend HTTP tests covered stable idempotency keys, provider IDs, and `429 Retry-After`.
- Slice 8 App Spec passed `doctl apps spec validate --schema-only`; it intentionally retains six deployment placeholders pending credentials, domain, sender, and cluster selection.
- Slice 9 applied the complete nine-migration chain to an empty PostgreSQL 17 database; the second `--migrate-only` run reported current, and `AffiliatePrograms`, `AffiliateLinks`, and `ClickEvents` were present.
- Impact controlled contracts cover ACTIVE/deeplink domains/regular Tracking Link, Basic auth, media property/Sub IDs, invalid domains/URLs, and 401/403/429/5xx. CJ controlled XML covers PAT, joined relationship, PID/CID/Link ID, deep-link permission, `clickUrl`, rate limit, and malformed response.
- Release validation passed with 0 build warnings/errors: 68 domain, 112 backend/provider, 50 frontend, and 26 Playwright tests, all with zero skips. App Spec remains schema-valid with provider activation disabled by default.
- Rakuten connector validation applied the complete ten-migration chain to an empty PostgreSQL 17 database; the second `--migrate-only` run was current and the three Rakuten tables, affiliate tables, and `pg_trgm` were present.
- Rakuten release validation passed with 0 build warnings/errors: 71 domain, 132 backend/provider, 50 frontend, and 27 Playwright tests, all with zero skips. NuGet/pnpm audits found no known vulnerabilities and the App Spec remained schema-valid with Rakuten disabled.
- Post-QA Rakuten stabilization passed on a new isolated PostgreSQL 17 database: 72 domain and 138 backend/provider tests, all with zero skips. Regression coverage now proves persisted `/go` fails closed after partnership removal, reconciliation suspends programs/disables links, existing mappings revalidate UPC, `AllowPriceHistory=UNKNOWN` creates no observations, `A -> B -> A` records the reversion, partial failures roll back and retry cleanly, and cancellation leaves no `RUNNING` audit. All 27 full-stack Playwright journeys also passed.
- Product recommendation implementation exposes online availability on discovery cards and source-proven seller, condition, availability, regional, shipping, and last-check facts on Product offers. Missing coupon, eligibility, or expiry evidence is explicitly labelled unknown. Saved-search/keyword alerts and structured community confirmations are recorded as later controlled experiments; votes, comments, reputation, and engagement/commission ranking remain outside MVP.
- UX competitive refinement now implements the approved store-led navigation, store banners, Category/Store-only controls, compact card hierarchy, mobile navigation, Wishlist-only return path, and owner-reviewed Product imagery. Connector/merchant Product images remain blocked pending verified display/cache rights, and coupon UI remains blocked pending verified code/eligibility/expiry evidence.
- Offer Page UX aligns identity, image, deal/regular price, retailer action, and Wishlist inside one responsive decision summary. Offer conditions and correction reporting are separated into a consistent sidebar; no comparison, related retailer, tracker, or alert surface is rendered.
- The Wishlist uses compact horizontal saved-product cards. The Product Page and fixture-copy refinements passed all 72 frontend tests, the 8 targeted PostgreSQL handoff integrations, and the optimized Next.js production build.
- Homepage Deal Cards now use a concise decision hierarchy: product image, store, two-line title, deal price, optional evidence-backed regular price/savings from the same listing, one freshness/evidence line, Wishlist, and one retailer action. The non-interactive card surface opens the internal `/offers/{listingId}` page in the current tab; Wishlist remains independent, and **Check retailer price** alone uses the retailer handoff in a protected new tab. Offer Page and mobile sticky retailer actions follow the same new-tab rule. Each listing renders independently even when several listings share one internal Product. Detailed availability, timestamps, conditions, and evidence explanations remain on the Offer Page.

- Individual-offer revision (2026-08-31): `RetailerListing` now owns optional regular price evidence and promotion start/end, discovery no longer selects one representative listing per Product, `/offers/{listingId}` is the canonical detail route, and Wishlist persistence migrated from Product to listing identity. Legacy saves with no listing are retained in an audit-only migration-orphan table rather than discarded. Cross-retailer comparison output was removed from public contracts and UI. Validation: 94 domain tests, 161 isolated PostgreSQL integrations, 91 frontend tests, 26 full-stack Playwright journeys, zero-warning solution build, and optimized Next.js build all pass.
- Homepage Lighthouse remediation aligns Store Banner, Wishlist, and Deal Card accessible names with their visible labels and gives only the first banner image eager/high fetch priority while preserving dimensions and lazy off-screen loading. Clean Chromium 151 production audits scored Desktop `100/100/96/100` and Mobile `98/100/96/100` for Performance/Accessibility/Best Practices/SEO, with no label-in-name failures, application hydration errors, or mobile overflow and effectively zero CLS. The 85 frontend tests, all 26 full-stack Playwright journeys, and the production build pass. The former missing favicon follow-up is resolved by the static `/icon.svg` GreatDeals.ca brand mark and explicit icon metadata.
- Product Page production hydration now keeps `API_BASE_URL` as a server-only required boundary while browser imports resolve through same-origin routes. This removes the `API_BASE_URL is required in production` client exception without weakening Production server configuration; `northstar-quiet-headphones` renders and hydrates with an empty error/warning console in the clean in-app browser.
- Missing Product routes now retain the correct HTTP 404 and `noindex` semantics while rendering a branded, responsive recovery state with plain-language explanation, Product search, current-deals navigation, and Wishlist access instead of the generic framework page.
- The owner-provided Amazon handoff and aligned Offer-editor revision builds with zero backend warnings/errors, passes 89 domain tests, 157 isolated PostgreSQL integrations plus 3 short-link resolver contract tests (160 API tests total), and 90 frontend tests, and passes the optimized Next.js production build. Link intake now resolves bounded `amzn.to` redirect headers, rejects non-Canadian destinations and untagged long Amazon URLs, autofills URL-contained Product identity, detects an existing offer, stages a reviewed image during first Product creation, and leaves unknown price/image retrieval behind the approved Creators API/content-rights gate. The admin navigation consolidates Brand and Category maintenance under Catalog, while inline Brand creation keeps routine offer entry in one flow and avoids silently assigning the first enabled Brand.
- Homepage Category/Store quick filters, active chips, Clear, Latest, Best savings, Lowest price, and search relevance now share one client discovery state and update through the same-site API. They replace cards/count in place, reset stale pagination, synchronize the URL through History API without a route reload, restore browser history, and retain the previous results on failure. Scroll is restored after the updated layout commits and remains at the nearest valid coordinate when a shorter result set cannot retain the former absolute offset. Browser validation confirmed filter apply and Clear without console warnings/errors, complete control/URL/result synchronization, and no top-of-page reload; all 93 frontend tests and the optimized Next.js production build pass.
- Confirmed Brand-candidate intake now keeps link validation read-only, selects an enabled exact catalog match, or fills an editable low-confidence candidate for owner confirmation. Offer save atomically reuses, creates, or reactivates the normalized Brand with Product/listing/audit persistence; existing Products retain their canonical Brand. `Brands.NormalizedKey` and its unique index prevent case, punctuation, whitespace, and trademark variants from becoming duplicate identities. The migration applied cleanly to the local and isolated PostgreSQL databases and was current on its second local startup; 93 domain tests, 161 PostgreSQL integrations, 91 frontend tests, the zero-warning backend build, and the optimized Next.js build pass.
- Store-led revision validation passed with 0 build warnings/errors: 72 domain, 138 isolated PostgreSQL integration, and 53 frontend tests passed with zero skips. Twenty-two unchanged Playwright scenarios passed in the final full-suite run and the sole selector-only correction then passed its targeted rerun, closing all 23 scenarios. Manual browser inspection confirmed four desktop card columns, two mobile columns, no horizontal overflow, working category navigation, and the clean four-store banner rail.
- A separate clean PostgreSQL 17 database applied all eight migrations through `AddPersistentDataProtectionKeys`; a second `--migrate-only` execution was current, `DataProtectionKeys` and `pg_trgm` were present, and all 91 integrations passed without skips.
- New restart integration coverage confirmed persisted Data Protection keeps an authenticated cookie and Identity confirmation token valid after API host replacement.
- API, worker, and web production images built successfully. In a Docker network matching the private `api` service name, web `/`, web `/healthz`, and web-routed `/api/v1/deals` returned 200; all runtime containers used non-root accounts.
- `dotnet list package --vulnerable --include-transitive` and `pnpm audit` found no known vulnerabilities. Docker Scout image scanning remains blocked by its required Docker Hub login.

## Production-readiness limitations

- The working implementation is not committed or pushed. `main` and `origin/main` still reference `feba527`, so GitHub does not yet contain the project state summarized here.
- The default frontend test command is not reliably green even though the isolated test and serial full suite pass. Release review should require repeated success under the repository's normal command.
- The latest owner decision to remove the standalone admin Brands tab has not been reconciled with the current `AdminPanel.tsx`, MVP wording, or UX admin documentation.
- `DEPLOYMENT PREPARED, OPERATIONAL VALIDATION BLOCKED`: no DigitalOcean credential/project, managed cluster, canonical production domain/DNS ownership, Resend API key, verified sender, webhook secret, or controlled mailbox was available. DNS/provider acceptance, provisioning, and controlled real-mail smoke tests remain a release gate.
- Affiliate provider operation is blocked by missing Canada Deals publisher approvals, credentials, provider identifiers, approved media property/deeplink evidence, live merchant listings, and controlled tracking-link smoke evidence.
- Rakuten live discovery/import is blocked: no securely configured Publisher Account ID, rotated Client Secret, approved advertiser partnership, merchant-specific data rights, or controlled production evidence is available. A credential pasted into chat is treated as compromised and was not used or stored.
- Password recovery and MFA are not implemented.
- No merchant-specific production connector or live affiliate relationship is configured.

## Next vertical slice

Recommended immediate engineering follow-up: stabilize the default frontend Clear-filters test and reconcile removal of the standalone admin Brands tab while preserving Offer-time Brand matching/creation. Then run the complete default frontend and full-stack Playwright suites and create a reviewed commit. After that, proceed to the **Owner Security + Operational Checkpoint**, followed by the **Rakuten Merchant Approval + Data Rights Checkpoint**. Bootstrap the intended owner account with a newly rotated password, manually smoke-test `/admin_panel`, and decide whether MFA/step-up authentication is required before production use. Provider credentials belong only in an approved secret store, and merchant affiliate/catalog rights must be verified separately before enabling live writes.

## Recorded future proposal

- Google sign-in for end-user Wishlist accounts is recorded in `docs/architecture/adr/ADR-013-google-user-sso-proposal.md` and UX-026 as **P2 / PROPOSED — NOT APPROVED FOR IMPLEMENTATION**. It retains email/password, uses the existing internal user and Wishlist identity, requires verified Google identity plus explicit secure linking for an existing local email, and excludes `/admin_panel` and `OwnerAdmin`. It must pass Product, privacy, Human Architecture / Security, proxy, desktop, Android, and iOS checkpoints before implementation or activation. This proposal does not change the current next checkpoint.
