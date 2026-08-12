# Frontend testing

Slice 7 has 43 component tests and 24 real Playwright journeys. New coverage includes confirmation success/already-confirmed/invalid/error states, generic resend, exact captured confirmation-token navigation, sign-in after confirmation, and exact captured alert content from the real worker path.

Validated toolchain (2026-08-12): Node.js 24.14.0 and the repository-declared `pnpm@10.15.0`.

Run component tests and a production build:

```powershell
pnpm --dir apps/web test
pnpm --dir apps/web build
```

The 38-test component/library suite covers all prior trust/report/account/Save/search behavior plus Product-history reliable/partial/unavailable/error/loading rendering, summary-first copy, selected range, chart semantics, textual data equivalent, and no fake unavailable chart.

The 24-test Playwright suite runs against real Next.js, ASP.NET Core, PostgreSQL, and the separately hosted Hangfire worker with no core API interception. It preserves Slices 1-6 and adds actual captured-email confirmation/token navigation, generic resend, sign-in after confirmation, and exact alert content from the real worker. Install Chromium once with `pnpm --dir apps/web exec playwright install chromium`.
