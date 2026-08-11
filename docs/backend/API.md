# Backend API contract

The MVP public contract is designed for same-site routing under `/api/*`. Local development uses `http://localhost:5099` as the API origin until the reverse proxy is configured.

## Implemented endpoints

### `GET /api/v1/deals?search={term}`

Returns fixture-backed discovery cards with product identity, brand, category, retailer, current CAD price when permitted, freshness and observation time, evidence state and explanation, human-readable match state, history availability, details and safe-handoff paths, and demo disclosure metadata.

Search checks product title, brand, and model number using PostgreSQL-compatible `ILIKE`. It is intentionally not full relevance tuning.

### `GET /api/v1/products/{slug}`

Returns product identity, structured variant attributes, primary offer, safe same-product comparisons, possible related listings for review, history summary, and evidence summary. Possible variants never enter `safeComparisons`.

### `GET /go/{listingId}`

The client supplies only the internal listing ID. The server loads the configured listing, validates the approved destination host, logs a minimal fixture handoff event, and redirects. Arbitrary query-string destinations are ignored. Production handoff remains disabled until an approved affiliate program exists.

### `GET /health`

Reports application health and PostgreSQL reachability. Nonexistent retailer integrations are not part of readiness.

## Deliberately deferred

Authentication, save/alert endpoints, report persistence, admin review, provider webhooks, live affiliate link generation, and merchant-specific APIs are future slices.
