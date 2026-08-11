# Database foundation

PostgreSQL is the system of record. EF Core/Npgsql owns the relational model and migrations.

## Initial entities

- `Brand`, `Category`, `Product`
- `Retailer`, `RetailerListing`
- `MerchantPolicy`, `PriceObservation`

`RetailerListing` stores source identifiers, original title, SKU, canonical product ID, URL, approved handoff reference, seller/marketplace information, condition, JSON structured variant attributes, pack/bundle fields, region/availability/shipping context, timestamps, freshness, current permitted price, matching state, and policy reference.

`UNKNOWN` policy values are explicit enum values. The API excludes protected fields when price storage is not `ALLOWED`.

## Migration

The first real migration is `20260811180731_InitialCreate`. It creates relational keys, unique source identity constraints, observation idempotency index, JSONB attribute columns, price precision, and the `pg_trgm` extension declaration.

Apply locally with:

```powershell
dotnet tool install --tool-path .tools dotnet-ef --version 10.0.4
.\.tools\dotnet-ef.exe database update --project src/backend/CanadaDeals.Infrastructure --startup-project src/backend/CanadaDeals.Api
```

Use the checked-in migration; do not use an in-memory database as the only persistence test.
