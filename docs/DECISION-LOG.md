# Decision Log

Only record decisions that have actually been approved. Proposed options and research findings belong in the relevant research documents until a checkpoint approves them.

## Current decisions

### DEC-000 - Project repository

- **Status:** Confirmed
- **Decision:** Use the `luisroza/canada-deals` GitHub repository for the Canada Deals project.
- **Scope:** Repository context only; no application technology has been selected.
- **Date:** 2026-08-11

### DEC-001 - Initial development phase

- **Status:** Confirmed by project bootstrap
- **Decision:** Establish the repository foundation and complete Product Owner / Canadian market research before UX, architecture, integrations, or application code.
- **Rationale:** Major product and technical decisions should be evidence-based and reviewed before implementation.
- **Date:** 2026-08-11

### DEC-002 - Product research draft completed

- **Status:** Draft; not approved
- **Decision:** Complete the Product Owner / Canadian market research and produce a proposed product definition, MVP, roadmap, and backlog before moving to UX.
- **Proposed direction:** A Canadian price-truth layer for planned online purchases, initially focused on electronics plus home improvement/tools.
- **Important constraint:** This is a recommendation for the Human Product Checkpoint, not an approved product or technology decision.
- **Evidence:** `docs/product/PRODUCT-RESEARCH.md`, `docs/product/PRODUCT.md`, `docs/product/MVP.md`, `docs/product/ROADMAP.md`, and `docs/product/PRODUCT-BACKLOG.md`.
- **Date:** 2026-08-11

### DEC-003 - Separate repository governance from specialized role instructions

- **Status:** Confirmed by governance refactor
- **Decision:** Keep concise repository-wide behavior rules in the root `AGENTS.md`; keep the detailed Product Owner / Market Research specification in `agents/product-owner.md`; use specialized role files or clearly marked placeholders for other agents.
- **Rationale:** Every agent needs the global workflow and safety rules, while role-specific research and implementation requirements should remain scoped to the responsible agent.
- **Constraints:** Preserve human checkpoints, avoid technology selection, and do not begin product research or application implementation as part of this refactor.
- **Date:** 2026-08-11

### DEC-004 - Product discovery recommendations

- **Status:** Approved at Human Product Checkpoint; technical validation pending
- **Recommendation:** Validate a Canadian price-truth workflow for planned online purchases, initially focused on electronics plus home improvement/tools.
- **Proposed audience:** Canadian shoppers planning meaningful online purchases, with expert deal hunters as a secondary tester group.
- **Proposed MVP retailers:** Amazon.ca, Best Buy Canada, and Home Depot Canada; Walmart Canada is the fallback candidate. All require Data/Affiliate validation.
- **Approved monetization direction:** approved affiliate links with transparent disclosure; affiliate commission must not silently influence organic Deal Quality or ranking.
- **Approved retention direction:** saved products and target-price email alerts. Weekly digest is deferred to P2 and remains on the roadmap.
- **Important constraint:** Retailer priorities are product-level only and require Data/Affiliate validation. No frontend, backend, database, cloud, hosting, search, or integration technology has been selected.
- **Evidence:** `docs/product/PRODUCT-RESEARCH.md`, `docs/product/PRODUCT.md`, `docs/product/MVP.md`, `docs/product/ROADMAP.md`, and `docs/product/PRODUCT-BACKLOG.md`.
- **Date:** 2026-08-11

### DEC-005 - Human Product Checkpoint approved

- **Status:** Confirmed
- **Decision:** Approve the Product Owner direction and advance the project to UX / Product Design.
- **Approved positioning:** Canada Deals is a Canadian price-truth layer for planned online purchases.
- **Approved audience and wedge:** Canadian planned-purchase shoppers, initially electronics plus home improvement/tools; expert deal hunters are a secondary audience.
- **Approved UX requirements:** evidence before enthusiasm, visible freshness, honest unknown states, safe same-product comparison, conservative historical-price claims, English-first responsive web, and transparent affiliate disclosure.
- **Approved MVP retention:** Save Product and Target-Price Alert are P1. Weekly digest moves from P1 to P2 and remains deferred, not rejected.
- **Approved exclusions:** community, AI shopping agent, native app, cashback/rewards, complex personalization, paid ranking, mass programmatic SEO, French-complete launch, push notifications, and browser extension remain outside MVP.
- **Retailer direction:** Amazon.ca, Best Buy Canada, and Home Depot Canada are approved product priorities requiring Data/Affiliate validation; Walmart Canada remains a fallback candidate. No technical integrations are approved.
- **Date:** 2026-08-11

### DEC-006 - UX / Product Design phase completed

- **Status:** Confirmed phase completion; superseded by DEC-007 Human UX approval
- **Decision:** Produce the UX source of truth, responsive wireframes, design system proposal, UX backlog, and live research synthesis against the approved product direction.
- **UX direction:** Keep price-truth, evidence, freshness, safe same-product comparison, honest unknown states, transparent affiliate disclosure, and accessibility first-class across desktop and mobile.
- **Scope:** P0 core discovery/verification/comparison flow; P1 Save Product and Target-Price Alert; weekly digest remains P2.
- **Constraint:** No technology, architecture, retailer integration, backend, frontend, hosting, database, authentication, or deployment decision is made by this phase.
- **Next checkpoint:** Superseded by DEC-007; proceed to Solution / Cloud Architecture and Data/Affiliate Integration Architecture planning.
- **Evidence:** `docs/ux/UX-RESEARCH.md`, `docs/ux/UX-DESIGN.md`, `docs/ux/WIREFRAMES.md`, `docs/ux/DESIGN-SYSTEM.md`, and `docs/ux/UX-BACKLOG.md`.
- **Date:** 2026-08-11

### DEC-007 - Human UX Checkpoint approved

- **Status:** Confirmed
- **Decision:** Approve the UX baseline and the documented Human UX Checkpoint refinements, and make the repository ready for coordinated Solution/Cloud Architecture and Data/Affiliate Integration Architecture planning.
- **Approved UX direction:** Search-first homepage; “Deals with strong evidence” evidence language; evidence-led Deal Card; decision-oriented Product Page; visible freshness; human-readable evidence and product-match states; safe same-product comparison; Reliable/Partial/Unavailable price-history states; expanded textual history interpretation; calm, trustworthy, restrained visual language; responsive mobile-first behavior; and WCAG 2.2 AA direction.
- **Approved interaction refinements:** “Most recently checked” is the initial deal-feed sort; mobile Product Page sticky CTA contains only the primary retailer handoff after the original CTA leaves the viewport; Save Product and Target Price remain normal page actions; affiliate disclosure remains adjacent to retailer CTAs with final legal wording subject to later review.
- **Approved retention:** Save Product and Target Price Alert remain P1; Weekly Digest remains P2 and is not part of the MVP retention requirement.
- **Validation sequencing:** UX user testing remains required before final MVP UX freeze and broader release, with 5–8 representative Canadian shoppers, but it does not block Solution Architecture or Data/Affiliate Architecture planning.
- **Constraint:** No application architecture or implementation technology was selected. No frontend, backend, database, cloud, hosting, authentication, QA, security, or integration implementation is authorized by this decision.
- **Next checkpoint:** Human Architecture / Data Integration Reconciliation Checkpoint before application implementation.
- **Evidence:** `docs/ux/UX-DESIGN.md`, `docs/ux/WIREFRAMES.md`, `docs/ux/DESIGN-SYSTEM.md`, `docs/ux/UX-BACKLOG.md`, and `docs/ux/UX-RESEARCH.md`.
- **Date:** 2026-08-11

### DEC-008 - Proposed Solution / Cloud / FinOps architecture

- **Status:** Superseded by DEC-010; approved with checkpoint refinements
- **Recommendation:** Use a single-repository modular monolith with Next.js + React + TypeScript for the public web, ASP.NET Core for the REST/domain API, PostgreSQL as the system of record, PostgreSQL search for MVP, Hangfire with PostgreSQL storage for durable jobs, and separate web/worker runtime components.
- **Hosting recommendation:** DigitalOcean App Platform and managed PostgreSQL in Toronto, with Cloudflare Free as an optional edge baseline and Azure Canada Central as a future growth/fallback path.
- **Cost direction:** Plan approximately $32-$62 USD/month without optional Spaces, or $37-$67 USD/month with Spaces, before tax, domain, overage, email/legal/affiliate costs; the CAD conversion is explicitly a planning assumption and must be rechecked before provisioning.
- **Constraints:** No infrastructure, application code, database migration, deployment pipeline, or hosting resource is approved by this proposal.
- **Evidence:** `docs/architecture/ARCHITECTURE.md`, `docs/architecture/COST-MODEL.md`, and `docs/architecture/adr/ADR-001` through `ADR-009`.
- **Date:** 2026-08-11

### DEC-009 - Proposed Data / Affiliate Integration architecture

- **Status:** Superseded by DEC-010; approved with checkpoint refinements
- **Recommendation:** Use a source-neutral ingestion contract with field-level merchant policy, API/feed-first acquisition, deterministic matching, idempotent retries, adaptive freshness tiers, bounded policy-compliant history, and internal allowlisted affiliate redirects.
- **Retailer direction:** Best Buy Canada and Home Depot Canada are conditional first integration targets; Amazon.ca is gated by Associates/PA API policy and legal review; Walmart Canada is a fallback/Phase 2 candidate pending Rakuten partnership and feed rights.
- **Affiliate principle:** commission, conversion, and network data remain separate from organic Deal Quality and ranking.
- **Constraints:** No connector, crawler, API credential, raw-feed archive, product migration, or affiliate implementation is approved by this proposal.
- **Evidence:** `docs/integrations/DATA-INTEGRATIONS.md`, `docs/integrations/AFFILIATE-NETWORKS.md`, `docs/integrations/MERCHANTS.md`, `docs/integrations/DATA-MODEL.md`, `docs/integrations/INTEGRATION-BACKLOG.md`, and `docs/architecture/ARCHITECTURE-DATA-RECONCILIATION.md`.
- **Date:** 2026-08-11

### DEC-010 - Human Architecture / Data Integration Checkpoint approved

- **Status:** Confirmed
- **Decision:** Approve the coordinated application architecture and data/integration refinements, and authorize the application foundation plus the first connector-neutral, fixture-backed vertical slice.
- **Approved stack:** Next.js + React + TypeScript; ASP.NET Core REST API; PostgreSQL; modular monolith; monorepo; PostgreSQL full-text search + `pg_trgm`; Hangfire with PostgreSQL storage; DigitalOcean App Platform and managed PostgreSQL in Toronto subject to final account/pricing/privacy checks; Cloudflare baseline; ASP.NET Core Identity; and an application email abstraction with Resend as the initial provider proposal.
- **Approved corrections:** same-site browser/API topology (`/`, `/api/*`, `/go/*`); expanded `RetailerListing` contract; structured variant attributes preserving source values; deterministic-first matching; explicit match states; tri-state MerchantPolicy where `UNKNOWN` blocks protected publication/retention; policy-controlled bounded history; no retailer image caching by default; safe internal affiliate handoff; and strict separation of affiliate economics from organic ranking/evidence.
- **Launch constraint:** two high-quality approved retailers are sufficient for launch. Best Buy Canada and Home Depot Canada are conditional targets. Amazon.ca is gated and not on the critical implementation path. Walmart Canada is fallback/Phase 2.
- **Connector gate:** no production Best Buy, Home Depot, Amazon, Walmart, or equivalent merchant connector may be implemented until program approval, data source/fields, retention/history/image rights, refresh/quota, deep-link behavior, attribution, and restrictions are verified in repository documentation.
- **Scope:** connector-neutral domain contracts, local PostgreSQL, migrations, synthetic fixtures, test adapters, REST API, Next.js discovery/product flow, worker foundation, and automated tests are authorized. Full authentication, alerts, production email, admin, live connectors, advanced history, and broad search remain future slices.
- **Evidence:** `docs/architecture/ARCHITECTURE.md`, approved ADRs `ADR-001` through `ADR-009`, `docs/architecture/ARCHITECTURE-DATA-RECONCILIATION.md`, and approved integration documents.
- **Date:** 2026-08-11

### DEC-011 - Target Price Alert delivery boundary

- **Status:** Implemented and validated in Vertical Slice 4
- **Decision:** Store one canonical Product Target Price Alert per user/Product, require confirmed email plus explicit alert-only consent for ACTIVE state, and keep alert configuration separate from notification delivery intent.
- **Eligibility:** Only current CAD observations that are policy-permitted, available, safely matched, valid, and fresh may qualify at or below the user target. History, Deal Quality, saves/popularity, and affiliate commission are not inputs.
- **Deduplication:** Use target version plus observation identity and a continuous-below-target cycle; target change or a rise-above/fall-below transition may create a new cycle.
- **Delivery:** Use a provider-neutral persisted boundary. Development/Test records controlled capture. Production records suppression with `PRODUCTION_EMAIL_PROVIDER_NOT_CONFIGURED`; it never claims `SENT`.
- **Production status:** `PRODUCTION EMAIL DELIVERY NOT YET CONFIGURED`. Production account-confirmation delivery remains unchanged and gated.
- **Evidence:** `docs/backend/JOBS.md`, `docs/integrations/DATA-MODEL.md`, and `docs/qa/SLICE-4-TEST-REPORT.md`.
- **Date:** 2026-08-11

### DEC-012 - Product-level history evidence rule

- **Status:** Implemented and validated in Vertical Slice 6
- **Decision:** Public Product history uses the lowest qualifying safely matched, policy-permitted new-product CAD observation per UTC day across the canonical Product's non-marketplace listings. Missing days are not generated or interpolated.
- **Coverage:** Fewer than two observed days is `UNAVAILABLE`. `RELIABLE` requires at least 6 days spanning 21 days with no gap over 10 days for 30d, or at least 10 days spanning 60 days with no gap over 21 days for 90d. Other usable coverage is `PARTIAL`.
- **Boundary:** Current freshness remains separate from historical coverage. `Tracking since` is the earliest retained qualifying observation. Denied/`UNKNOWN` history policy, unsafe variants, non-new condition, marketplace sellers, future/non-positive observations, and non-CAD values are excluded.
- **Evidence:** `docs/backend/BACKEND.md`, `docs/backend/DATABASE.md`, and `docs/qa/SLICE-6-TEST-REPORT.md`.
- **Date:** 2026-08-12

### DEC-013 - Production transactional email delivery boundary

- **Status:** Implemented and deterministically validated in Vertical Slice 7; live provider operation blocked pending credentials/DNS
- **Decision:** Keep application/domain behavior provider-neutral through `ITransactionalEmailSender`; use a Resend HTTPS adapter as the initial production implementation and a persisted, network-free capture adapter in Development/Test.
- **Account confirmation:** Use the supported ASP.NET Core Identity email-confirmation token provider with an explicit 24-hour default lifetime. Encode tokens with base64url, perform confirmation through a CSRF-protected POST, and build links only from configured `Email:PublicOrigin`, never request Host headers.
- **Delivery truth:** Distinguish durable intent, Development capture, provider acceptance, delivered, transient/permanent failure, suppressed, bounced, and complained. A successful send API response is only `ProviderAccepted`; `Delivered` requires a verified webhook.
- **Idempotency and retries:** Derive each Resend idempotency key from the immutable delivery ID, commit attempts before external calls, preserve the key across retries, honor `429 Retry-After`, bound retries, and stop ambiguous retries before the provider's 24-hour deduplication window expires.
- **Webhooks:** Verify the raw-body Svix signature and timestamp, deduplicate by provider event ID, correlate by provider message ID, and apply timestamp-aware monotonic transitions. Bounce, complaint, and provider suppression create an application suppression record.
- **Privacy/scope:** HTML and text transactional templates contain no marketing, open/click tracking, remote images, Weekly Digest, password recovery, MFA, retailer connector, or new Product behavior.
- **Production status:** `PRODUCTION EMAIL PROVIDER IMPLEMENTED — OPERATIONAL VALIDATION BLOCKED` until a verified sender domain/address, production API key, webhook signing secret, canonical origin, and controlled provider smoke evidence exist.
- **Evidence:** `docs/backend/EMAIL.md` and `docs/qa/SLICE-7-TEST-REPORT.md`.
- **Date:** 2026-08-12

### DEC-014 - Production deployment preparation boundary

- **Status:** Implemented and locally validated in Vertical Slice 8; external operation blocked pending authorized inputs
- **Decision:** Prepare the approved DigitalOcean App Platform direction as a declarative Toronto deployment topology: separate Next.js web and ASP.NET API services, one Hangfire worker, a managed PostgreSQL binding, and an idempotent EF Core `PRE_DEPLOY` migration job. Keep the same-site `/api/*` and `/go/*` ingress boundaries.
- **Data Protection:** Persist the ASP.NET Core Data Protection key ring in PostgreSQL with a fixed application name. Production startup requires a private-key PFX so key rows are encrypted at rest and container replacement does not invalidate active cookies or confirmation tokens.
- **Operational safety:** Run all application containers as non-root, require PostgreSQL TLS verification in Production, use security headers/forwarded-header handling, keep email emergency-stop enabled in the template, and provide smoke/preflight/runbook scripts.
- **Scope:** This does not provision infrastructure, DNS, database, email, or provider resources and does not add Product functionality or merchant connectors.
- **Blocked status:** `DEPLOYMENT PREPARED, OPERATIONAL VALIDATION BLOCKED` until validated source is published and DigitalOcean, canonical domain, managed PostgreSQL, Resend sender/webhook, Data Protection certificate, and controlled-mailbox access are supplied.
- **Evidence:** `docs/operations/DEPLOYMENT.md`, `docs/operations/PRODUCTION-RUNBOOK.md`, and `docs/qa/SLICE-8-TEST-REPORT.md`.
- **Date:** 2026-08-12

### DEC-015 - Affiliate link provider activation boundary

- **Status:** Implemented and deterministically validated in Vertical Slice 9; merchant operation blocked pending publisher approvals and credentials
- **Decision:** Keep `/go/{listingId}` as the only public retailer handoff and resolve only ACTIVE, persisted, non-expired `AffiliateLink` records associated with an ACTIVE `AffiliateProgram`. Generate/revalidate links outside the shopper path through provider-neutral `IAffiliateLinkProvider` adapters.
- **Initial providers:** Implement Impact first for a future approved Best Buy Canada relationship and CJ second for a future joined Home Depot Canada relationship. Impact validates active contract, deeplink permission/domains, media property, and returned TrackingURL. CJ validates PAT-authenticated Link Search XML, joined relationship, per-link deep-link permission, exact destination, and provider-returned `clickUrl`.
- **Security:** Require HTTPS/no-userinfo URLs, program-specific merchant and tracking-domain allowlists, exact persisted destination, non-PII Sub IDs/click events, server-only credentials, and fail-closed unknown/suspended relationships. Temporary provider outages do not break an already valid persisted link.
- **Commercial neutrality:** Commission, EPC, conversion, and other provider economics are not Product truth, Search, Deal Quality, comparison, history, or Target Price Alert inputs.
- **Gates:** Best Buy and Home Depot are not live without actual Canada Deals approval, credentials, provider identifiers, media property/deeplink evidence, and a controlled link test. Amazon Creators and Walmart Canada remain gated with no adapter. Affiliate approval does not authorize catalog/price ingestion.
- **Evidence:** `docs/operations/AFFILIATE-ACTIVATION.md`, `docs/integrations/AFFILIATE-NETWORKS.md`, and `docs/qa/SLICE-9-TEST-REPORT.md`.
- **Date:** 2026-08-12

### DEC-016 - Rakuten connector activation boundary

- **Status:** Implemented and deterministically validated in Vertical Slice 9; live operation blocked pending secure credentials, merchant approval, and data rights
- **Decision:** Add Rakuten behind the existing provider-neutral affiliate boundary and source-neutral ingestion model. OAuth uses Client ID + Client Secret as the token-key and the Publisher Account ID as `scope`; access/refresh tokens remain memory-only, are reused until the configured expiry skew, and are protected from concurrent refresh stampedes.
- **Discovery first:** Partnerships and Advertisers are read before any merchant activation. Correlated capability snapshots record advertiser/partnership state, Canada relevance, Product Feed and deep-link capability, while unknown/inactive states disable live behavior.
- **Affiliate gate:** Deep links require an ACTIVE advertiser and partnership, explicit operator enablement, Canada relevance, MerchantPolicy permission, and exact destination/tracking host allowlists. Generated links are persisted and validated before `/go` can use them.
- **Catalog gate:** Product Search is MID-scoped, bounded, XML-safe, and dry-run-first. Live persistence additionally requires explicit catalog enablement and policy permission for metadata and price storage. Only CAD is stored; new canonical Products are never created from weak/title-only matches, image content is not cached, and seller/condition/availability are not fabricated.
- **Commercial neutrality:** Rakuten offer/commission fields do not affect Search, ranking, evidence, price history, comparison, or Target Price Alert eligibility.
- **Operational status:** Rakuten remains disabled in default and deployment configuration. No credential pasted into chat was used or stored. Live validation is blocked until the credential is rotated again and the Publisher Account ID, approved partnership, merchant rights, and controlled evidence are supplied securely.
- **Evidence:** `docs/operations/RAKUTEN.md`, `docs/integrations/AFFILIATE-NETWORKS.md`, and `docs/qa/SLICE-9-TEST-REPORT.md`.
- **Date:** 2026-08-14

### DEC-017 - Competitive product recommendation boundary

- **Status:** P0 transparency refinement implemented; growth experiments deferred by phase gates
- **Decision:** Strengthen the approved evidence-to-click experience with a standardized Offer Conditions panel. Discovery cards expose online availability. Product offers expose only source-proven seller, condition, availability, region, shipping, and last-check facts; missing coupon, membership/payment eligibility, and retailer offer-expiry evidence is labelled unverified rather than inferred.
- **Trust boundary:** Freshness is an observation timestamp, not a retailer guarantee. Affiliate-link expiry is not represented as offer expiry. Community popularity, commission, clicks, or saves do not become Price Truth, evidence, comparison, or organic-ranking inputs.
- **Deferred experiments:** Saved-search/keyword/brand/category/retailer alerts may proceed only after the canonical Target Price loop is reliable and must default to fresh/strong evidence with frequency controls. Structured confirmations such as price changed, coupon worked, or out of stock require abuse controls and a review queue and may not mutate public truth automatically.
- **Rejected for MVP:** Open comments, votes, reputation, gamification, forums, open publication of community deals, native push, and sponsored/commission-driven organic ranking.
- **Evidence:** `docs/product/MVP.md`, `docs/product/PRODUCT-BACKLOG.md`, `docs/product/ROADMAP.md`, and the implemented API/Product Page contracts.
- **Date:** 2026-08-14
