# Backend foundation

**Status:** IMPLEMENTED AND VALIDATED - Vertical Slices 1 through 7
**Scope:** fixture-backed trusted discovery, reports, accounts/Saved Products, Target Price Alerts, Search + Filters, bounded Product-history evidence, and production transactional email delivery.

Vertical Slice 7 adds the provider-neutral transactional email boundary, production Resend HTTP adapter, explicit Identity account-confirmation token provider, durable confirmation and alert delivery state, bounded retry/idempotency handling, signed webhook lifecycle reconciliation, and application suppression. The API owns account confirmation and webhook ingress; the worker owns alert evaluation/delivery. See `EMAIL.md` for the full contract.

The backend is an ASP.NET Core REST API in a modular monolith. Domain rules live in `src/backend/CanadaDeals.Domain`; PostgreSQL/EF Core/seed infrastructure lives in `src/backend/CanadaDeals.Infrastructure`; the API host lives in `src/backend/CanadaDeals.Api`; the Hangfire worker host lives in `src/backend/CanadaDeals.Worker`.

Implemented modules for this slice:

- Catalog: Product, Brand, Category.
- Retailers/Listings: Retailer and the expanded RetailerListing contract.
- PriceTruth: permitted current price, evidence state, history availability, and freshness.
- Matching: deterministic-first match states and safe comparison filtering.
- Affiliate boundary: fixture-only `/go/{listingId}` server-side handoff with host allowlist.
- Ingestion foundation: MerchantPolicy and PriceObservation persistence; no live connector.
- Worker foundation: Hangfire PostgreSQL storage and an opt-in fixture-safe sample job.
- Reporting: anonymous `ListingIssueReport` review signals with controlled reasons, bounded plain-text notes, and Development-only operator review.
- Accounts: ASP.NET Core Identity with normalized email identifiers, Identity password hashing, confirmed-email sign-in policy, lockout, same-site cookie sessions, and a minimal register/login/logout/me contract.
- Saved Products: current-user-only canonical Product intent with idempotent save/unsave and server-computed price-truth context.
- Target Price Alerts: current-user-only canonical Product targets, explicit transactional-email consent, deterministic price eligibility, continuous-condition deduplication, and a provider-neutral persisted delivery boundary.
- Product History: a focused public 30/90-day projection over persisted controlled observations, with backend-owned policy/match/condition/currency filtering, deterministic daily-low aggregation, coverage states, and no background aggregation job.

Product-history state is deliberately explainable. Fewer than two observed days is `UNAVAILABLE`. A 30-day window is `RELIABLE` with at least 6 observed days spanning at least 21 days and no gap over 10 days; a 90-day window requires at least 10 observed days spanning at least 60 days and no gap over 21 days. Other usable series are `PARTIAL`. Current-offer freshness is not an input to historical coverage, so valid history can coexist with a visibly `STALE` current price. `Tracking since` is the earliest retained qualifying observation, not the start of the selected window and never an inferred date.

The production cookie is `Secure`, `HttpOnly`, `SameSite=Lax`, host-only, and has an eight-hour sliding ticket lifetime. Development permits HTTP with `SecurePolicy=SameAsRequest`; production requires HTTPS. State-changing account and Saved Product endpoints validate a framework anti-forgery request token. Register/login share an IP-partitioned fixed-window limit of 10 requests per minute; five failed password attempts lock an existing account for five minutes.

Alert mutations use a user-partitioned fixed-window limit of 30 per minute. An alert cannot be ACTIVE unless the account email is confirmed. Evaluation considers only available, policy-permitted, safely matched, fresh current observations for the canonical Product; history, Deal Quality, saves/popularity, and affiliate commission are not inputs. One configuration is stored per `(UserId, ProductId)`. A changed/reactivated target increments `TargetVersion`; a below-target cycle is notified once until the price rises above target or the target changes.

Production registration creates an unconfirmed account, sends a durable confirmation message through the configured provider boundary, and does not sign in until Identity confirms the token. Development/Test can either auto-confirm or persist exact `DEVELOPMENT_CAPTURED` email evidence. Production records provider acceptance separately from webhook-confirmed delivery. Provider/DNS operational validation is still blocked; password recovery, MFA, full admin workflows, real affiliate links, and merchant-specific connectors remain unimplemented.
