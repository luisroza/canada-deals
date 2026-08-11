# GreatDeals.ca Product Definition

**Status:** DRAFT - proposed for Human Product Checkpoint
**Owner:** Product Owner / Market Research
**Date:** 2026-08-11

## Vision

GreatDeals.ca helps Canadians decide whether an online offer is genuinely worth considering by combining current CAD pricing, freshness, historical context when reliable, and permitted retailer comparison in one clear experience.

## Value proposition

For Canadian shoppers planning a meaningful online purchase, who cannot easily tell whether a sale is genuinely good across retailers, GreatDeals.ca is a price-intelligence and deal-discovery service that explains current offers with Canadian context. Unlike community feeds, flyer apps, cashback portals, and single-retailer trackers, it combines transparent evidence, freshness, and a fast retailer handoff.

## Proposed target

- Primary: Canadian online shoppers planning electronics, home, or tools purchases.
- Secondary: expert deal hunters who want faster comparison and lower alert noise.
- Initial language: English, with a French validation slice considered after the product wedge is approved.
- Initial channel: responsive web; no native app or browser extension in MVP.

## Problem to solve

Canadian deal discovery is fragmented. Community sites provide context but can be dense and stale; flyer apps are strong for local and grocery shopping; cashback services optimize merchant conversion; Amazon trackers provide deep history for one marketplace. A shopper planning a high-consideration purchase still has to determine whether the current price is actually good, whether the product variant matches, whether the offer is fresh, and where to buy.

## Positioning proposal

**A Canadian price-truth layer for planned online purchases.** GreatDeals.ca should show fewer, better offers and make the evidence behind each offer understandable.

## Product principles

1. Trust is visible: source, timestamp, currency, availability caveat, and affiliate disclosure are product UI.
2. Evidence before enthusiasm: do not use a discount badge or historical-low claim without defensible data.
3. Neutrality matters: commission must not silently determine ranking.
4. Start narrow: category depth and data quality are more valuable than retailer-count claims.
5. No account gate for first discovery; account/consent is earned by saved intent and alerts.

## Proposed business model

Begin with approved affiliate links and transparent disclosure. Do not start with paid ranking, intrusive ads, cashback reconciliation, or sponsored-looking recommendations. Consider sponsored placements, newsletter sponsorship, or premium alerts/history only after trust and conversion are measured.

## Core hypotheses to validate

| Hypothesis | Validation signal | Failure condition |
|---|---|---|
| Shoppers want evidence, not just a larger deal feed | Users choose evidence-rich cards in a smoke test | No lift in click or trust rating |
| Electronics/home/tools is a viable wedge | Users search and save products in all three categories | One category dominates or none creates repeat intent |
| Freshness and product identity improve retailer CTR | Compare cards with and without confidence/timestamp | Added explanation lowers comprehension or clicks |
| Target-price alerts create return behaviour | Opt-in, open, and return rate | Low opt-in or high unsubscribe rate |
| Affiliate links can support the product | Approved programs and qualified outbound clicks | Data rights or economics are insufficient |

## Non-goals for the first release

Community moderation, full flyer/grocery coverage, cashback, native apps, browser extension, AI shopping agent, mass programmatic SEO, and a 20+ retailer promise.

## Open decisions for the Human Product Checkpoint

1. Approve the price-truth positioning.
2. Approve electronics/home/tools as the initial category wedge.
3. Approve English-first launch with French validation later.
4. Approve proposed first retailers: Amazon.ca, Best Buy Canada, and Home Depot Canada, with Walmart Canada as a fallback pending permissions.
5. Approve target-price email alerts as a P1 MVP experiment.
6. Approve the rule that unknown history or weak product matching is shown as unknown, not inferred as a deal.

## Downstream handoff

After approval, UX should translate this definition into desktop/mobile flows and a design system. Architecture and data/integration work must then verify source permissions, product data feasibility, affiliate access, and Canadian-region requirements before implementation begins.
