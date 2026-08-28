# Owner Administration UX

## Status

Approved by the explicit owner request and implemented as a focused operational workspace at `/admin_panel`. The route is absent from public navigation, carries `noindex`, and is disallowed in `robots.txt`; those discovery controls are not security boundaries. Every API operation requires the server-side `OwnerAdmin` role.

## UX concept

The panel is a trustworthy editorial desk, not a general CMS. It keeps the GreatDeals.ca green, neutral surfaces, typography, cards, visible focus, and plain-language statuses while using a separate administrative shell. Desktop uses a compact side navigation. Narrow screens use a horizontally scrollable section navigation, one-column editors, card-based tables, 44px actions, and no hover-only behavior.

## Information architecture

- **Overview:** enabled/draft offers, ready/blocked banners, open customer reports, and publication rules.
- **Offers:** searchable operational list; create a Product with its first offer, attach another store offer to an existing Product, edit, preview, enable, or reversibly disable.
- **Catalog:** categories are the default operational view; brand lifecycle management is retained as a secondary advanced view with backend product/public-offer impact counts, immutable slugs, and reversible activation.
- **Stores:** searchable operational directory with offer, banner, and affiliate-program readiness summaries, immutable store keys, and reversible activation.
- **Banners:** explicit homepage-carousel selection, public eligibility status, retailer-by-retailer preview, reviewed artwork upload/library, and editing of all persisted profile fields.
- **Reports:** close the public stale/wrong listing feedback loop with Reviewed, Resolved, or Dismissed states and a required resolution note.
- **Audit:** recent administrative actions and reasons.

## Offer editor

The editor follows three visible steps: **Analyze link**, **Review card**, and **Publish**. It starts in a safe draft/manual-review state and never publishes from link inspection alone. The owner can then create a new Product or add an offer to an existing Product. Reuse selects the canonical Product and locks its identity, preventing duplicate Product records while allowing safe same-product store comparisons. Product slugs are immutable after creation so saved and shared routes remain stable.

Frequently used Product and offer essentials are open by default. Matching identity and source/retailer metadata live in collapsed advanced sections. The complete contract remains available: title/slug/brand/category/model/MPN/GTIN/variants, retailer/policy/external ID/SKU/original title/Product URL/seller/condition/marketplace/pack/bundle/external identifiers, CAD price, observed/fetched times, optional offer-valid-until, availability/region/shipping, match decision, and enabled state. Brand begins unselected for a new Product so the first active database record is never silently assigned. Link validation selects a matching active Brand when available. Otherwise it may fill a clearly labelled, editable low-confidence candidate without creating a catalog record. The owner must confirm that candidate; creation or reactivation then occurs with the offer save. The optional validity time automatically removes an expired offer from public discovery and retailer handoff without requiring a scheduled job.

For an existing Product, a dedicated Product image section shows the current reviewed preview and publication state. The owner can upload a bounded PNG/JPEG/WebP file, record the rights evidence and optional effective/expiry dates, choose whether it activates immediately, and later activate or archive any retained version with an audit reason. A new offer must be saved before an image can be associated with its stable Product identity. Product imagery is allowed only in the fixed `DEAL_CARD`, `PRODUCT_PAGE`, and `WISHLIST` placements; the public experience uses a neutral fallback when no eligible image exists.

The link-intake card accepts a finished retailer-program link and reports `READY`, `NEEDS REVIEW`, or `UNSUPPORTED`. For Amazon.ca, a full Product URL can prefill ASIN, canonical Product page, and Partner Tag. A shortened `amzn.to` link is preserved exactly while the owner-triggered validation follows only bounded redirect headers to confirm an HTTPS Amazon.ca destination; it never downloads the Product page. The owner must attest that the Canada Associates relationship is approved for GreatDeals.ca and attach a redacted evidence reference. The side panel previews only the essential public card hierarchy and clearly separates Save draft from Publish.

Link analysis now applies safe autofill immediately: matching store, ASIN/external ID, canonical destination, visible Partner Tag, external identifier JSON, a reviewable title suggestion, and an optional Brand candidate derived only from the validated descriptive URL. If the candidate exactly matches an enabled normalized catalog Brand, it is selected. A new or inactive candidate remains visibly pending until the owner confirms it; URL text is labelled low-confidence and is not provider-authoritative metadata. If the same store/external ID already exists, the panel directs the owner to that complete catalog offer instead of creating a duplicate; its approved identity, price, and reviewed image remain reusable. A new unknown Amazon item still leaves price, category judgment, model number, and image unresolved. Automatic Amazon offer/image retrieval requires an approved Creators API `GetItems` connector and applicable content policy; page scraping is not used.

For a new Product, the image picker is part of the first entry flow instead of requiring a save-and-reopen cycle. The browser holds the selected PNG/JPEG/WebP locally, previews it, collects rights evidence and dates, then creates the offer as a draft, uploads the reviewed image against the new stable Product ID, and only then publishes when requested. If image attachment fails, the offer remains a recoverable draft and the owner receives a specific partial-success message.

Freshness, evidence, history, retailer handoff, and reference price are read-only derived states. Amazon owner-provided links are direct and exact; Impact, CJ, and Rakuten links remain provider-generated protected `/go` handoffs. Existing retailer, Merchant Policy, external listing identity, Product association, and Product slug cannot be changed after creation. Deactivation or match-decision changes require a reason and remain audited.

## Catalog and store management

Categories and Brands share one top-level **Catalog** area because both support Product identity. Categories are shown first because they control the public discovery structure; Brands remain available through a clearly labelled advanced secondary tab. Stores retain their independent top-level operational area. All three use responsive cards rather than the Offers table, with name/key search and All, Active, Inactive, With public offers, and Empty filters. Counts are projected by the backend from the complete database rather than inferred from the dashboard's bounded offer list.

Inline Brand intake no longer performs separate create and activation requests. It shows the proposed display name and immutable slug, requires an explicit `Create, activate, and use this brand when I save the offer` confirmation, and submits the candidate with the Offer. The backend reuses an exact normalized Brand or creates/reactivates it together with Product, listing, affiliate-link state, and audit records. Advanced rename, impact review, activation, and deactivation remain outside the routine offer workflow.

Brands created directly from Catalog, plus all new categories and stores, begin inactive. The explicit Offer-save confirmation defined by ADR-012 is the narrow exception: it may create or reactivate the reviewed Brand as active in the same transaction as the offer. The owner can edit display names and activate or deactivate records; brand/category slugs and store keys are immutable after creation. There is no destructive delete action. Deactivating a brand or category hides its products and offers from public discovery while preserving products, offers, wishlists, history, and audit. Deactivating a store hides its offers and banners and blocks product/store handoffs while preserving listings, programs, links, destinations, artwork, history, and audit.

Editors show impact counts and plain-language consequences before deactivation, and a reason is mandatory when changing an active record to inactive. Store activation explicitly does not grant data rights, connector permission, banner rights, or affiliate eligibility; those capabilities remain independently derived and fail closed.

## Banner editor

The banner list exposes `Active in homepage carousel` directly on every configured store card. Selection is saved as one audited operation; removing any active banner requires a reason. The UI distinguishes selected, actually public, and needs-attention counts because a selected banner still requires an enabled retailer and a publicly eligible offer. Stores without an explicitly active profile never enter through an implicit fallback. Filters cover All, Active, Inactive, and Needs attention.

The editor groups Banner copy, Banner artwork, Artwork provenance and rights, and Carousel placement. A 360px desktop side panel presents a public-like 16:9 preview, current selection, position, artwork state, and plain-language public eligibility reason; it moves into normal document flow on narrower screens. Carousel membership has one source of truth: the list-level selection workflow. The content editor cannot independently activate or deactivate a banner. Title/subtitle counters, 44px controls, text states, and associated help preserve keyboard and mobile usability.

`Upload artwork` accepts PNG, JPEG, or WebP files up to 2 MB. The API validates the declared type and file signature, persists the immutable bytes in PostgreSQL, audits the owner upload, and exposes a same-origin public asset path. This keeps low-volume owner assets durable across container restarts without introducing object storage. Uploaded files enter the reviewed artwork library and are not sufficient by themselves to publish merchant artwork.

Artwork provenance remains a controlled rights taxonomy: `GreatDeals original` or `Merchant-approved affiliate asset`. The owner edits the applicable provenance selection and, for merchant artwork, provider, redacted evidence reference, fixed `store_banner` placement, and rights dates. Arbitrary source types are not accepted because they would bypass server-side rights interpretation. Public affiliate destination remains derived from approved persisted provider records. Merchant assets fail closed unless provider, placement, evidence, and validity window all pass.

## States and accessibility

- Authentication: loading, signed out, unauthorized account, unavailable API, and active owner session.
- Offers: draft/deactivated, enabled, policy-blocked, and publicly eligible.
- Brands, Categories, and Stores: active, inactive, empty/no public offers, saving, conflict, and impact warning.
- Banners: not configured, selected, inactive, public, no eligible offer, fallback artwork, scheduled, expired, and rights-blocked.
- Artwork intake: no file selected, uploading, uploaded/selected, invalid type/signature, oversized, and server error.
- Mutations: saving, saved, validation failure, conflict, and server error.
- Reports: open, reviewed, resolved, and dismissed.

Forms use semantic labels, field-associated errors, status/alert live regions, keyboard-complete controls, textual state labels, responsive reflow, and reduced visual density on narrow screens. No meaning depends on color alone.

## Deliberately excluded

User/role management, public admin links, destructive brand/category/store deletion, category hierarchy, category imagery/SEO, arbitrary SVG or unbounded file upload, artwork deletion, page building, coupons, recurring campaign scheduling, connector configuration, Product-page scraping, merchant-rights approval, advanced analytics, MFA, and password recovery are not part of this slice. Owner-provided tracking links do not authorize Product data ingestion. Automated crop/background removal, metadata re-encoding, optimistic concurrency, and MFA/re-authentication remain follow-ups.
