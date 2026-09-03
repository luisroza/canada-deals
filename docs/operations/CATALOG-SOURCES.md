# Catalog Sources Operations

**Status:** technical implementation validated with deterministic fixtures; live provider/merchant activation blocked  
**Default posture:** every provider disabled, discovery read-only, persistence disabled

## Architecture

`IOfferCatalogSource` isolates provider authentication, pagination/feed parsing, and field mapping. Every adapter emits a bounded `ExternalOffer`. `CatalogImportService` then applies the shared sequence:

```text
provider response
  -> bounded parse and normalization
  -> explicit CatalogMerchantSource mapping
  -> approved destination-host and CAD validation
  -> MerchantPolicy gate
  -> strong Product identity or review
  -> idempotent CatalogSourceMapping
  -> independent RetailerListing
  -> permitted same-listing PriceObservation
```

The importer never creates a public provider-specific API, never compares retailers, never stores raw provider payloads, never downloads connector images, and never activates affiliate handoff merely because catalog data is readable.

## Configuration

Secrets are server/worker-only environment variables. Do not place values in appsettings or browser/admin fields.

| Provider | Required names for live discovery | Optional names |
|---|---|---|
| Rakuten | `Rakuten__AccountId`, `Rakuten__ClientId`, `Rakuten__ClientSecret` | existing Rakuten pacing/page settings |
| eBay | `CatalogProviders__Ebay__ClientId`, `CatalogProviders__Ebay__ClientSecret` | `CatalogProviders__Ebay__AffiliateCampaignId`, privacy-safe `AffiliateReferenceId` |
| Impact | `Affiliate__Impact__AccountSid`, `Affiliate__Impact__AuthToken` | none |
| Awin | `CatalogProviders__Awin__DataFeedApiKey` | bounded timeout/feed-byte limit |
| CJ | `Affiliate__Cj__PersonalAccessToken`, `CatalogProviders__Cj__WebsiteId` | `CatalogProviders__Cj__CandidateAdvertiserIds__0...` |

Enable only the provider being inspected (`CatalogProviders__{Provider}__Enabled=true`; Rakuten retains `Rakuten__Enabled=true`). `CatalogIngestion__PersistenceEnabled` must remain `false` through discovery and dry-run.

## Discovery

Discovery makes live provider calls. It never imports offers. Without `-PersistSnapshot` it does not write the database.

```powershell
./scripts/integrations/catalog-discover.ps1 -Provider ebay
./scripts/integrations/catalog-discover.ps1 -Provider impact -PersistSnapshot
./scripts/integrations/catalog-discover.ps1 -Provider awin
```

`-PersistSnapshot` records only bounded capability/catalog metadata. It does not map a Retailer, grant rights, or enable a catalog.

## Mapping and dry-run

Before dry-run, an operator must review and configure one `CatalogMerchantSource` row with:

- exact provider + advertiser + catalog identity;
- explicit `RetailerId` and default `CategoryId`;
- reviewed `MerchantPolicyId`;
- one or more approved destination hosts;
- verified active/access relationship and Canada relevance;
- `CatalogEnabled=false` (`READY_FOR_DRY_RUN`).

Then run:

```powershell
./scripts/integrations/catalog-dry-run.ps1 -Provider impact -AdvertiserId 123456 -CatalogId 4321
./scripts/integrations/catalog-dry-run.ps1 -Provider ebay -AdvertiserId EBAY_CA -Query headphones
```

Dry-run fetches, parses, validates, maps, and classifies records but never mutates Product, RetailerListing, or PriceObservation. Its durable audit reports valid/CAD/mapped/unmapped/policy-blocked/review/invalid/unsupported-currency counts.

## Activation

Live persistence requires all of the following:

1. read-only discovery evidence;
2. account-specific relationship/feed/API entitlement;
3. Canada/CAD evidence;
4. explicit merchant and destination-host mapping;
5. field-by-field MerchantPolicy review;
6. successful bounded dry-run with identifier conflicts reviewed;
7. operator sets `CatalogEnabled=true` and separately sets `CatalogIngestion__PersistenceEnabled=true` for the worker;
8. first import is one provider, one merchant, and bounded records.

Affiliate link activation is a separate `AffiliateProgram`/handoff review. `ProviderAffiliateUrl` in normalized input never grants outbound activation.

## Disable and outage handling

Set the provider `Enabled=false` or global `CatalogIngestion__PersistenceEnabled=false`; then set the source `CatalogEnabled=false`. Existing public reads and valid handoffs do not call catalog providers and remain available during provider outages. Relationship loss reconciles the source to a blocked state. Do not delete mappings or audit history.

## Limits and retries

- All HTTP payloads, pages, records, metadata, XML depth/entities, and Awin decompressed bytes are bounded.
- `401/403` and relationship/rights failures do not retry indefinitely.
- `429` honors a short `Retry-After`; longer waits return a rate-limited failure for scheduled retry.
- Transient `5xx` gets one bounded retry. Hangfire jobs have one delayed retry and prohibit concurrent execution.
- Awin feed downloads stream incrementally. The feed-list URL is fetched with HTTP-client logging removed because its path contains the data-feed key. Only approved Awin feed hosts are accepted.

Provider-specific operating limits must be rechecked before activation. eBay documents application call limits and Buy API production eligibility; Impact documents Product Search rate headers/limits; Awin recommends checking `Last Imported`, staggering downloads, and waiting five minutes after failure; CJ account/API limits remain authoritative.

## Security and troubleshooting

- No command prints authorization headers, keys, tokens, or signed feed URLs.
- Catalog HTTP clients disable automatic redirects; a provider `3xx` fails closed instead of escaping the approved origin/host boundary.
- eBay custom references must be privacy-safe and contain no email, user ID, search history, or other PII.
- Provider destination URLs must be HTTPS, credential-free, and match an operator-approved host.
- XML uses DTD prohibition and no resolver. Awin gzip/CSV parsing is bounded and rejects truncated quoted records.
- `*_AUTHENTICATION_FAILED`: rotate/check secret-store values; do not retry continuously.
- `*_ACCESS_DENIED` / relationship blocked: verify publisher relationship and exact feed/catalog entitlement.
- `*_RATE_LIMITED`: honor provider window and reduce cadence/page count.
- `*_MALFORMED_RESPONSE`: disable the source, retain the safe error code, and update fixtures only after reviewing current official documentation.
- `CATALOG_MERCHANT_POLICY_BLOCKED`: review rights; do not change policy merely to make import pass.
