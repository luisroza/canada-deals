# ADR-013: Google sign-in for end-user accounts

**Status:** PROPOSED — NOT APPROVED FOR IMPLEMENTATION
**Date:** 2026-08-28
**Checkpoint:** requires Human Architecture / Security approval before implementation

## Context

GreatDeals.ca keeps public discovery anonymous and uses ASP.NET Core Identity cookie sessions only when a shopper needs Wishlist persistence. The current approved authentication decision deliberately defers social login. A future Google sign-in option could reduce account friction at the Wishlist boundary without changing the Product's anonymous discovery model.

The existing Identity schema already includes `AspNetUserLogins`, Wishlist rows reference the internal `ApplicationUser.Id`, browser/API traffic is same-site, Data Protection keys are persistent, and local email/password authentication must remain available. Google sign-in is for end users only and must not become an authentication or privilege path for `/admin_panel`.

## Options

1. Keep email/password authentication only.
2. Accept a Google identity token directly in the Next.js client and exchange it with the API.
3. Add a backend-controlled Google external-login flow integrated with ASP.NET Core Identity while retaining email/password fallback.
4. Replace ASP.NET Core Identity with a managed identity platform.

## Decision

**Proposed decision, not yet approved:** choose option 3 if Product, Architecture, Security, privacy, and operational checkpoints approve implementation.

- Use the supported ASP.NET Core Google external-authentication middleware with an authorization-code redirect handled by the backend.
- The frontend starts the flow but never receives, stores, or logs Google access or ID tokens.
- Issue the existing GreatDeals.ca secure cookie after the callback succeeds; Google does not become the application's authorization system.
- Persist the external identity as `LoginProvider = Google` and `ProviderKey = sub`. Never use email as the external identity key.
- Require a verified Google email before creating a new local user. A new Google-only user receives one internal `ApplicationUser.Id`, and the Wishlist remains attached to that ID.
- If the normalized email already belongs to a local account, do not link automatically. Require proof of the existing local account through password reauthentication or another approved recovery/step-up mechanism before adding the Google login.
- Keep email/password sign-in as a complete fallback. Do not add One Tap, automatic sign-in, Google profile features, or Google API scopes beyond basic identity and email in the initial implementation.
- Reject Google linking or login as a route to the `OwnerAdmin` role. Email equality, including the owner's email, never grants a role. The owner administration boundary remains local and subject to its separate security checkpoint.
- Allow unlinking only when another usable sign-in method remains.

## UX boundary

- Show a branding-compliant **Continue with Google** action on sign-in, registration, and the signed-out Wishlist boundary only.
- Keep discovery, search, Product pages, and retailer handoff available without an account.
- Preserve a validated relative `returnTo`, Product context, and pending Wishlist intent across the redirect; complete a pending save idempotently after successful sign-in.
- Use a full-page redirect as the initial cross-browser flow, including iOS Safari. Do not introduce a popup-only dependency.
- Provide accessible loading, processing, cancelled, provider-unavailable, account-link-required, locked, and success states. Email/password remains usable when Google or its script is unavailable.
- Explain that GreatDeals.ca receives only the information needed to sign the shopper in and does not request Gmail, Drive, contacts, Calendar, purchases, or promotional consent.

## Security and operational requirements

- Validate provider identity, correlation/state, redirect destination, and the returned stable subject; fail closed on missing or unverified email.
- Use exact environment-specific HTTPS redirect URIs in production. Localhost development uses a separately registered redirect URI.
- Store Client ID/Client Secret through local user secrets and the approved production secret store; never commit or expose them to the frontend.
- Keep correlation and external-authentication cookies compatible with the cross-site callback while retaining Secure, HttpOnly, bounded lifetime, and least-permissive SameSite settings.
- Preserve trusted-proxy and forwarded-scheme handling so production callbacks are generated as HTTPS. Do not trust arbitrary Host or forwarded headers.
- Apply authentication rate limits, generic public errors, bounded temporary state, safe logs, and transactional uniqueness around account creation/linking.
- Publish appropriate Privacy Policy and Terms references before production activation.

## Acceptance and validation gate

Implementation is not complete until automated coverage proves:

- new and returning Google login;
- `email_verified = false` rejection;
- stable `sub` lookup and no email-keyed external identity;
- existing-email flow cannot auto-link or create a duplicate account;
- explicit linking after local reauthentication;
- Wishlist and validated `returnTo` preservation, including idempotent pending save;
- invalid/replayed callback, correlation/state failure, open-redirect rejection, provider cancellation, and concurrency handling;
- external provider outage leaves email/password available;
- unlinking cannot remove the only sign-in method;
- no Google path grants or links `OwnerAdmin` access;
- secure cookies and callback behavior behind the production proxy; and
- accessible end-to-end behavior on desktop, Android Chrome, and iOS Safari.

## Reasoning

This approach reuses the approved Identity store and secure application cookie, adds no browser token store, preserves one canonical Wishlist owner, and keeps a reversible password fallback. It minimizes product friction without replacing the current architecture or making discovery dependent on Google.

## Tradeoffs

- Google becomes an optional external dependency for affected sign-ins.
- The project must operate OAuth credentials, consent configuration, privacy documentation, redirect URIs, and provider-failure support.
- Secure account linking and recovery add more complexity than email matching; that complexity is necessary to prevent account takeover.
- Google-only users depend on Google until another sign-in method is added.
- The admin account intentionally does not gain the convenience of Google sign-in under this proposal.

## Migration path

1. Obtain explicit Product and Human Architecture / Security approval for this ADR.
2. Update approved account UX and privacy documentation.
3. Register separate local and production Google OAuth web clients and configure secrets outside the repository.
4. Add the backend provider, challenge/callback, pending-link service, sign-in-method management, and audited security controls.
5. Add the frontend action and accessible states while retaining email/password.
6. Run integration, security, proxy, desktop, Android, and iOS validation before enabling the feature.
7. Keep the provider feature-gated so it can be disabled without affecting email/password sessions or Wishlist data.
