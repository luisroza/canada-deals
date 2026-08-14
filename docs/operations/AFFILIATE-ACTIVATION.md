# Affiliate provider activation runbook

**Technical status:** provider boundary implemented and deterministically validated on 2026-08-12.  
**Commercial status:** no Canada Deals publisher account, merchant acceptance, live credentials, provider identifiers, or controlled live tracking-link evidence was available. No merchant is live.

Affiliate approval and catalog/price-data rights are independent. Completing this runbook activates tracking links only; it does not authorize a Product feed, price ingestion, history, image use, retention, or scraping.

## Shared safety sequence

1. Record redacted evidence of the Canada Deals publisher account, approved production media property, merchant relationship, current terms/search restrictions, deep-link permission/domains, reviewer, and verification timestamp.
2. Create or update the database `AffiliateProgram` for the exact `Retailer`. Use `ACTIVE` only when every required fact is verified; otherwise use `PENDING_APPROVAL` or `CONFIGURATION_INCOMPLETE`.
3. Confirm each target `RetailerListing.ApprovedAffiliateDestinationReference` is an HTTPS URL on a provider-approved merchant domain. This value must come from persisted operator/connector data, never a `/go` query string.
4. Put provider secrets only in the worker component secret store. Keep provider adapters disabled in the API; enable `AffiliateHandoff__Enabled=true` there only after a persisted controlled link passes review.
5. Enable the worker provider and `Worker__EnqueueAffiliateLinkRefreshJob=true` for one controlled refresh. Inspect structured status only; do not print credentials or full tracking URLs.
6. Verify the persisted `AffiliateLink`, then request `/go/{listingId}` with redirects disabled. Confirm the response location uses the approved provider tracking host. Follow it in a controlled browser and confirm the intended retailer destination; do not purchase or create artificial conversions.
7. Return one-shot enqueue to `false`. Keep a low-frequency operator/Hangfire schedule only after current provider limits and link revalidation needs are confirmed.

## Best Buy Canada

- Provider: **Impact**.
- Operational status: **IMPLEMENTED — AWAITING PUBLISHER APPROVAL**.
- Current official evidence: Best Buy Canada routes applications to Impact, prohibits Best Buy brand/trademark bidding, pays eligible mobile-web purchases, and currently excludes Best Buy app purchases from commission.
- Required provider evidence: Canada Deals Impact AccountSID and AuthToken, accepted/ACTIVE Best Buy contract, actual ProgramId, approved website MediaPartnerPropertyId, `AllowsDeeplinking=true`, current `DeeplinkDomains`, approved Impact tracking domain(s), and one controlled Tracking Link API result.
- Worker secrets/settings: `Affiliate__Impact__Enabled=true`, `Affiliate__Impact__AccountSid`, `Affiliate__Impact__AuthToken`. Never place these in the web component, frontend, database, docs, shell history, or Git.
- Database identifiers: actual ProgramId and MediaPartnerPropertyId; no invented IDs. Use the approved Best Buy domains returned by Impact plus the exact expected Impact tracking domain.
- Adapter behavior: Basic authentication; relationship lookup; `ContractStatus=Active`; provider deeplink-domain validation; `POST .../Programs/{ProgramId}/TrackingLinks` with `Type=Regular`, exact persisted `DeepLink`, media property, `subId1=product-page`, and opaque non-PII listing classification. The returned `TrackingURL` is validated and persisted.
- Rate handling: Impact currently documents 1,000 calls/hour for its “other” endpoint group and returns `429` with `Retry-After`; the adapter classifies it and the refresh boundary defers without breaking discovery or existing valid links.
- Disable: set `AffiliateHandoff__Enabled=false` for an immediate public stop; set Impact Enabled and refresh enqueue false; mark the program `DISABLED` or `SUSPENDED`; retain click/link audit rows.

Smoke test:

```text
approved Best Buy listing -> controlled refresh -> ACTIVE persisted AffiliateLink
-> GET /go/{listingId} (no auto redirect) -> approved Impact host
-> controlled browser follow -> expected bestbuy.ca destination
```

## Home Depot Canada

- Provider: **CJ**.
- Operational status: **IMPLEMENTED — AWAITING PUBLISHER APPROVAL**.
- Current official evidence: Home Depot Canada directs affiliates to Commission Junction. CJ documents PAT Bearer authentication, Website ID/PID, joined advertiser relationship, per-link `allow-deep-linking`, provider-returned `clickUrl`, and 25 Link Search calls/minute.
- Required provider evidence: Canada Deals CJ publisher account, PAT, approved Website/PID, joined Home Depot Canada advertiser relationship, actual advertiser CID, approved active Link ID, product destination behavior, deep-link permission, and approved CJ tracking host.
- Worker secrets/settings: `Affiliate__Cj__Enabled=true`, `Affiliate__Cj__PersonalAccessToken`. The PID/CID/Link ID are operational identifiers, not secrets, but still belong in the program record/configuration—not React.
- Adapter behavior: `GET /v2/link-search` with PAT, PID, advertiser CID, specific Link ID, Canada target, and deep-link requirement; parses official XML; requires `relationship-status=joined`, `allow-deep-linking=true`, exact returned destination match, and an allowlisted provider-returned `clickUrl`. It never manufactures CJ URL parameters.
- Catalog boundary: Link Search is not used as Product ingestion. CJ Product Feed/API capability remains deferred to a separate rights-validated slice.
- Disable: use the shared public stop, disable CJ/refresh, and mark the program disabled or suspended without deleting links/clicks.

## Amazon.ca

Status: **GATED**.

No Amazon adapter exists. Activation requires Amazon Associates Canada, a valid Canada marketplace Partner Tag, eligible/approved Creators API access, and a separate policy review. Creators API vends attributed links; current official guidance says not to alter their parameters. Product content, caching, comparison, image, history, and retention rules remain a separate high-risk data gate.

## Walmart Canada

Status: **GATED / UNVERIFIED FOR CANADA DEALS**.

The current official Walmart.ca affiliate page points applicants to Rakuten, but Canada Deals has no approved relationship and no recorded deep-link/data rights. Walmart.com terms are not accepted as evidence for Walmart.ca. No adapter or provider mapping was added.

## Official sources verified 2026-08-12

- [Best Buy Canada Affiliate Program](https://www.bestbuy.ca/en-ca/about/affiliate-program/blt82df225e80ec75e9)
- [Impact program object](https://integrations.impact.com/impact-publisher/reference/the-campaigns-object), [Tracking Link creation](https://integrations.impact.com/impact-publisher/reference/create-a-tracking-link), [media properties](https://integrations.impact.com/impact-publisher/reference/media-properties-overview), and [rate limits](https://integrations.impact.com/impact-publisher/reference/rate-limits)
- [Home Depot Canada Affiliate Program](https://amp.homedepot.ca/en/home/the-home-depot-canada-affiliate-program.html)
- [CJ authentication](https://developers.cj.com/authentication/overview), [Link Search](https://developers.cj.com/docs/rest-apis/link-search), and [Product Feed capability](https://developers.cj.com/docs/data-imports/product-feeds)
- [Amazon Creators API migration](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/migrating-to-creatorsapi-from-paapi), [Canada marketplace/Partner Tag](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/concepts/common-request-headers-and-parameters), [eligibility](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/troubleshooting/error-codes-and-messages), and [link/rate guidance](https://affiliate-program.amazon.com/creatorsapi/docs/en-us/concepts/api-rates)
- [Walmart Canada Affiliate Program](https://www.walmart.ca/en/cp/affiliate-program/6000208941216)
