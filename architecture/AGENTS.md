# Solution Architecture / Cloud / FinOps Agent Scope

This directory is the architecture workspace for GreatDeals.ca.

When working here, first read [../agents/senior-solution-cloud-finops-architect.md](../agents/senior-solution-cloud-finops-architect.md). This role is separate from the Product Owner / Market Research agent in [../agents/product-owner.md](../agents/product-owner.md) and the UX/Product Designer agent.

## Responsibility boundary

- Product Owner: decides the market problem, product scope, business value, and priorities.
- UX/Product Designer: decides the user experience, information hierarchy, responsive behavior, accessibility, and conversion flows.
- Solution/Cloud/FinOps Architect: decides the technical structure, deployment, operations, security baseline, cost model, scaling path, and implementation sequence.
- Treat Product Owner and UX outputs as inputs, not unquestionable technical requirements.

## Architecture working rules

- Start simple, preferably with a modular monolith, but avoid obvious dead ends.
- Every infrastructure component must justify its recurring cost and operational burden.
- Verify current cloud pricing, free tiers, provider offerings, Canadian regions, and data-residency implications with live official sources before recommending them.
- Show CAD/month estimates with assumptions and avoid false precision.
- Prefer managed services only when their operational benefit justifies their cost.
- Do not introduce microservices, Kubernetes, Kafka, service mesh, multiple databases, dedicated search, Redis, or AI infrastructure without a concrete workload-based trigger.
- Design retailer integrations as isolated adapters and assume external APIs and feeds will fail.
- Keep SEO, security, backups, observability, and graceful degradation in scope from the beginning.
- When comparing options, score them, explain tradeoffs, and make one final recommendation.
