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

- **Status:** Proposed — awaiting Human Architecture / Data Integration Checkpoint
- **Recommendation:** Use a single-repository modular monolith with Next.js + React + TypeScript for the public web, ASP.NET Core for the REST/domain API, PostgreSQL as the system of record, PostgreSQL search for MVP, Hangfire with PostgreSQL storage for durable jobs, and separate web/worker runtime components.
- **Hosting recommendation:** DigitalOcean App Platform and managed PostgreSQL in Toronto, with Cloudflare Free as an optional edge baseline and Azure Canada Central as a future growth/fallback path.
- **Cost direction:** Plan approximately $32-$62 USD/month without optional Spaces, or $37-$67 USD/month with Spaces, before tax, domain, overage, email/legal/affiliate costs; the CAD conversion is explicitly a planning assumption and must be rechecked before provisioning.
- **Constraints:** No infrastructure, application code, database migration, deployment pipeline, or hosting resource is approved by this proposal.
- **Evidence:** `docs/architecture/ARCHITECTURE.md`, `docs/architecture/COST-MODEL.md`, and `docs/architecture/adr/ADR-001` through `ADR-009`.
- **Date:** 2026-08-11

### DEC-009 - Proposed Data / Affiliate Integration architecture

- **Status:** Proposed — awaiting Human Architecture / Data Integration Checkpoint
- **Recommendation:** Use a source-neutral ingestion contract with field-level merchant policy, API/feed-first acquisition, deterministic matching, idempotent retries, adaptive freshness tiers, bounded policy-compliant history, and internal allowlisted affiliate redirects.
- **Retailer direction:** Best Buy Canada and Home Depot Canada are conditional first integration targets; Amazon.ca is gated by Associates/PA API policy and legal review; Walmart Canada is a fallback/Phase 2 candidate pending Rakuten partnership and feed rights.
- **Affiliate principle:** commission, conversion, and network data remain separate from organic Deal Quality and ranking.
- **Constraints:** No connector, crawler, API credential, raw-feed archive, product migration, or affiliate implementation is approved by this proposal.
- **Evidence:** `docs/integrations/DATA-INTEGRATIONS.md`, `docs/integrations/AFFILIATE-NETWORKS.md`, `docs/integrations/MERCHANTS.md`, `docs/integrations/DATA-MODEL.md`, `docs/integrations/INTEGRATION-BACKLOG.md`, and `docs/architecture/ARCHITECTURE-DATA-RECONCILIATION.md`.
- **Date:** 2026-08-11
