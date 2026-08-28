# ADR-011: Controlled Amazon short-link resolution

**Status:** APPROVED - explicit owner decision, 2026-08-27  
**Checkpoint:** this refines owner link intake only; Product-data, price, image, API, and merchant-rights gates remain unchanged.

## Context

Owner-created Amazon Canada affiliate links are commonly supplied as `amzn.to` URLs. Preserving the exact short link is required for the direct public handoff, but inspecting only its syntax leaves the Product identity blank even when the redirect itself contains the Amazon.ca ASIN, Partner Tag, and descriptive Product path.

## Options

1. Keep all short-link destination fields manual.
2. Download and scrape the destination Product page.
3. Resolve only redirect headers through a tightly bounded allowlist, stop before downloading the Product page, and derive only URL-contained identity.

## Decision

Use option 3 for the owner-only validation action.

- Accept only an absolute HTTPS `amzn.to` source URL on port 443 without credentials or a fragment.
- Issue bounded `HEAD` requests with automatic redirects and cookies disabled.
- Allow at most three `amzn.to` redirects and accept only a final HTTPS `amazon.ca` host.
- Stop at the final redirect header. Never request or parse the Amazon Product page body.
- Preserve the exact pasted short link as the public direct-provider handoff.
- Use the validated destination only to fill canonical Product URL, ASIN, visible Partner Tag, external identifier, and a reviewable title suggestion from the descriptive URL path.
- Reject links that resolve to another country, an insecure URL, an unexpected host, or an unusable redirect.
- Continue to require manual or separately licensed sources for price, brand/category judgment, model number, and Product imagery.

## Reasoning

This removes repetitive transcription and makes an invalid country-specific link visible immediately without broadening Product-data rights. The redirect header is enough to validate destination identity; downloading the destination page would add scraping, content-rights, reliability, and security risk without an approved connector.

## Tradeoffs

- Validation now depends on a short bounded network request to `amzn.to` and can time out.
- A descriptive title is a suggestion and still requires owner review.
- Price and image remain unfilled for a new Product until an approved Product-data source or reviewed upload is available.

## Migration path

No database migration is required. ADR-011 supersedes only ADR-010's prohibition on following an `amzn.to` redirect during owner validation. ADR-010's exact-link preservation, direct handoff, relationship evidence, and Product-data-rights boundaries remain active.
