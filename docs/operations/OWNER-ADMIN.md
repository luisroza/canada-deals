# Owner administrator operations

## Security boundary

`/admin_panel` is intentionally absent from public navigation, but the URL is not a secret and is not an authorization control. Backend routes under `/api/v1/admin/*` require an authenticated ASP.NET Core Identity cookie and the `OwnerAdmin` role. Mutations also require anti-forgery, use the dedicated per-user admin rate limit, validate input and policies, and write an `AdminAuditEvent` in the same database unit of work.

The public registration API cannot grant the role. The panel has no user or role management. The local bootstrap command refuses to configure a second, different owner administrator.

## Bootstrap the owner account

Never put the administrator password in source, `appsettings*.json`, shell history, command arguments, environment files, screenshots, tickets, or chat. Run the interactive command from a trusted local terminal:

```powershell
dotnet run --project src/backend/CanadaDeals.Api -- --bootstrap-owner-admin
```

The command applies pending migrations, asks for the email, then reads and confirms the new password without echoing it. It creates or securely updates that same account, confirms the email, grants the single owner role, and rotates the security stamp so older sessions become invalid. It refuses a different email after an owner has been configured.

## Reset the existing owner password

After the owner account exists, reset its password without entering or changing its email:

```powershell
dotnet run --project src/backend/CanadaDeals.Api -- --reset-owner-admin-password
```

The command requires exactly one configured owner, asks for the new password twice with hidden input, and invalidates existing sessions. It cannot create a second owner or transfer ownership to another email.

Production execution requires the normal production database and Data Protection configuration; secrets remain in the approved secret store.

If a password has ever been pasted into chat or another durable message, treat it as compromised and choose a different password during bootstrap.

## Local use

1. Start PostgreSQL and apply migrations.
2. Run the interactive bootstrap command once and enter the intended owner email plus a new, unique password. Use the password-reset command for later rotations.
3. Start API and web normally.
4. Open `http://localhost:3000/admin_panel` directly.
5. Verify that a normal account receives the unauthorized state and that the owner account reaches Overview.

## Operational rules

- Prefer draft/deactivation over deletion.
- Do not enable an offer until its Merchant Policy permits current-price publication.
- Never type a retailer discount or reference price; those remain evidence-derived.
- Do not paste tracking URLs into offers or banners. Affiliate handoff is created only by approved provider records.
- Use only reviewed first-party banner assets or documented merchant-approved assets with valid placement and dates.
- Add a concise reason when changing match state, deactivating content, or resolving a report.
- Review the Audit area after sensitive changes.

MFA and password recovery are not implemented and remain production security follow-ups.
