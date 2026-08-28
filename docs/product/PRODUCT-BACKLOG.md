# GreatDeals.ca Product Backlog

**Status:** APPROVED - Human Product Checkpoint completed; UX handoff refined
**Priority:** P0 essential, P1 high priority, P2 valuable later, P3 optional/experimental

## Major feature contract

| Feature | Priority | User problem | Expected outcome | Dependencies | Acceptance-level product outcome |
|---|---|---|---|---|---|
| Verified category deal feed | P0 | Shoppers do not know where to start | A focused feed surfaces current Canadian offers | Approved source, freshness rules, category taxonomy | A visitor can browse offers with source, timestamp, CAD price, and freshness state |
| Product and brand search | P0 | Planned purchases require repeated retailer searches | A shopper finds a relevant product without an account | Product catalog, identity confidence, search relevance test | Search distinguishes verified, weak, and no-result states |
| Evidence-rich deal card | P0 | Discount badges do not explain whether a price is good | The card makes the next decision faster | Price evidence, source provenance, deal-quality explanation | Card shows price, retailer, availability, evidence state, timestamp, and CTA |
| Product decision page | P0 | Shoppers open several tabs to validate an offer | One page explains current value and caveats | Approved UX, product data, disclosure policy | Page exposes variant, seller, condition, availability, region, shipping, last check, unknown commercial conditions, evidence, comparison, CTA, and report path |
| Safe retailer comparison | P0 | Cross-retailer comparison is manual and error-prone | Equivalent offers can be compared safely | Matching rules, variant/seller/condition confidence, retailer data | Comparison is hidden when identity confidence is insufficient |
| Freshness and mismatch reporting | P0 | Automated prices can become stale or mismatched | Users and operators can detect and correct trust failures | Source timestamps, report workflow, review ownership | Reports are attributable, reviewable, and reflected in offer state |
| Save product/deal | P1 | Planned shoppers need a return path | Users can preserve intent without a complex account | Lightweight identity/session model, privacy/consent decision | Saved state is visible, reversible, and linked to the product |
| Target-price email alert | P1 | Shoppers want to wait for a better price | A user receives a relevant, controlled notification | Fresh evidence, consent, deliverability, unsubscribe, frequency policy | Alert fires only for the saved variant and valid evidence |
| Retailer handoff and affiliate disclosure | P0 | Commercial links can obscure incentives | Qualified clicks are measured without reducing trust | Approved program, redirect policy, disclosure copy, attribution | CTA clearly discloses commercial relationship and records the outbound event |
| Weekly digest experiment | P2 | Users may forget the product after the alert loop is validated | A low-noise digest creates repeat behaviour | Saved/category signals, email consent, content freshness | Digest is opt-in, useful, labelled, and easy to unsubscribe |
| Saved-search/keyword alert experiment | P2 | A shopper may know the desired category, brand, or term before choosing one canonical Product | Test broader planned-purchase return value without weakening Target Price truth | Reliable search, alert frequency controls, strong-evidence eligibility, consent | Alerts are opt-in, bounded, explain why an item matched, and default to strong/fresh evidence |
| Structured offer confirmation experiment | P2/P3 | Automated freshness can lag a retailer change | Test low-cost correction signals without opening a moderation-heavy community | Abuse controls, review queue, source reconciliation, measurable correction time | Users may submit only controlled signals such as price changed, coupon worked, or out of stock; signals never directly change Price Truth or ranking |

## Epic 1 - Trustworthy discovery (P0)

### P0-01 Browse verified category deals

**Outcome:** A visitor can browse current offers in approved categories.
**Acceptance:** each card has product identity, CAD price, retailer, online availability, source, last-checked time, freshness state, and a clear next action.

### P0-02 Search products and brands

**Outcome:** A planned shopper can start from an intended purchase.
**Acceptance:** search works without account creation and distinguishes no result, weak match, and verified result.

### P0-03 Filter discovery results

**Outcome:** Shoppers can reduce noise.
**Acceptance:** filter by category, retailer, price, discount, freshness, and online availability; active filters are visible and removable.

### P0-04 Explain offer quality

**Outcome:** A shopper understands why an offer deserves attention.
**Acceptance:** explanation uses only available evidence and can say “unknown” or “wait” when evidence is insufficient.

## Epic 2 - Product decision pages (P0)

### P0-05 Product detail page

**Outcome:** A shopper can evaluate an offer without opening several tabs.
**Acceptance:** page includes variant, current price, reference context, retailer, seller, condition, availability, region, shipping, timestamp, evidence state, CTA, and affiliate disclosure. Missing coupon, membership/payment eligibility, or offer-expiry facts are labelled unverified rather than inferred.

### P0-06 Safe retailer comparison

**Outcome:** A shopper can compare equivalent offers.
**Acceptance:** comparison is suppressed when identity, condition, seller, size, pack, or variant confidence is below threshold.

### P0-07 Report stale or incorrect information

**Outcome:** Users can help correct unreliable data.
**Acceptance:** report includes reason, offer context, and visible acknowledgement; reports are available for review.

## Epic 3 - Retention (P1)

### P1-01 Save a product or deal

**Outcome:** A shopper can return to planned purchases.
**Acceptance:** saving is low-friction, state is visible, and a user can remove an item.

### P1-02 Target-price email alert

**Outcome:** A shopper can wait for a defined price.
**Acceptance:** target price, product variant, consent, frequency, and unsubscribe are explicit; alert fires only when evidence is fresh enough.

### P2-01 Weekly digest experiment

**Outcome:** Test low-noise repeat traffic after Save -> Target Price -> Alert -> Return is validated.
**Acceptance:** digest is opt-in, uses saved/category signals, labels affiliate content, and exposes unsubscribe.

## Epic 4 - Commercial measurement (P0)

### P0-08 Retailer handoff measurement

**Outcome:** Product and business teams can measure qualified outbound intent.
**Acceptance:** clicks are attributable to page, offer, retailer, and timestamp without hiding the destination or disclosure.

### P0-09 Affiliate disclosure and ranking policy

**Outcome:** Users can understand commercial incentives.
**Acceptance:** disclosure is adjacent to monetized actions and methodology explains that commission does not silently override offer quality.

## Epic 5 - Data quality (P0/P1)

### P0-10 Freshness state

**Outcome:** Users know whether an offer may have changed.
**Acceptance:** stale thresholds are category/source-specific and stale offers are hidden, downgraded, or clearly labelled.

### P0-11 Product identity confidence

**Outcome:** Wrong comparisons are prevented.
**Acceptance:** identity confidence is stored and used by search, cards, comparison, and alerts.

### P1-04 History evidence state

**Outcome:** Historical claims are honest.
**Acceptance:** the product distinguishes complete/partial/unavailable history and never labels an inferred low as a verified historical low.

## Epic 6 - Deferred growth capabilities

| Backlog item | Priority | Reason to defer |
|---|---|---|
| Community posts/comments/votes | P2 | Moderation and abuse cost; validate core trust first |
| Open-ended deal submissions | P2 | Structured correction signals must prove useful before accepting publishable community content |
| Browser extension | P2 | Useful but adds distribution and policy complexity |
| French-complete experience | P2 | Valuable, but translation/data workflow must be funded |
| Full flyer/grocery coverage | P3 | Strong incumbents and local-data complexity |
| Cashback wallet | P3 | Reconciliation, compliance, and support burden |
| Native mobile app/push | P3 | Prove web retention first |
| AI shopping assistant | P3 | Only after evidence pipeline is reliable |
| Mass programmatic SEO | P3 | Avoid thin pages and index bloat |
| Google sign-in for end-user Wishlist accounts | P2 | Reduce Wishlist account friction only after ADR-013, privacy, Architecture, and Security approval; email/password remains available and owner administration stays excluded |
