# Owner Administration UX

## Status

Approved by the explicit owner request and implemented as a focused operational workspace at `/admin_panel`. The route is absent from public navigation, carries `noindex`, and is disallowed in `robots.txt`; those discovery controls are not security boundaries. Every API operation requires the server-side `OwnerAdmin` role.

## UX concept

The panel is a trustworthy editorial desk, not a general CMS. It keeps the GreatDeals.ca green, neutral surfaces, typography, cards, visible focus, and plain-language statuses while using a separate administrative shell. Desktop uses a compact side navigation. Narrow screens use a horizontally scrollable section navigation, one-column editors, card-based tables, 44px actions, and no hover-only behavior.

## Information architecture

- **Overview:** enabled/draft offers, ready/blocked banners, open customer reports, and publication rules.
- **Offers:** searchable operational list; create, edit, exact published-page preview, enable, or reversibly disable.
- **Banners:** retailer-by-retailer visual preview and editing of all persisted profile fields.
- **Reports:** close the public stale/wrong listing feedback loop with Reviewed, Resolved, or Dismissed states and a required resolution note.
- **Audit:** recent administrative actions and reasons.

## Offer editor

The editor progressively groups Product identity, Retailer listing, current offer facts, and publication. It supports the existing domain fields: slug/title/brand/category/model/MPN/GTIN/variants, retailer/policy/external ID/SKU/original title/URL/seller/condition/marketplace/pack/bundle/external identifiers, CAD price, observed/fetched times, availability/region/shipping, match decision, and enabled state.

Freshness, evidence, history, affiliate handoff, and reference price are read-only derived states. The administrator cannot type a discount/reference price or tracking URL. Existing retailer, Merchant Policy, and external listing identity cannot be changed after creation. Deactivation or match-decision changes require a reason and remain audited.

## Banner editor

The editor manages title, subtitle, reviewed `/store-banners/` asset, original or merchant-approved asset source, provider, redacted rights evidence, fixed `store_banner` placement, effective/expiry time, order, and enabled state. Public affiliate destination remains derived from approved persisted provider records. Merchant assets fail closed unless provider, placement, evidence, and validity window all pass.

## States and accessibility

- Authentication: loading, signed out, unauthorized account, unavailable API, and active owner session.
- Offers: draft/deactivated, enabled, policy-blocked, and publicly eligible.
- Banners: not configured, disabled, scheduled, enabled, expired, and rights-blocked.
- Mutations: saving, saved, validation failure, conflict, and server error.
- Reports: open, reviewed, resolved, and dismissed.

Forms use semantic labels, field-associated errors, status/alert live regions, keyboard-complete controls, textual state labels, responsive reflow, and reduced visual density on narrow screens. No meaning depends on color alone.

## Deliberately excluded

User/role management, public admin links, arbitrary file upload, page building, coupons, campaign scheduling, connector configuration, manual tracking URLs, merchant-rights approval, advanced analytics, MFA, and password recovery are not part of this slice. Asset intake, stronger preview-before-publication, optimistic concurrency, and MFA/re-authentication remain follow-ups.
