# Database foundation

PostgreSQL is the system of record. EF Core/Npgsql owns the relational model and migrations.

## Initial entities

- `Brand`, `Category`, `Product`
- `Retailer`, `RetailerListing`
- `AffiliateProgram`, `AffiliateLink`, `ClickEvent`
- `StoreBannerProfile`, `StoreAffiliateDestination`
- `RakutenAdvertiserCapability`, `RakutenSourceMapping`, `RakutenImportRun`
- `MerchantPolicy`, `PriceObservation`
- `ListingIssueReport`
- ASP.NET Core Identity tables, `SavedProduct`, `PriceAlert`, `NotificationDelivery`, `AccountConfirmationDelivery`, `ControlledEmailCapture`, `ProcessedEmailWebhook`, and `EmailSuppression`

Slice 7 migrations `20260812134659_AddProductionEmailDelivery` and `20260812135446_AddEmailRetrySchedule` add provider message/status timestamps, durable confirmation deliveries, exact Development/Test captures, replay-safe webhook events, normalized suppression, and persisted retry scheduling. Slice 8 migration `20260812143802_AddPersistentDataProtectionKeys` adds the ASP.NET Core Data Protection key ring table. Slice 9 migration `20260812213653_AddAffiliateLinkProviders` adds provider-neutral program lifecycle, persisted validated/failure link state, and minimum non-PII click telemetry. Migration `20260814173414_AddRakutenConnector` adds the MerchantPolicy affiliate permission and the three Rakuten capability/source/run tables. Provider credentials and tokens are never stored in these tables.

`RetailerListing` stores source identifiers, original title, SKU, canonical product ID, URL, approved handoff reference, seller/marketplace information, condition, JSON structured variant attributes, pack/bundle fields, region/availability/shipping context, timestamps, freshness, current permitted price, matching state, and policy reference.

`UNKNOWN` policy values are explicit enum values. The API excludes protected fields when price storage is not `ALLOWED`.

## Migration

The migration chain is:

1. `20260811180731_InitialCreate`
2. `20260811185543_AddListingIssueReports`
3. `20260811192055_AddIdentityAndSavedProducts`
4. `20260811202709_AddPriceAlertsAndNotificationDeliveries`
5. `20260811205846_AddPostgresProductSearch`
6. `20260812134659_AddProductionEmailDelivery`
7. `20260812135446_AddEmailRetrySchedule`
8. `20260812143802_AddPersistentDataProtectionKeys`
9. `20260812213653_AddAffiliateLinkProviders`
10. `20260814173414_AddRakutenConnector`
11. `20260820185739_AddStoreAffiliateBanners`
12. `20260820191750_AddStoreBannerAssetRightsMetadata`
13. `20260824132853_AddOwnerAdminPanel`

No earlier migration was modified retroactively.

`AddOwnerAdminPanel` adds `RetailerListings.IsEnabled` with a safe `TRUE` backfill/default so an offer can be drafted or reversibly deactivated without disabling its retailer or deleting history. Public discovery, Product detail, store eligibility, and listing affiliate handoff all exclude disabled listings. It also adds `AdminAuditEvents`, linked restrictively to the Identity actor, with action/entity/summary/time indexes and no password, token, IP address, or raw authorization data.

`StoreBannerProfiles` uniquely configures one retailer banner with original/merchant-approved asset provenance, tri-state brand rights, approved asset provider and placement, evidence, neutral editorial order, enabled state, effective/expiry rights, and a reviewed first-party asset path. Expired or incomplete merchant rights fall back to Canada Deals original artwork rather than removing the store. `StoreAffiliateDestinations` uniquely stores one current provider-neutral storefront destination per affiliate program without inventing a `RetailerListing`. `ClickEvents` now supports exactly one product-link source or one store-destination source through `CK_ClickEvents_Source`; store clicks include retailer/program IDs and no user, email, IP, fingerprint, or query history. Retailer, program, banner, destination, and click relationships use restrictive deletion.

`RakutenAdvertiserCapabilities` has a unique MID and records provider state separately from explicit operator mapping/enablement. `RakutenSourceMappings` uniquely binds `(AdvertiserMid, SourceListingKey)` and one listing to prevent duplicate ingestion. `RakutenImportRuns` records dry-run/live status and bounded counters without response payloads or secrets. Foreign keys use restrictive behavior so capability/policy/source audit is not silently erased.

The Slice 9 Rakuten validation database applied all ten migrations from empty, ran a second current/no-op migration, and contained the Rakuten tables, affiliate tables, and `pg_trgm`. The 132-test PostgreSQL/provider suite then passed with zero skips.

`AffiliatePrograms` has one provider relationship per `(RetailerId, Provider)` and stores only operational identifiers, current lifecycle, deep-link permission, approved destination/tracking domains, and redacted evidence references. `AffiliateLinks` retains provider-returned tracking URL, exact retailer destination, validation/revalidation/expiry state, and bounded failure reason. `ClickEvents` stores only an opaque ID, link/listing IDs, server-selected placement, and timestamp; it has no email, user ID, full IP, fingerprint, or arbitrary query data. Affiliate tables are separate from Product search, Deal Quality, price truth, and alert eligibility.

`ListingIssueReports` references `RetailerListings` with `RESTRICT` delete behavior so quality/audit signals are not silently cascade-deleted. It stores a typed reason, typed lifecycle status, optional 500-character plain-text note, and creation/update timestamps. The `(Status, CreatedAt)` index supports review of OPEN reports in creation order. Multiple reports per listing are intentionally allowed.

`SavedProducts` has composite primary key `(UserId, ProductId)`, which is also the database-level duplicate guard. User deletion cascades only the user's intent records; Product deletion is `RESTRICT` so canonical intent is not silently erased. `(UserId, CreatedAt)` supports the current user's newest-first list. Identity's normalized username uniqueness prevents normalized-equivalent email accounts because email is also the initial username.

`PriceAlerts` has a unique `(UserId, ProductId)` index, target range/version check constraints, user `CASCADE`, Product `RESTRICT`, consent timestamp/version, target version, and durable continuous-below-target state. `NotificationDeliveries` is separate from alert configuration and stores channel/destination, target and qualifying price, attempt/status/reason timestamps, and a unique `(PriceAlertId, TargetVersion, PriceObservationId)` durable deduplication key. Observation deletion is `RESTRICT`; alert/user deletion cascades owned delivery audit rows.

`AddPostgresProductSearch` adds a maintained Product search projection (`SearchDocument`), normalized model and manufacturer-part identifiers, and a generated English `tsvector`. It backfills existing Products from title, Brand, Category, model/MPN, and GTIN without rewriting prior migrations. PostgreSQL indexes are a GIN full-text index, a GIN `gin_trgm_ops` index for controlled typo fallback, normalized identifier indexes, and Listing observed-time/price/availability-match indexes. The public discovery query remains policy-first: data with a denied or `UNKNOWN` price-storage policy cannot enter its search projection, result set, ranking, or facets.

Apply locally with:

```powershell
dotnet tool install --tool-path .tools dotnet-ef --version 10.0.4
.\.tools\dotnet-ef.exe database update --project src/backend/CanadaDeals.Infrastructure --startup-project src/backend/CanadaDeals.Api
```

Use the checked-in migration; do not use an in-memory database as the only persistence test.

The Slice 5 validation run created a separate empty PostgreSQL 17 database, applied all five migrations, confirmed a second update was already current, verified `pg_trgm` and all four Product search indexes, then ran all 69 integration tests with zero skips. `EXPLAIN (ANALYZE, BUFFERS)` confirmed bitmap scans through `IX_Products_SearchVector` for FTS and `IX_Products_SearchDocument` for trigram word search. Development fixture seed remains serialized by a PostgreSQL transaction-scoped advisory lock.

## Product-history query

Vertical Slice 6 adds no schema change or migration. The API resolves one Product, selects only safely matched and policy-permitted listings, then queries `PriceObservations` with a server-side 30- or 90-day lower bound and current-time upper bound. It does not load lifetime observations for the requested chart. A separate aggregate `MIN(ObservedAt)` over retained qualifying evidence supports truthful `Tracking since` copy.

Multiple qualifying observations on the same UTC date become one public point using the lowest safely matched, policy-permitted new-product CAD price for that day; the point retains the number of actual observations. Missing dates are never inserted or interpolated. The existing `IX_RetailerListings_ProductId` and unique `IX_PriceObservations_RetailerListingId_ObservedAt_SourceHash` indexes cover the Product-to-listing lookup and listing/time range. Slice 6 `EXPLAIN (ANALYZE, BUFFERS)` with sequential scans disabled used both indexes (0.289 ms on the small controlled fixture database), so no speculative index was added.

A separate empty PostgreSQL 17 database named `canadadeals_slice6_validation` applied all five existing migrations in order. A second application was current, and the complete 80-test PostgreSQL integration suite then passed with zero skips.

Slice 7 repeated that validation on `canadadeals_slice7_validation`: all seven migrations applied from empty, the second application reported current, and all 87 PostgreSQL integrations passed with zero skips. The exact temporary database was then removed.

Slice 8 validated the full eight-migration chain in `canadadeals_slice8_validation`. The API `--migrate-only` command applied the clean database without fixture seeding; its second execution was current. `DataProtectionKeys` and `pg_trgm` were present, and all 91 PostgreSQL integrations passed with zero skips. In Production, the Data Protection key ring is persisted here and encrypted with the configured PFX; startup rejects an absent certificate or non-persistent configuration.
