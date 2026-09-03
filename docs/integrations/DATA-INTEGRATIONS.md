# Canada Deals - Proposed Data Integration Architecture

**Status:** APPROVED STRATEGY - Human Architecture / Data Integration Checkpoint completed
**Prepared:** 2026-08-11
**Implementation status:** connector-neutral contracts, the provider-neutral affiliate boundary, Impact/CJ adapters, and an opt-in Rakuten Advertisers/Partnerships/Deep Links/Product Search connector are implemented. Rakuten has only controlled fixture validation; live credentials, merchant activation, and merchant-specific data rights remain blocked by their separate gates.

## Rakuten implementation profile

Rakuten follows the source-neutral flow without bypassing it: scoped OAuth with memory-only token reuse; read-only Advertisers/Partnerships correlation; persisted capability snapshot; MID-scoped bounded Product Search; safe XML normalization; dry-run audit; then policy/mapping/matching gates before any listing or price write. Strong exact unique UPC is the only automatic canonical attachment in this slice. Weak or conflicting identifiers go to review, and no canonical Product is created. CAD-only persistence, no image caching, and unknown source fields preserve the approved price-truth boundary.

## Executive recommendation

Build a **source-neutral ingestion platform** around approved affiliate/network APIs and product feeds. Begin with two launch candidates, **Best Buy Canada and Home Depot Canada**, only if a current affiliate/network relationship provides lawful product data and deep links. Treat **Amazon.ca as a gated candidate** because Associates/Creators API policies impose special rules for price, availability, caching, comparisons, attribution, timestamps, historical data, and mobile use. Keep Walmart Canada gated/fallback until the specifically Canadian relationship and feed rights are confirmed.

The connector contract must normalize provider data into canonical entities without leaking provider DTOs into the domain. Each source is controlled by a `MerchantPolicy` record that decides whether price storage, price history, image caching, metadata caching, comparison, attribution, and link expiration are allowed.

The connector-neutral core may be implemented before merchant approval using synthetic fixtures and test adapters. Slice 9 implements affiliate-link adapters only; this does not satisfy Product-data rights. No live retailer content or scraped data may be introduced as a shortcut. A merchant-specific catalog adapter may move from fixture-only to production only after the verified evidence listed in `INTEGRATION-BACKLOG.md` exists in the repository.

## Source policy hierarchy

1. A signed merchant/network contract and current program terms.
2. Provider API/feed documentation and quotas.
3. Repository policy record with reviewer, verification date, scope, and expiry.
4. Technical connector behavior.

If these disagree, the stricter rule wins and the source is quarantined until a human resolves it. “The endpoint returned the field” is not permission to retain or publish it.

## End-to-end ingestion flow

```text
Connector schedule
  -> credential and policy check
  -> fetch with timeout, rate limit, conditional request, and source timestamp
  -> raw envelope (short retention only if terms permit)
  -> schema validation
  -> normalize to canonical DTO
  -> policy filter
  -> deterministic product matching
  -> conflict/quarantine review
  -> idempotent upsert of listing and permitted price state
  -> freshness/deal evaluation
  -> search projection refresh
  -> alert eligibility and delivery job
  -> metrics, audit, and source health record
```

Every step is retryable. A failed fetch must not erase the last known permitted state; it must increase staleness and expose an honest unavailable/unknown state when the policy-defined freshness window expires.

## Connector contract

Each connector must provide, conceptually:

- `GetCapabilities`: API/feed type, countries, currencies, identifiers, image/content rights, deep-link support, quota, and policy flags.
- `DiscoverChanges`: cursor, last-modified/ETag or provider update marker where supported.
- `FetchPageOrBatch`: bounded page/batch with timeout and provider rate limit.
- `Normalize`: provider DTO to canonical product/listing/observation candidate.
- `BuildAffiliateLink`: approved destination, network tracking, opaque sub-ID, expiry/revalidation metadata.
- `HealthCheck`: last successful fetch, latency, row counts, errors, quota state, and policy expiry.
- `Replay`: reprocess a retained, permitted source envelope or normalized record without duplicating domain state.

No connector may write directly to public search or redirect tables. It writes through ingestion services that apply policy, matching, idempotency, and audit rules.

## Freshness strategy

Use adaptive tiers, constrained by source policy and quota:

- **Hot:** approximately 1-6 hours for high-intent/high-traffic listings when the source permits it.
- **Normal:** approximately 24 hours for active catalog records.
- **Cold:** approximately 3-7 days for low-traffic or low-change records.
- **Event-driven:** immediately after provider feed update markers, webhooks, or a user-requested recheck where allowed.

These are planning tiers, not a promise to poll every source at a fixed interval. Amazon's official Associates policy and API quota must determine its actual refresh behavior. The UI should show “most recently checked”, stale/unknown states, and the reason history is unavailable.

## Idempotency and failure controls

- Fetch idempotency: network + merchant + source request/cursor + source update marker.
- Listing idempotency: network + merchant + external listing/product ID.
- Observation idempotency: listing + observed-at bucket + source hash + provider version.
- Retry with bounded exponential backoff and jitter; respect `Retry-After`.
- Quarantine malformed rows, policy-denied fields, conflicting identifiers, and suspicious price/availability changes.
- Use a dead-letter/review state; never silently drop a source row.
- Record source time, fetch time, normalization version, policy version, connector version, and match decision.
- Use circuit breaking when a provider is failing or quota is exhausted.

## Matching strategy

1. Exact GTIN/UPC/EAN/ISBN, when present and trusted.
2. Exact manufacturer part number + normalized brand.
3. Exact model number + normalized brand.
4. Structured attributes: category, capacity, size, colour, pack count, generation, and variant.
5. Normalized title/brand candidate generation.
6. Fuzzy score only for review; it cannot publish a new canonical match by itself.

Internal states are `AUTO_MATCHED`, `CONFIRMED`, `POSSIBLE_MATCH_REVIEW`, `NO_MATCH`, and `MANUAL_REVIEW`. Public copy should expose understandable “same product” confidence, not raw algorithm scores.

## Source precedence and conflicts

For a field or observation, prefer: official merchant feed/API > affiliate network feed/API > approved partner feed > approved manual correction. When two trusted sources disagree, retain both source facts, mark the listing conflict, and block a strong Deal Quality claim until reviewed. Affiliate commission is not a precedence signal.

## Retention rules proposed

- Amazon Creators API responses: do not retain by default; store only permitted normalized state, source reference, timestamp, and policy version. Historical price storage is `RESTRICTED/UNKNOWN` until written terms and legal review permit it. Provider-vended links must remain unmodified.
- Other network/merchant raw feeds: retain encrypted snapshots for 7-30 days only if the contract permits; otherwise retain hashes, counts, and normalized audit facts.
- Permitted normalized price observations: retain up to 12 months for MVP, or the shorter source limit; extend only after review. Avoid an unlimited archive.
- Image caching: off by default unless permission is explicit. Prefer current authorized URLs.
- Metadata caching: field-level policy and maximum age, not a global assumption.

## Privacy and compliance boundary

Use minimum data for alerts and click telemetry. Do not send personal data to affiliate networks unless a contract and privacy review explicitly require it. Keep provider credentials out of the repository. Record consent, unsubscribe, source disclosure, and policy-review evidence.

## Research status

The following capabilities were verified against official documentation on 2026-08-11; merchant approval, Canadian coverage, and contractual permission remain separate unknowns:

- [Amazon Associates Canada help](https://associates.amazon.ca/help) and [Amazon policy](https://associates.amazon.ca/help/operating/policies?ac-ms-src=ac-nav).
- [Rakuten Deep Links API](https://developers.rakutenadvertising.com/guides/deep_link) and [advertiser capabilities](https://developers.rakutenadvertising.com/guides/advertisers/reference).
- [Awin product feed publisher guide](https://help.awin.com/developers/docs/product-feed-publisher-guide-intro) and [feed list/download API](https://help.awin.com/developers/docs/product-feed-list-download).
- [CJ product feeds](https://developers.cj.com/docs/data-imports/product-feeds) and [CJ developer portal](https://lab-developers.d.cjpowered.com/).
- [Impact product catalogs](https://help.impact.com/brand/platform-features/product-catalogs/add-product-catalogs-as-a-brand) and [partner marketplace](https://impact.com/partners/).

## Approved checkpoint refinements

- the source-neutral canonical model and policy engine are approved;
- launch may proceed with two non-Amazon retailers if their rights are verified; Amazon is not required for launch;
- any history, image, or raw-feed retention remains controlled by the source policy record;
- the first approved network/merchant contracts must be recorded after outreach;
- Amazon comparison, caching, disclosure, and historical claims remain a legal/policy gate;
- freshness targets are configured only after each approved source provides quota and update evidence.
# Multi-network catalog ingestion — 2026-09-01

Rakuten, eBay Browse, Impact Catalog, Awin Product Feed, and CJ Product Search now share the provider-neutral `IOfferCatalogSource` boundary. Adapters normalize only source-proven fields into bounded `ExternalOffer` records. Provider concepts, raw payloads, credentials, commission, and tracking metadata do not enter Product domain entities or public APIs.

The common importer requires an explicit provider/advertiser/catalog → Retailer mapping, default Category, approved destination hosts, active/access relationship evidence, Canada relevance, and a fail-closed MerchantPolicy. CAD is required; there is no FX conversion. Strong GTIN/UPC or Brand + MPN identity may match/create a Product; title-only/fuzzy identity routes to review. Source listing identity is deterministic and unique, so repeat imports update one independent `RetailerListing`. Connector image URLs are not persisted or displayed by this increment.

All new providers and global persistence are disabled by default. Discovery is read-only, dry-run does not mutate catalog state, and live merchant activation remains blocked pending account-specific credentials and rights. See `CATALOG-PROVIDERS.md` and `../operations/CATALOG-SOURCES.md`.
