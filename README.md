# Canada Deals

Canada Deals is a Canadian e-commerce deal-discovery platform intended to help shoppers evaluate prices, historical pricing, retailer alternatives, alerts, and affiliate offers.

## Current status

The Human Product and Human UX Checkpoints are complete. The approved UX baseline and checkpoint refinements are documented, and the repository is ready for coordinated Solution/Cloud Architecture and Data/Affiliate Integration Architecture planning. No application technology or implementation has been selected.

## Development workflow

1. Product Owner / Canadian market research
2. Human Product Checkpoint
3. UX / Product Design
4. Solution/Cloud Architecture and Data/Affiliate Architecture
5. Human Architecture Checkpoint
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

## Important constraints

- The initial market is Canada and prices are primarily CAD.
- Affiliate revenue must never secretly influence deal quality or organic ranking.
- Retailer APIs, feeds, affiliate programs, pricing, and policies must be verified before implementation.
- Scraping is not the default and is prohibited when terms do not allow it.
- Amazon must be reviewed separately because its data and affiliate rules may differ from other merchants.
- Start simple without creating an obvious dead end.
