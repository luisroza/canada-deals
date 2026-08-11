# GreatDeals.ca Product Definition

**Status:** APPROVED - Human Product Checkpoint completed
**Owner:** Product Owner / Market Research
**Date:** 2026-08-11

## Vision

GreatDeals.ca helps Canadians decide whether an online offer is genuinely worth considering by combining current CAD pricing, freshness, historical context when reliable, and permitted retailer comparison in one clear experience.

## Value proposition

For Canadian shoppers planning a meaningful online purchase, who cannot easily tell whether a sale is genuinely good across retailers, GreatDeals.ca is a price-intelligence and deal-discovery service that explains current offers with Canadian context. Unlike community feeds, flyer apps, cashback portals, and single-retailer trackers, it combines transparent evidence, freshness, and a fast retailer handoff.

## Primary users

- Primary: Canadian online shoppers planning electronics, home-improvement, or tools purchases.
- Secondary: expert deal hunters who want faster comparison and lower alert noise.
- Behavioural trigger: a meaningful planned purchase where the shopper is willing to compare before buying.
- Initial language: English, with a French validation slice considered after the product wedge is approved.
- Initial channel: responsive web; no native app or browser extension in MVP.

## Problem to solve

Canadian deal discovery is fragmented. Community sites provide context but can be dense and stale; flyer apps are strong for local and grocery shopping; cashback services optimize merchant conversion; Amazon trackers provide deep history for one marketplace. A shopper planning a high-consideration purchase still has to determine whether the current price is actually good, whether the product variant matches, whether the offer is fresh, and where to buy.

## Primary product promise

Help Canadian shoppers know whether an online deal is actually worth considering before they click through to a retailer.

## Positioning proposal

**A Canadian price-truth layer for planned online purchases.** GreatDeals.ca should show fewer, better offers and make the evidence behind each offer understandable.

## Product principles

1. Trust is visible: source, timestamp, currency, availability caveat, and affiliate disclosure are product UI.
2. Evidence before enthusiasm: do not use a discount badge or historical-low claim without defensible data.
3. Neutrality matters: commission must not silently determine ranking.
4. Start narrow: category depth and data quality are more valuable than retailer-count claims.
5. No account gate for first discovery; account/consent is earned by saved intent and alerts.

## Trust principles

1. Every offer exposes source, currency, timestamp, freshness, and relevant availability caveats.
2. Historical-low and discount claims require defensible evidence; otherwise the state is `UNKNOWN`.
3. Product comparisons require variant, seller, condition, and pack-size confidence.
4. Affiliate commission never secretly changes organic Deal Quality or ranking.
5. Sponsored or commercial placements, if introduced later, must be clearly separated from organic recommendations.
6. Users can report stale prices or mismatched products, and the product must measure correction time.

## Proposed business model

Begin with approved affiliate links and transparent disclosure. Do not start with paid ranking, intrusive ads, cashback reconciliation, or sponsored-looking recommendations. Consider sponsored placements, newsletter sponsorship, or premium alerts/history only after trust and conversion are measured.

## Core hypotheses to validate

| Hypothesis | Validation signal | Failure condition |
|---|---|---|
| Shoppers want evidence, not just a larger deal feed | Users choose evidence-rich cards in a smoke test | No lift in click or trust rating |
| Electronics/home-improvement/tools is a viable wedge | Users search and save products in all three categories | One category dominates or none creates repeat intent |
| Freshness and product identity improve retailer CTR | Compare cards with and without confidence/timestamp | Added explanation lowers comprehension or clicks |
| Target-price alerts create return behaviour | Opt-in, open, and return rate | Low opt-in or high unsubscribe rate |
| Affiliate links can support the product | Approved programs and qualified outbound clicks | Data rights or economics are insufficient |

## Non-goals for the first release

Community moderation, full flyer/grocery coverage, cashback, native apps, browser extension, AI shopping agent, mass programmatic SEO, and a 20+ retailer promise.

## Approved product decisions

1. Canada Deals is a Canadian price-truth layer for planned online purchases.
2. Primary users are Canadian shoppers planning meaningful purchases, initially in electronics, home improvement, and tools.
3. Launch is English-first responsive web; French-complete UX is deferred.
4. Amazon.ca, Best Buy Canada, and Home Depot Canada are approved product priorities for downstream validation; Walmart Canada is a fallback candidate. These are not approved technical integrations.
5. Evidence, freshness, safe comparison, conservative historical claims, and transparent affiliate disclosure are product requirements.
6. Save Product and Target-Price Alert are the P1 retention loop. Weekly digest is deferred to P2.
7. Community, AI shopping agent, native app, cashback, complex personalization, paid ranking, mass SEO, and French-complete launch remain outside MVP.

## Downstream handoff

After approval, UX should translate this definition into desktop/mobile flows and a design system. Architecture and data/integration work must then verify source permissions, product data feasibility, affiliate access, and Canadian-region requirements before implementation begins.
