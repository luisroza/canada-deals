# Frontend foundation

Vertical Slice 7 adds `/account/confirm-email` with accessible confirming, confirmed, already-confirmed, invalid/expired, retry-error, and resend states. Registration remains on the existing minimal account surface and exposes a resend action only after an unconfirmed registration response. Confirmation is performed by a CSRF-protected POST; secrets/tokens are never sent to analytics.

The frontend is a Next.js 16 + React 19 + TypeScript application in `apps/web`.

The owner workspace at `/admin_panel` separates Offers, Catalog, Stores, Banners, Reports, and Audit. Offer entry may create a Product or reuse an internal Product identity, while every saved retailer listing remains an independent public offer. The form captures deal price, optional evidence-backed regular price, promotion start/end, source facts, and reviewed imagery.

Implemented routes:

- `/` - search-first “Deals with strong evidence” fixture feed.
- `/offers/[listingId]` - canonical public server-rendered Offer Page for one exact retailer listing, with deal/regular price context, evidence, freshness, offer facts, reporting, Save offer, and a route-specific unavailable state.
- `/products/[slug]` - compatibility route that resolves one eligible listing and redirects to its canonical Offer Page; it is not a comparison page.
- `/account/sign-in` and `/account/register` - minimal account forms with safe internal `returnTo` context.
- `/saved` - private client-layered exact-offer Wishlist with local search/category/store/sort, empty/signed-out/error states, and removal.

SEO-critical Offer content remains public and server-rendered; authenticated state is layered on without making Offer responses private. A signed-out Save action explains why identity is needed and preserves the exact Offer path through sign-in/register. Account credentials/tokens are never stored in localStorage, sessionStorage, URLs, or JavaScript-readable cookies.

Client JavaScript also handles the focused report form, account/session state, listing-keyed Save/Saved/Remove state, and `/saved`. Every mutation obtains a fresh framework anti-forgery request token and sends it through the same-site `/api/*` rewrite.

Public detail pages do not render Target Price controls, price history, comparison tables, or related retailer offers. Historical components/contracts remain only for rollback compatibility. The active hierarchy is deal price, optional verified regular price/savings for that same listing, freshness/evidence, and retailer action.

Server-rendered API reads use `API_BASE_URL`; Production server startup still fails closed when it is missing. Browser code uses the same-site `/api` and `/go` boundaries without requiring this server-only variable, so importing shared API contracts or handoff helpers cannot fail during client hydration. In local development, server reads point to `http://localhost:5099`.

When a Product slug cannot be resolved, the route keeps the HTTP 404 response and `noindex,nofollow` metadata for truthful indexing while replacing the generic framework error with a branded recovery state. The page explains that the Product may have moved or been removed and offers an accessible Product search, current-deals navigation, and Wishlist access.

The public homepage exposes search, category, store, sort, and page. General discovery defaults to recently checked; searches default to relevance unless the shopper explicitly selects another sort. Active category/store filters are removable chips, Clear preserves search/sort as appropriate, pagination preserves controls, and narrowed/search result pages use `noindex,follow` to avoid thin SEO surfaces.

The homepage feed is server-rendered for its initial state, then changes filters and switches between Latest, Best savings, Lowest price, and search relevance through the same-site discovery API. Applying Category/Store, removing either active chip, or clearing both filters keeps the current page mounted, replaces cards/count in place, resets pagination, and synchronizes the query string through the browser History API. Filter and sort updates preserve the prior scroll coordinate whenever the filtered document remains tall enough; if fewer results make that coordinate impossible, the browser remains at the nearest valid position instead of jumping to the top. Back and Forward restore the corresponding feed and control state; a failed update leaves the current results usable and reports the failure inline.

Every feed item is keyed by `listingId` and links to `/offers/{listingId}`. Multiple listings attached to one internal Product are independent cards. Best savings uses only the optional `regularPrice` and `currentPrice` from that listing; no frontend code groups or compares retailer offers.

Desktop and mobile keep Category and Store as the only public filter controls below search. Their menus are mutually exclusive, Clear resets active values in place, and sorting remains visible. The UI displays only backend-provided freshness/evidence and same-listing savings facts; it does not calculate ranking, history claims, cross-retailer comparisons, commercial economics, or personalization.
