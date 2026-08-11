# Canada Deals - Affiliate Network Evaluation

**Status:** APPROVED STRATEGY - Human Architecture / Data Integration Checkpoint completed
**Date checked:** 2026-08-11

Scores are inferred screening scores from 0-100. They do not represent approval, commercial terms, merchant coverage, or legal permission.

| Network | Verified capability | Canada/merchant uncertainty | Data path | Link path | MVP posture |
|---|---|---|---|---|---|
| Amazon Associates Canada / PA API | Associates account, PA API, product content, prices/availability, tracking | strict policies, account eligibility, quota, and Canadian program terms | official PA/API only; no silent extraction | tagged Amazon link under policy | **Gated candidate** |
| Rakuten Advertising | advertiser lookup, product catalogs, deep-links API, tracking/custom `u1` | advertiser partnership and Canada coverage must be confirmed per merchant | catalog/feed or API where enabled | Bearer-token deep links; one link/request and documented rate limit | **Priority network** |
| Awin | product feeds, feed update endpoint, publisher APIs, deep links | publisher membership, advertiser availability, Canadian merchant coverage | CSV/XML/other approved feeds | feed deep link / advertiser link | **Fallback candidate** |
| CJ Affiliate | product feeds, Product Feed API/GraphQL, link search and advertiser tools | account/advertiser approval, exact Canada coverage, terms | product feed/API | CJ tracking/link tools | **Evaluate after first approvals** |
| impact.com | partner marketplace, product catalogs, API-managed catalog capability, deep links where enabled | program availability, contract rights, Canadian coverage | catalog/feed/API | approved deep link/custom landing page | **Evaluate after first approvals** |

## Recommended order

1. Contact/validate Rakuten for Walmart Canada and any approved Best Buy/Home Depot programs.
2. Apply to Best Buy Canada and Home Depot Canada programs and confirm product feed/API rights, price retention, image rights, update cadence, and deep-link rules.
3. Complete Amazon Associates Canada/PA API eligibility and policy review as a separate gate.
4. Use Awin as the next feed-oriented fallback; evaluate CJ/Impact only when a named merchant opportunity requires them.

## Non-negotiable network controls

- never generate a link for a merchant/program that is not marked `APPROVED` in the policy table;
- use server-side allowlisted redirect records rather than client-supplied URLs;
- keep commission and conversion data separate from price-truth and Deal Quality;
- preserve required disclosure and attribution text adjacent to retailer CTAs;
- store only the minimum click/sub-ID data and define retention;
- treat provider quotas and link expiry as operational data;
- make source removal reversible without deleting canonical product identity.

## Evidence notes

- Amazon policy and API limits: [Associates Canada policy](https://associates.amazon.ca/help/operating/policies?ac-ms-src=ac-nav) and [PA API rate limits](https://associates.amazon.ca/help/node/topic/GLL6HEVVWUKMQDDQ) - VERIFIED 2026-08-11.
- Rakuten: [Deep Links API](https://pubhelp.rakutenadvertising.com/hc/en-us/articles/5949836672653-Deep-Links-API), [developer deep-link guide](https://developers.rakutenadvertising.com/guides/deep_link) - VERIFIED 2026-08-11.
- Awin: [product feed guide](https://help.awin.com/developers/docs/product-feed-publisher-guide-intro) - VERIFIED 2026-08-11.
- CJ: [product feed documentation](https://developers.cj.com/docs/data-imports/product-feeds) - VERIFIED 2026-08-11.
- Impact: [product catalogs](https://help.impact.com/brand/platform-features/product-catalogs/add-product-catalogs-as-a-brand) - VERIFIED 2026-08-11.
