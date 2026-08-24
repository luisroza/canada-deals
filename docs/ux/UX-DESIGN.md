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

The primary interaction is **browse → scan → verify → hand off**, with **wishlist → return** as the only account-based retention loop. Current price, check time, retailer context, match confidence, and disclosure remain visible at the moment of decision.

## 2. Approved product basis

- Positioning: Canadian price-truth layer for planned online purchases.
- Initial wedge: electronics plus home improvement/tools.
- Primary audience: Canadian shoppers planning a meaningful online purchase.
- Secondary audience: expert deal hunters who can help stress-test quality.
- Retailer priorities: Amazon.ca, Best Buy Canada, and Home Depot Canada; Walmart Canada remains a fallback candidate.
- MVP platform: English-first responsive web.
- P0: deal feed, search, category/store menus, category/store filters, store banners, compact Deal Card, Product Page, freshness, evidence/confidence, safe comparison, retailer handoff, disclosure, and stale/wrong reporting.
- P1: Wishlist persistence.
- Removed: public price tracker/history, target-price alerts, alert navigation, and weekly digest.

## 2.1 Approved competitive UI interpretation

Promobit was reviewed as a structural reference, not a visual identity to copy. GreatDeals adopts its low-friction information architecture: search with adjacent Categories and Stores menus, visual store entry points, feed-mode tabs, and image-led compact cards. GreatDeals deliberately excludes community authorship, votes, comments, gamification, coupon claims without evidence, countdown urgency, and promotional ranking. Product imagery remains a rights-gated enhancement; category visuals occupy that space until source display rights are verified.

## 3. UX principles

### Evidence before enthusiasm

The interface leads with current price, reference basis, observed time, retailer, and match confidence. Superlatives and promotional language are secondary.

### Freshness is a product feature

Every price claim has an understandable observation time and a clear stale/unknown treatment. “Last checked” is not hidden in a tooltip.

### Unknown is safer than invented

Missing history, uncertain matching, unavailable shipping, and unverified savings use honest labels and useful next actions. No fake chart, fake discount, or implied certainty.

### Compare only when safe

The product identity comes before the price comparison. Similar-looking offers are not presented as equivalent unless the match is strong enough.

### Short path to a useful decision

The first screen is scannable, filters are reversible, and retailer handoff is explicit. The product should help a shopper decide without requiring an account.

### Neutral economics

Affiliate disclosure is visible near retailer actions. Commission status must not be presented as a quality signal or silently change organic ranking.

## 4. Behaviour model and personas

### Primary: planned-purchase shopper

Needs confidence before spending, often compares a small number of known products, and may return later. Success is a credible answer in under two minutes.

### Secondary: expert deal hunter

Scans more often, notices stale or weak evidence, and values history and alerts. Success is efficient triage without lowering trust standards.

### Behavioural modes

- **Explore:** browse a category or deal feed.
- **Verify:** inspect evidence, freshness, history, and product identity.
- **Compare:** view safe retailer alternatives for the same product.
- **Return:** revisit saved products or respond to a target-price alert.

## 5. Core journeys

### Journey A — first visit

1. Visitor lands on a plain-language value proposition.
2. Visitor searches, selects a category, or opens a featured deal.
3. Feed immediately exposes price, reference, freshness, and evidence labels.
4. Visitor opens a Product Page without creating an account.
5. Visitor compares or continues to a retailer with disclosure.

### Journey B — verify a deal

1. Open Deal Card.
2. Confirm exact/variant match and retailer.
3. Read current price, reference basis, savings wording, and observation time.
4. Inspect history state: reliable, partial, or unavailable.
5. Choose retailer handoff or report stale/wrong information.

### Journey C — compare safely

1. Product Page identifies the canonical product and important attributes.
2. Comparison panel groups same-product offers.
3. Each retailer row shows price, observed time, availability context, shipping uncertainty, and CTA.
4. Uncertain-match alternatives are separated and never merged into the primary comparison.

### Journey D — wishlist

1. Visitor selects Save to wishlist.
2. If signed out, the UI explains the minimum account step and preserves context.
3. The signed-in shopper can revisit or remove the product from Wishlist.
4. No target price, alert consent, or promotional notification is requested.

### Journey E — correct the record

1. Visitor selects Report stale or wrong.
2. A short, accessible form offers reason, optional note, and source context.
3. Confirmation explains that the report is a review signal, not a guarantee of immediate correction.

## 6. Information architecture and navigation

Primary routes:

- Home
- Deals
- Search results
- Product Page
- Wishlist
- Account / preferences
- Report confirmation

Desktop navigation: logo, search, Deals, Categories, Stores, Wishlist, and a restrained account entry. Mobile navigation: Home, Categories, Search, Wishlist, and Account.

Avoid top-level navigation for community, cashback, coupons, or unsupported retailer breadth. Do not imply that every retailer or category is covered.

## 7. Homepage

The homepage hierarchy is:

1. **Compact promise:** “Find the right deal. Fast.”
2. **Global search:** product/model suggestions in the sticky header.
3. **Store banners:** a responsive grid of eligible stores using original GreatDeals artwork, accessible HTML retailer text, and visibly distinct affiliate/discovery-only states.
4. **Quick filters:** category and store only.
5. **Feed modes:** latest, best supported savings, and lowest price.
6. **Compact deal grid:** retailer, visual area, title, current price, check time, and CTA.
7. **Optional return path:** wishlist, without forcing account creation during discovery.

On mobile, the search and first useful card must appear before educational content. Promotional modules cannot displace the main search task.

## 8. Deals feed and filters

The feed supports category, retailer, price range, discount/reference availability, freshness, match confidence, and availability-context filters. Filters show active counts, can be cleared individually, and preserve results when a user returns from a Product Page.

Sort choices:

- Most recently checked
- Largest supported savings
- Lowest current price

The initial MVP default is **Most recently checked** because it is transparent, deterministic, and reinforces freshness. “Best evidence” may be tested later, but is not the initial default and must not be presented as an opaque “Recommended” or “Best Deals” ranking.

## 9. Deal Card specification

### Standard card

Order: product image, category/retailer, product title and identifying variant, current price, reference/evidence label or “No verified reference,” savings statement only when supported, freshness label, public product-match state, and actions.

Actions: View details, Compare retailers when safe, Save, and Report stale/wrong in an overflow menu.

### Compact card

Used in dense lists and mobile. Retains title, current price, retailer, freshness, and one evidence label. It must not hide the observation time behind a hover interaction.

### Featured card

May add a short reason such as “Recently checked” or “Strong historical evidence.” Never use “unbeatable,” “guaranteed,” or a large savings badge without supporting evidence.

### Card states

- Verified current price + reliable reference.
- Current price only; no verified reference.
- Partial history.
- Stale observation.
- Review before comparing.
- No safe comparison.
- Expired or unavailable retailer offer.
- Loading, empty, and error.

Each state has a visible label, one-sentence explanation, and a next action.

## 10. Trust, evidence, and freshness language

Use a consistent evidence block:

> Current price: $X CAD
> Observed: 2 hours ago
> Reference: observed history / retailer reference / unavailable
> Product match: Same product confirmed

Freshness labels: Just checked, Checked today, Checked recently, May be stale, and Last observation unavailable. The exact timestamp is available in the detail view.

Public product-match states are human-readable and distinct from internal confidence signals:

- **Same product confirmed:** included in the safe retailer comparison.
- **Review before comparing:** a listing may differ by model, size, storage, seller, condition, pack size, bundle, generation, or another meaningful variant.
- **No safe comparison available:** no listing is confidently equivalent and it is not included in the price comparison.

Do not expose implementation confidence percentages in the MVP.

Savings copy follows evidence:

- “$X below the observed reference” when supported.
- “Current price” when no reference is available.
- “Reference unavailable” when the comparison basis is incomplete.

Do not show a percentage discount when the reference is unknown or when product identity is uncertain.

## 11. Product Page

Above the fold:

1. Canonical product title and identifying attributes.
2. Current CAD price and retailer context.
3. Evidence/freshness/match summary.
4. Primary retailer CTA with affiliate disclosure.
5. Save and Target Price actions.

Below the fold:

- retailer comparison,
- price-history module,
- evidence details,
- product attributes and variant clarification,
- report stale/wrong,
- related products only when clearly labeled as alternatives, not same-product matches.

The page should answer “what is this?” before “how much could I save?”

## 12. Price history

P0 requires the Product Page to render all price-history evidence states correctly. It does not require complete history for every MVP product. A product or retailer with insufficient permitted or reliable history must remain usable and show **“Price history unavailable.”** Lack of history for one product or retailer does not automatically block MVP launch.

### Reliable history

Show the textual interpretation expanded by default, followed by a readable chart with period selector, current marker, and observation coverage. For example: “Current price: $499 CAD. Lowest observed price since tracking began: $449 CAD. Tracking since: March 2026.” Use exact wording only when supported; never imply all-time coverage without all-time evidence. Include a text summary for screen readers.

### Partial history

Show the chart only for the supported period and label coverage limitations. Avoid implying an all-time low.

### Unavailable history

Use a compact explanation: “Price history unavailable — we do not have enough verified history for this product yet.” Keep current price and retailer actions usable; do not render an empty axis that looks like missing data.

The chart must not imply a continuous observation when data points are sparse.

## 13. Retailer comparison

Desktop uses a comparison table with one row per safely matched retailer offer. Columns: retailer, current price, observation time, availability/store context, shipping note when known, evidence, and action.

Mobile uses stacked offer cards with the same information in the same priority order. The primary CTA is “View at retailer,” with “Opens retailer site” and affiliate disclosure close to it.

Offers with an uncertain product match belong in a separate “Review before comparing” section. “No safe comparison available” is a valid final state and should explain that a similar listing was found but not treated as equivalent.

Affiliate disclosure stays close to each retailer CTA, visible, neutral, and understandable. Conceptual baseline copy: “We may earn a commission if you buy through this link.” This is UX copy guidance; final legal and compliance wording remains subject to later review.

## 14. Search and results

Search accepts product names, model numbers, and common category terms. Autocomplete distinguishes products from categories and recent searches. Results preserve the typed query and expose a clear no-result path with spelling/category suggestions.

Search result rows retain the same trust vocabulary as Deal Cards. A result must not become a deal merely because it has a low current price.

## 15. Save, target price, and account friction

Save is available from cards and Product Pages. Signed-out users can explore and compare first; account creation occurs only when persistence or alerts require it. The sign-in boundary states the benefit and preserves the product context.

Target Price asks for a CAD amount, validates sensible input, and confirms: product, target, channel, and alert condition. P1 email alerts are explicit. Weekly digest is P2 and must not appear as an MVP default.

## 16. Reporting stale or wrong information

Report reasons: price changed, product mismatch, retailer page unavailable, incorrect variant, expired offer, or other. Keep the form short, include source context automatically, and show a confirmation without promising an SLA.

## 17. Mobile and responsive behavior

- Mobile-first content order: search, trust summary, key price, CTA, evidence, comparison, history.
- One primary action per card; secondary actions move to a visible overflow.
- After the original retailer CTA leaves the viewport, the mobile Product Page may show a sticky bar containing only the primary retailer handoff, such as “View at Best Buy.” It must not contain Save, Target Price, or multiple competing retailer actions.
- The sticky retailer CTA must not cover evidence, content, or focus targets; it must remain keyboard accessible, have an accessible name, and preserve nearby affiliate-disclosure expectations.
- Comparison tables become stacked cards at narrow widths.
- Filters open as a full-height sheet with applied count and clear/apply controls.
- Touch targets are at least 44 CSS pixels; focus indicators remain visible.
- No essential claim depends on hover, pointer precision, or color alone.

## 18. Accessibility

Target WCAG 2.2 AA. Use semantic headings, landmarks, labeled controls, keyboard-complete flows, visible focus, sufficient contrast, reduced-motion support, and text alternatives for price-history charts. Announce filter changes and alert confirmation to assistive technology. Error messages identify the field and correction.

## 19. Design system direction

The visual personality is trustworthy, practical, and editorially restrained: neutral surfaces, one confident accent for actionable states, and semantic status colors used with text labels. Price is prominent; evidence and freshness are equally legible.

Core components: App Shell, Search Bar, Filter Bar, Deal Card, Price Block, Evidence Badge, Freshness Label, Match Confidence Label, Retailer Offer Card, Comparison Table, Price History Panel, Save Button, Target Price Dialog, Disclosure, Report Dialog, Alert Banner, Empty State, Loading Skeleton, Error State, and Pagination/Load More.

## 20. States and content rules

Every primary screen defines loading, empty, error, stale, expired, unavailable, and success states. The state copy says what happened, what is known, and what the user can do next. Ads or sponsored content, if later introduced, must be visually separated, labeled, and excluded from organic trust signals.

## 21. SEO-ready UX constraints

Public Product Pages should have stable, readable titles, useful structured content, canonical product identity, and indexable evidence summaries only where data supports them. Avoid thin pages, mass-generated category pages, fake review language, and claims that cannot be refreshed.

## 22. Measurement and validation

Measure time to first useful result, search-to-Product-Page rate, evidence expansion, comparison use, retailer handoff, save conversion, alert creation, report rate, stale-rate by surface, and accessibility defects. User testing remains required, but does not block Solution Architecture or Data/Affiliate Architecture. The recommended sequence is: approved UX baseline → architecture and data planning → interactive/coded prototype → 5–8 representative Canadian shopper usability sessions → UX refinement → final MVP UX freeze and broader release.

Usability sessions should test homepage comprehension, Deal Card comprehension, freshness and evidence interpretation, safe comparison, price-history states, affiliate disclosure, retailer handoff expectations, Save Product, Target Price Alert, and mobile usability. This is a formative comprehension study, not a statistical study.

## 23. Candidate experiments after baseline approval

- Search-first homepage versus curated-feed-first homepage.
- Evidence summary expanded by default versus collapsed after the first screen; the approved baseline is expanded textual history interpretation.
- “Best evidence” versus “Most recently checked” as a later ranking experiment; the approved initial default is Most recently checked.
- Save-first versus alert-first return prompt after a verified Product Page.

Experiments must not hide freshness, disclosure, match confidence, or evidence, and must not optimize retailer clicks at the expense of safe decisions.

## 24. Screen inventory

1. Home — desktop and mobile.
2. Deals feed — desktop and mobile.
3. Search results — desktop and mobile.
4. Product Page — desktop and mobile.
5. Price history states.
6. Retailer comparison states.
7. Save and target-price flow.
8. Report stale/wrong flow.
9. Saved products and alerts.
10. Loading, empty, stale, unavailable, uncertain-match, and error states.

Detailed text wireframes are in `docs/ux/WIREFRAMES.md`; tokens and component rules are in `docs/ux/DESIGN-SYSTEM.md`; prioritized UX stories are in `docs/ux/UX-BACKLOG.md`.

## 25. Approved Human UX Checkpoint refinements

- Public evidence wording is “Deals with strong evidence.”
- Public product-match wording is “Same product confirmed,” “Review before comparing,” and “No safe comparison available.”
- Initial feed sort is Most recently checked.
- Textual price-history interpretation is expanded by default.
- Mobile sticky bar contains only the primary retailer handoff after the original CTA leaves the viewport.
- Affiliate disclosure remains adjacent to retailer CTAs; final legal/compliance wording remains subject to later review.

These are UX decisions only. They do not authorize technology selection, integrations, or implementation.

## 26. Final UX recommendation

The approved UX is a narrow, evidence-led responsive experience centered on one trustworthy Product Page and a repeatable Deal Card. Current price, observation time, evidence basis, and product-match state are first-class content. No-history and no-safe-comparison remain useful honest outcomes. Save and Target Price are P1 retention flows, weekly digest remains P2, and the repository is now ready for coordinated Solution/Cloud Architecture and Data/Affiliate planning without starting implementation.
