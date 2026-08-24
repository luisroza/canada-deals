# Owner Admin Panel validation

## Result

Implemented and validated locally on 2026-08-24 with controlled data. No administrator password, owner email, merchant credential, tracking URL, or live retailer data was committed.

## Automated evidence

- .NET solution build: succeeded with 0 warnings and 0 errors.
- Domain tests: 87 passed, 0 failed, 0 skipped, including Brand lifecycle and exact optional-validity boundary behavior.
- Owner-admin PostgreSQL coverage includes anonymous `401`, ordinary-account `403`, owner access, CSRF rejection, Brand/Category/Store lifecycle, Product reuse for a second retailer offer, immutable Product slugs, automatic expired-offer exclusion, offer create/public projection/reversible disable, audit persistence, reviewed images, first-party banner update, and report resolution/audit.
- Full PostgreSQL suite on a new isolated database: 155 passed, 0 failed, 0 skipped. The full migration chain applied during startup and a second migration-only execution reported no pending migration.
- Frontend component tests: 86 passed across 24 files, including progressive offer entry, Product reuse, Brands, offer validity, and one-source carousel selection.
- Full-stack Playwright: 26 passed, 0 failed, 0 skipped against real Next.js, API, Worker, and PostgreSQL services.
- Next.js production build: succeeded and includes the dynamic `/admin_panel` route.
- EF migration `20260824212134_AddAdminCatalogWorkflow`: applied successfully after the existing migration chain and was idempotent on reapplication.

## Security and UX checks

- No public navigation link to the route.
- `noindex`, `noarchive`, `nosnippet`, and robots disallow are present but documented as non-security controls.
- Owner role is enforced by backend policy; no client-only authorization.
- Cookie authentication, existing lockout, CSRF, dedicated rate limit, and generic login errors remain in force.
- Bootstrap reads the password interactively without echo or command-line/env persistence.
- Offer disabling removes the listing from discovery, Product detail, and affiliate handoff.
- Brand deactivation and offer expiration remove dependent listings from discovery and handoff without destructive data changes.
- A second store offer attaches to the canonical Product; Product slugs and existing listing identity remain immutable.
- Merchant policy, evidence, history, reference price, and affiliate destination remain fail-closed or derived.
- Desktop/mobile responsive rules and semantic form/table/card adaptations are implemented.

## Remaining follow-ups

- Perform a manual browser pass after bootstrapping a local owner account.
- Add MFA or step-up authentication before production owner operation.
- Consider optimistic concurrency and richer before/after audit diffs if multiple administrators are ever introduced; the current design intentionally allows one owner only.
