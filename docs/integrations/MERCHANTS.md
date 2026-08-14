# Canada Deals - Merchant Integration Matrix

**Status:** APPROVED STRATEGY - Human Architecture / Data Integration Checkpoint completed
**Date checked:** 2026-08-14

The scores below are screening judgements, not approvals. Two high-quality approved retailers are sufficient for launch; retailer count is not an MVP KPI. A merchant enters production implementation only after a current program, feed/API rights, permitted data fields, update cadence, and deep-link behavior are recorded.

| Merchant | Category fit | Affiliate evidence | Feed/API evidence | Policy risk | Screening score | Recommended posture |
|---|---|---|---|---|---:|---|
| Best Buy Canada | strong electronics | official page identifies Impact; Canada Deals approval **ABSENT** | feed/API **UNKNOWN** | medium; terms/license review | 72 | **Impact adapter implemented — awaiting publisher approval/credentials** |
| Home Depot Canada | strong home improvement/tools | official page identifies Commission Junction; Canada Deals approval **ABSENT** | feed/API **UNKNOWN** | medium; terms/license review | 70 | **CJ adapter implemented — awaiting publisher approval/credentials** |
| Amazon.ca | broad electronics/home/tools | Associates Canada + Creators API direction; Canada Deals eligibility **UNKNOWN** | official API exists, not authorized here | high; strict policy and comparison rules | 66 | **Gated; no adapter** |
| Walmart Canada | broad general merchandise | current Walmart.ca page points to Rakuten; Canada Deals approval/link rights **ABSENT** | merchant feed/API **UNKNOWN** | medium/high | 60 | **Gated / unverified for Canada Deals** |
| Canadian Tire | strong Canadian relevance/tools | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 48 | Phase 2 research |
| Staples Canada | electronics/home office | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 45 | Phase 2 |
| Wayfair Canada | home | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 40 | Phase 2/3 |
| Costco Canada | broad, membership | current program/network **UNKNOWN** | **UNKNOWN** | membership/content rights | 35 | Not MVP |
| Sport Chek | out of initial wedge | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 35 | Phase 3 |
| Sephora Canada | out of initial wedge | current program/network **UNKNOWN** | **UNKNOWN** | **UNKNOWN** | 38 | Phase 3 |
| The Bay | broad but not initial wedge | current program/network **UNKNOWN** | **UNKNOWN** | commercial/program volatility | 25 | Not MVP |

## Launch recommendation

Target **two launch retailers** from Best Buy Canada and Home Depot Canada if lawful feeds/APIs and affiliate links are actually available. This is sufficient for MVP; retailer count is not a success metric. Keep Amazon.ca outside the launch commitment until Associates/Creators API eligibility, pricing/display/caching/history/comparison rules, and required attribution are reviewed. Keep Walmart Canada gated/fallback until Rakuten confirms the Canada Deals relationship and data access.

This deliberately favors a smaller, evidence-quality catalog over a broad directory with uncertain rights. Retailer count is not a success metric by itself.

No live Rakuten discovery was authorized in Vertical Slice 9, so this document records no newly discovered advertiser count, merchant candidate, partnership, Product Feed entitlement, or Canada coverage. Those facts must come from the read-only discovery checkpoint and must not be inferred from the connector implementation or a credential alone.

## Source-specific rules

### Amazon.ca

Use official Associates/Creators API or another written approved path only. Do not scrape or alter provider-vended links. Treat price/availability display, timestamp, comparison with other retailers, image caching, historical price storage, and mobile use as policy-controlled. Do not build a permanent historical archive from Amazon content without written review.

### Best Buy Canada

The official affiliate page confirms Impact, application review, unique affiliate links, prohibition on brand/trademark bidding, commission for mobile-web purchases, and no commission for Best Buy app purchases. Canada Deals has no recorded approval, AccountSID/AuthToken, ProgramId, MediaPartnerPropertyId, or verified deeplink domains. Product feed/API access, price-history rights, and update cadence remain separate unknowns.

### Home Depot Canada

The official affiliate page confirms a Canadian program through Commission Junction. Canada Deals has no recorded CJ publisher account/PAT/PID, joined Home Depot Canada advertiser relationship, CID, or approved deep-link Link ID. Catalog/API/feed details remain a separate connector gate.

### Walmart Canada

The current official Walmart.ca affiliate page points applicants to Rakuten, but Canada Deals is not approved and no permitted deep-link/data behavior is recorded. This retailer remains gated; Walmart.com terms are not substituted for Walmart.ca evidence.

## Evidence

- [Best Buy Canada affiliate program](https://www.bestbuy.ca/en-ca/about/affiliate-program/blt82df225e80ec75e9) - VERIFIED 2026-08-12.
- [Home Depot Canada affiliate program](https://amp.homedepot.ca/en/home/the-home-depot-canada-affiliate-program.html) - VERIFIED 2026-08-12.
- [Walmart Canada affiliate program](https://www.walmart.ca/en/cp/affiliate-program/6000208941216) - VERIFIED 2026-08-12.
- [Amazon Creators API onboarding](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/onboarding/sign-up-as-an-amazon-associate) and [best practices](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/concepts/best-programming-practices) - VERIFIED 2026-08-12.
