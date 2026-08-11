# Canada Deals - Canonical Data Model and Policy Contract

**Status:** PROPOSED - awaiting Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11

## Canonical entities

| Entity | Purpose | Key identifiers |
|---|---|---|
| `Product` | canonical same-product identity | internal ID; GTIN/UPC/EAN/ISBN where trusted; brand + MPN/model |
| `Brand` | normalized brand identity | normalized name, aliases |
| `Category` | approved taxonomy and filters | stable slug, parent |
| `Retailer` | merchant identity and region | merchant key, country, currency |
| `AffiliateProgram` | network/program relationship | network, merchant, account/program ID, status |
| `RetailerListing` | retailer-specific product offer | retailer + external listing ID, product ID, URL, availability |
| `PriceObservation` | source observation when permitted | listing + observed-at + source hash, amount, currency, availability |
| `Deal` | derived public opportunity | listing/product, score inputs, freshness, evidence state |
| `PriceAlert` | user threshold and delivery state | user + product/listing, target, consent, status |
| `SavedProduct` | user intent | user + product, created-at |
| `AffiliateLink` | approved server-side handoff | listing/program, destination, expiry, disclosure |
| `ClickEvent` | minimum non-PII redirect telemetry | opaque event, link, placement, timestamp |
| `ImportJob` | fetch/normalize/match execution | connector, cursor, state, retries, counts |
| `Connector` | provider adapter configuration | network/merchant, version, health, quota |
| `MerchantPolicy` | rights and retention controls | merchant/source, field flags, max age, reviewer, expiry |
| `MatchDecision` | product identity audit | candidate, method, confidence state, reviewer |
| `AuditRecord` | sensitive operational decision trail | actor/service, action, reason, before/after reference |

## Policy flags

Each source must have explicit values for:

`AllowPriceStorage`, `AllowPriceHistory`, `AllowImageCaching`, `AllowMetadataCaching`, `PriceMaxAge`, `AllowedComparison`, `RequiredAttribution`, `DisclosureText`, `LinkExpiration`, `RawRetentionDays`, and `DataResidencyNotes`.

Unknown is a first-class value. Unknown means the connector may not publish or retain the affected field until a reviewer changes the policy record.

## Price truth and history

For permitted sources, store observations with amount, currency, availability, observed-at, fetched-at, source timestamp, and policy version. Keep a bounded history and daily snapshots for active items rather than an unlimited raw archive. For restricted sources, keep only the permitted current state, source reference, timestamp, and hash/audit facts; expose history as unavailable or partial.

The public model must distinguish:

- `RELIABLE`: enough permitted observations and freshness for the documented claim;
- `PARTIAL`: some history or source coverage exists but not enough for a strong claim;
- `UNAVAILABLE`: no permitted/usable history;
- `STALE`: current offer is outside its source-defined freshness window.

## Source and matching identifiers

Never use a retailer URL alone as proof of product identity. Preserve source external IDs, canonical IDs, raw identifier value, normalized identifier value, and identifier type. Matching decisions must be reversible and auditable. A merge or split may not delete the underlying source listing or audit record.

## Affiliate separation

`AffiliateProgram`, `AffiliateLink`, and `ClickEvent` are separate from `Deal` and its score inputs. A commission value may support business reporting, but it must not be available to the organic ranking calculation. The redirect uses an internal listing ID, an allowlisted destination, and an opaque sub-ID; arbitrary destination query strings are rejected.

## Import and idempotency keys

- external product/listing identity: `network + merchant + externalId`;
- source update: `connector + cursor/updateMarker`;
- observation: `listingId + sourceObservedAt + sourceHash`;
- alert delivery: `alertId + thresholdVersion + observationVersion`;
- click event: server-generated opaque event ID.

## Data classification and retention

- public catalog facts: source-permitted, bounded retention;
- account/email/alert data: personal data, minimum necessary, consent and deletion flow required;
- click telemetry: pseudonymous operational data, short retention and no unnecessary IP/email;
- provider credentials: secrets, never in source control or raw logs;
- raw feeds: restricted and short-lived only when contract permits.

## Future-proofing

The model supports additional networks, retailers, product categories, currencies, and mobile clients without exposing provider schemas. It does not assume that every merchant provides historical prices, stable IDs, images, or an API.
