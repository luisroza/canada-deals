# Canada Deals

Canada Deals is a Canadian e-commerce deal-discovery platform intended to help shoppers evaluate prices, historical pricing, retailer alternatives, alerts, and affiliate offers.

## Current status

The Human Product, Human UX, and Human Architecture / Data Integration Checkpoints are complete. The current store-led, Wishlist-only experience includes API-backed store banners with original Canada Deals artwork, internal discovery fallback, and a provider-neutral `/go/store/{retailerKey}` handoff for approved destinations. Vertical Slice 9 retains the provider-neutral affiliate boundary and opt-in Rakuten connector. All behavior is deterministically validated with controlled fixtures; no merchant is live, no real Rakuten credential was used, and production activation remains blocked by merchant approval, storefront destination, data-rights, and asset-rights evidence.

## Development workflow

1. Product Owner / Canadian market research
2. Human Product Checkpoint
3. UX / Product Design
4. Solution/Cloud Architecture and Data/Affiliate Architecture
5. Human Architecture / Data Integration Checkpoint
6. Project foundation and vertical-slice implementation
7. QA and test automation
8. Security review
9. Release review

Do not silently skip checkpoints, expand the MVP, or replace approved technologies. Use the specialized agent scopes under `agents/` and the directory-specific instructions under `ux/`, `architecture/`, `integrations/`, `backend/`, `frontend/`, `qa/`, and `security/`.

## Documentation

- Product: `docs/product/`
- UX: `docs/ux/`
- Architecture: `docs/architecture/`
- Integrations: `docs/integrations/`
- Backend: `docs/backend/`
- Frontend: `docs/frontend/`
- QA: `docs/qa/`
- Security: `docs/security/`
- SEO: `docs/seo/`
- Analytics: `docs/analytics/`
- Agent scopes: `agents/` (the detailed Product Owner role is `agents/product-owner.md`; other specialized roles are maintained there or represented by clearly marked placeholders)

The source-of-truth documents are not complete until the relevant phase is approved. Do not present proposals as decisions.

Owner administration is available at the intentionally unlinked `/admin_panel` route after the single owner role is configured interactively. See `docs/operations/OWNER-ADMIN.md`; the hidden path is not treated as a security boundary.

## Important constraints

- The initial market is Canada and prices are primarily CAD.
- Affiliate revenue must never secretly influence deal quality or organic ranking.
- Retailer APIs, feeds, affiliate programs, pricing, and policies must be verified before production connector implementation.
- Fixture-backed, connector-neutral development may proceed before merchant approval.
- Scraping is not the default and is prohibited when terms do not allow it.
- Amazon must be reviewed separately because its data and affiliate rules may differ from other merchants.

## Current deployment status

Vertical Slice 8 is `DEPLOYMENT PREPARED, OPERATIONAL VALIDATION BLOCKED`. Local Docker images, migration, health routes, App Spec schema, tests, and operations procedures are validated; no DigitalOcean/Resend/DNS resource was provisioned. See `docs/operations/DEPLOYMENT.md`, `docs/operations/PRODUCTION-RUNBOOK.md`, and `docs/qa/SLICE-8-TEST-REPORT.md`.
- Start simple without creating an obvious dead end.
