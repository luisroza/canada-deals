# Canada Deals - Affiliate Network Evaluation

**Status:** APPROVED STRATEGY - Human Architecture / Data Integration Checkpoint completed
**Date checked:** 2026-08-14

Scores are inferred screening scores from 0-100. They do not represent approval, commercial terms, merchant coverage, or legal permission.

| Network | Verified capability | Canada/merchant uncertainty | Data path | Link path | MVP posture |
|---|---|---|---|---|---|
| Amazon Associates Canada / Creators API | Associates account, marketplace Partner Tag, eligible Creators API access, provider-vended links | strict policies, account eligibility, rate allocation, Canadian program terms, no link alteration | official Creators API only; no silent extraction | unmodified provider-vended link | **Gated; no adapter** |
| Rakuten Advertising | scoped OAuth, Advertisers v2, Partnerships, Product Search XML, deep-links API, privacy-safe `u1` | Publisher Account ID, advertiser partnership, Canada coverage, and merchant rights must be confirmed | MID-scoped Product Search where enabled | one validated provider deep link/request | **Connector implemented; live activation blocked** |
| Awin | product feeds, feed update endpoint, publisher APIs, deep links | publisher membership, advertiser availability, Canadian merchant coverage | CSV/XML/other approved feeds | feed deep link / advertiser link | **Fallback candidate** |
| CJ Affiliate | PAT-authenticated publisher API, Link Search XML, joined relationship, per-link deep-link flag; 25 Link Search calls/minute | Canada Deals publisher/PID and Home Depot advertiser approval absent | catalog/feed deferred | provider-returned `clickUrl`; no manufactured parameters | **Adapter implemented, awaiting approval/credentials** |
| impact.com | Basic-auth publisher API, active contract, `AllowsDeeplinking`, `DeeplinkDomains`, media property, regular tracking-link creation and Sub IDs; standard “other” endpoints currently 1,000/hour with `429`/`Retry-After` | Canada Deals account/Best Buy relationship/IDs absent | catalog deferred | Tracking Link API | **First adapter implemented, awaiting approval/credentials** |

## Recommended order

1. Apply to Best Buy Canada through Impact; record ACTIVE contract, ProgramId, approved MediaPartnerPropertyId, current deeplink domains, and a controlled Tracking Link result.
2. Apply to Home Depot Canada through CJ; record Canada Deals PID/PAT, joined advertiser CID, approved Link ID/deep-link behavior, and a controlled provider-returned `clickUrl`.
3. Keep Amazon.ca gated until Associates Canada and eligible Creators API access/Partner Tag are approved; never alter provider-vended links.
4. Keep Walmart Canada gated until Canada Deals joins the specifically Canadian program and current linking/data rights are recorded. Walmart.ca currently points applicants to Rakuten, but that is not Canada Deals approval.

## Non-negotiable network controls

- never generate a link for a merchant/program that is not marked `APPROVED` in the policy table;
- use server-side allowlisted redirect records rather than client-supplied URLs;
- keep commission and conversion data separate from price-truth and Deal Quality;
- preserve required disclosure and attribution text adjacent to retailer CTAs;
- store only the minimum click/sub-ID data and define retention;
- treat provider quotas and link expiry as operational data;
- make source removal reversible without deleting canonical product identity.

## Evidence notes

- Amazon Creators API: [migration guidance](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/migrating-to-creatorsapi-from-paapi), [marketplace/Partner Tag parameters](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/concepts/common-request-headers-and-parameters), [eligibility/errors](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/troubleshooting/error-codes-and-messages), and [link integrity/rates](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/concepts/api-rates) - VERIFIED 2026-08-12.
- Rakuten: [Deep Links API](https://pubhelp.rakutenadvertising.com/hc/en-us/articles/5949836672653-Deep-Links-API), [developer deep-link guide](https://developers.rakutenadvertising.com/guides/deep_link) - VERIFIED 2026-08-11.
- Awin: [product feed guide](https://help.awin.com/developers/docs/product-feed-publisher-guide-intro) - VERIFIED 2026-08-11.
- CJ: [PAT authentication](https://developers.cj.com/authentication/overview), [Link Search contract/rate](https://developers.cj.com/docs/rest-apis/link-search), and [product feed capability](https://developers.cj.com/docs/data-imports/product-feeds) - VERIFIED 2026-08-12.
- Impact: [tracking-link creation](https://integrations.impact.com/impact-publisher/reference/create-a-tracking-link), [program contract/deeplink fields](https://integrations.impact.com/impact-publisher/reference/the-campaigns-object), [media properties](https://integrations.impact.com/impact-publisher/reference/media-properties-overview), and [rate limits](https://integrations.impact.com/impact-publisher/reference/rate-limits) - VERIFIED 2026-08-12.

## Implemented Rakuten boundary

The Rakuten adapter is disabled by default and uses Client ID + Client Secret only to form the token-key; the Publisher Account ID remains a separate mandatory OAuth scope. Tokens are cached/refreshed in memory. Read-only Advertisers/Partnerships discovery precedes activation. Product Search and Deep Links require the exact advertiser MID and remain behind explicit capability, operator, retailer, policy, Canada, and host gates. Commission/offer fields are not ingested into Product truth or ranking.

Official current contracts: [access tokens](https://developers.rakutenadvertising.com/guides/access_tokens), [advertisers](https://developers.rakutenadvertising.com/guides/advertisers/reference), [partnerships](https://developers.rakutenadvertising.com/guides/partnerships/reference), [Product Search](https://developers.rakutenadvertising.com/guides/product_search/reference), and [deep links](https://developers.rakutenadvertising.com/guides/deep_link) — VERIFIED 2026-08-14.

## Store-level affiliate boundary

Storefront destinations are persisted separately from listing-level `AffiliateLink` rows and are never generated during a shopper click. `/go/store/{retailerKey}` applies the same ACTIVE relationship, HTTPS, destination-domain, tracking-domain, expiry, provider, and Rakuten capability gates as product handoff. Impact, CJ, Rakuten, Amazon, or another provider is not activated merely because a visual banner exists. Candidate banners remain `DISCOVERY_ONLY` until merchant-account approval, destination rights, tracking URL, allowlists, and evidence are configured.
