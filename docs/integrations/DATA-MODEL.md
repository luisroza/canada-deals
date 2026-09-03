# Canada Deals - Canonical Data Model and Policy Contract

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint completed
**Date:** 2026-08-11

## Canonical entities

| Entity | Purpose | Key identifiers |
|---|---|---|
| `Product` | canonical same-product identity | internal ID; GTIN/UPC/EAN/ISBN where trusted; brand + MPN/model |
| `Brand` | normalized brand identity | unique normalized key; display name; immutable slug; future provider aliases/source mappings |
| `Category` | approved taxonomy and filters | stable slug, parent |
| `Retailer` | merchant identity and region | merchant key, country, currency |
| `AffiliateProgram` | implemented network/program relationship gate | provider, merchant, provider program/media/link IDs, lifecycle, deeplink permission, approved destination/tracking domains, validation evidence |
| `RetailerListing` | independent retailer-specific offer and current permitted state | retailer; external listing ID; retailer SKU; internal canonical product ID; original title; product URL; approved retailer destination reference; seller; marketplace seller flag; condition; variant attributes; pack quantity; bundle contents; region/availability context; online availability; shipping context; external identifiers; source timestamps; freshness; deal price; optional same-listing regular price with evidence; promotion start/end |
| `PriceObservation` | source observation when permitted | listing + observed-at + source hash, amount, currency, availability |
| `Deal` | derived public opportunity | listing/product, score inputs, freshness, evidence state |
| `PriceAlert` | implemented user-owned canonical Product threshold configuration | user + product, CAD target, status/version, consent, evaluation/trigger state |
| `NotificationDelivery` | implemented provider-neutral alert delivery intent/audit | alert + target version + price observation, channel/destination, target/qualifying price, attempts/retry schedule, provider ID, acceptance/delivery/event timestamps |
| `AccountConfirmationDelivery` | implemented account-confirmation delivery audit | user, destination, attempts, provider ID, provider lifecycle timestamps/status |
| `ControlledEmailCapture` | Development/Test-only deterministic evidence | stable idempotency key, destination, exact subject/HTML/text, captured timestamp |
| `ProcessedEmailWebhook` | provider webhook replay boundary | provider + unique event ID, type, message ID, provider/processed timestamps |
| `EmailSuppression` | minimal application send suppression | normalized destination, bounce/complaint/provider-suppression reason and timestamps |
| `SavedOffer` | implemented authenticated user intent; never a ranking or price-truth input | composite user + retailer-listing key, created-at |
| `AffiliateLink` | implemented provider-specific handoff | listing/program, exact tracking URL and destination, acquisition mode, handoff mode, validation/revalidation/expiry/failure state |
| `ClickEvent` | implemented minimum non-PII redirect telemetry | opaque event, link/listing, server-selected placement, timestamp |
| `RakutenAdvertiserCapability` | provider discovery and operator activation gate | MID, advertiser/partnership state, ships-to, feed/deep-link capabilities, Canada relevance, retailer/policy mapping, explicit affiliate/catalog enablement |
| `RakutenSourceMapping` | stable provider-to-listing idempotency | MID + source listing key, listing, first/last seen timestamps |
| `RakutenImportRun` | bounded import/dry-run audit | MID, dry-run/status/timestamps, page/record/write/skip/policy/review counters, safe failure reason |
| `ImportJob` | fetch/normalize/match execution | connector, cursor, state, retries, counts |
| `Connector` | provider adapter configuration | network/merchant, version, health, quota |
| `MerchantPolicy` | rights and retention controls | merchant/source, field flags, max age, reviewer, expiry |
| `MatchDecision` | product identity audit | candidate, method, confidence state, reviewer |
| `ListingIssueReport` | anonymous correction/review signal for a retailer listing | report ID; retailer listing ID; controlled reason; OPEN/REVIEWED/RESOLVED/DISMISSED status; optional bounded note; timestamps |
| `AuditRecord` | sensitive operational decision trail | actor/service, action, reason, before/after reference |

## Policy flags

Each source must have explicit values for:

`AllowPriceStorage`, `AllowPriceHistory`, `AllowImageCaching`, `AllowMetadataCaching`, `AllowAffiliateLinks`, `PriceMaxAge`, `AllowedComparison`, `RequiredAttribution`, `DisclosureText`, `LinkExpiration`, `RawRetentionDays`, and `DataResidencyNotes`.

Unknown is a first-class value. Unknown means the connector may not publish or retain the affected field until a reviewer changes the policy record.

## RetailerListing contract details

`RetailerListing` must preserve the following fields when a source provides them and must represent absent values as unknown/null rather than fabricated data:

- retailer and external listing ID;
- retailer SKU and canonical Product ID;
- original source title and product URL;
- approved affiliate destination reference, never an arbitrary client URL;
- seller and marketplace-seller indicator;
- condition;
- structured variant attributes and their original source values;
- pack quantity and bundle contents;
- region, online availability, and shipping context;
- external identifiers such as GTIN/UPC/EAN/ISBN/MPN/model;
- source-observed and fetched timestamps;
- freshness state;
- current permitted price amount, currency, and availability state.
- optional regular price amount/currency, observed-at, and evidence reference from the same listing; the regular amount must exceed the deal price;
- optional promotion validity start/end; public queries fail closed outside that window.

Category-specific dimensions belong in structured variant attributes, for example screen size/storage/RAM/generation for electronics and voltage/tool-only/battery/charger/pack quantity for tools. Keep raw source values alongside normalized values; do not create dozens of category-specific nullable columns.

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

`AffiliateProgram`, `AffiliateLink`, and `ClickEvent` are separate from `Deal` and its score inputs. A commission value may support business reporting, but it must not be available to the organic ranking calculation. Provider-generated links use an internal listing ID, an allowlisted destination, and an opaque sub-ID; arbitrary destination query strings are rejected. An owner-provided Amazon link uses a constrained direct handoff and is exposed only after its exact URL, program, hosts, Merchant Policy, Product identity, and disclosure pass validation.

Vertical Slice 9 implements provider values `IMPACT`, `CJ`, `RAKUTEN`, reserved `AMAZON_CREATORS`, and `OTHER`; program lifecycle `PENDING_APPROVAL`, `ACTIVE`, `SUSPENDED`, `EXPIRED`, `DISABLED`, and `CONFIGURATION_INCOMPLETE`; and link lifecycle `PENDING`, `ACTIVE`, `INVALID`, and `DISABLED`. Links also record `PROVIDER_GENERATED`/`OWNER_PROVIDED` acquisition and `INTERNAL_REDIRECT`/`DIRECT_PROVIDER` handoff. ACTIVE requires provider identifiers, current relationship evidence, explicit deep-link permission, and non-empty destination/tracking domain allowlists; Rakuten does not require the Impact/CJ media-property field. Amazon has no API adapter; its only implemented exception is the explicitly reviewed owner-provided direct-link path from ADR-010.

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
- listing issue reports: product-quality signals with no required name, email, full IP address, or other direct personal data; notes remain untrusted plain text.

## Implemented Saved Product contract

`SavedOffer` is implemented by migration `20260831135128_IndividualOfferPricing`. `(UserId, RetailerListingId)` is the composite primary key and duplicate guard. The migration deterministically maps each prior Product-level save to one existing listing before dropping `SavedProducts`. The User foreign key cascades because the row is user-owned intent; the RetailerListing foreign key restricts deletion so catalog changes cannot silently discard intent. User identity always comes from the authenticated server session and is never client-assigned. A save contains no price, evidence, Deal Quality, commercial economics, or ranking fields and does not change those systems.

## Individual-offer pricing boundary

Every public card/detail/Wishlist item is keyed by `RetailerListing.Id`. `ProductId` remains an internal normalization relationship for catalog identity, images, search terms, and administrative reuse. It does not deduplicate discovery results, select a public “representative” offer, or authorize comparing prices from different listings. `RegularPrice*` describes the ordinary price asserted for that exact listing; another listing, another retailer, or a historical high cannot populate it.

## Implemented Target Price Alert contract

Migration `20260811202709_AddPriceAlertsAndNotificationDeliveries` implements one configuration per `(UserId, ProductId)`. Targets are canonical-Product-level, CAD in the MVP, versioned on change/reactivation, and ACTIVE only for a confirmed account email with explicit consent timestamp/version. Alert configuration is separate from delivery attempts.

Eligibility uses current `PriceObservation` plus `RetailerListing`/`MerchantPolicy`: policy-permitted storage, safe confirmed/auto matching, online availability, matching currency, valid value, and source-defined freshness (24-hour default). History is not required. Deal Quality, affiliate commission, and Save/popularity are not inputs.

`NotificationDelivery` enforces unique `(PriceAlertId, TargetVersion, PriceObservationId)`. `IsBelowTargetCycle` prevents repeated notification while price remains continuously at/below target; above-target evaluation resets the cycle, and target change creates a new version. A durable delivery ID also produces the stable provider idempotency key. Development/Test persists exact captured evidence. Production distinguishes provider acceptance from webhook-confirmed delivery and never fabricates `Delivered`.

## Future-proofing

The model supports additional networks, retailers, product categories, currencies, and mobile clients without exposing provider schemas. It does not assume that every merchant provides historical prices, stable IDs, images, or an API.
# Multi-network source model — 2026-09-01

- `CatalogMerchantSource` uniquely identifies `(Provider, ProviderAdvertiserId, CatalogId)` and keeps provider discovery evidence separate from explicit Retailer, MerchantPolicy, default Category, approved destination-host, and operator activation decisions.
- `CatalogSourceMapping` uniquely identifies `(Provider, ProviderAdvertiserId, SourceListingKey)` and points to exactly one `RetailerListing`. It is the cross-provider idempotency boundary.
- `CatalogImportRun` stores bounded provider-neutral audit counters and safe failure codes; it stores neither provider payloads nor credentials.
- `ExternalOffer` is an in-memory normalization contract, not a raw-payload table. `ProviderMetadata` is bounded to approved scalar evidence.
- `Product` remains an internal canonical identity. `RetailerListing.Id` remains the public offer and Wishlist identity. Multiple sources/listings may reference one Product without public comparison or representative-card selection.
