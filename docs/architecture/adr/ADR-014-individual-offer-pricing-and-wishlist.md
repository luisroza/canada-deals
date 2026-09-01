# ADR-014: Individual offer pricing and Wishlist identity

**Status:** Accepted
**Date:** 2026-08-31

## Context

The approved store-led experience no longer compares the same Product across retailers. The prior public model selected one representative `RetailerListing` per canonical Product, could expose same-Product alternatives, derived a reference value from observations, and saved Wishlist intent by Product ID. That conflicts with the current product decision: every retailer listing is a unique promotion and its “regular price” means the ordinary price for that exact listing outside the promotion.

## Decision

- `RetailerListing.Id` is the public offer identity for discovery cards, detail routes, reports, retailer handoff, and Wishlist entries.
- Discovery returns every eligible listing independently. It does not deduplicate by Product or choose a representative retailer.
- `/offers/{listingId}` and `GET /api/v1/offers/{listingId}` are the canonical detail boundaries. The legacy Product slug route may resolve and redirect to one eligible offer but cannot expose comparisons.
- `RetailerListing` stores optional `RegularPriceAmount`, currency, observed-at, and evidence reference plus optional offer-valid-from/to timestamps.
- Savings are displayed and sorted only when the same listing has a regular price greater than its deal price, matching currency, observation time, and evidence reference.
- `SavedOffer` uses the composite key `(UserId, RetailerListingId)`. Existing Product-level saves migrate deterministically to one existing listing.
- Canonical `Product` and matching data remain for search, catalog normalization, images, source reconciliation, and administration. They do not merge public cards or authorize cross-retailer price claims.

## Consequences

- Two stores selling the same internal Product produce two cards, two Offer Pages, two possible Wishlist entries, and two independent outbound destinations.
- A retailer with two distinct listings for the same Product also produces independent offers.
- Public contracts no longer return safe-comparison or related-listing arrays, and the frontend removes comparison/history/alert surfaces from Offer Pages.
- Admin and connectors must supply regular-price evidence for the exact listing; absent or invalid data remains null and no savings claim is rendered.
- Legacy Product, history, and alert code may remain for rollback compatibility, but it is not an active product experience and cannot implicitly add a Saved Offer.
- Existing Product-level saves migrate to one deterministic listing. If a legacy Product has no listing, its save is retained in an audit-only orphan table and excluded from the active Wishlist until an explicit recovery decision can identify an exact offer.

## Rejected alternatives

- Keep one representative card per Product: rejected because it hides valid promotions and makes Wishlist identity ambiguous.
- Use another retailer's price or a historical maximum as “regular price”: rejected because it changes the meaning of the promotion and can create misleading savings.
- Remove canonical Product matching entirely: rejected because normalization, search, images, admin reuse, and connector reconciliation still need a stable internal identity.
