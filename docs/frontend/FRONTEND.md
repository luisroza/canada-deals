# Frontend foundation

Vertical Slice 7 adds `/account/confirm-email` with accessible confirming, confirmed, already-confirmed, invalid/expired, retry-error, and resend states. Registration remains on the existing minimal account surface and exposes a resend action only after an unconfirmed registration response. Confirmation is performed by a CSRF-protected POST; secrets/tokens are never sent to analytics.

The frontend is a Next.js 16 + React 19 + TypeScript application in `apps/web`.

Implemented routes:

- `/` - search-first “Deals with strong evidence” fixture feed.
- `/products/[slug]` - public server-rendered Product Page with evidence, freshness, bounded Product history, comparisons, reporting, Save, and a client-layered Target Price Alert control.
- `/account/sign-in` and `/account/register` - minimal account forms with safe internal `returnTo` context.
- `/saved` - private client-layered saved Product list with alert target/status, target-management link, alert removal, empty/signed-out/error states, and Unsave.

SEO-critical Product content remains public and server-rendered; authenticated state is layered on without making Product responses private. A signed-out Save action explains why identity is needed and preserves the Product path through sign-in/register. After Development/Test registration or successful login, the shopper returns to that path and explicitly completes Save. Account credentials/tokens are never stored in localStorage, sessionStorage, URLs, or JavaScript-readable cookies.

Client JavaScript also handles the focused report form, account/session state, Save/Saved/Unsave state, and `/saved`. Every mutation obtains a fresh framework anti-forgery request token and sends it through the same-site `/api/*` rewrite.

`TargetPriceAlertControl` preserves public discovery for signed-out users and safe `returnTo` for the existing account flow. Confirmed users enter a numeric CAD target and separately check explicit transactional-alert consent. Active state is textual and exposes Edit/Remove. Unconfirmed accounts never see a false active state. Copy states that alerts use fresh, verified offers, equality qualifies, creation also saves the Product, and consent does not include marketing or Weekly Digest. The UI does not calculate eligibility.

Development/Test shows the honest delivery boundary. Production email delivery remains unconfigured; the frontend does not claim that a real email was sent.

`PriceHistoryEvidence` sits below primary price/evidence and above safe current comparison. Its always-visible hierarchy is current price/freshness, history state, lowest observed summary, truthful tracking start, observation/coverage explanation, then a lightweight SVG. Shareable `?history=30d|90d` links expose selected state through `aria-current`; any other value safely selects 30 days. The backend supplies all eligibility, aggregation, and state decisions.

The history request streams inside a React `Suspense` boundary after the primary Product response. Its contained loading state retains current price/freshness and selected range, so history latency does not block primary Product content. The SVG renders only actual daily points. Larger evidence gaps use dashed segments, while the summary explicitly rejects continuous-monitoring inference. A disclosure table provides every date, lowest observed price, and observation count without hover. `UNAVAILABLE` renders explanation and no chart; a technical request failure renders `History temporarily unavailable` while current Product content remains usable. On mobile the Product identity/current evidence remain above history, grid children can shrink, and the responsive SVG does not create horizontal page overflow.

The API is read through `API_BASE_URL`. In production same-site deployment this becomes the `/api` route boundary; in local development it points to `http://localhost:5099`.

The homepage keeps search, category, retailer, price range, supported-reference, freshness, match confidence, availability, sort, and page in the URL. General discovery defaults to recently checked; searches default to relevance unless the shopper explicitly selects another sort. Active filters are removable chips, clear actions reset the public URL state, pagination preserves controls, and narrowed/search result pages use `noindex,follow` to avoid thin SEO surfaces. Product navigation relies on browser history, so Back restores the same discovery state.

Desktop exposes all controls inline. On mobile, the same form becomes a labelled modal filter sheet with focus moved to its heading and restored to the trigger on close. The UI displays only backend-provided evidence/freshness/match/supported-savings facts; it does not calculate ranking, history claims, affiliate economics, or personalization.
