# Rakuten Advertising connector runbook

**Implementation status:** implemented and deterministically validated; disabled by default  
**Live status:** blocked pending a securely configured Publisher Account ID, rotated credentials, advertiser partnership, and merchant-specific data rights  
**Last official-documentation verification:** 2026-08-14

## Safety boundary

The connector is opt-in. Do not enable it merely because an application credential exists. Affiliate links and catalog ingestion are separately gated by an ACTIVE Rakuten advertiser/partnership snapshot, Canada relevance, explicit operator enablement, retailer mapping, MerchantPolicy mapping, and the applicable policy permissions. `UNKNOWN` blocks protected behavior.

Never paste credentials into source, documentation, command history, tickets, or chat. A secret disclosed in chat must be rotated before use. No currently disclosed credential was used to validate this slice.

## Required secure configuration

Set these only in a process environment or approved secret store:

- `Rakuten__AccountId`: Publisher Account ID; sent as OAuth `scope`.
- `Rakuten__ClientId`: OAuth client ID.
- `Rakuten__ClientSecret`: rotated OAuth client secret.
- `Rakuten__Enabled=true`: enables configuration and service use.
- `Rakuten__LiveDiscoveryEnabled=true`: allows read-only provider requests.

Optional bounded settings are defined under `Rakuten` in API/Worker configuration. Keep `Rakuten__CatalogImportEnabled=false` until the merchant checkpoint is approved. Never add secret-valued entries to `.do/app.yaml`; configure encrypted runtime variables in the deployment control plane only after approval.

## Authentication behavior

Client ID and Client Secret form the base64 token-key sent to the token endpoint. Publisher Account ID is mandatory `scope`. The connector keeps access and refresh tokens in process memory only, reuses a valid access token, renews before expiry, serializes concurrent refresh with one lock, and invalidates/retries once after an authentication failure. It does not request a token per API call.

Provider requests are paced conservatively and use bounded retries for `429` and transient `5xx`, honoring `Retry-After`. Logs include operation/path/status/delay but never authorization headers, token bodies, credentials, raw provider bodies, or contact records.

## Authorized sequence

1. Rotate any disclosed secret and configure all three required values securely.
2. Run read-only Partnerships + Advertisers discovery without persistence:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\integrations\rakuten-discover.ps1
   ```

3. Review counts and candidate MIDs. Do not infer rights from presence alone.
4. After human approval to retain the capability facts, rerun with `-PersistCapabilities`.
5. Record one retailer mapping, Canada relevance, relationship evidence, deep-link domains, Product Feed permission, fields, storage/history/image/retention/cadence limits, and MerchantPolicy review.
6. Enable only the approved capability dimensions. Affiliate enablement does not imply catalog enablement.
7. Run bounded Product Search dry-run for the approved MID:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\integrations\rakuten-product-dry-run.ps1 -AdvertiserMid 123456
   ```

8. Review received/skipped/policy-blocked/review counts and representative normalized records. Dry-run writes only the import audit.
9. Obtain a separate human approval before enabling catalog persistence or executing a controlled deep-link request.

The Worker also accepts `--rakuten-discover [--persist-capabilities]` and `--rakuten-dry-run <mid>`. Output is redacted and bounded.

## Mapping and matching

`RakutenAdvertiserCapability` stores provider capability/state and operator mapping, not approval assumptions. `RakutenSourceMapping` gives stable `(MID, source listing key)` idempotency. `RakutenImportRun` records bounded run results and safe failure codes.

Product Search is MID-scoped and XML-only. The parser disables DTD/external resolution and applies a document-size limit. Persistence accepts CAD only. Exact unique UPC may attach to an existing canonical Product; conflicting UPC and weak/title-only candidates go to review. The connector does not create canonical Products from weak evidence, cache retailer images, invent seller/marketplace/condition/availability, or use commission data in product truth/ranking.

## Failure and rollback behavior

- Missing/invalid configuration: startup/service call fails closed with configuration names only.
- Inactive/unknown advertiser or partnership: affiliate and catalog activation are disabled.
- Lost feed/deep-link capability: the related operator enable flag is revoked during reconciliation.
- `401`: cached token is invalidated and one authenticated retry is attempted.
- `429`/transient `5xx`: bounded retry; existing valid persisted affiliate links remain usable until their own validity boundary.
- XML/schema/provider failure: run is failed with a safe reason; partial catalog writes are rolled back.
- Policy denial/unknown: no protected field/listing/observation is persisted.
- Identifier conflict/weak match: candidate is counted for review; no canonical Product is created.

To stop activity, set `Rakuten__Enabled=false`, `Rakuten__CatalogImportEnabled=false`, and `Worker__EnqueueRakutenCatalogImportJob=false`. Do not delete prior audit/source records as an emergency response; disable the mapped capability/program and investigate.

## Evidence required before live activation

- secure credential rotation/configuration and successful scoped token request;
- read-only Advertisers and Partnerships snapshots with timestamps;
- ACTIVE advertiser and ACTIVE partnership for the exact MID;
- Canada relevance and allowed merchant/destination/tracking domains;
- explicit deep-link permission and one controlled generated-link result;
- explicit Product Feed/API entitlement and permitted fields;
- written metadata, price storage/history, image, retention, cadence, attribution, and termination rules;
- reviewed retailer and MerchantPolicy mapping;
- bounded Product Search dry-run evidence and reviewer approval;
- operational monitoring, rollback owner, and credential-rotation owner.

## Official references

- [Affiliate APIs](https://developers.rakutenadvertising.com/documentation/en/affiliate_apis)
- [Access tokens](https://developers.rakutenadvertising.com/guides/access_tokens)
- [Advertisers guide](https://developers.rakutenadvertising.com/guides/advertisers) and [reference](https://developers.rakutenadvertising.com/guides/advertisers/reference)
- [Partnerships guide](https://developers.rakutenadvertising.com/guides/partnerships) and [reference](https://developers.rakutenadvertising.com/guides/partnerships/reference)
- [Product Search guide](https://developers.rakutenadvertising.com/guides/product_search) and [reference](https://developers.rakutenadvertising.com/guides/product_search/reference)
- [Deep Links](https://developers.rakutenadvertising.com/guides/deep_link)
- [Offers](https://developers.rakutenadvertising.com/guides/offers)
