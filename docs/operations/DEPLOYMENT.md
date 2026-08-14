# Production deployment preparation

Vertical Slice 8 prepares the approved DigitalOcean App Platform topology in Toronto. It does not provision a cloud account, DNS record, Resend domain, mailbox, database, or sender. The declarative source is [`.do/app.yaml`](../../.do/app.yaml).

## Components and routing

| Component | App Platform role | Size | Purpose |
| --- | --- | --- | --- |
| `web` | service | 1 GiB fixed | Next.js public web and `/healthz` |
| `api` | service | 1 GiB fixed | ASP.NET Core public API, `/go/*`, and `/health` |
| `worker` | worker | 512 MiB | Hangfire evaluation/retry work and liveness `/health` |
| `migrate` | `PRE_DEPLOY` job | 512 MiB while running | Applies EF migrations only; never seeds fixtures |
| `postgres` | managed PostgreSQL | 1 GiB starting tier | System of record, Hangfire, identity, delivery, and Data Protection keys |

Public ingress preserves path prefixes: `/api/*`, `/go/*`, and `/health` go directly to `api`; all other public paths go to `web`. Browser traffic remains same-site. Server-side web calls use the private API URL. No service exposes PostgreSQL publicly.

Secrets are component-scoped: `web` receives only `API_BASE_URL`; `api` receives database, email, Data Protection, and the non-secret affiliate handoff switch; `worker` receives database/email and—only after activation—affiliate provider secrets; and `migrate` receives only the database URL and CA. The migration executable exits before registering email, Identity, Hangfire, Data Protection, or affiliate providers.

The images use multi-stage Docker builds, listen on port `8080`, and run as non-root users. The API and worker include the Kerberos runtime library required by the Npgsql runtime image.

## Required production inputs

Do not replace placeholders or run `doctl apps create` until every item below is available through the deployment secret store:

| Input | App Spec setting | Rule |
| --- | --- | --- |
| Published validated source | GitHub `main` | must contain the reviewed Slice 8 source |
| Canadian production hostname | `domains[0].domain` and sender | canonical HTTPS hostname, not `example.ca` |
| Managed PostgreSQL cluster | `postgres.cluster_name` | Toronto (`tor1`) and application trusted source |
| PostgreSQL CA | `${postgres.CA_CERT}` | used for `VerifyFull` TLS in production |
| Resend sending API key | `Email__ApiKey` | secret, sending access limited to the verified domain |
| Resend sender | `Email__FromAddress` | address on a SPF/DKIM-verified domain/subdomain |
| Resend webhook secret | `Email__WebhookSigningSecret` | secret from the exact production webhook |
| Data Protection PFX and password | `DataProtection__CertificateBase64`, `DataProtection__CertificatePassword` | secret base64 PKCS#12 with private key; rotate deliberately |
| Impact activation, optional | worker `Affiliate__Impact__AccountSid`, `Affiliate__Impact__AuthToken` plus approved program/media IDs in PostgreSQL | enable only after Best Buy accepts Canada Deals and controlled validation passes |
| CJ activation, optional | worker `Affiliate__Cj__PersonalAccessToken` plus approved PID/CID/Link ID in PostgreSQL | enable only after Home Depot relationship is joined and controlled validation passes |
| Rakuten activation, optional | API/worker secret store `Rakuten__ClientId`, `Rakuten__ClientSecret`, `Rakuten__AccountId` plus approved MID capability/policy mapping in PostgreSQL | rotate disclosed credentials; enable discovery first, then affiliate/catalog independently only after the merchant/data-rights checkpoint |

`Email__EmergencyStop` is intentionally `true` in the template. Leave it true through deploy and HTTP smoke checks. Change it to `false` only immediately before the controlled Resend operational validation, then return it to the intended operational setting after evidence is recorded.

`AffiliateHandoff__Enabled`, both provider `Enabled` values, and `Worker__EnqueueAffiliateLinkRefreshJob` are intentionally `false` in the App Spec. Affiliate credentials are not placeholders in the checked-in spec because no approved relationship exists. Add them only to the worker secret store during the human activation checkpoint described in `AFFILIATE-ACTIVATION.md`; the web component never receives them.

`Rakuten__Enabled` and `Worker__EnqueueRakutenCatalogImportJob` are also intentionally `false`. The checked-in App Spec has no Rakuten credential placeholders so credentials cannot be mistaken for deploy-ready values. Follow `RAKUTEN.md`; the web and migration components never receive Rakuten credentials.

## Validation commands

Run from the repository root. The scripts print presence/state only and never print secret values.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy/validate-app-spec.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/deploy/validate-production-config.ps1
```

The first command runs `doctl apps spec validate --schema-only`; the second deliberately fails until the real credentials, domain, and published `origin/main` exist. Once all placeholders are replaced in a protected deployment copy and the preflight passes, use the account owner’s approved DigitalOcean workflow to create/propose the App Platform app. Do not commit a rendered spec containing secrets.

## Data Protection

Authentication cookies and Identity confirmation tokens are protected with a shared application name and a key ring persisted to PostgreSQL. In Production startup fails if persistence or the certificate secret is absent. The key ring is encrypted with the configured PFX before it is stored, allowing API container replacement/restart without invalidating valid cookies or confirmation tokens. Back up the database and retain the PFX securely; losing both makes existing protected payloads unreadable.

## Status

`DEPLOYMENT PREPARED, OPERATIONAL VALIDATION BLOCKED`.

The App Spec passed local provider schema validation again on 2026-08-14 with Rakuten disabled. Provisioning and real email validation remain blocked by missing DigitalOcean credentials, production domain/managed database, and Resend credentials/sender/webhook. Rakuten activation is a separate blocked checkpoint and does not change the Slice 8 deployment status.
