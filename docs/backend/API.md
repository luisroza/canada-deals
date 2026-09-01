# Backend API contract

The MVP public contract is designed for same-site routing under `/api/*`. Local development runs the API on `http://localhost:5099` and uses the Next.js rewrite equivalent so the browser still addresses `/api/*` and `/go/*` on the web origin.

Vertical Slice 9 adds no public Rakuten credential, discovery, import, or URL-generation endpoint. Those operations remain server-side Worker/operator commands. Public clients can only observe policy-safe catalog projections and use the existing `/go/{listingId}` boundary when a validated persisted link exists.

## Implemented endpoints

### `GET /api/v1/deals`

Returns one card per publishable `RetailerListing`, including listing/Product identity, brand, category, retailer, current deal price, optional same-listing regular price and savings, availability, freshness, observation time, evidence, offer-identity state, detail path, and a safe handoff only when an approved destination exists. Multiple listings attached to one internal Product remain separate results.

Supported query parameters are `search`, comma-separated `category` and `retailer`, `minPrice`, `maxPrice`, `hasReference`, comma-separated `freshness` (`recent`, `aging`, `stale`, `unknown`), `match` (`safe`, `review`, `none`), `availability` (`online`, `unavailable`, `unknown`), `sort` (`relevance`, `recent`, `savings`, `price-asc`), bounded `page`, and bounded `pageSize` (1–48). Filters are OR within one comma-separated dimension and AND across dimensions. Invalid values, unknown category/retailer keys, malformed numeric values, contradictory prices, and invalid page bounds return `400`.

The unfiltered feed defaults to `recent`; a search with no explicit sort defaults to `relevance`. Relevance is deterministic and explainable: exact normalized model/MPN/GTIN, exact title, title prefix, PostgreSQL full-text rank, controlled `pg_trgm` word similarity, recency, then listing ID. It never uses commercial economics, click behavior, Wishlist state, or user data. `savings` is shown/sorted only when the same listing has an explicit higher regular price, matching currency, observation time, and evidence reference. No price from another listing or retailer enters the calculation.

The response includes `count`, effective `sort`, page metadata, `hasNext`, and policy-safe category/retailer facets. `UNKNOWN` or denied merchant-policy data is excluded before search, filtering, ranking, cards, and facets.

### `GET /api/v1/offers/{listingId}`

Returns the selected listing with internal Product identity/attributes, its deal and optional regular price/savings, seller, condition, availability, region, shipping, observation time, evidence summary, image, and retailer handoff. It never returns comparison arrays or alternative listings.

### `GET /api/v1/products/{slug}`

Compatibility resolver that returns one eligible listing projection for the legacy Product slug. The frontend redirects it to `/offers/{listingId}`. It is not a canonical public comparison contract.

### `GET /api/v1/products/{slug}/history?window=30d|90d`

Returns a focused public Product-history projection for the requested bounded window. Missing `window` defaults to `30d`; unsupported windows return `400`, and an unknown Product returns `404`.

The response contains Product identity, effective window/days, `RELIABLE`, `PARTIAL`, or `UNAVAILABLE` state, defensible retained-evidence `trackingStart`, in-window observation boundaries, lowest/highest observed daily price, raw qualifying-observation count, observed-day count, largest gap, factual coverage/interpretation copy, and daily points (`observedDate`, lowest qualifying CAD price, currency, and observation count). `UNAVAILABLE` returns an empty point collection and null price/boundary fields rather than a fabricated series.

The projection is backend-authoritative. It includes only permitted observations from confirmed/auto-matched canonical Product listings that are new condition and not marketplace sellers. `AllowPriceStorage` and `AllowPriceHistory` must both be `ALLOWED`; denied or `UNKNOWN` policy data is excluded. Future, non-positive, non-CAD, unsafe-variant, used/refurbished, and marketplace observations cannot enter the response. Current price/freshness remain part of the existing Product response and are intentionally independent from historical coverage.

### Minimal account endpoints

- `GET /api/v1/account/antiforgery` stores the HttpOnly anti-forgery cookie and returns the request token required in `X-CSRF-TOKEN`; the response is never cached.
- `POST /api/v1/account/register` accepts only email and password. Normalized duplicate email/user names fail generically. Development/Test may use the explicit auto-confirm convenience; otherwise registration persists an unconfirmed account and sends the transactional confirmation message through the configured boundary.
- `POST /api/v1/account/confirm-email` accepts the user ID and bounded base64url Identity token, requires anti-forgery, and returns `CONFIRMED`, `ALREADY_CONFIRMED`, or `INVALID_OR_EXPIRED`.
- `POST /api/v1/account/resend-confirmation` requires anti-forgery and the authentication rate limit, always returns the same `202` response, and sends only when an unconfirmed account exists.
- `POST /api/v1/account/login` establishes a non-persistent Identity cookie session and returns the same `Invalid email or password.` detail for unknown accounts and wrong passwords.
- `POST /api/v1/account/logout` ends the authenticated session.
- `GET /api/v1/account/me` returns `isAuthenticated`, the current account email, and `emailConfirmed`; anonymous visitors receive false/null/false.

Register, login, and logout require anti-forgery validation. Register/login also use the `authentication` rate-limit policy. API authentication failures return `401` and authorization failures return `403`; cookie middleware never redirects API clients to HTML login pages.

### Saved Offer endpoints

- `GET /api/v1/saved-offers` returns only the authenticated user's exact saved listings with Product/store context, deal/regular price projection, evidence, freshness, saved timestamp, and `/offers/{listingId}` detail path.
- `PUT /api/v1/saved-offers/{listingId}` idempotently saves an existing retailer listing. First save returns `201`; an existing save returns `200` without duplication.
- `DELETE /api/v1/saved-offers/{listingId}` idempotently removes only the current user's matching save and returns `204`.

All three endpoints derive the User ID from the authenticated server session; no request accepts a User ID or SavedOffer database ID. PUT/DELETE require `X-CSRF-TOKEN`. Unknown listings return `404`; anonymous requests return `401`. `/api/v1/saved-products` remains a temporary route alias but uses listing IDs and the Saved Offer response contract.

### Target Price Alert endpoints

- `GET /api/v1/price-alerts` returns only the authenticated user's alert configurations with canonical Product context, CAD target, ACTIVE/DISABLED status, target version, consent evidence, and evaluation/trigger timestamps.
- `PUT /api/v1/price-alerts/{productId}` idempotently creates or updates one historical canonical Product alert configuration. It does not add any listing to the Wishlist.
- `DELETE /api/v1/price-alerts/{productId}` idempotently disables only the current user's historical alert and returns `204`; it does not remove a Saved Offer.

These routes are retained for rollback/record compatibility but are disabled as a current product capability. No frontend control or worker enqueue exposes alerts. If enabled in a controlled environment, mutations retain their authentication, confirmed-email, anti-forgery, validation, and rate-limit protections.

Development/Test-only authenticated diagnostics support deterministic E2E without external mail: `POST /api/internal/price-alert-evaluation/scenarios/{productId}`, `POST /api/internal/price-alert-evaluation/run`, `GET /api/internal/price-alert-evaluation/jobs/{jobId}`, and `GET /api/internal/price-alert-evaluation/deliveries`. `GET /api/internal/email-captures/latest?to=...` exposes the latest exact controlled HTML/text capture only in Development/Test. They are `404` outside Development/Test and never fetch retailer data.

### `POST /api/v1/webhooks/email/resend`

Accepts Resend lifecycle webhooks. It verifies the raw body against `svix-id`, `svix-timestamp`, and `svix-signature`, rejects invalid or stale signatures, deduplicates provider event IDs, tolerates out-of-order delivery, and correlates by provider message ID. Supported events are sent, delivered, failed, bounced, complained, and suppressed. Open/click events are ignored. This endpoint does not use cookie authentication or anti-forgery because the signed provider payload is its authentication boundary.

### `GET /go/{listingId}`

The client supplies only the internal listing ID. The server loads the listing, ACTIVE `AffiliateProgram`, and current persisted `AffiliateLink`; validates HTTPS, no userinfo, retailer destination equality/domain, provider tracking domain, program status, and link expiry; persists a minimal opaque `ClickEvent`; and redirects to the provider-returned tracking URL. Arbitrary query-string destinations are ignored. Provider APIs are never called in this request. Missing, suspended, expired, or unapproved relationships return no redirect; invalid URL policy returns `503` without exposing the target. Production handoff stays disabled until the human activation checkpoint is satisfied.

### `GET /api/v1/store-banners`

Returns enabled banners for retailers with publishable catalog listings. Copy, first-party asset path, original/merchant-approved asset provenance, brand-rights state, neutral editorial order, affiliate/discovery status, safe internal href, and new-tab state are backend-controlled. Raw destination and tracking URLs are never returned. Missing profiles use an original first-party fallback; explicitly disabled profiles and disabled retailers are omitted.

### `GET /go/store/{retailerKey}`

Resolves a controlled retailer key to one persisted `StoreAffiliateDestination`. Redirect requires an enabled retailer, at least one affiliate-permitted MerchantPolicy, ACTIVE complete `AffiliateProgram`, usable destination, exact retailer/program/provider association, HTTPS destination and tracking URLs, allowlisted merchant/tracking hosts, and current Rakuten capability where applicable. The request cannot supply a destination. A valid request writes one privacy-minimal `ClickEvent` with `placement=store_banner` and returns `302`; missing or inactive state returns `404`, while invalid URL policy returns `503` without revealing the target.

### `GET /health`

Reports application health and PostgreSQL reachability. Nonexistent retailer integrations are not part of readiness.

### `POST /api/v1/listings/{listingId}/reports`

Creates an anonymous review signal for an existing retailer listing. The request accepts only `reason` and an optional plain-text `note` of at most 500 characters:

```json
{
  "reason": "PRICE_CHANGED",
  "note": "The retailer now shows $549."
}
```

Supported reasons are `PRICE_CHANGED`, `WRONG_PRODUCT`, `WRONG_VARIANT`, `OFFER_EXPIRED`, `RETAILER_PAGE_UNAVAILABLE`, and `OTHER`. New reports always start as `OPEN`; clients cannot set status, timestamps, listing ID, or internal fields. Unknown listings return `404`, invalid reasons/notes return `400`, and valid submissions return `201` with the report ID, status, and honest confirmation. A report never changes price, matching, availability, evidence, or public visibility automatically.

### Owner administration API

`GET /api/v1/admin/session` and `GET /api/v1/admin/dashboard` require the `OwnerAdminOnly` policy. The dashboard returns operational counts, existing Product/brand/category/retailer/policy options, bounded offer/banner/report projections, publication readiness, and recent audit events. It never returns password hashes, tracking URLs, provider credentials, or arbitrary personal data.

`POST /api/v1/admin/offers` either creates an ad-hoc Product with its first RetailerListing or attaches another RetailerListing to a selected canonical Product. `PUT /api/v1/admin/offers/{listingId}` edits mutable identity/listing fields and can reversibly enable or disable it. Product association and slug plus Retailer, Merchant Policy, and external listing ID are immutable after creation. Both writes require CSRF and the owner role. CAD price/timestamps/optional validity/HTTPS/JSON bounds/duplicate identity/policy/link permission are validated; evidence, history, freshness, reference price, and handoff remain derived. Once `offerValidUntil` is reached, public discovery and `/go/{listingId}` fail closed without waiting for a background job.

`POST /api/v1/admin/brands`, `/categories`, and `/retailers` creates inactive records. Their `PUT` endpoints update display name and reversible enabled state while preserving immutable brand/category slugs and retailer keys. Deactivation requires an audit reason and hides dependent public content without deleting linked records.

`PUT /api/v1/admin/banners/{retailerId}` creates or updates one StoreBannerProfile using all persisted banner-rights fields. `PUT /api/v1/admin/banners/selection` is the sole carousel-membership operation. It accepts only the reviewed first-party path boundary and the domain's original/merchant-approved rules. Tracking destination remains read-only and provider-managed.

`PUT /api/v1/admin/reports/{reportId}/status` changes a ListingIssueReport to OPEN, REVIEWED, RESOLVED, or DISMISSED with a required resolution note and audit event. Administrative writes are rate-limited per authenticated user and audited transactionally.

### `GET /api/internal/listing-issue-reports?status=OPEN`

Provides legacy minimal operator reviewability only when the API host is running in `Development`. It returns at most 100 reports ordered by creation time with listing context and is `404` outside Development. Production review uses the role-protected owner administration API.

Anonymous reporting stores no name, email, full IP address, or other required PII. Submission rate limiting, bot abuse, and repeated false-report controls remain explicit Security Review follow-ups.

## Deliberately deferred

Password reset, MFA/step-up authentication, live provider credential validation, and merchant catalog/price APIs remain deferred. Impact/CJ tracking-link adapters are implemented deterministically, but no merchant is marked live. Production email code is implemented; live provider/DNS validation remains blocked as documented in `docs/backend/EMAIL.md`.
