# ADR-012: Confirmed brand-candidate intake

**Status:** APPROVED - explicit owner decision, 2026-08-28  
**Checkpoint:** this changes owner-only catalog intake; retailer data rights, Product matching, imagery, price, and publication gates remain unchanged.

## Context

The owner Offer editor can derive a reviewable Product title from a validated retailer URL and already selects an enabled Brand when that title starts with an existing catalog Brand. When no Brand matches, the previous flow required a separate create request followed by a separate activation request before the offer could be saved. That was repetitive and could leave an inactive orphan Brand if the second request failed.

A URL path is not authoritative Product metadata. Its first term may be a Brand, seller, Product line, or promotional text. Repeated entries can also create semantic duplicates such as `DeWalt`, `DEWALT`, and `DeWalt®` when uniqueness is based only on an immutable slug.

## Options

1. Keep all unknown Brand entry separate and manual.
2. Create and activate a Brand immediately whenever link validation infers one.
3. Return a non-persistent Brand candidate, reuse exact normalized catalog matches, and create or reactivate a Brand only after explicit owner confirmation while saving the offer.

## Decision

Use option 3.

- Link inspection remains read-only and may return a `BrandCandidate` with name, proposed slug, normalized key, source, confidence, match state, and any exact catalog match.
- An enabled exact match is selected automatically.
- A new or inactive candidate is displayed as a proposal and remains editable. The owner must explicitly confirm creation/activation before saving.
- URL-path inference is always low confidence. It is never treated as provider-authoritative metadata and never publishes by itself.
- Saving a new Product may create or reactivate the confirmed Brand in the same EF/PostgreSQL unit of work as Product, listing, affiliate-link state, and audit records.
- Existing Products retain their canonical Brand. A new candidate cannot overwrite it.
- `Brand.NormalizedKey` is deterministic and unique. Display name and immutable slug remain separate. Case, whitespace, simple punctuation, and trademark marks do not create duplicate normalized identities.
- A normalized match is reused instead of creating another Brand. Database uniqueness remains the final concurrency guard.
- Provider aliases and stable source mappings remain a future extension when authorized structured Brand fields are activated.

## Reasoning

This removes the routine two-request Brand workflow while preserving the Human Product/UX requirement that canonical identity changes remain reviewable. It also prevents abandoned link analysis from polluting the catalog and makes a failed offer save roll back its new Brand.

## Tradeoffs

- URL-only candidates still require one owner confirmation.
- A concurrent first save can return a recoverable conflict and require revalidation; the unique normalized key prevents duplicate persistence.
- Brands with aliases that do not normalize to the same value still require manual catalog reconciliation until source mappings are implemented.

## Migration path

Add and backfill `Brands.NormalizedKey`, fail migration when existing rows normalize to duplicates, then create a unique index. Update owner link inspection, Offer request contracts, the editor, audit coverage, and integration tests. ADR-012 refines ADR-011 by allowing a low-confidence review candidate from URL-contained text; ADR-011's prohibition on treating that text as licensed Product metadata remains active.
