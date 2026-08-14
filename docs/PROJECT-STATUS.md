# Project Status

## Current phase

Application Implementation — Vertical Slice 9 Rakuten Connector — IMPLEMENTED AND VALIDATED; LIVE MERCHANT ACTIVATION BLOCKED

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

## Approved product direction

- Positioning: Canadian price-truth layer for planned online purchases
- Initial wedge: electronics plus home improvement/tools
- English-first responsive web MVP
- Evidence before enthusiasm, visible freshness, conservative history, and safe same-product comparison
- Save Product and Target Price Alert remain P1; Weekly Digest remains P2
- Two high-quality approved retailers are sufficient for launch; retailer count is not an MVP KPI

## Current implementation

- Backend: .NET 10 modular monolith projects under `src/backend/` for Domain, Infrastructure, API, and Worker.
- Persistence: PostgreSQL/Npgsql through `20260814173414_AddRakutenConnector`; local development uses Docker Compose.
- API: public discovery/product/history/health/report/handoff, PostgreSQL FTS + `pg_trgm` typo fallback, deterministic policy-safe P0 filtering/sorting/pagination, Identity accounts, current-user Saved Products, and idempotent current-user Target Price Alert list/create/update/disable.
- Fixtures: Products A-H cover reliable/partial/unavailable/policy-hidden Product history, stale current price with valid history, unsafe cheaper history, discovery states, and alerts without live retailer sources.
- Frontend: Next.js 16 + React 19 public URL-driven discovery search/filters and `/products/[slug]`, summary-first accessible 30/90-day Product history, an accessible Offer Conditions panel with seller/condition/availability/region/shipping/last-check facts and explicit unknown coupon/eligibility/expiry boundaries, account routes, Product Page Save and Target Price controls, and `/saved` alert management; public Product content remains server-rendered and SEO-visible.
- UX refinement: a sticky global search provides accessible endpoint-backed product/category suggestions; catalog-backed category shortcuts and deterministic feed modes expose recently checked, supported savings, and lowest-price views; mobile navigation exposes Home/Deals/Search/Saved/Account; Deal Cards prioritize product, observed price, retailer, availability, evidence, and freshness; Product Page Save and Target Price actions are colocated after the primary evidence-led retailer offer.
- Reports: controlled reasons, optional bounded plain-text note, `OPEN` default status, non-cascading listing FK, no required PII, and no automatic mutation/suppression of listing truth.
- Identity: normalized email identifier, Identity hashing/token support, confirmed-email policy, explicit 24-hour email-confirmation token provider, base64url confirmation links from a configured canonical origin, secure same-site cookie session, anti-forgery on mutations, generic resend/login behavior, lockout, and register/login/resend rate limit.
- Saved Products: composite `(UserId, ProductId)` persistence, current-session ownership, canonical Product identity, user cascade/Product restrict delete behavior, and no influence on Price Truth, Deal Quality, evidence, freshness, affiliate economics, or organic ranking.
- Target Price Alerts: one canonical Product configuration per user, confirmed-email ACTIVE gate, explicit alert-only consent, CAD target/version lifecycle, fresh/policy-permitted/safely-matched evaluation, and continuous-condition deduplication.
- Delivery: provider-neutral transactional email with a production Resend HTTP adapter; durable account-confirmation and alert deliveries; exact Development/Test HTML/text capture; delivery-derived idempotency keys; bounded persisted retries; provider acceptance versus webhook-confirmed delivery; signed replay-safe lifecycle webhooks; and bounce/complaint/provider suppression.
- Deployment preparation: Docker multi-stage images for web/API/worker, non-root runtime users, health endpoints, production security headers, component-scoped secrets, DigitalOcean App Spec with Toronto services/worker/PRE_DEPLOY migration job/managed PostgreSQL binding, and operations scripts. No cloud resource has been provisioned.
- Data Protection: PostgreSQL-backed shared key ring with explicit application name and PFX encryption required in Production; API restart preserves valid cookies and confirmation tokens.
- Product history: 30/90-day bounded server projection, daily lowest qualifying safe price, explicit `RELIABLE`/`PARTIAL`/`UNAVAILABLE`, truthful tracking/coverage, no interpolation, and current freshness kept independent.
- Tests: 72 domain tests, 138 PostgreSQL/provider integration tests, and 55 frontend tests pass with zero skips against PostgreSQL 17.
- Browser validation: 28 Playwright tests pass against real Next.js/API/PostgreSQL/Worker, including global-search autocomplete, responsive mobile navigation, and a controlled Rakuten search → Product Page → `/go` → persisted safe tracking-link journey without live provider calls.
- Worker: Hangfire PostgreSQL storage, health endpoint, and retry-safe Price Alert evaluation; no merchant ingestion.
- Affiliate handoff: provider-neutral Impact/CJ adapters, persisted program/link/click lifecycle, pre-generated link refresh, HTTPS/domain/deeplink/relationship gates, and a provider-independent `/go/{listingId}` click path. No live credentials or merchant approval is configured.
- Rakuten connector: opt-in OAuth token-key + Publisher Account ID scope, memory-only token cache/refresh with anti-stampede, bounded Advertisers/Partnerships discovery, fail-closed partnership-removal reconciliation, provider capability snapshots, one-link deep-link generation, Product Search XML normalization, dry-run, MID-scoped import audit, UPC revalidation for existing mappings, transition-safe price observations, and MerchantPolicy-gated current-price/history persistence. Cancelled imports close their durable audit before propagating cancellation. It is disabled by default.

## Blocked external integrations

- Best Buy Canada: Impact adapter implemented; `IMPLEMENTED — AWAITING PUBLISHER APPROVAL`. Account, active contract, ProgramId, MediaPartnerPropertyId, credentials, deeplink domains, and controlled live link evidence are absent. Catalog/feed rights remain separate and unresolved.
- Home Depot Canada: CJ adapter implemented; `IMPLEMENTED — AWAITING PUBLISHER APPROVAL`. Publisher account/PAT/PID, joined advertiser relationship, CID/Link ID, and controlled live evidence are absent. Catalog/feed rights remain separate and unresolved.
- Amazon.ca: `GATED`; no adapter. Associates Canada/Creators API eligibility, Partner Tag, and policy review are absent.
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
- UX competitive refinement preserves the Promobit-inspired discovery benefits without importing its promotional/community model: search suggestions, category shortcuts, transparent feed modes, card scan hierarchy, mobile navigation, and contextual Save/Target Price actions are implemented; product images remain blocked pending verified display rights, and coupon UI remains blocked pending verified code/eligibility/expiry evidence.
- A separate clean PostgreSQL 17 database applied all eight migrations through `AddPersistentDataProtectionKeys`; a second `--migrate-only` execution was current, `DataProtectionKeys` and `pg_trgm` were present, and all 91 integrations passed without skips.
- New restart integration coverage confirmed persisted Data Protection keeps an authenticated cookie and Identity confirmation token valid after API host replacement.
- API, worker, and web production images built successfully. In a Docker network matching the private `api` service name, web `/`, web `/healthz`, and web-routed `/api/v1/deals` returned 200; all runtime containers used non-root accounts.
- `dotnet list package --vulnerable --include-transitive` and `pnpm audit` found no known vulnerabilities. Docker Scout image scanning remains blocked by its required Docker Hub login.

## Production-readiness limitations

- `DEPLOYMENT PREPARED, OPERATIONAL VALIDATION BLOCKED`: no DigitalOcean credential/project, managed cluster, canonical production domain/DNS ownership, Resend API key, verified sender, webhook secret, or controlled mailbox was available. DNS/provider acceptance, provisioning, and controlled real-mail smoke tests remain a release gate.
- Affiliate provider operation is blocked by missing Canada Deals publisher approvals, credentials, provider identifiers, approved media property/deeplink evidence, live merchant listings, and controlled tracking-link smoke evidence.
- Rakuten live discovery/import is blocked: no securely configured Publisher Account ID, rotated Client Secret, approved advertiser partnership, merchant-specific data rights, or controlled production evidence is available. A credential pasted into chat is treated as compromised and was not used or stored.
- Password recovery and MFA are not implemented.
- No merchant-specific production connector or live affiliate relationship is configured.

## Next vertical slice

Recommended next checkpoint: **Rakuten Merchant Approval + Data Rights Checkpoint**. Rotate/configure credentials through an approved secret store, verify the Publisher Account ID, run read-only advertiser/partnership discovery, and approve one merchant's affiliate and catalog rights separately before enabling any live write.
