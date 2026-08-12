# Vertical Slice 3 Test Report

**Vertical Slice:** Save Product Persistence + Minimal Account Boundary
**Status:** IMPLEMENTED AND VALIDATED
**Date:** 2026-08-11
**Release recommendation:** RELEASE WITH KNOWN RISKS for continued local/fixture-backed development. Production account launch remains gated by real email confirmation delivery and password recovery.

## Environment

- Windows 11
- .NET SDK 10.0.300 / .NET runtime 10.0.8
- ASP.NET Core Identity + EF Core/Npgsql 10.0.3
- PostgreSQL 17 Alpine in Docker Compose
- Next.js 16.3.0, React 19.2.8, Node.js 24.14.0, pnpm 10.15.0
- Playwright 1.62.1 / Chromium

No live retailer API, scraper, affiliate program, email provider, or frontend API interception was used.

## Migration and database validation

The separate empty database `canadadeals_slice3_validation` applied this complete chain:

1. `20260811180731_InitialCreate`
2. `20260811185543_AddListingIssueReports`
3. `20260811192055_AddIdentityAndSavedProducts`

A second `database update` returned `No migrations were applied. The database is already up to date.` EF migration listing reported all three migrations as applied. The clean database then passed all 28 integration tests.

`SavedProducts` validation confirmed:

- composite primary key `(UserId, ProductId)` prevents duplicate intent;
- User FK uses `CASCADE`;
- Product FK uses `RESTRICT`;
- `(UserId, CreatedAt)` supports current-user ordering;
- an invalid Product cannot create an orphan;
- deleting an Identity user removes only that user's saves and leaves Product truth intact.

## Authentication and session validation

- Valid Development/Test registration creates an Identity account, confirms it server-side without exposing a token, and establishes a cookie session.
- Production registration leaves the account unconfirmed and anonymous.
- After controlled confirmation, Production login emits `__Host-CanadaDeals.Auth` with `Secure`, `HttpOnly`, and `SameSite=Lax`.
- Development uses a non-Secure local HTTP cookie intentionally; Production requires Secure HTTPS.
- Email is the initial normalized Identity username/identifier; normalized-equivalent duplicate registration is rejected generically.
- Valid login establishes a session; unknown-account and wrong-password responses both use `Invalid email or password.`
- Logout ends private API access while public discovery remains available.
- `GET /api/v1/account/me` exposes only authentication state and the current account's email.
- Register/login fixed-window rate limiting returned `429` after the configured test budget; production default is 10 requests per IP per minute.
- Identity lockout is five failures for five minutes.

## CSRF, authorization, and IDOR

- Account and Saved Product mutations require ASP.NET Core anti-forgery validation through `X-CSRF-TOKEN`.
- Register, Save, and Unsave without the expected token returned `400`; tests did not bypass the protection.
- Anonymous Save/List returned `401`.
- Saved Product endpoints never accept User ID, role, owner, timestamp, or a SavedProduct database ID.
- User identity is derived from the authenticated server principal.
- User B could neither list nor delete User A's save; User A's persisted row remained present.
- Save/Unsave requests are scoped by `(current UserId, ProductId)` and repeated requests are idempotent.

## Saved Product behavior

- First Save returned `201` and persisted one row.
- Repeated Save returned `200` without a duplicate.
- `/saved` returned canonical Product title, current permitted price, retailer context, evidence, freshness, history state, and saved timestamp.
- Repeated Unsave returned `204` safely.
- Save survived logout and a separate login session.
- Public discovery response remained byte-for-byte unchanged after Save, covering Price Truth, evidence, freshness, and organic ordering neutrality.

## Automated test results

| Layer | Result | Coverage notes |
|---|---:|---|
| Domain | 22 passed, 0 failed, 0 skipped | Existing truth/matching/report rules plus Saved Product identity and field neutrality |
| PostgreSQL integration | 28 passed, 0 failed, 0 skipped | Full Slice 1/2 regression plus auth, CSRF, rate limit, persistence, constraints, session survival, and IDOR isolation |
| Frontend component/integration | 21 passed, 0 failed, 0 skipped | Account labels/errors, return-path safety, Save states, `/saved` states, report and trust regressions |
| Playwright | 11 passed, 0 failed, 0 skipped | Real Next.js + API + PostgreSQL; 6 existing journeys and 5 auth/save/security journeys |

The Release backend build completed with 0 warnings and 0 errors. The production Next.js build completed successfully and generated `/`, `/products/[slug]`, `/account/sign-in`, `/account/register`, and `/saved` as intended.

## Playwright journeys

The complete browser suite validated:

- Slice 1 evidence/freshness/safe comparison, possible variant, unavailable history, and mobile behavior;
- Slice 2 persisted Price changed and Wrong variant reports;
- signed-out Save explanation → register → return to Product → Save → `/saved`;
- persisted Save after logout and a separate login session;
- User A/User B saved-list isolation;
- Unsave from `/saved`;
- rejection of an absolute external `returnTo` destination.

No core auth/save API was mocked or intercepted.

## Accessibility and responsive validation

- Email and password inputs have labels, bounded values, and correct autocomplete intent.
- Validation/authentication errors use announced alert text and preserve entered values.
- Pending buttons are disabled with text state.
- Save/Saved/Remove states are textual and expose `aria-pressed` when authenticated.
- The signed-out explanation receives focus; Cancel restores focus to Save.
- `/saved` provides semantic headings plus loading, signed-out, empty, populated, error, and Unsave states.
- Existing representative 390×844 mobile test remains free of critical horizontal overflow; account, Save, and saved-list layouts stack at the existing 760px breakpoint.

## Worker regression

The worker started against the Slice 3 validation database, created/retained 12 Hangfire PostgreSQL tables, and the opt-in fixture-safe sample job reached `Succeeded`. Save Product added no background job and no merchant fetch.

## Security findings

| Control | Result | Evidence |
|---|---|---|
| Cookie/session | PASS | HttpOnly/SameSite and Production Secure host-only cookie integration coverage; bounded eight-hour sliding ticket |
| CSRF | PASS | framework token required for account and Saved Product mutations; missing-token tests return 400 |
| IDOR/BOLA | PASS | no client User ID; cross-user list/delete integration and E2E isolation |
| Open redirect | PASS | internal-relative `returnTo` allow rule, unit cases, and real-browser external destination rejection |
| Enumeration | PASS WITH LIMITATION | generic login and duplicate-registration language; broader abuse review remains a production activity |
| Rate limiting | PASS | per-IP fixed window on register/login plus test returning 429 |
| Sensitive logging | PASS | opaque User/Product IDs only; no passwords, cookies, CSRF/confirmation tokens, or raw credentials logged |
| Mass assignment | PASS | explicit account/save request contracts; ownership and timestamps are server-controlled |

No Critical or High security defect remains known within the implemented Slice 3 boundary.

## Defects found and fixed

### POST exception masking

- **Symptom:** an exception during POST account requests surfaced as `405 Method Not Allowed`.
- **Root cause:** exception handling re-executed the original method against a GET-only `/error` endpoint.
- **Fix:** terminal exception handler now emits a generic `500` Problem Details response without method re-execution.
- **Coverage:** account integration tests exercise POST failure/success paths.

### Anti-forgery MVC service registration

- **Symptom:** `[ValidateAntiForgeryToken]` could not resolve its authorization filter.
- **Root cause:** API-only controller registration omitted MVC ViewFeatures services required by that framework attribute.
- **Fix:** controller registration now includes the framework MVC services; anti-forgery remains middleware/filter enforced.
- **Coverage:** missing-token and valid-token account/save integration tests.

### Persistent shell session state

- **Symptom:** after registration the Product Save control recognized the session, but the persistent header still showed signed out.
- **Root cause:** Next.js preserved the root layout and `AccountNav` loaded session state only on initial mount.
- **Fix:** AccountNav refreshes session state when the route changes.
- **Coverage:** persistence and cross-user Playwright journeys require header logout after registration.

### Concurrent fixture seed race

- **Symptom:** clean-database integration startup could violate unique catalog constraints when two test hosts seeded simultaneously.
- **Root cause:** both hosts observed an empty Product table before either committed fixtures.
- **Fix:** Development seed now uses a PostgreSQL transaction-scoped advisory lock before the existing idempotency check.
- **Coverage:** the complete 28-test suite passed from an empty database with multiple fixture hosts.

## Production-readiness limitations

- `PRODUCTION EMAIL DELIVERY NOT YET CONFIGURED`: Production registration remains unconfirmed; Resend or another live provider was not added.
- Password reset/recovery is not implemented.
- MFA and broader account lifecycle/profile management are not implemented.
- Target Price Alert entity, scheduler, evaluation, consent UI, and email are not implemented.
- External privacy/legal review for email and account processing remains required.
- No production retailer connector or live affiliate destination was added.

## Regression conclusion

Vertical Slice 1 and Vertical Slice 2 remain green across domain, PostgreSQL integration, frontend, production build, worker, and all 6 pre-existing Playwright journeys.

## Recommended next slice

Implement exactly one next slice: **Target-Price Alert persistence and evaluation boundary**, including explicit consent and a deliberate production email-confirmation/delivery decision. Do not start it automatically.
