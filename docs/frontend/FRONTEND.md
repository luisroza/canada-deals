# Frontend foundation

The frontend is a Next.js 16 + React 19 + TypeScript application in `apps/web`.

Implemented routes:

- `/` - search-first “Deals with strong evidence” fixture feed.
- `/products/[slug]` - server-rendered Product Page with evidence, freshness, variants, safe comparisons, and related listings for review.

SEO-critical product content is server-rendered. Client JavaScript is limited to browser-native navigation in this slice. The design uses semantic headings, visible text states, responsive stacked offers, focus styles, and no color-only meaning.

The API is read through `API_BASE_URL`. In production same-site deployment this becomes the `/api` route boundary; in local development it points to `http://localhost:5099`.
