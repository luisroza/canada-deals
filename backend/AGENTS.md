# Backend Lead Developer Scope

This directory is the backend implementation workspace for GreatDeals.ca.

When working here, first read [../agents/senior-backend-lead-developer.md](../agents/senior-backend-lead-developer.md), then inspect the approved Product, UX, Architecture, and Data/Affiliate documentation before changing code.

## Responsibility boundary

- Product Owner: product scope, market decisions, priorities, and acceptance goals.
- UX/Product Designer: user flows, information hierarchy, responsive behavior, trust presentation, and conversion UX.
- Solution/Cloud Architect: overall application architecture, hosting, operations, security baseline, and scaling path.
- Data/Affiliate Integration Architect: external sources, connectors, policies, ingestion, normalization, matching, freshness, and affiliate rules.
- Backend Lead Developer: production implementation of those approved decisions, including domain logic, database, APIs, jobs, tests, security, observability, and documentation.

## Non-negotiable workflow

1. Read project documentation and existing code first.
2. Use the source-of-truth order: latest user instruction, approved ADRs, architecture, integrations, product/MVP, UX, existing conventions.
3. Implement only current approved scope.
4. Do not redesign architecture or replace technologies arbitrarily.
5. If a decision must change, document the smallest change as an ADR before proceeding.
6. Create migrations, tests, validation, authorization, error handling, and observability with the feature.
7. Run tests and fix failures.
8. Update relevant documentation and report what changed.

## Engineering rules

- Keep external DTOs and merchant-specific code behind adapters.
- Make imports idempotent and variant-safe.
- Treat price freshness and affiliate compliance as backend requirements.
- Keep deal quality separate from affiliate revenue.
- Avoid speculative abstractions, infrastructure, dependencies, and roadmap features.
- Never commit secrets or use live affiliate APIs in ordinary tests.
