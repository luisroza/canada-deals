# Frontend local development

Install the workspace dependencies and start the Next.js dev server:

```powershell
$env:Path = "C:\path\to\bundled\node\bin;" + $env:Path
pnpm install --dir apps/web
$env:API_BASE_URL = "http://localhost:5099"
$env:NEXT_PUBLIC_API_ORIGIN = "http://localhost:5099"
pnpm --dir apps/web dev
```

Open `http://localhost:3000`. Start the API separately as documented in `docs/backend/LOCAL-DEVELOPMENT.md`.
