# Canada Deals — UX Backlog

**Status:** APPROVED — Human UX Checkpoint completed
**Scope:** UX stories and acceptance criteria; not an engineering task list

Priority meanings: **P0** required for the core MVP decision loop; **P1** retention experiment; **P2** planned later; **P3** outside the current roadmap or explicitly excluded.

| ID | Surface / component | User need | Priority | Acceptance criteria |
|---|---|---|---|---|
| UX-001 | Homepage | Understand the price-truth promise immediately | P0 | Search and the four trust concepts are visible above the first useful deal; copy is English-first and plain language; mobile preserves the same order. |
| UX-002 | Search | Find a product by name or model | P0 | Search supports product/category intent, suggestions, loading, no-result, retry, and keyboard navigation; query context is preserved. |
| UX-003 | Deals feed | Scan deals with strong evidence quickly | P0 | Feed shows current price, retailer, freshness, evidence state, and a human-readable product-match state on every standard card; initial default sort is Most recently checked; ranking does not hide unknown states. |
| UX-004 | Filters | Narrow results without losing trust context | P0 | Category, retailer, price, freshness, confidence, and availability filters expose active counts, clear actions, and a mobile filter sheet. |
| UX-005 | Deal Card | Decide whether to open a listing | P0 | Standard, compact, featured, stale, partial, and no-reference states are defined; savings is hidden when unsupported. |
| UX-006 | Product Page | Verify the exact product before purchase | P0 | Page identifies title/variant, current CAD price, retailer, observation time, evidence, product-match state, and next action above the fold. |
| UX-007 | Price history | Understand whether price is meaningful | P0 | Product Page renders Reliable, Partial, and Unavailable states; textual interpretation is expanded by default; unavailable history has no fake chart; chart has text alternative; complete history for every product is not required for MVP. |
| UX-008 | Retailer comparison | Compare the same product safely | P0 | Confident matches are grouped; uncertain listings are separated; no-safe-comparison is a useful explicit state on desktop and mobile. |
| UX-009 | Retailer handoff | Continue purchase with clear expectations | P0 | CTA identifies retailer, external navigation, current price context, and nearby affiliate disclosure; mobile sticky CTA contains only the primary retailer handoff after the original CTA leaves the viewport; no paid-ranking implication. |
| UX-010 | Freshness | Know whether the price may have changed | P0 | Human-readable freshness and exact observation time are visible; stale state provides a verification path. |
| UX-011 | Evidence panel | Understand the source of a claim | P0 | Reference basis is labeled as observed history, retailer reference, partial, or unavailable with a concise explanation. |
| UX-012 | Report flow | Correct stale or wrong information | P0 | User can report price change, mismatch, expired offer, unavailable page, or other; form is short, accessible, and confirms submission honestly. |
| UX-013 | Loading / empty / error | Recover from incomplete product data | P0 | Each primary surface has loading, empty, error, expired, and unavailable states with a next action; no blank UI implies zero price. |
| UX-014 | Responsive shell | Use the product on desktop and mobile | P0 | Navigation, cards, filters, comparison, and Product Page have documented responsive behavior; no essential claim relies on hover. |
| UX-015 | Accessibility baseline | Complete core decisions with assistive technology | P0 | Keyboard, focus, labels, landmarks, contrast, touch targets, reduced motion, chart alternatives, and async announcements are specified for all P0 flows. |
| UX-016 | Save Product | Return to a product later | P1 | Save is available on card and Product Page; signed-out boundary explains the benefit, preserves context, and confirms saved state. |
| UX-017 | Target-price alert | Be notified at a chosen price | P1 | User enters CAD target, sees product and trigger condition, receives confirmation, and can later manage the alert. |
| UX-018 | Saved products | Review products intentionally saved | P1 | Saved list shows current price, freshness, evidence, alert status, and stale/unavailable states; mobile remains scannable. |
| UX-019 | Alert preferences | Control email expectations | P1 | User can see alert condition, email destination, active/inactive state, and unsubscribe/manage path without implying weekly digest. |
| UX-020 | Weekly digest | Receive a periodic discovery summary | P2 | Remains on roadmap only; future design must be opt-in, evidence-led, clearly separate from P1 alerts, and never block core MVP. |
| UX-021 | UX research | Validate Canadian shopping comprehension | P1 | Required before final MVP UX freeze and broader release, but does not block Solution Architecture or Data/Affiliate Architecture; 5–8 representative Canadian shoppers test trust, freshness, safe comparison, history states, disclosure, alerts, and mobile usability. |
| UX-022 | SEO-ready Product Page | Discover useful public product information | P1 | Page structure supports stable title, canonical identity, useful evidence summary, and honest unavailable states; no mass-generated thin pages. |
| UX-023 | Sponsored placement | Understand paid content if introduced | P3 | Any future sponsored module is labeled and separated from organic evidence/ranking; excluded from MVP. |
| UX-024 | Community and voting | Share opinions or vote on deals | P3 | Explicitly outside MVP; no comment, reputation, or community affordance appears in core flows. |
| UX-025 | Native app / push / extension | Access deals outside responsive web | P3 | Explicitly outside MVP; no app-install or push request interrupts the core web decision loop. |

## P0 release slice

UX-001 through UX-015 form the minimum coherent UX for the core loop: discover, verify, compare, and safely hand off. A P0 story is not complete if its stale, unknown, error, mobile, and accessibility states are missing.

## P1 release slice

UX-016 through UX-019 add the return loop: Save → Target Price → Alert → Return. These flows must not make account creation a prerequisite for browsing or verifying a product.

## P2 and explicit exclusions

Weekly digest is P2, as approved at the Human Product Checkpoint. Community, cashback/rewards, native app, push notifications, browser extension, AI shopping agent, complex personalization, paid ranking, mass programmatic SEO, French-complete launch, and broad retailer/category promises remain outside MVP.

## Usability validation sequence

The UX baseline is approved. Proceed with Solution Architecture and Data/Affiliate Architecture planning, then validate an interactive/coded prototype with 5–8 representative Canadian shoppers. Use the findings for UX refinement and final MVP UX freeze before broader release. User testing remains required and does not authorize or block application implementation by itself.
