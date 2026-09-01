# GreatDeals.ca — ChatGPT project evaluation pack

**Snapshot date:** 2026-08-31  
**Purpose:** self-contained evidence for a Product, UX, Architecture, Engineering, QA, Security, and Release assessment.  
**Repository:** `luisroza/canada-deals`  
**Branch:** `main`  
**Local HEAD and `origin/main`:** `feba527ea2e7ecf2fe536c544a02484306de168a`  
**Source state:** local working tree with substantial uncommitted implementation changes.

## Evidence rules for the evaluator

- Treat statements under **Validated now** as observed during the 2026-08-31 refresh.
- Treat older E2E, Lighthouse, deployment, and provider evidence as **last recorded evidence**, not as a fresh rerun.
- Do not infer that a merchant relationship, data right, image right, production credential, cloud resource, or production deployment exists unless explicitly marked verified.
- The latest explicit owner direction takes precedence over older documents. A known conflict involving the admin Brands tab is called out below.
- This file contains no credential or owner password.

## Executive status

GreatDeals.ca is a technically substantial local MVP for Canadian deal discovery. Its current product model is store-led and treats every retailer listing as an independent offer. The public product is centered on Search, Categories, Stores, compact deal cards, canonical Offer Pages, retailer handoff, and a signed-in Wishlist. Public cross-retailer comparison, price tracking/history, target-price alerts, and weekly digests are intentionally removed.

The modular-monolith architecture, PostgreSQL persistence, search, accounts, admin operations, audit, reviewed assets, provider-neutral affiliate handoff, and gated provider integrations are implemented. The project is not production-ready because the current implementation is not committed, one normal frontend test command is not reliably green, the admin Brands-tab requirement is inconsistent, owner-security decisions remain open, and external merchant/provider/production prerequisites are absent.

| Dimension | Current assessment | Evidence level |
|---|---|---|
| Product direction | Coherent and approved for an individual-offer, Wishlist-only MVP | Approved documents plus current implementation |
| Public UX | Implemented and responsive with store-led discovery and concise cards | Code, component tests, prior browser/Lighthouse evidence |
| Backend/data | Implemented modular monolith with current migration chain | Build and PostgreSQL integration tests validated now |
| Accounts/Wishlist | Implemented with listing-level ownership and IDOR boundaries | Domain/integration evidence validated now |
| Admin operations | Broadly implemented, but Brands-tab direction is unresolved | Code/document inspection |
| Affiliate/data connectors | Provider boundaries implemented; live merchant use remains gated | Code and prior provider tests; no live approval inferred |
| QA | Backend green; frontend serial green but normal parallel command unstable | Validated now |
| Security | Strong baseline controls; owner MFA/step-up and operational review remain open | Architecture/docs; no fresh security audit in this refresh |
| Deployment | Prepared declaratively, not provisioned or operationally validated | Last recorded evidence |
| Git/release | Not releasable from GitHub yet because the current implementation is uncommitted | Git inspection validated now |

## Approved product model

- English-first responsive Canadian deals site.
- Initial emphasis on electronics and home improvement/tools.
- Public navigation: Deals, Categories, Stores, Search, Wishlist.
- Public filters: Category and Store only.
- One public card and one Offer Page per retailer listing; no representative-product deduplication.
- Deal price and optional regular price refer to the same retailer listing and require compatible evidence.
- Product cards open the internal `/offers/{listingId}` page. Only **Check retailer price** performs the outbound retailer handoff and opens a protected new tab.
- Signed-in users can save the exact offer to a Wishlist and remove or organize it later.
- Public price history/tracking, target-price alerts, weekly digests, and cross-retailer comparison are not part of the current product.
- Store banners provide a store-first discovery path. No more than four are visible per desktop carousel page, with responsive one/two/four pagination.

## Implemented public experience

- Next.js 16 and React 19 application with server-rendered dynamic routes.
- Sticky global search and mutually exclusive Categories/Stores menus.
- Store carousel backed by eligible persisted banner profiles and owner-controlled ordering.
- In-place Category/Store filtering and sorting with URL synchronization and scroll preservation intent.
- Compact responsive deal cards with reviewed image/fallback, retailer, title, deal price, optional regular price/savings, freshness/evidence cue, Wishlist, and retailer CTA.
- Canonical `/offers/{listingId}` detail page with offer identity, imagery, same-listing pricing, retailer facts, reporting, Wishlist, and outbound action.
- Legacy `/products/{slug}` compatibility/recovery behavior without public comparison.
- Friendly branded unavailable-product state that preserves HTTP 404 and `noindex` semantics.
- Account registration, email confirmation, sign-in, sign-out, and Wishlist route.
- GreatDeals.ca favicon and metadata.
- Public-facing copy avoids exposing internal affiliate terminology.

## Implemented owner administration

- Unlinked, `noindex` `/admin_panel` route protected by the server-side `OwnerAdmin` role.
- Overview, Offers, Catalog, Stores, Banners, Reports, and Audit areas.
- Offer workflow: analyze retailer link, review/autofill safe fields, save draft, review card, and publish.
- Manual Amazon short-link validation follows bounded redirect headers but does not scrape the Product page.
- Link intake can identify store, ASIN/external ID, canonical destination, visible Partner Tag, title suggestion, and Brand candidate when present in the URL or existing catalog.
- Product price, category judgment, model, and image are not invented when the URL does not contain authoritative data. Amazon automatic content requires an approved Creators API/content-rights boundary.
- Offer-time Brand matching or explicitly confirmed creation/reactivation is implemented transactionally.
- Category and Store lifecycle management uses immutable identifiers, reversible activation, impact counts, mandatory reasons for deactivation, and no destructive deletion.
- Offer deal/regular pricing, promotion validity, publication state, reviewed image workflow, and exact destination lifecycle.
- Store-banner selection, order, copy, reviewed artwork library, provenance/rights metadata, preview, and fail-closed publication.
- Customer issue-report review lifecycle and administrative audit trail.

### Known owner-admin requirement conflict

The latest owner direction was to remove a separate **Brands** tab because Brand should be populated or explicitly created during Offer entry. The current `AdminPanel.tsx`, `docs/product/MVP.md`, and `docs/ux/ADMIN-PANEL.md` still expose or describe **Catalog > Brands**. This is not merely a documentation typo: the current UI code contains the tab and full Brand-management component. The project should either implement the latest direction and update tests/docs, or obtain a new explicit owner decision retaining advanced Brand management.

## Architecture and data model

- Monorepo with Next.js web, ASP.NET Core API, ASP.NET Core Worker, Domain, and Infrastructure projects.
- .NET 10 modular monolith organized around vertical slices and shared PostgreSQL transactions.
- PostgreSQL 17 through EF Core/Npgsql; latest migration is `20260831135128_IndividualOfferPricing`.
- PostgreSQL FTS plus `pg_trgm` for search; no dedicated search service at MVP.
- Hangfire on PostgreSQL for durable jobs; no Redis, Kafka, Kubernetes, or speculative microservices.
- ASP.NET Core Identity with secure same-site cookie sessions, confirmed-email policy, anti-forgery on mutations, lockout, rate limits, and IDOR-safe current-user ownership.
- PostgreSQL-backed shared Data Protection key ring for restart-safe sessions/tokens; Production requires protected key encryption configuration.
- `RetailerListing.Id` is the public offer, report, handoff, and Wishlist identity.
- `Product` remains an internal canonical identity for catalog normalization, search, images, source reconciliation, and administration; it no longer merges or compares public offers.
- Same-listing `RegularPriceAmount`, currency, observation/evidence, and validity fields support defensible savings display.
- Provider-neutral `/go/{listingId}` and `/go/store/{retailerKey}` boundaries prevent raw tracking URLs from leaking through normal provider flows. Approved owner-provided Amazon direct links are a separately governed exception.
- Owner-reviewed Product and Store Banner asset libraries validate type/signature/dimensions/size, store immutable bytes and SHA-256 identity, and enforce placement/rights/effective-date gates.

## Integrations and external state

| Integration | Code state | Operational state |
|---|---|---|
| Impact / Best Buy Canada candidate | Adapter and lifecycle implemented | Awaiting publisher approval, program identifiers, credentials, deeplink-domain evidence, and live proof |
| CJ / Home Depot Canada candidate | Adapter and lifecycle implemented | Awaiting publisher approval, PAT/PID/CID/link permissions, and live proof |
| Rakuten | OAuth/cache, advertiser/partnership discovery, links, Product Search normalization, import/audit, reconciliation, and policy gates implemented | Disabled; Canada Deals account/merchant approval, Account ID/rotated secret, rights, and live proof absent |
| Amazon.ca | Owner-provided exact-link and bounded short-link resolution implemented | Partner relationship/tag/evidence required; no Creators API connector; Product content/price/image rights remain gated |
| Walmart Canada | Candidate only | Canada-specific relationship and rights unverified |
| Resend | Transactional email boundary implemented | Production key, verified sender/domain, webhook secret, mailbox, and real-mail smoke evidence absent |
| DigitalOcean Toronto | App Spec and operational preparation recorded | No project, database, domain, DNS, or live deployment provisioned |

No live credentials or merchant rights should be inferred from implementation. A provider relationship and content/catalog rights are separate approvals.

## Validated now — 2026-08-31

| Validation | Result |
|---|---|
| `dotnet build CanadaDeals.slnx --configuration Release --no-restore` | PASS — 0 warnings, 0 errors |
| Domain tests | PASS — 94/94, 0 skipped |
| PostgreSQL integration/provider tests | PASS — 161/161, 0 skipped, dedicated database removed afterward |
| Next.js optimized production build | PASS |
| Default frontend test command | FAIL — 90/91; Clear-filters Category assertion failed |
| Failing frontend test in isolation | PASS — 1/1 |
| Complete frontend suite with one worker | PASS — 91/91 |
| Full-stack Playwright | Not rerun; latest recorded evidence is 26/26 |

The frontend result indicates an order/concurrency-sensitive test or state isolation defect. It must not be hidden by citing only the serial run; the repository's documented default command should pass repeatedly before release.

## Last recorded evidence not rerun in this refresh

- 26 full-stack Playwright journeys against real Next.js, API, PostgreSQL, and Worker.
- Clean Chromium Lighthouse: Desktop `100/100/96/100`, Mobile `98/100/96/100` for Performance/Accessibility/Best Practices/SEO before the latest small UI changes; favicon follow-up was implemented later.
- Docker web/API/worker images and private-service routing built successfully with non-root runtime users.
- DigitalOcean App Spec passed schema validation with intentional deployment placeholders.
- NuGet and pnpm vulnerability audits previously found no known vulnerabilities; Docker Scout remained blocked by login.

These are useful regression evidence but should be rerun after the working tree is stabilized and committed.

## Git and release integrity

Before these status files were added, the local tree contained:

- 70 modified tracked files;
- 2 deleted tracked files (`OfferCard.tsx` and its test, superseded by the individual-offer card flow);
- 7 untracked implementation files, including the canonical Offer route/page, ADR-014, Offers controller, favicon, and IndividualOfferPricing migration;
- 72 tracked files in the diff, with 867 insertions and 821 deletions.

Both local `main` and `origin/main` still reference `feba527`. Therefore GitHub does not contain the implementation described in this pack. A reviewed commit and push are mandatory before any external release review based on repository state.

## Open risks and blockers

### P0 — blocks a trustworthy release candidate

1. Stabilize the normal parallel frontend test command and prove repeated 91/91 runs.
2. Reconcile the standalone admin Brands tab with the latest owner direction.
3. Review the large uncommitted diff, run the complete regression suite, then commit and push it.
4. Complete the Owner Security checkpoint: secure bootstrap/reset procedure, manual admin smoke test, and an explicit MFA/step-up decision.
5. Keep all merchant/provider integrations disabled until account approval, deeplink/data/image rights, identifiers, credentials, and controlled evidence are recorded.

### P1 — blocks production launch but not local product review

1. Provision production infrastructure, database, canonical domain/DNS, and protected Data Protection configuration.
2. Configure Resend with verified sender/domain and signed webhook, then run controlled delivery/bounce/complaint/suppression tests.
3. Run deployment migration, health, rollback, backup/restore, and restart smoke tests in the actual environment.
4. Run a fresh security review, dependency audit, E2E suite, and Lighthouse/accessibility pass against the committed candidate.
5. Define password recovery and decide whether it is required for launch; MFA remains absent.

### P2 — approved only as future consideration

- Google end-user sign-in is documented as a proposal, not approved implementation. It must preserve the internal user/Wishlist identity, use explicit safe account linking, and remain excluded from `/admin_panel` and `OwnerAdmin`.
- Saved searches, community confirmation, and broader personalization remain outside the current MVP unless separately approved.

## Recommended execution order

1. Fix or isolate the Clear-filters frontend state leak under the normal test runner.
2. Apply the latest Brands-tab decision and synchronize MVP, UX, frontend tests, and admin implementation.
3. Run: Release build, 94 domain tests, 161 PostgreSQL tests, 91 frontend tests under the normal command, optimized frontend build, and 26 Playwright journeys.
4. Review the complete diff for secrets, stale compatibility behavior, accidental public copy, and migration safety.
5. Commit and push one reviewed implementation checkpoint.
6. Perform Owner Security + Operational Checkpoint.
7. Obtain and record merchant/program/data/image rights before enabling one live provider.
8. Provision production and execute the operational validation runbook.

## Questions the external evaluator should answer

1. Is the individual-offer, Category/Store discovery, and Wishlist-only model coherent enough for a focused Canadian MVP?
2. Which implemented capabilities are unnecessary complexity for launch and can remain disabled or be removed safely?
3. Does the current admin workflow expose the minimum safe operations for a solo owner without bypassing rights and publication gates?
4. Are the Identity, cookie, CSRF, rate-limit, IDOR, audit, Data Protection, and redirect controls sufficient for the next checkpoint?
5. Which P0 risks must be closed before committing the current working tree?
6. What is the smallest defensible production launch plan with one or two approved retailers?
7. Which documents or code claims conflict with the latest product direction?

## Primary source documents

- `docs/PROJECT-STATUS.md`
- `docs/product/MVP.md`
- `docs/architecture/ARCHITECTURE.md`
- `docs/architecture/adr/ADR-014-individual-offer-pricing-and-wishlist.md`
- `docs/backend/API.md`
- `docs/backend/DATABASE.md`
- `docs/backend/TESTING.md`
- `docs/frontend/FRONTEND.md`
- `docs/frontend/TESTING.md`
- `docs/integrations/DATA-INTEGRATIONS.md`
- `docs/integrations/AFFILIATE-NETWORKS.md`
- `docs/ux/UX-DESIGN.md`
- `docs/ux/ADMIN-PANEL.md`
- `docs/operations/PRODUCTION-RUNBOOK.md`
- `docs/architecture/adr/ADR-013-google-user-sso-proposal.md`
