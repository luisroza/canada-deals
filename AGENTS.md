# GreatDeals.ca Repository Governance

## Project workflow

The project progresses through these stages:

1. Product Owner / Market Research
2. Human Product Checkpoint
3. UX / Product Design
4. Solution/Cloud Architecture and Data/Affiliate Architecture
5. Human Architecture Checkpoint
6. Backend and Frontend implementation through vertical slices
7. QA / Test Automation
8. Security Review
9. Release Review

Agents must not silently skip checkpoints or continue past a required human approval.

## Source of truth

Read the relevant project documentation before modifying code or documentation. Once approved, use:

- `docs/product/MVP.md` for approved product scope.
- `docs/architecture/ARCHITECTURE.md` for approved architecture.
- Approved ADRs for architectural decisions.
- UX documents for the approved product experience.
- Integration documents for affiliate, retailer, and data restrictions.
- `docs/PROJECT-STATUS.md` for the current phase and checkpoint state.

Latest explicit user instructions take precedence. Proposals, research findings, and assumptions must not be presented as approved decisions.

## Scope discipline

- Work only within the assigned task and directory scope.
- Do not silently expand the MVP or implement future-roadmap functionality.
- Avoid unrelated refactoring.
- Prefer small, reviewable changes.
- Document assumptions, unresolved dependencies, and external blockers.

## Architecture discipline

- Never silently replace an approved technology.
- Do not introduce major infrastructure without a justified workload and cost reason.
- Avoid speculative infrastructure and premature distributed systems.
- Prefer solutions appropriate for a solo developer or small team.
- Record significant architecture changes in an ADR.

## Affiliate and data discipline

- Respect affiliate-network and retailer policies.
- Never assume APIs, feeds, affiliate relationships, image rights, or price-storage rights exist.
- Verify time-sensitive integration information before implementation.
- Scraping is not the default and must not be used where terms prohibit it.
- Treat Amazon-specific rules separately when required.
- Keep affiliate commission separate from organic Deal Quality and ranking.

## Engineering discipline

- Implementation agents must run relevant tests before declaring completion.
- Keep documentation synchronized with implementation.
- Never commit credentials, secrets, or sensitive personal data.
- Preserve security controls and existing valid work.
- Do not ship placeholder production behaviour when real implementation is expected.
- Clearly report tests not run and external blockers.

## Quality principles

Treat these as product and engineering quality requirements:

- Price correctness and product-matching correctness
- Affiliate-link and alert correctness
- Data freshness and source provenance
- User trust and transparent commercial disclosure
- Mobile usability and accessibility
- SEO quality and performance

## Checkpoint rule

Stop when a required human checkpoint is reached. Do not automatically continue from Product Discovery to UX or from Architecture to Implementation when `docs/PROJECT-STATUS.md` requires approval.

Do not create application source code or select a technology stack while the project is still awaiting the relevant product or architecture approval.
