# Canada Deals - UX Research

**Status:** Research record supporting the approved UX direction; not an approved specification
**Date:** 2026-08-11
**Scope:** Current UX patterns only; no implementation or technology decisions

## Research question

What should Canada Deals adopt, improve, or avoid so a Canadian shopper can understand whether an offer is worth considering without becoming a forum expert or opening several retailer pages?

## Products inspected

- [Flipp](https://flipp.com/en-ca) - postal-code-first local/flyer experience, item search, coupons, lists, loyalty cards, and app handoff.
- [SmartCanucks](https://smartcanucks.ca/) - editorial deal stream, coupons, flyers, forum, stores, Amazon deals, dates, authors, and outbound merchant links.
- [PrixSnap](https://prixsnap.com/) - cross-retailer comparison promise, target alerts, price history, AI, weekly flyers, and a mobile-oriented navigation model.
- [Keepa](https://keepa.com/) - Amazon-centric historical price and alert value proposition; the public home surface is data-oriented and sparse when loading is incomplete.
- [Honey](https://www.joinhoney.com/) - coupon and checkout assistance; the experience is strongest at the final retailer handoff rather than early product evaluation.
- [Slickdeals](https://slickdeals.net/deal-alerts/) - search-first custom alerts, popular alert templates, community navigation, extension promotion, and explicit promoted-deal disclosure.
- [Best Buy Canada](https://www.bestbuy.ca/en-ca) - prominent search, deals, product titles, ratings, CAD prices, savings, and campaign expiry language.
- [Home Depot Canada](https://www.homedepot.ca/en/home.html) - store selection, search, departments, events, tool promotions, availability context, and Canadian French entry point.

## Evidence labels

- `VERIFIED`: directly observed in the current live surface.
- `INFERRED`: design implication from observed behaviour.
- `RECOMMENDED`: proposed Canada Deals direction.

## UX benchmark matrix

Scores are directional 1-5 ratings from the observed surface, not usability-test results. Higher is better for the dimension.

| Product | First-use clarity | Deal scanning | Search | Price comprehension | History | Comparison | Trust/freshness | Alert UX | Mobile signal | Decision speed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Flipp | 5 | 4 | 4 | 3 | 1 | 2 | 4 | 3 | 5 | 4 |
| SmartCanucks | 3 | 3 | 2 | 3 | 1 | 2 | 2 | 2 | 3 | 2 |
| PrixSnap | 4 | 4 | 5 | 4 | 4 | 5 | 3 | 5 | 4 | 4 |
| Keepa | 2 | 2 | 3 | 4 | 5 | 1 | 3 | 4 | 3 | 3 |
| Honey | 4 | 2 | 2 | 3 | 2 | 2 | 3 | 4 | 4 | 4 |
| Slickdeals | 3 | 3 | 4 | 2 | 2 | 2 | 3 | 5 | 4 | 3 |
| Best Buy Canada | 4 | 4 | 5 | 4 | 1 | 1 | 3 | 2 | 4 | 4 |
| Home Depot Canada | 4 | 3 | 4 | 3 | 1 | 1 | 4 | 2 | 4 | 3 |

### Benchmark interpretation

- Flipp's strongest pattern is immediate relevance: postal code plus clear “what can I do here?” actions. It should inspire location/context onboarding, but not the grocery-first information architecture.
- SmartCanucks exposes real deal content quickly, but the page combines editorial, affiliate handoffs, dates, authors, categories, and social links. Canada Deals should keep source/date context while reducing scan cost.
- PrixSnap is the closest direct UX threat: search, cross-retailer coverage, alerts, AI, flyers, and mobile navigation are all presented as one promise. Canada Deals cannot win by simply adding more features; it needs better evidence hierarchy and a narrower, calmer decision flow.
- Keepa demonstrates the value of history for a known listing but also the cost of data density. Canada Deals should translate history into an answer, not reproduce a trader chart.
- Slickdeals demonstrates that alert templates reduce setup effort, but its broad community and promoted content require strong disclosure and alert controls.
- Retailer sites make price, promotion, availability, seller/variant, and campaign expiry visible in different ways. Canada Deals should normalize these fields without pretending checkout conditions are identical.

## Patterns to adopt

1. **Search as the primary action:** Flipp, PrixSnap, Best Buy, and Home Depot all make search easy to find.
2. **Context before commitment:** postal/store or category context improves relevance.
3. **Short decision labels:** a shopper needs a plain-language reason, not a score alone.
4. **Visible time signals:** retailer campaign dates and “last checked” states should be treated as first-class data.
5. **Progressive comparison:** show a quick summary first; reveal detail only when the shopper needs to inspect a variant or retailer.
6. **Templates for alerts:** popular targets can reduce setup friction without forcing personalization onboarding.

## Patterns to avoid

1. Forum-like density as the default for new visitors.
2. “Buy now” language when the user is leaving for another retailer.
3. Countdown or urgency treatment without a verified expiry.
4. Large feature lists that make the product promise sound like a generic super-app.
5. Price-history charts without an accessible text interpretation.
6. Comparison rows that silently mix model generations, sellers, conditions, pack sizes, or tool kits.
7. Affiliate disclosure hidden in a footer or obscured by promotional language.

## UX opportunity statement

`For a Canadian shopper with a planned purchase, Canada Deals should make the evidence behind a price legible within one screen: what the product is, how much it costs, why the price may be good, how fresh the information is, and what safe next actions exist.`
