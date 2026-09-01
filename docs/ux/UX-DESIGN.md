# Canada Deals — UX / Product Design

**Status:** APPROVED — Human UX revision approved 2026-08-20
**Product basis:** Human Product Checkpoint approved on 2026-08-11
**Scope:** English-first responsive web MVP; no application code or technology decisions

## 1. Executive UX summary

GreatDeals.ca should feel like a fast, visual deal-discovery product with calm trust cues. The 2026-08-20 revision supersedes earlier UX references to price-history charts, target-price alerts, broad filters, Saved naming, and alert navigation. The experience must answer four questions quickly:

1. Is this the product I want?
2. Is the current price meaningful?
3. How fresh and trustworthy is the evidence?
4. Where can I safely continue the purchase?

The primary interaction is **browse → scan → verify one offer → hand off**, with **wishlist → return** as the only account-based retention loop. Deal price, evidence-backed regular price when available, check time, retailer context, identity confidence, and disclosure remain visible at the moment of decision. Offers are never compared across retailers.

## 2. Approved product basis

- Positioning: Canadian price-truth layer for planned online purchases.
- Initial wedge: electronics plus home improvement/tools.
- Primary audience: Canadian shoppers planning a meaningful online purchase.
- Secondary audience: expert deal hunters who can help stress-test quality.
- Retailer priorities: Amazon.ca, Best Buy Canada, and Home Depot Canada; Walmart Canada remains a fallback candidate.
- MVP platform: English-first responsive web.
- P0: deal feed, search, category/store menus, category/store filters, store banners, compact individual-offer Deal Card, Offer Page, freshness, evidence/confidence, same-listing regular/deal price context, retailer handoff, disclosure, and stale/wrong reporting.
- P1: Wishlist persistence.
- Removed: public price tracker/history, target-price alerts, alert navigation, and weekly digest.

## 2.1 Approved competitive UI interpretation

Promobit was reviewed as a structural reference, not a visual identity to copy. GreatDeals adopts its low-friction information architecture: search with adjacent Categories and Stores menus, visual store entry points, feed-mode tabs, and image-led compact cards. GreatDeals deliberately excludes community authorship, votes, comments, gamification, coupon claims without evidence, countdown urgency, and promotional ranking. Product imagery remains a rights-gated enhancement; category visuals occupy that space until source display rights are verified.

## 3. UX principles

### Evidence before enthusiasm

The interface leads with the offer's deal price, its own verified regular price when available, observed time, retailer, and identity confidence. Superlatives and promotional language are secondary.

### Freshness is a product feature

Every price claim has an understandable observation time and a clear stale/unknown treatment. “Last checked” is not hidden in a tooltip.

### Unknown is safer than invented

Missing history, uncertain matching, unavailable shipping, and unverified savings use honest labels and useful next actions. No fake chart, fake discount, or implied certainty.

### One offer, one decision

Each listing is a complete independent offer. The UI never uses another retailer or listing as a price reference, never groups alternatives into a comparison table, and never hides one listing behind a representative Product card. Internal matching may support catalog hygiene, but it does not create a public comparison.

### Short path to a useful decision

The first screen is scannable, filters are reversible, and retailer handoff is explicit. The product should help a shopper decide without requiring an account.

### Neutral economics

Affiliate disclosure is visible near retailer actions. Commission status must not be presented as a quality signal or silently change organic ranking.

## 4. Behaviour model and personas

### Primary: planned-purchase shopper

Needs confidence before spending, often reviews a small number of known offers, and may return later. Success is a credible answer in under two minutes.

### Secondary: expert deal hunter

Scans more often, notices stale or weak evidence, and values history and alerts. Success is efficient triage without lowering trust standards.

### Behavioural modes

- **Explore:** browse a category or deal feed.
- **Verify:** inspect one offer's evidence, freshness, regular/deal price context, and product identity.
- **Continue:** open the retailer destination for that exact offer.
- **Return:** revisit saved offers in the Wishlist.

## 5. Core journeys

### Journey A — first visit

1. Visitor lands on a plain-language value proposition.
2. Visitor searches, selects a category, or opens a featured deal.
3. Feed immediately exposes deal price, verified regular price/savings when available, freshness, and evidence labels.
4. Visitor opens an Offer Page without creating an account.
5. Visitor continues to the retailer with disclosure.

### Journey B — verify a deal

1. Open Deal Card.
2. Confirm exact/variant match and retailer.
3. Read the deal price, same-listing regular-price basis when available, savings wording, and observation time.
4. Inspect seller, condition, availability, shipping, and evidence state.
5. Choose retailer handoff or report stale/wrong information.

### Journey C — inspect an individual offer

1. Offer Page identifies the product and the selected retailer listing.
2. The decision summary shows only that listing's deal price and its own supported regular price/savings.
3. Offer facts show observed time, availability context, seller/condition, shipping uncertainty, and CTA.
4. Other listings for the same internal Product are not rendered as comparisons or alternatives.

### Journey D — wishlist

1. Visitor selects Save to wishlist.
2. If signed out, the UI explains the minimum account step and preserves context.
3. A signed-in shopper can save the exact listing directly from a Deal Card or Offer Page; card state and the navigation count stay synchronized from one Wishlist load per navigation.
4. The shopper can search the Wishlist, narrow it by category or store, sort by saved date/current price/name/store, revisit the exact offer, or remove it.
5. Loading, signed-out, empty, no-match, mutation-error, and load-error/retry states are mutually exclusive and announced accessibly.
6. No target price, alert consent, or promotional notification is requested.

### Journey E — correct the record

1. Visitor selects Report stale or wrong.
2. A short, accessible form offers reason, optional note, and source context.
3. Confirmation explains that the report is a review signal, not a guarantee of immediate correction.

## 6. Information architecture and navigation

Primary routes:

- Home
- Deals
- Search results
- Offer Page
- Wishlist
- Account / preferences
- Report confirmation

Desktop navigation: logo, search, Deals, Categories, Stores, Wishlist, and a restrained account entry. Mobile navigation: Home, Categories, Search, Wishlist, and Account.

Avoid top-level navigation for community, cashback, coupons, or unsupported retailer breadth. Do not imply that every retailer or category is covered.

## 7. Homepage

The homepage hierarchy is:

1. **Compact promise:** “Find the right deal. Fast.”
2. **Global search:** product/model suggestions in the sticky header.
3. **Store banners:** a one-row responsive carousel of all enabled eligible stores using original GreatDeals artwork, accessible HTML retailer text, and visibly distinct affiliate/discovery-only states. Desktop exposes no more than four banners at once; tablet and mobile expose fewer, preserve a next-item cue, and support touch scrolling plus explicit Previous/Next controls.
4. **Quick filters:** category and store only.
5. **Feed modes:** latest, largest supported same-offer savings, and lowest deal price.
6. **Compact deal grid:** one card per retailer listing with retailer, visual area, title, deal price, optional verified regular price/savings, check time, Wishlist toggle, and CTA.
7. **Optional return path:** wishlist, without forcing account creation during discovery.

On mobile, the search and first useful card must appear before educational content. Promotional modules cannot displace the main search task.

## 8. Deals feed and filters

The public feed exposes category and store filters only. Filter and sort changes update results in place, retain scroll when possible, synchronize browser history, and provide a working Clear action. Sort choices are Latest, Best savings, and Lowest price. Best savings compares the deal price only with the verified regular price on the same listing.

## 9. Deal Card specification

Order: product image, retailer, product title, deal price, optional regular price and savings, one freshness/evidence line, Wishlist toggle, and one retailer action. One listing always produces one card; cards are keyed and linked by listing ID. Clicking the non-interactive card surface opens the internal Offer Page in the current tab, while Wishlist remains independent and **Check retailer price** uses only the protected retailer handoff and opens the retailer in a new tab. The card does not show another retailer, a comparison reference, history, or a “best store” claim.

Supported price states are:

- Deal price plus a higher verified regular price from the same listing.
- Deal price only when regular-price evidence is absent or invalid.
- Price unavailable.
- Stale, scheduled, expired, or unavailable offer.

## 10. Trust, evidence, and freshness language

Use “Deal price” for the current promotional amount and “Regular price” only for the same retailer listing. Show savings only when the regular amount is greater than the deal amount and has a source reference plus observation time. Freshness labels remain Just checked, Checked today, Checked recently, May be stale, and Check time unavailable. Internal matching status may be summarized as offer identity verified, under review, or unavailable; it must not imply comparison eligibility.

## 11. Offer Page

The canonical public detail route is `/offers/{listingId}`. Above the fold: product identity and image, selected retailer, deal price, optional regular price/savings, freshness/evidence, primary retailer CTA, and Save offer. Below the fold: seller, condition, availability, region, shipping, product attributes, source evidence, and report stale/wrong. No comparison table, related-listing panel, price-history chart, target-price action, or alternative retailer row is rendered.

The legacy Product slug route may resolve an eligible listing and redirect to its Offer Page; it is not a comparison surface.

## 12. Wishlist and account friction

Wishlist persistence is keyed by listing ID. Saving one store's offer does not save another listing attached to the same internal Product. Signed-out visitors keep public access and receive a focused sign-in boundary that preserves the exact return path. The private list supports local search/category/store/sort and removal, without target prices or notifications.

## 13. Search and results

Search accepts product names, model numbers, brands, and category terms. Results retain the same one-listing-per-card contract and preserve the typed query. A result does not become a promotion merely because its price is low; regular-price savings require same-listing evidence.

## 14. Reporting stale or wrong information

Report reasons are price changed, wrong product, retailer page unavailable, incorrect variant, expired offer, or other. The selected listing ID and source context are attached automatically. Confirmation does not promise an SLA.

## 15. Mobile and responsive behavior

- Content order is search, store carousel, category/store controls, feed, and individual offer cards.
- One primary retailer action per card and Offer Page decision summary; every real retailer handoff opens in a new tab and communicates that behavior accessibly.
- A mobile sticky action may repeat only the selected offer's retailer handoff.
- Touch targets are at least 44 CSS pixels and no essential fact depends on hover, pointer precision, or color alone.
- The carousel shows one mobile item per viewport with touch scrolling and explicit controls; it never lays all banners into one overflowing row.

## 16. Accessibility and design system

Target WCAG 2.2 AA with semantic landmarks/headings, labeled controls, keyboard-complete flows, visible focus, sufficient contrast, reduced motion, and live announcements for result and Wishlist changes. Core components are App Shell, Search Bar, Category/Store Menus, Store Carousel, Deal Card, Deal/Regular Price Block, Evidence/Freshness Badge, Save Offer Button, Retailer Action, Offer Facts, Report Dialog, Empty/Loading/Error State, and Pagination.

## 17. SEO and measurement

Public Offer Pages use stable listing routes, useful titles, canonical metadata, and indexable source-backed facts. Avoid thin pages, mass-generated claims, fake reviews, and discount claims that cannot be refreshed. Measure time to first useful offer, search-to-offer rate, retailer handoff, exact-offer Wishlist save/revisit/removal, report rate, stale rate, and accessibility defects. Cross-retailer comparison usage and alert creation are not product metrics.

## 18. Current screen inventory

1. Home/deals feed — desktop and mobile.
2. Search results.
3. Individual Offer Page.
4. Wishlist.
5. Account boundaries.
6. Report stale/wrong.
7. Admin offer/catalog/banner/report screens.
8. Loading, empty, stale, scheduled, expired, unavailable, not-found, and error states.

Detailed wireframes and older checkpoint artifacts remain historical references where they conflict with this approved individual-offer revision.

## 19. Final UX recommendation

GreatDeals.ca is an evidence-led offer discovery experience. Every card, detail page, Wishlist entry, price claim, and outbound action refers to one exact retailer listing. Deal price, same-listing regular price when verified, observation time, and evidence are first-class. Cross-retailer comparisons, public price tracking, target-price alerts, and promotional digests are outside the product.
