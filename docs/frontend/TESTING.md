# Frontend testing

Run component tests and a production build:

```powershell
pnpm --dir apps/web test
pnpm --dir apps/web build
```

The component suite covers Deal Card strong evidence, recent freshness, stale/unavailable states, and the rule that a possible variant has no retailer handoff CTA. A focused Playwright path exists in `apps/web/e2e` and requires PostgreSQL, the API, the web server, and a Chromium installation before it can run end to end.
