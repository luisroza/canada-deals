# Frontend testing

Validated toolchain (2026-08-20): Node.js 24 and the repository-declared `pnpm@10.15.0`.

Run component tests and a production build:

```powershell
pnpm --dir apps/web test
pnpm --dir apps/web build
```

The 61-test component/library suite covers the current store-led, Wishlist-only product plus all retained trust, report, account, search, and safe affiliate behaviors. Store banner coverage includes ACTIVE protected new-tab handoff, DISCOVERY_ONLY internal navigation, missing-asset fallback, disabled state, raw-URL fail-closed behavior, visible commercial disclosure, and supplied neutral ordering. CSP and development-origin coverage prove local private-network HTTP assets remain loadable only for explicitly allowed LAN hosts while Production retains insecure-request upgrading.

The 25-test Playwright suite runs against real Next.js, ASP.NET Core, PostgreSQL, and the separately hosted Hangfire worker with no core API interception. Store banner journeys verify the controlled backend 302 and click record without navigating to an external provider, plus internal store-filtered discovery. The complete suite has zero skips. Install Chromium once with `pnpm --dir apps/web exec playwright install chromium`.
