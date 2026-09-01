# GreatDeals.ca MVP

**Status:** APPROVED - Human Product Revision approved 2026-08-20
**Scope:** Product proposal only; no technology or integration is approved by this document.

## MVP statement

Build a responsive English-first deal-discovery experience that helps Canadian shoppers scan current offers quickly, narrow by category or store, verify the exact product, save it to a wishlist, or click to a permitted retailer.

## MVP objective

Validate that Canadian shoppers can find relevant promotions quickly, trust the current-offer context enough to click to a retailer, and return to a lightweight wishlist.

## In scope

| Capability | Problem solved | Why now | MVP acceptance outcome |
|---|---|---|---|
| Category deal feed | Shoppers need a focused starting point | Validates the core discovery loop | A visitor can browse current offers with category and freshness context |
| Search | Shoppers often begin with a planned product | Captures high-intent demand | Search returns relevant products and does not require an account |
| Filters | Broad feeds create noise | Keeps discovery simple | Results can be narrowed by category and store only |
| Store banners | Shoppers often start from a trusted retailer | Adds a visual store-first path | Every enabled eligible store appears in one admin-ordered carousel with no more than four banners visible at once; discovery-only stores open their GreatDeals feed and approved active stores use the safe backend store handoff |
| Deal card | Users need a fast scan-and-click surface | Core UI for click testing | Each card is one retailer offer and prioritizes store, visual product area, title, deal price, evidence-backed regular price/savings when available, freshness, and the permitted retailer CTA |
| Offer page | A deal feed alone cannot confirm the exact item | Supports trust and conversion | Page shows the selected offer only, with deal/regular price context, seller/condition/availability/shipping/region facts, disclosure, wishlist action, and report action |
| Freshness and confidence | Stale or weak data destroys trust | Differentiation depends on it | Each offer has a visible last-checked/freshness state and identity-confidence rule |
| Individual-offer pricing | A discount claim needs a clear basis | Keeps promotion context understandable without cross-store matching | Regular price belongs to the same retailer listing, must exceed the deal price, and appears only with explicit source evidence; another retailer's price is never used as the basis |
| Wishlist | Shoppers need a lightweight return path | Low-cost retention test | A signed-in user can save the exact retailer offer from Deal Cards or Offer Pages, see a synchronized count, organize the list locally, and remove offers without alerts or target prices |
| Affiliate/outbound measurement | Business viability needs qualified traffic | Must be measurable from day one | Retailer handoff is trackable and affiliate disclosure is visible |
| Stale/incorrect report | Automated data will be imperfect | Creates a correction loop | User can report a stale price or wrong product |
| Owner operations | Catalog, offers, banners, and reports require a safe correction path | Keeps public data manageable without a large CMS | One role-protected owner can manage brands/categories/stores, create a Product or attach a store offer to an existing Product, apply optional offer validity, draft/publish/deactivate offers, select the homepage carousel, upload bounded reviewed raster artwork, manage rights-gated banners, resolve reports, and inspect audit; public users cannot discover or access these operations |

## Priority bands

### P0 - Essential for the validation loop

Deal feed, search, filters, evidence-rich individual-offer cards, offer pages, freshness state, product identity confidence, same-listing regular/deal price context, commercial disclosure, outbound measurement, and stale/mismatch reporting.

### P1 - High-priority retention experiment

Wishlist persistence and usability only: card-level save, synchronized count, revisit, local search/filter/sort, and removal. Price tracking, target-price alerts, and weekly digests are removed from the current product scope.

## Major assumptions

- A small set of approved retailer sources can provide enough current price, product, image, availability, and outbound-link data.
- Planned-purchase shoppers value clear offer evidence enough to change from existing habits.
- Product identity can be normalized internally without merging retailer offers in the public experience.
- Affiliate revenue can be earned without letting commission change organic Deal Quality.
- English-first web is sufficient for the first behavioural validation, with French expansion treated as a later product decision.

## Dependencies requiring validation

- Amazon Associates Canada acceptance, Product Advertising API eligibility, display rules, and refresh limits.
- Affiliate or permitted data access for Best Buy Canada, Home Depot Canada, and Walmart Canada.
- Product variant, seller, condition, pack-size, regular-price evidence, promotion-validity, and regional-availability rules.
- Storefront-level affiliate destinations and image display rights for each approved merchant.
- Manual audit of at least 100 product/offer matches before automated scale.

## MVP measurement set

- Search success rate and time to first relevant offer.
- Deal-card-to-product and product-page-to-retailer CTR.
- Wishlist save, revisit, and removal rate.
- Stale-price and product-mismatch report rate.
- Qualified affiliate clicks per session after approved programs are active.

## Launch boundaries

- Proposed first retailers: Amazon.ca, Best Buy Canada, and Home Depot Canada; Walmart Canada is a fallback candidate.
- A retailer enters MVP only after the Data/Affiliate Architect verifies source permission, data fields, refresh limits, and affiliate feasibility.
- Every retailer listing is presented as an independent offer, including multiple listings attached to one internal canonical Product. No public cross-retailer price comparison is produced.
- A regular price is never inferred from another store, another listing, historical extrema, or an unverified crossed-out value.
- Product price-history charts, historical-low claims, and target-price alerts are not exposed.
- Store banners never invent a direct retailer destination. Until a persisted provider-approved store-level affiliate destination exists, they open the store-filtered GreatDeals feed. Approved store destinations use `/go/store/{retailerKey}`; product CTAs continue through `/go/{listingId}`.
- The owner controls carousel membership through one explicit active selection and controls sequence through Carousel position. A store without an active configured profile never appears implicitly. The public rail remains one row, shows at most four banners at desktop width, and progressively exposes fewer banners per viewport on smaller screens without limiting the total enabled set.
- Owner administration can create and manage Brand, Category, and Store records. New records begin inactive; brand/category slugs and store keys are immutable; deactivation is audited and reversible; no linked Product, offer, Wishlist, banner, affiliate, or history data is deleted. Inactive brands, categories, and stores fail closed across public discovery and handoff.
- A second retailer offer for the same confirmed item may attach to the existing internal canonical Product, but remains a separate discovery card, detail route, wishlist item, price claim, and retailer handoff. Product slugs remain immutable, and optional source-provided start/end times hide an offer outside its valid window.

## Success criteria

- Visitors can reach a relevant offer without signing in.
- Every displayed offer has a source and freshness state.
- Product pages produce measurable retailer click-through.
- A measurable subset of visitors saves and revisits an exact offer through the wishlist.
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
- Public price tracking/history, target-price alerts, and promotional email digests.
- Cross-retailer price comparison, representative-offer deduplication, and “best store” claims for the same Product.

These exclusions keep the first release focused on the evidence-to-click loop and avoid taking on four difficult systems at once: community moderation, flyer/local data, affiliate reconciliation, and cross-market app distribution.

The current retention loop is Browse -> Wishlist -> Return. Price alerts and historical tracking are explicitly outside the product.
