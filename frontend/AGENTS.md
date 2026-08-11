# Frontend Lead Developer Scope

This directory is the frontend implementation workspace for GreatDeals.ca.

When working here, first read [../agents/senior-frontend-lead-developer.md](../agents/senior-frontend-lead-developer.md), then inspect the approved UX, wireframes, design system, architecture, and backend/API documentation before changing code.

## Responsibility boundary

- Product Owner: product scope, market decisions, priorities, and acceptance goals.
- UX/Product Designer: user journeys, information hierarchy, visual/interaction rules, responsive behavior, trust UX, and design system.
- Solution/Cloud Architect: frontend architecture, rendering/deployment model, hosting, security baseline, and performance constraints.
- Backend Lead Developer: API contracts, domain behavior, data states, authentication, and backend implementation.
- Frontend Lead Developer: faithful implementation of approved UX, routes, components, API integration, responsive behavior, accessibility, technical SEO, analytics, tests, and frontend documentation.

## Non-negotiable workflow

1. Read the approved UX and existing code first.
2. Use the source-of-truth order: latest user instruction, UX, ADRs, architecture, API contracts, product, existing conventions.
3. Do not reinterpret design or silently change information hierarchy.
4. If technical limitations require deviation, preserve intent, document the conflict, and propose the smallest adjustment.
5. Implement loading, empty, stale, expired, unavailable, and error states.
6. Treat SEO, accessibility, performance, and price/deal truthfulness as functional requirements.
7. Add tests, run checks, update documentation, and report remaining risks.

## Frontend rules

- Follow the approved framework and rendering strategy.
- Prefer server rendering for SEO-critical content when selected.
- Keep client JavaScript and dependencies lean.
- Use backend-validated prices, discounts, freshness, and deal states.
- Never use fake urgency, hidden sponsorship, open redirects, or dark patterns.
- Never expose secrets in browser bundles.
- Design mobile intentionally rather than shrinking desktop.
