# Architecture and Data/Affiliate Reconciliation

**Status:** PROPOSED - awaiting Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11
**Tracks reconciled:** Solution/Cloud/FinOps and Data/Affiliate Integration.

## Joint recommendation

Use one source-neutral modular monolith in the existing repository:

- Next.js + React + TypeScript public web;
- ASP.NET Core REST API with explicit catalog, price truth, search, accounts, alerts, ingestion, matching, affiliate, and admin modules;
- separate worker runtime from the same image using Hangfire + PostgreSQL storage;
- managed PostgreSQL as the transactional and policy system of record;
- PostgreSQL search for MVP;
- DigitalOcean App Platform and managed PostgreSQL in Toronto as the initial hosting proposal;
- approved network/merchant feeds/APIs only, with a field-level policy engine;
- internal allowlisted `/go/{listingId}` redirects;
- no implementation until the human checkpoint approves the proposed choices.

## Conflicts resolved between tracks

| Tension | Resolution | Why |
|---|---|---|
| Low-cost MVP vs reliable ingestion | Use one database plus a separate worker component; no Redis/Kafka initially | persistent jobs and independent worker scaling without a second infrastructure class |
| SEO vs API purity | Next.js owns public rendering; ASP.NET Core owns domain truth | supports UX/SEO without duplicating price/policy rules |
| Broad retailer ambition vs rights uncertainty | launch target is two approved retailers; Amazon gated; Walmart fallback | source rights and policy are launch gates, not follow-up tasks |
| Price history UX vs source licences | history is policy-controlled with Reliable/Partial/Unavailable states | preserves trust without assuming universal archive rights |
| Image-rich deal cards vs caching risk | no retailer image caching by default; use permitted URLs/owned assets only | avoids an unverified content license assumption |
| Fast refresh vs affiliate quotas | adaptive freshness tiers and source-specific schedules | user-visible freshness remains honest and quota-safe |
| Affiliate conversion vs ranking trust | affiliate data is separate from Deal Quality and ranking | prevents commercial incentives from corrupting price-truth claims |
| Future scale vs MVP simplicity | explicit measured triggers for search, queue, replicas, and microservices | avoids paying for anticipated scale while preserving seams |
| Canadian region preference vs global vendors | host core DB/app in Toronto; label third-party processing unknown | does not make an unsupported Canadian-only residency claim |

## Shared data contract

The architecture track depends on the integration track's canonical model: `Product`, `RetailerListing`, `PriceObservation`, `Deal`, `PriceAlert`, `AffiliateLink`, `ImportJob`, `MerchantPolicy`, `MatchDecision`, and `AuditRecord`. The frontend must receive evidence/freshness/history state, not just a price number. The redirect service must receive a listing ID, not an arbitrary URL.

## Shared operational contract

- All source fetches are observable and replayable where terms permit.
- All writes are idempotent and safe to retry.
- All policy-denied content is blocked before public projection.
- All uncertain product matches are quarantined.
- All stale/unknown states remain visible to the UX.
- All email and click flows minimize personal data.
- All source credentials are external secrets.

## Cost reconciliation

The recommended baseline is approximately **$32-$62 USD/month without optional Spaces** or **$37-$67 USD/month with Spaces**, before tax, domain, overage, and legal/affiliate costs, using the documented 1 USD = 1.38 CAD planning assumption. The key cost-control decision is not to provision a dedicated search engine, Redis, Kafka, Kubernetes, or an HA database before a measurable trigger.

## Unresolved blockers

1. **Merchant/network approval:** no connector can enter implementation without a current relationship and permitted data/link rights.
2. **Amazon policy interpretation:** price/availability display, source timestamps, comparison rules, caching, historical data, images, and mobile use need explicit review.
3. **Canadian processing boundary:** DigitalOcean core hosting can be in Toronto, but Resend, Cloudflare, and affiliate networks are external dependencies whose processing locations and contractual terms need privacy review.
4. **Exact quotas and cadence:** refresh tiers remain provisional until each approved source supplies quota, update markers, and terms.
5. **History promise:** UX copy and product claims must be tied to the policy state; no global “price history” promise is approved.
6. **Email deliverability and consent:** provider selection is proposed; domain authentication, unsubscribe, suppression, and alert volume need a later implementation/security decision.

## Human checkpoint questions

- Approve the one-repository modular monolith and the proposed Next.js/ASP.NET Core/PostgreSQL stack?
- Approve DigitalOcean Toronto as the MVP hosting direction, subject to account-level verification and privacy review?
- Approve the source-neutral policy engine and canonical model?
- Approve Best Buy Canada and Home Depot Canada as conditional first integration targets, with Amazon gated and Walmart fallback?
- Approve PostgreSQL search and Hangfire/PostgreSQL jobs for MVP?
- Decide whether any source may retain price history, image data, or raw feed snapshots, and for what retention period?
- Confirm the owner and date for affiliate/legal/network outreach before implementation starts?

## After approval

Only after a positive checkpoint should the project create the application foundation, establish solution folders, add local development containers/configuration, implement the first connector-neutral domain contracts, and execute a vertical slice. The next implementation slice should be selected from the approved architecture and the first approved source, not invented by the backend/frontend agents.
