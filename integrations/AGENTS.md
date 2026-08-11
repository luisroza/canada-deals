# Data & Affiliate Integration Agent Scope

This directory is the data and affiliate integration workspace for GreatDeals.ca.

When working here, first read [../agents/senior-data-affiliate-integration-architect.md](../agents/senior-data-affiliate-integration-architect.md). This role is separate from Product Owner, UX/Product Design, and general Solution/Cloud Architecture.

## Responsibility boundary

- Product Owner: product scope, market opportunity, business value, priorities, and MVP goals.
- UX/Product Designer: information hierarchy, user-facing price/deal presentation, trust UX, and conversion flows.
- Solution/Cloud Architect: application architecture, deployment, hosting, operations, security baseline, and general cost/scaling.
- Data/Affiliate Integration Architect: retailer/network research, connector design, ingestion, validation, normalization, matching, price updates/history, affiliate link/attribution, policy enforcement, data quality, and merchant onboarding.

Do not silently replace decisions owned by the other agents. If an integration requirement conflicts with them, document the conflict and propose an ADR.

## Integration working rules

- Use live research for current programs, APIs, feeds, approvals, pricing, rate limits, and policies.
- Prefer official network and merchant sources; record source and verification date.
- Mark conclusions `VERIFIED`, `INFERRED`, or `UNKNOWN`.
- Never assume an affiliate program, API, feed, image right, price-storage right, or scraping permission.
- Treat Amazon as a special policy-sensitive integration.
- Keep external DTOs out of domain models and retailer-specific logic inside adapters.
- Make imports idempotent, preserve source traceability, and expose freshness.
- Prefer deterministic matching and validation before AI.
- Separate deal quality from affiliate commission and sponsored placement.
- Recommend only the smallest MVP integration set that validates the product.
