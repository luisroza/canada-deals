# Store Affiliate Banners

## Status

The provider-neutral store banner system is implemented. Controlled fixtures exercise ACTIVE and DISCOVERY_ONLY states; no real merchant is activated.

## Runtime flow

```text
StoreBanner
-> /go/store/{retailerKey}
-> enabled Retailer
-> affiliate-permitted MerchantPolicy
-> ACTIVE AffiliateProgram
-> usable StoreAffiliateDestination
-> HTTPS and merchant/tracking domain validation
-> minimal ClickEvent (placement=store_banner)
-> 302 to the persisted tracking URL
```

The browser never submits a destination URL and provider APIs are not called during the click. Invalid or inactive configuration fails closed. A discovery-only banner links to `/?retailer={key}#deals` instead.

## Adding or changing a banner

1. Create or locate the `Retailer`; keep it disabled until catalog eligibility is intentional.
2. Add one `StoreBannerProfile` with concise factual copy, neutral `BannerOrder`, and a first-party `/store-banners/*.svg` asset.
3. Use `CanadaDealsOriginal` and `BrandAssetPolicy=UNKNOWN` unless approved merchant creative evidence exists.
4. Confirm the banner appears as DISCOVERY_ONLY before affiliate activation.
5. To activate outbound handoff, separately verify the merchant relationship, affiliate-permitted policy, provider identifiers, destination/tracking domains, storefront destination rights, tracking URL, validation/revalidation time, and expiry.
6. Persist one `StoreAffiliateDestination` for the ACTIVE program and run a controlled `302` smoke test.

No affiliate secret belongs in a banner profile, frontend component, SVG, or destination row.

## Official merchant assets

An official logo/banner may be configured only with `AssetSource=MerchantApprovedAffiliateAsset`, `BrandAssetPolicy=ALLOWED`, a redacted evidence reference, allowed placement, effective date, and expiry when supplied. UNKNOWN or expired rights revert to Canada Deals original art. External asset hosts require a separate minimal CSP review; wildcard sources are prohibited.

## Disabling

- Disable only the external destination: set the destination status to Disabled; the banner becomes discovery-only.
- Disable the banner: set `StoreBannerProfile.IsEnabled=false`.
- Disable the retailer: set `Retailer.IsEnabled=false`; the API omits it and store handoff returns `404`.
- Suspend the relationship: change `AffiliateProgram.Status`; both listing and store handoff fail closed.

## Ordering and privacy

Banner order is `BannerOrder`, then retailer name. Commission, EPC, conversion, click counts, saves, and user data are not ordering inputs. Store click events contain opaque IDs, retailer/program/destination, `store_banner`, and timestamp only—no email, account, raw IP, fingerprint, or browsing profile.
