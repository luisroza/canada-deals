# Security Documentation

Security baseline, threat model, security reviews, findings, and release checklists belong here.

## Owner administration boundary

The intentionally unlinked `/admin_panel` path is not a security control. `/api/v1/admin/*` requires the server-side `OwnerAdminOnly` policy; writes additionally require anti-forgery, the dedicated admin rate limit, bounded validation, merchant/asset policy gates, and an `AdminAuditEvent`. Public registration cannot assign roles and the panel exposes no user/role management or tracking-URL editor. Account bootstrap/reset uses the interactive no-echo command documented in `docs/operations/OWNER-ADMIN.md`.

Before production owner use, review MFA or step-up authentication, recovery, session revocation, backup/restore of Identity and audit rows, and operational alerting for repeated admin login failures.
