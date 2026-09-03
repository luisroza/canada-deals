# Catalog Provider Status

**Status:** IMPLEMENTED WITH DETERMINISTIC FIXTURES — LIVE VALIDATION BLOCKED  
**Checked:** 2026-09-01

Implementation means that a disabled, provider-specific adapter exists behind the common catalog boundary and passes network-free contract tests. It does not mean that Canada Deals has credentials, a publisher relationship, a feed entitlement, data rights, image rights, affiliate rights, or permission to activate a merchant.

| Provider | Implementation | Live credentials | Discovery | Merchant candidate | Catalog rights | Affiliate rights | Live import | Blocker |
|---|---|---|---|---|---|---|---|---|
| Rakuten | IMPLEMENTED; existing OAuth/XML connector also implements the common boundary | absent | fixture validated; operator command retained | Newegg.ca and Walmart Canada are unverified candidates only | unknown | unknown | blocked | Account ID, rotated credentials, ACTIVE partnership, MID, Canada relevance, feed entitlement, and MerchantPolicy evidence |
| eBay | IMPLEMENTED; Browse API, `EBAY_CA`, OAuth client credentials, pagination, seller/condition/shipping, optional EPN context | absent | fixture validated; live read-only command available | eBay.ca | Buy API production eligibility and Canadian inventory access unverified | EPN approval/campaign absent | blocked | developer credentials, Buy API production approval/license, EPN status, policy and destination-host review |
| Impact | IMPLEMENTED; accessible-catalog discovery and catalog-item pagination using existing AccountSID/AuthToken boundary | absent | fixture validated; live read-only command available | Best Buy Canada, Lenovo Canada, Sport Chek, Mark's, Staples Canada are candidates only | account catalogs unknown | publisher relationships unknown | blocked | publisher credentials, accessible catalog IDs, Canada/CAD evidence, merchant policy and relationship evidence |
| Awin | IMPLEMENTED; official feed-list download plus bounded streaming CSV/gzip parser | absent | fixture validated; live read-only command available | Samsung Canada is a candidate only | accessible/joined feeds unknown | publisher relationships unknown | blocked | data-feed API key, joined feed, Canada/CAD evidence, approved feed URL and merchant policy |
| CJ | IMPLEMENTED; Product Search v2 parser separated from existing Link Search affiliate adapter | absent | fixture validated; candidate CIDs must be configured explicitly | Wayfair Canada is a candidate only | Product Search/feed availability unknown | joined relationship/PAT/PID unknown | blocked | PAT, PID, exact advertiser CID, joined access, product-data entitlement and destination policy |
| Amazon Creators API | GATED | absent | not implemented | Amazon.ca | eligibility unknown | owner-entered approved links only | blocked | `AMAZON_CREATORS_API=GATED_PENDING_ELIGIBILITY`; no HTML retrieval or scraping |
| Etsy | DEFERRED | absent | not implemented | none | unknown | unknown | blocked | Phase 2 only |

## Merchant candidate matrix

No row below is an approval. Blank/unknown provider IDs are deliberate.

| Merchant | Candidate network/source | Advertiser ID | Relationship | Canada relevance | Catalog/feed | Deep-link capability | Currency | Image rights | Price rights | Activation |
|---|---|---|---|---|---|---|---|---|---|---|
| Newegg.ca | Rakuten candidate | unknown | unknown | unverified | unknown | unknown | unknown | unknown | unknown | GATED |
| eBay.ca | eBay Browse | marketplace `EBAY_CA` | developer/EPN status unknown | technical marketplace target verified | API implementation only | optional EPN header implemented; approval absent | adapter requires CAD for publication | unknown | unknown | GATED |
| Best Buy Canada | Impact candidate | unknown | unknown | unverified in owner account | unknown | unknown | unknown | unknown | unknown | GATED |
| Lenovo Canada | Impact candidate | unknown | unknown | unverified in owner account | unknown | unknown | unknown | unknown | unknown | GATED |
| Sport Chek | Impact candidate | unknown | unknown | unverified in owner account | unknown | unknown | unknown | unknown | unknown | GATED |
| Mark's | Impact candidate | unknown | unknown | unverified in owner account | unknown | unknown | unknown | unknown | unknown | GATED |
| Samsung Canada | Awin candidate | unknown | unknown | unverified in owner account | unknown | unknown | unknown | unknown | unknown | GATED |
| Wayfair Canada | CJ candidate | unknown | unknown | unverified in owner account | unknown | unknown | unknown | unknown | unknown | GATED |
| Amazon.ca | owner-entered links; Creators API gated | n/a | owner evidence required | Canada URL required | API eligibility unknown | exact approved owner link only | manually reviewed | unknown | unknown | GATED |
| Walmart Canada | Rakuten candidate | unknown | unknown | unverified | unknown | unknown | unknown | unknown | unknown | GATED |

## Current official contracts used

- eBay: [Browse API overview and EPN request context](https://developer.ebay.com/api-docs/buy/static/api-browse.html), [Browse API reference](https://developer.ebay.com/develop/api/buy/browse_api), and [API call limits](https://developer.ebay.com/develop/get-started/api-call-limits).
- Impact: [catalog item search](https://integrations.impact.com/impact-publisher/reference/search-catalog-items), [catalog object](https://integrations.impact.com/impact-publisher/reference/the-catalogs-object), and [catalog item object](https://integrations.impact.com/impact-publisher/reference/the-catalog-item-object).
- Awin: [Product Feed List Download](https://help.awin.com/developers/docs/product-feed-list-download) and [Product Feed Publisher Guide](https://help.awin.com/developers/docs/product-feed-publisher-guide-intro).
- CJ: [Product Search](https://developers.cj.com/docs/rest-apis/product-search), [Product Feeds](https://developers.cj.com/docs/data-imports/product-feeds), and [authentication overview](https://developers.cj.com/authentication/overview).
