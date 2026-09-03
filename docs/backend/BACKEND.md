# Backend foundation

**Status:** IMPLEMENTED AND VALIDATED - Vertical Slices 1 through 9 plus fixture-validated multi-network catalog ingestion
**Scope:** trusted discovery, reports, accounts/Saved Products, Target Price Alerts, Search + Filters, bounded Product-history evidence, production transactional email, deployment preparation, and provider-neutral persisted affiliate handoff.

Vertical Slice 7 adds the provider-neutral transactional email boundary, production Resend HTTP adapter, explicit Identity account-confirmation token provider, durable confirmation and alert delivery state, bounded retry/idempotency handling, signed webhook lifecycle reconciliation, and application suppression. The API owns account confirmation and webhook ingress; the worker owns alert evaluation/delivery. See `EMAIL.md` for the full contract.

The backend is an ASP.NET Core REST API in a modular monolith. Domain rules live in `src/backend/CanadaDeals.Domain`; PostgreSQL/EF Core/seed infrastructure lives in `src/backend/CanadaDeals.Infrastructure`; the API host lives in `src/backend/CanadaDeals.Api`; the Hangfire worker host lives in `src/backend/CanadaDeals.Worker`.

Implemented modules for this slice:

- Catalog: Product, Brand, Category.
- Retailers/Listings: Retailer and the expanded RetailerListing contract.
- Owner administration: inactive-by-default Brand/Category/Store lifecycle, canonical Product reuse for additional offers, immutable Product slugs, optional offer validity, reviewed assets, reversible publication, and audit.
- PriceTruth: permitted current price, evidence state, history availability, and freshness.
- Matching: deterministic-first match states and safe comparison filtering.
- Affiliate boundary: provider-neutral `IAffiliateLinkProvider`, Impact and CJ HTTP adapters, persisted `AffiliateProgram`/`AffiliateLink`/`ClickEvent`, refresh service/job, and fail-closed `/go/{listingId}`. Public clicks never call provider APIs and React never receives provider URLs or credentials.
- Ingestion foundation: MerchantPolicy and PriceObservation persistence; no live connector activation.
- Multi-network catalog ingestion: `IOfferCatalogSource`, bounded `ExternalOffer`, explicit merchant mapping, provider-neutral discovery/dry-run/import jobs, deterministic source identity, strong Product matching, independent `RetailerListing` upsert, same-listing regular price/history evidence, and safe audit. Rakuten aligns with the common boundary; eBay, Impact, Awin, and CJ adapters are disabled by default and have no live merchant activation.
- Worker foundation: Hangfire PostgreSQL storage and an opt-in fixture-safe sample job.
- Reporting: anonymous `ListingIssueReport` review signals with controlled reasons, bounded plain-text notes, and Development-only operator review.
- Accounts: ASP.NET Core Identity with normalized email identifiers, Identity password hashing, confirmed-email sign-in policy, lockout, same-site cookie sessions, and a minimal register/login/logout/me contract.
- Saved Products: current-user-only canonical Product intent with idempotent save/unsave and server-computed price-truth context.
- Target Price Alerts: current-user-only canonical Product targets, explicit transactional-email consent, deterministic price eligibility, continuous-condition deduplication, and a provider-neutral persisted delivery boundary.
- Product History: a focused public 30/90-day projection over persisted controlled observations, with backend-owned policy/match/condition/currency filtering, deterministic daily-low aggregation, coverage states, and no background aggregation job.

Product-history state is deliberately explainable. Fewer than two observed days is `UNAVAILABLE`. A 30-day window is `RELIABLE` with at least 6 observed days spanning at least 21 days and no gap over 10 days; a 90-day window requires at least 10 observed days spanning at least 60 days and no gap over 21 days. Other usable series are `PARTIAL`. Current-offer freshness is not an input to historical coverage, so valid history can coexist with a visibly `STALE` current price. `Tracking since` is the earliest retained qualifying observation, not the start of the selected window and never an inferred date.

The production cookie is `Secure`, `HttpOnly`, `SameSite=Lax`, host-only, and has an eight-hour sliding ticket lifetime. Development permits HTTP with `SecurePolicy=SameAsRequest`; production requires HTTPS. State-changing account and Saved Product endpoints validate a framework anti-forgery request token. Register/login share an IP-partitioned fixed-window limit of 10 requests per minute; five failed password attempts lock an existing account for five minutes.

Alert mutations use a user-partitioned fixed-window limit of 30 per minute. An alert cannot be ACTIVE unless the account email is confirmed. Evaluation considers only available, policy-permitted, safely matched, fresh current observations for the canonical Product; history, Deal Quality, saves/popularity, and affiliate commission are not inputs. One configuration is stored per `(UserId, ProductId)`. A changed/reactivated target increments `TargetVersion`; a below-target cycle is notified once until the price rises above target or the target changes.

Production registration creates an unconfirmed account, sends a durable confirmation message through the configured provider boundary, and does not sign in until Identity confirms the token. Development/Test can either auto-confirm or persist exact `DEVELOPMENT_CAPTURED` email evidence. Production records provider acceptance separately from webhook-confirmed delivery. Provider/DNS operational validation is still blocked; password recovery, MFA, live affiliate credentials, and live merchant catalog activation remain unimplemented.

Affiliate activation is optional. Disabled Impact/CJ providers require no credentials and do not prevent startup. Enabling a provider validates its server-only credentials at startup. A refresh first validates the local ACTIVE relationship record and merchant destination, then asks the provider to verify current relationship/deep-link capability and return an allowlisted tracking URL. Relationship/deep-link failures suspend the program; authentication/configuration/destination failures mark it incomplete; rate limits and temporary outages retain an existing valid link. Commission and EPC are neither persisted in the Product model nor available to ranking, evidence, comparison, or alert evaluation.

## Rakuten connector boundary

Rakuten registers only as a disabled opt-in integration. Authentication is scoped by Publisher Account ID, cached in memory, refreshed before expiry, and synchronized across concurrent callers. A shared authenticated client applies conservative pacing, bounded `429`/transient retries, and one token invalidation retry. Discovery correlates Partnerships and Advertisers by MID before persistence or activation. Product Search parses bounded XML with DTD resolution disabled. Deep-link generation goes through the existing `IAffiliateLinkProvider` and persists a validated link before shopper handoff.

Catalog writes require live-import enablement plus an active capability, Canada relevance, explicit operator mapping, and MerchantPolicy `ALLOWED` for metadata and price storage. The importer is transactional, MID-scoped, idempotent, CAD-only, does not cache images, and does not fabricate missing listing facts. It attaches only by exact unique UPC; weak/conflicting candidates remain review output and cannot create canonical Products.
