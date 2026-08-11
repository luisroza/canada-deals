# Canada Deals — UX Design System

**Status:** Proposed — awaiting Human UX Checkpoint
**Purpose:** Shared visual and interaction language for the approved product direction. This is a design specification, not an implementation instruction.

## 1. Visual personality

Canada Deals should feel clear, grounded, and useful. The system favors generous whitespace, legible prices, restrained emphasis, and editorial evidence over urgency. It should not resemble a casino-like coupon wall or an aggressive retailer landing page.

## 2. Semantic color roles

Use semantic roles so meaning survives theme changes and accessibility review:

- **Canvas:** page background and elevated surface.
- **Text:** primary, secondary, muted, and inverse text.
- **Action:** primary CTA, secondary CTA, link, and focus ring.
- **Evidence:** verified/strong evidence and neutral evidence.
- **Freshness:** recent, aging, stale, and unknown.
- **Confidence:** high match, review, and no safe comparison.
- **Feedback:** success, warning, error, and informational.
- **Disclosure:** neutral affiliate/sponsored information.

Never communicate a state by color alone. Pair color with a label, icon, or text explanation. Status colors must meet WCAG 2.2 AA contrast requirements in their actual text and background combinations.

## 3. Typography hierarchy

The type scale should make a price scannable without making a discount claim dominant:

- Display: product promise or page hero, used sparingly.
- Page heading: one clear `h1` per route.
- Section heading: groups history, comparison, evidence, and related content.
- Product title: readable at two lines on mobile; never truncate the identifying model silently.
- Price: strongest numeric emphasis on cards and Product Pages.
- Supporting metadata: retailer, observation time, variant, and availability.
- Label text: badges and compact status labels, always readable at normal zoom.
- Body and helper text: plain-language explanations and disclosure.

Use tabular numerals for prices, timestamps, and comparison columns where available. Do not use all caps for long explanatory text.

## 4. Layout, grid, and spacing

Use a consistent spacing rhythm based on small reusable increments, with larger jumps between sections. Recommended conceptual levels: micro, control, card, section, and page. The exact implementation tokens remain open for architecture.

Desktop layouts use a readable content max-width with a persistent filter rail only where it improves scanning. Mobile layouts use one column, full-width cards, and bottom sheets for filters. Content order must remain meaningful when columns collapse.

## 5. Surfaces, borders, and elevation

Cards use a quiet surface, clear boundary, and modest radius. Elevation is reserved for transient surfaces such as dialogs, filter sheets, and sticky actions. Do not use shadows to imply confidence or deal quality.

Use dividers to separate evidence sections and retailer offers. A high-confidence label must not look like a paid promotion.

## 6. Iconography

Icons should be familiar, simple, and paired with accessible labels. Suggested meanings:

- Search: find a product.
- Bookmark: save.
- Bell: target-price alert.
- Clock: observed time/freshness.
- Check: verified or complete state.
- Question/info: evidence explanation.
- Warning: stale or partial state.
- External-link: retailer handoff.
- Flag: report stale/wrong.

Do not use a star, flame, or trophy as a proxy for evidence quality unless its meaning is explicitly explained.

## 7. Core component inventory

### App Shell

Provides responsive header, search, primary navigation, account entry, and mobile navigation. It preserves route context and has skip-to-content support.

### Search Bar

Has a visible label or accessible name, product/category suggestions, clear action, loading state, and no-result path. Model numbers must be treated as first-class input.

### Deal Card

Contains title, current price, retailer, evidence, freshness, confidence, and a clear details action. Compact variants may reduce metadata only when the information remains available on the Product Page.

### Price Block

Shows CAD currency, current price, and reference state. A missing reference is explicit, not represented by a crossed-out price.

### Evidence Badge / Panel

Uses controlled labels such as Observed history, Retailer reference, Partial evidence, or Reference unavailable. Each label can open a short explanation.

### Freshness Label

Shows human-readable freshness and exposes exact observation time in the detail context. “May be stale” has a clear next action.

### Match Confidence Label

High confidence, Review before comparing, or No safe comparison. The label is text-first and appears near the comparison claim.

### Retailer Offer Card / Comparison Table

Uses the same field order across desktop and mobile: retailer, price, observed time, availability context, evidence, CTA, and disclosure.

### Price History Panel

Has reliable, partial, and unavailable variants. Charts have text summaries, labeled axes, and no implication of continuous coverage where observation is sparse.

### Save Button and Target Price Dialog

Support signed-in and signed-out states, preserve context, and explain the exact alert trigger. Buttons have pending, success, error, and unavailable states.

### Disclosure

Appears close to retailer handoff. It is concise, readable, and visually neutral: the user should understand that Canada Deals may earn a commission without interpreting that as a product endorsement.

### Report Dialog

Short, keyboard-complete, with reason choices, optional note, source context, validation, and confirmation.

### Feedback and content states

Loading skeleton, empty, error, stale, expired, unavailable, partial, and success patterns use the same structure: what is known, what is not known, and what to do next.

## 8. Interaction states

Every interactive component defines default, hover (where relevant), focus-visible, pressed, disabled, loading, success, and error states. Mobile behavior cannot depend on hover. Focus indicators must be visually obvious and not clipped by cards or sticky bars.

## 9. Price and evidence rules

- Use CAD explicitly where ambiguity is possible.
- Current price is visually primary; savings is secondary.
- Reference unavailable means no discount badge.
- Partial history means partial coverage copy.
- Stale price means a visible stale warning and verification CTA.
- An unavailable retailer price is not a zero or blank that looks like a bargain.
- Product identity and variant are shown before cross-retailer comparison.

## 10. Responsive rules

- Desktop: comparison table and filter rail are allowed when content remains readable.
- Tablet: filter rail may become a collapsible toolbar; comparison may use fewer columns.
- Mobile: stack offers, use filter sheet, preserve the same evidence order, and keep one primary action per card.
- At every width, product title, current price, observation time, and confidence state remain visible without interaction.
- Sticky actions must not cover content or focus targets.

## 11. Accessibility requirements

Target WCAG 2.2 AA:

- keyboard access for every flow,
- visible focus and logical focus order,
- semantic landmarks and headings,
- accessible names and descriptions,
- text alternatives for charts and icons,
- contrast that passes for text and controls,
- no color-only state communication,
- touch targets of at least 44 CSS pixels,
- reduced-motion support,
- zoom/reflow support without loss of evidence,
- inline errors with programmatic association,
- announcements for asynchronous results and alert confirmation.

## 12. Content and tone

Use plain, specific language: “Checked 2 hours ago,” “No verified reference,” “Review before comparing,” and “Opens retailer site.” Avoid “hottest,” “unbeatable,” “guaranteed,” “lowest ever,” and other claims not established by evidence.

## 13. Sponsored and affiliate treatment

Affiliate disclosures are close to retailer CTAs and use neutral styling. If sponsored placements are introduced later, they must be separated from organic results, labeled before interaction, and excluded from evidence/confidence signals. Paid ranking is outside MVP.

## 14. Quality checklist for new components

Before adding a component, confirm:

1. What user decision does it support?
2. What evidence and freshness data does it require?
3. What is the unknown, stale, error, and empty state?
4. Does it work on mobile and with keyboard/screen reader?
5. Does it preserve safe comparison and neutral economics?
6. Is the content understandable without color, hover, or animation?

## 15. Open design-token decisions

Exact font family, token values, icon library, and implementation naming remain open until the architecture and frontend decisions are approved. This document defines semantic intent and component behavior only.
