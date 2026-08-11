# Canada Deals - Merchant Integration Matrix

**Status:** APPROVED STRATEGY - Human Architecture / Data Integration Checkpoint completed
**Date checked:** 2026-08-11

The scores below are screening judgements, not approvals. Two high-quality approved retailers are sufficient for launch; retailer count is not an MVP KPI. A merchant enters production implementation only after a current program, feed/API rights, permitted data fields, update cadence, and deep-link behavior are recorded.

| Merchant | Category fit | Affiliate evidence | Feed/API evidence | Policy risk | Screening score | Recommended posture |
|---|---|---|---|---|---:|---|
| Best Buy Canada | strong electronics | official Canadian affiliate program | feed/API **UNKNOWN** | medium; terms/license review | 72 | **MVP candidate if feed/API approved** |
| Home Depot Canada | strong home improvement/tools | official Canadian affiliate program | feed/API **UNKNOWN** | medium; terms/license review | 70 | **MVP candidate if feed/API approved** |
| Amazon.ca | broad electronics/home/tools | Associates Canada + PA API path | official API path exists | high; strict policy and comparison rules | 66 | **Gated candidate** |
| Walmart Canada | broad general merchandise | official affiliate path through Rakuten | merchant feed/API **UNKNOWN** | medium/high; program and catalog rights | 60 | Phase 2/fallback |
| Canadian Tire | strong Canadian relevance/tools | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 48 | Phase 2 research |
| Staples Canada | electronics/home office | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 45 | Phase 2 |
| Wayfair Canada | home | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 40 | Phase 2/3 |
| Costco Canada | broad, membership | current program/network **UNKNOWN** | **UNKNOWN** | membership/content rights | 35 | Not MVP |
| Sport Chek | out of initial wedge | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 35 | Phase 3 |
| Sephora Canada | out of initial wedge | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 38 | Phase 3 |
| The Bay | broad but not initial wedge | current program/network **UNKNOWN** | **UNKNOWN** | commercial/program volatility | 25 | Not MVP |

## Launch recommendation

Target **two launch retailers** from Best Buy Canada and Home Depot Canada if lawful feeds/APIs and affiliate links are actually available. This is sufficient for MVP; retailer count is not a success metric. Keep Amazon.ca outside the launch commitment until the PA/API account, pricing/display/caching/history/comparison rules, and required attribution are reviewed. Keep Walmart Canada as a fallback/Phase 2 candidate if Rakuten confirms program and data access.

This deliberately favors a smaller, evidence-quality catalog over a broad directory with uncertain rights. Retailer count is not a success metric by itself.

## Source-specific rules

### Amazon.ca

Use official Associates/PA API or another written approved path only. Do not scrape. Treat price/availability display, timestamp, comparison with other retailers, image caching, historical price storage, and mobile use as policy-controlled. Do not build a permanent historical archive from Amazon content without written review.

### Best Buy Canada

The official affiliate page confirms the program, application, affiliate links, qualifying purchases, and program-specific restrictions. Product feed/API access, price-history rights, and update cadence remain unknown and must be answered before connector work.

### Home Depot Canada

The official affiliate page confirms a Canadian affiliate program, but current catalog/API/feed details require partner outreach and written confirmation.

### Walmart Canada

The official Canadian affiliate page points to Rakuten. Confirm advertiser partnership, product catalog/feed availability, Canadian currency/availability, and link rules before scheduling ingestion.

## Evidence

- [Best Buy Canada affiliate program](https://www.bestbuy.ca/en-ca/about/affiliate-program/blt82df225e80ec75e9) - VERIFIED 2026-08-11.
- [Home Depot Canada affiliate program](https://amp.homedepot.ca/en/home/the-home-depot-canada-affiliate-program.html) - VERIFIED 2026-08-11.
- [Walmart Canada affiliate program](https://www.walmart.ca/en/cp/affiliate-program/6000208941216) - VERIFIED 2026-08-11.
- [Amazon Associates Canada help](https://associates.amazon.ca/help) and [policy](https://associates.amazon.ca/help/operating/policies?ac-ms-src=ac-nav) - VERIFIED 2026-08-11.
