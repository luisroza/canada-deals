# Vertical Slice 8 test report

Date: 2026-08-12

## Verdict

- Deployment preparation: `IMPLEMENTED AND VALIDATED LOCALLY`
- External status: `DEPLOYMENT PREPARED, OPERATIONAL VALIDATION BLOCKED`
- Product scope: unchanged; no retailer connector, MFA, password recovery, or new Product feature was added.

## Local evidence

| Gate | Result |
| --- | --- |
| App Platform App Spec schema | passed with `doctl 1.164.0`; six intentional operational placeholders remain |
| Domain tests | 64 passed, 0 failed, 0 skipped |
| PostgreSQL API integrations | 91 passed, 0 failed, 0 skipped |
| Frontend component tests | 43 passed, 0 failed |
| Release frontend build | passed |
| Full-stack Playwright | 24 passed, 0 failed; isolated `canadadeals_e2e` database |
| NuGet vulnerability audit | no known vulnerable packages |
| pnpm audit | no known vulnerabilities |
| Docker images | API, worker, and web built successfully |
| Docker local production-shape smoke | web `/`, web `/healthz`, and API through web `/api/v1/deals` returned 200; all containers ran non-root |

## Migration and Data Protection evidence

A clean PostgreSQL 17 database `canadadeals_slice8_validation` applied the eight-migration chain through `20260812143802_AddPersistentDataProtectionKeys`. The second `--migrate-only` execution reported no pending migrations. The `DataProtectionKeys` table and `pg_trgm` extension were present. After least-privilege refinement, a second clean `canadadeals_slice8_migration_boundary` run proved `--migrate-only` succeeds with only the database configuration and does not initialize the email or Data Protection providers.

Two real PostgreSQL integration tests create an authenticated session/confirmation token, dispose the API host, start another host, and prove that the cookie and token remain valid through the shared database key ring. Production startup fails closed when email or Data Protection certificate configuration is absent.

## Container observations

The first runtime image pin attempted (`10.0.10-bookworm-slim`) was not present in the Microsoft container registry even though the corresponding NuGet package version exists. The Dockerfiles pin the verified published images `mcr.microsoft.com/dotnet/sdk:10.0.300` and `mcr.microsoft.com/dotnet/aspnet:10.0.8`; Data Protection’s NuGet package remains at the current non-vulnerable 10.0.10 patch. The Npgsql native Kerberos dependency was added after the initial container migration run reported a missing `libgssapi_krb5.so.2` library.

Docker Scout image-CVE scanning was not run because the installed Scout client requires Docker Hub authentication; no login was attempted.

## External blockers

- `BLOCKED_BY_UNPUBLISHED_VALIDATED_SOURCE`: `origin/main` did not expose the validated source.
- `DIGITALOCEAN_PROVISIONING_BLOCKED_BY_CREDENTIALS`: no DigitalOcean account credential/project access or managed cluster was available.
- `BLOCKED_BY_MISSING_PRODUCTION_DOMAIN`: no canonical production hostname/DNS ownership was available.
- `EMAIL_OPERATIONAL_VALIDATION_BLOCKED_BY_CREDENTIALS`: no Resend key, verified sender domain/address, webhook secret, or controlled mailbox was available.

No cloud resource, billing event, DNS record, provider email, webhook registration, or production database was created or changed.
