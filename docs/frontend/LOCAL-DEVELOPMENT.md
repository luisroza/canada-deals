# Frontend local development

For the complete confirmation journey, run the API with `Email__AutoConfirmDevelopmentAccounts=false`. After registration, retrieve the controlled message through the Development-only capture endpoint or use Playwright, which follows the exact captured link automatically. No external provider is called.

Validated environment (2026-08-11): Next.js 16.3.0, React 19.2.8, Node.js 24.14.0, and `pnpm@10.15.0`.

Install the workspace dependencies and start the Next.js dev server:

```powershell
$env:Path = "C:\path\to\bundled\node\bin;" + $env:Path
pnpm install --dir apps/web
$env:API_BASE_URL = "http://localhost:5099"
$env:API_ORIGIN = "http://localhost:5099"
$env:DEV_ALLOWED_ORIGINS = "<computer-LAN-IP>"
pnpm --dir apps/web dev
```

Open `http://localhost:3000`. Start the API separately as documented in `docs/backend/LOCAL-DEVELOPMENT.md`. The Next.js development rewrites keep `/api/*` and `/go/*` same-site in the browser while forwarding them to the local API. Leave `NEXT_PUBLIC_API_ORIGIN` unset for same-site links; set it only when intentionally using a separate API origin.

To test on a phone, connect it to the same private Wi-Fi, set `DEV_ALLOWED_ORIGINS` to the computer's exact LAN IP before startup, and open `http://<computer-LAN-IP>:3000`. Next.js blocks development scripts, styles, and HMR from unlisted network origins; do not use a wildcard. The development server listens on `0.0.0.0`, and its development CSP deliberately does not upgrade local HTTP assets to HTTPS. Production retains `upgrade-insecure-requests`. If the page is unreachable, permit Node.js on private Windows networks and ensure a VPN is not blocking local-network access. Do not expose the development port to the public internet.

Use `/account/register` to create a local account. In Development/Test only, the API confirms and signs in that account without exposing a token; production does not. Save from a Product Page, revisit it at `/saved`, and use the header Sign out action to validate session boundaries. Do not add browser token storage or call the API on a separate origin for these flows.

The owner administration route is not linked publicly. Configure it from a trusted terminal with the interactive, no-echo command in `docs/operations/OWNER-ADMIN.md`, then open `/admin_panel` directly. Never put the owner password in a command argument, environment variable, repository file, or chat.
