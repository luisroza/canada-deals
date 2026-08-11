# GreatDeals.ca MVP

**Status:** DRAFT - awaiting Human Product Checkpoint
**Scope:** Product proposal only; no technology or integration is approved by this document.

## MVP statement

Build a responsive English-first web experience that helps Canadian shoppers compare trustworthy online offers in electronics, home, and tools. The MVP should make a shopper's next decision faster: investigate, save/alert, or click to a permitted retailer.

## In scope

| Capability | Problem solved | Why now | MVP acceptance outcome |
|---|---|---|---|
| Category deal feed | Shoppers need a focused starting point | Validates the core discovery loop | A visitor can browse current offers with category and freshness context |
| Search | Shoppers often begin with a planned product | Captures high-intent demand | Search returns relevant products and does not require an account |
| Filters | Broad feeds create noise | Makes the wedge usable | Retailer, category, price, discount, freshness, and online availability can be narrowed |
| Deal card | Users need a fast comparison surface | Core UI for click testing | Card shows product, CAD price, retailer, evidence state, timestamp, and CTA |
| Product page | A deal feed alone cannot explain value | Supports trust and conversion | Page shows offer context, safe comparisons, history when available, disclosure, and report action |
| Freshness and confidence | Stale or weak data destroys trust | Differentiation depends on it | Each offer has a visible last-checked/freshness state and identity-confidence rule |
| Safe same-product comparison | Users otherwise open several retailer sites | Main cross-retailer value | Comparisons appear only when identity/variant confidence passes a defined threshold |
| Save product/deal | Planned shoppers need a lightweight return path | Low-cost retention test | User can save without a complex onboarding flow |
| Target-price email alert | Users want to wait for a better price | Direct retention hypothesis | User can set a target and consent to relevant email notifications |
| Affiliate/outbound measurement | Business viability needs qualified traffic | Must be measurable from day one | Retailer handoff is trackable and affiliate disclosure is visible |
| Stale/incorrect report | Automated data will be imperfect | Creates a correction loop | User can report a stale price or wrong product |

## Launch boundaries

- Proposed first retailers: Amazon.ca, Best Buy Canada, and Home Depot Canada; Walmart Canada is a fallback candidate.
- A retailer enters MVP only after the Data/Affiliate Architect verifies source permission, data fields, refresh limits, and affiliate feasibility.
- A product is not presented as a comparison match when variant, seller, condition, or pack size is ambiguous.
- “Historical low” is displayed only when the history is sufficiently complete and permitted; otherwise the UI says history is unavailable.

## Success criteria

- Visitors can reach a relevant offer without signing in.
- Every displayed offer has a source and freshness state.
- Product pages produce measurable retailer click-through.
- A measurable subset of visitors saves a product or requests an alert.
- Stale and mismatch reports are visible and actionable.
- Early users report that the explanation improves purchase confidence.

## Not in MVP

- Community posts, votes, comments, profiles, reputation, and moderation.
- Full grocery/flyer/local inventory experience.
- Cashback, rewards, or wallet balances.
- Native apps, push notifications, and browser extensions.
- AI agent or autonomous shopping.
- Mass programmatic SEO.
- Twenty-plus retailer coverage.
- French-complete experience.

These exclusions keep the first release focused on the evidence-to-click loop and avoid taking on four difficult systems at once: community moderation, flyer/local data, affiliate reconciliation, and cross-market app distribution.
