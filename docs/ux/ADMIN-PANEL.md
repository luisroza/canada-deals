# Owner Administration UX

## Status

Approved by the explicit owner request and implemented as a focused operational workspace at `/admin_panel`. The route is absent from public navigation, carries `noindex`, and is disallowed in `robots.txt`; those discovery controls are not security boundaries. Every API operation requires the server-side `OwnerAdmin` role.

## UX concept

The panel is a trustworthy editorial desk, not a general CMS. It keeps the GreatDeals.ca green, neutral surfaces, typography, cards, visible focus, and plain-language statuses while using a separate administrative shell. Desktop uses a compact side navigation. Narrow screens use a horizontally scrollable section navigation, one-column editors, card-based tables, 44px actions, and no hover-only behavior.

## Information architecture

- **Overview:** enabled/draft offers, ready/blocked banners, open customer reports, and publication rules.
- **Offers:** searchable operational list; create, edit, exact published-page preview, enable, or reversibly disable.
- **Categories:** searchable, status-filtered catalog structure with backend product/public-offer impact counts, immutable slugs, and reversible activation.
- **Stores:** searchable operational directory with offer, banner, and affiliate-program readiness summaries, immutable store keys, and reversible activation.
- **Banners:** explicit homepage-carousel selection, public eligibility status, retailer-by-retailer preview, reviewed artwork upload/library, and editing of all persisted profile fields.
- **Reports:** close the public stale/wrong listing feedback loop with Reviewed, Resolved, or Dismissed states and a required resolution note.
- **Audit:** recent administrative actions and reasons.

## Offer editor

The editor progressively groups Product identity, Retailer listing, current offer facts, and publication. It supports the existing domain fields: slug/title/brand/category/model/MPN/GTIN/variants, retailer/policy/external ID/SKU/original title/URL/seller/condition/marketplace/pack/bundle/external identifiers, CAD price, observed/fetched times, availability/region/shipping, match decision, and enabled state.

For an existing Product, a dedicated Product image section shows the current reviewed preview and publication state. The owner can upload a bounded PNG/JPEG/WebP file, record the rights evidence and optional effective/expiry dates, choose whether it activates immediately, and later activate or archive any retained version with an audit reason. A new offer must be saved before an image can be associated with its stable Product identity. Product imagery is allowed only in the fixed `DEAL_CARD`, `PRODUCT_PAGE`, and `WISHLIST` placements; the public experience uses a neutral fallback when no eligible image exists.

Freshness, evidence, history, affiliate handoff, and reference price are read-only derived states. The administrator cannot type a discount/reference price or tracking URL. Existing retailer, Merchant Policy, and external listing identity cannot be changed after creation. Deactivation or match-decision changes require a reason and remain audited.

## Category and store management

Categories and Stores are independent top-level areas because they have different lifecycle and impact boundaries. Both use responsive cards rather than the Offers table, with name/key search and All, Active, Inactive, With public offers, and Empty filters. Counts are projected by the backend from the complete database rather than inferred from the dashboard's bounded offer list.

New categories and stores always begin inactive. The owner can edit display names and activate or deactivate records; category slugs and store keys are immutable after creation. There is no destructive delete action. Deactivating a category hides its products and offers from public discovery while preserving products, offers, wishlists, history, and audit. Deactivating a store hides its offers and banners and blocks product/store handoffs while preserving listings, programs, links, destinations, artwork, history, and audit.

Editors show impact counts and plain-language consequences before deactivation, and a reason is mandatory when changing an active record to inactive. Store activation explicitly does not grant data rights, connector permission, banner rights, or affiliate eligibility; those capabilities remain independently derived and fail closed.

## Banner editor

The banner list exposes `Active in homepage carousel` directly on every configured store card. Selection is saved as one audited operation; removing any active banner requires a reason. The UI distinguishes selected, actually public, and needs-attention counts because a selected banner still requires an enabled retailer and a publicly eligible offer. Stores without an explicitly active profile never enter through an implicit fallback. Filters cover All, Active, Inactive, and Needs attention.

The editor groups Banner copy, Banner artwork, Artwork provenance and rights, and Carousel placement. A 360px desktop side panel presents a public-like 16:9 preview, publication switch, position, artwork state, and plain-language public eligibility reason; it moves into normal document flow on narrower screens. Title/subtitle counters, 44px controls, text states, and associated help preserve keyboard and mobile usability.

`Upload artwork` accepts PNG, JPEG, or WebP files up to 2 MB. The API validates the declared type and file signature, persists the immutable bytes in PostgreSQL, audits the owner upload, and exposes a same-origin public asset path. This keeps low-volume owner assets durable across container restarts without introducing object storage. Uploaded files enter the reviewed artwork library and are not sufficient by themselves to publish merchant artwork.

Artwork provenance remains a controlled rights taxonomy: `GreatDeals original` or `Merchant-approved affiliate asset`. The owner edits the applicable provenance selection and, for merchant artwork, provider, redacted evidence reference, fixed `store_banner` placement, and rights dates. Arbitrary source types are not accepted because they would bypass server-side rights interpretation. Public affiliate destination remains derived from approved persisted provider records. Merchant assets fail closed unless provider, placement, evidence, and validity window all pass.

## States and accessibility

- Authentication: loading, signed out, unauthorized account, unavailable API, and active owner session.
- Offers: draft/deactivated, enabled, policy-blocked, and publicly eligible.
- Categories and Stores: active, inactive, empty/no public offers, saving, conflict, and impact warning.
- Banners: not configured, selected, inactive, public, no eligible offer, fallback artwork, scheduled, expired, and rights-blocked.
- Artwork intake: no file selected, uploading, uploaded/selected, invalid type/signature, oversized, and server error.
- Mutations: saving, saved, validation failure, conflict, and server error.
- Reports: open, reviewed, resolved, and dismissed.

Forms use semantic labels, field-associated errors, status/alert live regions, keyboard-complete controls, textual state labels, responsive reflow, and reduced visual density on narrow screens. No meaning depends on color alone.

## Deliberately excluded

User/role management, public admin links, destructive category/store deletion, category hierarchy, category imagery/SEO, arbitrary SVG or unbounded file upload, artwork deletion, page building, coupons, campaign scheduling, connector configuration, manual tracking URLs, merchant-rights approval, advanced analytics, MFA, and password recovery are not part of this slice. Automated crop/background removal, metadata re-encoding, optimistic concurrency, and MFA/re-authentication remain follow-ups.
