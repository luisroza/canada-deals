# GreatDeals.ca Product Backlog

**Status:** DRAFT - product backlog for checkpoint review
**Priority:** P0 essential, P1 high priority, P2 valuable later, P3 optional/experimental

## Epic 1 - Trustworthy discovery (P0)

### P0-01 Browse verified category deals

**Outcome:** A visitor can browse current offers in approved categories.
**Acceptance:** each card has product identity, CAD price, retailer, source, last-checked time, freshness state, and a clear next action.

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
**Acceptance:** page includes variant, current price, reference context, retailer, availability caveat, timestamp, evidence state, CTA, and affiliate disclosure.

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

### P1-03 Weekly digest experiment

**Outcome:** Test low-noise repeat traffic.
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
| Browser extension | P2 | Useful but adds distribution and policy complexity |
| French-complete experience | P2 | Valuable, but translation/data workflow must be funded |
| Full flyer/grocery coverage | P3 | Strong incumbents and local-data complexity |
| Cashback wallet | P3 | Reconciliation, compliance, and support burden |
| Native mobile app/push | P3 | Prove web retention first |
| AI shopping assistant | P3 | Only after evidence pipeline is reliable |
| Mass programmatic SEO | P3 | Avoid thin pages and index bloat |
