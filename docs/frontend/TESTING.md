# Frontend testing

Validated toolchain (2026-08-20): Node.js 24 and the repository-declared `pnpm@10.15.0`.

Run component tests and a production build:

```powershell
pnpm --dir apps/web test
pnpm --dir apps/web build
```

The 71-test component/library suite covers the current store-led, Wishlist-only product plus all retained trust, report, account, search, and safe affiliate behaviors. Wishlist coverage includes one shared session/list load, card-level signed-out return context, save/remove state, synchronized navigation count, local search/sort controls, exclusive error state, and retry. Store banner coverage includes ACTIVE protected new-tab handoff, DISCOVERY_ONLY internal navigation, missing-asset fallback, disabled state, raw-URL fail-closed behavior, commercial-state labeling, all enabled profiles in supplied neutral order, responsive one/two/four-banner pagination, accessible carousel controls, owner selection, public-state distinctions, reviewed artwork selection/upload affordance, and provenance controls. Catalog-menu coverage verifies consistent marker/label structure for short and long store names and mutually exclusive Category/Store expansion. Owner-panel coverage includes independent Category and Store navigation, immutable identifiers, Canadian market presentation, and absence of destructive delete controls. CSP and development-origin coverage prove local private-network HTTP assets remain loadable only for explicitly allowed LAN hosts while Production retains insecure-request upgrading.

The 25-test Playwright suite runs against real Next.js, ASP.NET Core, PostgreSQL, and the separately hosted Hangfire worker with no core API interception. Store banner journeys verify the controlled backend 302 and click record without navigating to an external provider, plus internal store-filtered discovery. The complete suite has zero skips. Install Chromium once with `pnpm --dir apps/web exec playwright install chromium`.
