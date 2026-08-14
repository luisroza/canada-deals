# Backend API contract

The MVP public contract is designed for same-site routing under `/api/*`. Local development runs the API on `http://localhost:5099` and uses the Next.js rewrite equivalent so the browser still addresses `/api/*` and `/go/*` on the web origin.

Vertical Slice 9 adds no public Rakuten credential, discovery, import, or URL-generation endpoint. Those operations remain server-side Worker/operator commands. Public clients can only observe policy-safe catalog projections and use the existing `/go/{listingId}` boundary when a validated persisted link exists.

## Implemented endpoints

### `GET /api/v1/deals`

Returns one fixture-backed canonical Product card per result with identity, brand, category, representative permitted retailer offer, current CAD price, online availability, freshness and observation time, evidence state/explanation, human-readable match state, history availability, supported reference price/savings when defensible, details, and a safe-handoff path only when an ACTIVE program and usable persisted AffiliateLink exist. Otherwise `handoffPath` is null and no broken CTA is exposed.

Supported query parameters are `search`, comma-separated `category` and `retailer`, `minPrice`, `maxPrice`, `hasReference`, comma-separated `freshness` (`recent`, `aging`, `stale`, `unknown`), `match` (`safe`, `review`, `none`), `availability` (`online`, `unavailable`, `unknown`), `sort` (`relevance`, `recent`, `savings`, `price-asc`), bounded `page`, and bounded `pageSize` (1–48). Filters are OR within one comma-separated dimension and AND across dimensions. Invalid values, unknown category/retailer keys, malformed numeric values, contradictory prices, and invalid page bounds return `400`.

The unfiltered feed defaults to `recent`; a search with no explicit sort defaults to `relevance`. Relevance is deterministic and explainable: exact normalized model/MPN/GTIN, exact title, title prefix, PostgreSQL full-text rank, controlled `pg_trgm` word similarity, recency, then Product ID. It never uses affiliate economics, click behavior, save/alert state, or user data. `savings` is shown/sorted only when a permitted earlier observation is higher than the current price. Price-range filtering also requires a safe online same-product match.

The response includes `count`, effective `sort`, page metadata, `hasNext`, and policy-safe category/retailer facets. `UNKNOWN` or denied merchant-policy data is excluded before search, filtering, ranking, cards, and facets.

### `GET /api/v1/products/{slug}`

Returns canonical Product ID and slug, structured variant attributes, primary offer, safe same-product comparisons, possible related listings for review, history summary, and evidence summary. Each offer exposes source-proven seller, condition, online availability, regional context, shipping context, and observation time. Null source facts remain null so the UI can label them unknown; coupon, membership/payment eligibility, and retailer offer expiry are not inferred from an affiliate link or a current-price observation. Possible variants never enter `safeComparisons`.

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

### Saved Product endpoints

- `GET /api/v1/saved-products` returns only the authenticated user's canonical products with title, current publishable price, currency, retailer context, evidence, freshness, history availability, saved timestamp, and public details path.
- `PUT /api/v1/saved-products/{productId}` idempotently saves an existing canonical Product. First save returns `201`; an existing save returns `200` without duplication.
- `DELETE /api/v1/saved-products/{productId}` idempotently removes only the current user's matching save and returns `204`.

All three endpoints derive the User ID from the authenticated server session; no request accepts a User ID or SavedProduct database ID. PUT/DELETE require `X-CSRF-TOKEN`. Unknown Products return `404`; anonymous requests return `401`.

### Target Price Alert endpoints

- `GET /api/v1/price-alerts` returns only the authenticated user's alert configurations with canonical Product context, CAD target, ACTIVE/DISABLED status, target version, consent evidence, and evaluation/trigger timestamps.
- `PUT /api/v1/price-alerts/{productId}` idempotently creates or updates one canonical Product alert. Body: `{ "targetPrice": 399.99, "consentToEmail": true }`. First creation returns `201`; retry/update returns `200`. Creating an alert also ensures that Product is saved.
- `DELETE /api/v1/price-alerts/{productId}` idempotently disables only the current user's alert and returns `204`; it does not remove the Saved Product.

PUT/DELETE require authentication, confirmed email, anti-forgery validation, and the `price-alert-mutations` rate limit. User ID is always derived from the server principal. Targets are CAD, greater than zero, no more than 1,000,000, and limited to two decimals. Missing consent is rejected; no newsletter or Weekly Digest consent is inferred.

Development/Test-only authenticated diagnostics support deterministic E2E without external mail: `POST /api/internal/price-alert-evaluation/scenarios/{productId}`, `POST /api/internal/price-alert-evaluation/run`, `GET /api/internal/price-alert-evaluation/jobs/{jobId}`, and `GET /api/internal/price-alert-evaluation/deliveries`. `GET /api/internal/email-captures/latest?to=...` exposes the latest exact controlled HTML/text capture only in Development/Test. They are `404` outside Development/Test and never fetch retailer data.

### `POST /api/v1/webhooks/email/resend`

Accepts Resend lifecycle webhooks. It verifies the raw body against `svix-id`, `svix-timestamp`, and `svix-signature`, rejects invalid or stale signatures, deduplicates provider event IDs, tolerates out-of-order delivery, and correlates by provider message ID. Supported events are sent, delivered, failed, bounced, complained, and suppressed. Open/click events are ignored. This endpoint does not use cookie authentication or anti-forgery because the signed provider payload is its authentication boundary.

### `GET /go/{listingId}`

The client supplies only the internal listing ID. The server loads the listing, ACTIVE `AffiliateProgram`, and current persisted `AffiliateLink`; validates HTTPS, no userinfo, retailer destination equality/domain, provider tracking domain, program status, and link expiry; persists a minimal opaque `ClickEvent`; and redirects to the provider-returned tracking URL. Arbitrary query-string destinations are ignored. Provider APIs are never called in this request. Missing, suspended, expired, or unapproved relationships return no redirect; invalid URL policy returns `503` without exposing the target. Production handoff stays disabled until the human activation checkpoint is satisfied.

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

### `GET /api/internal/listing-issue-reports?status=OPEN`

Provides minimal operator reviewability only when the API host is running in `Development`. It returns at most 100 reports ordered by creation time with listing context and is `404` outside Development. It is not a public or production admin API. Until the approved authentication/admin boundary is implemented, operators may use this local diagnostic endpoint or query `ListingIssueReports` directly.

Anonymous reporting stores no name, email, full IP address, or other required PII. Submission rate limiting, bot abuse, and repeated false-report controls remain explicit Security Review follow-ups.

## Deliberately deferred

Password reset, MFA, production admin review, live provider credential validation, and merchant catalog/price APIs remain deferred. Impact/CJ tracking-link adapters are implemented deterministically, but no merchant is marked live. Production email code is implemented; live provider/DNS validation remains blocked as documented in `docs/backend/EMAIL.md`.
