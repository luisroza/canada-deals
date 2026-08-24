# Canada Deals - Proposed Solution Architecture

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint completed
**Prepared:** 2026-08-11
**Scope:** Solution architecture, cloud, FinOps, application boundaries, delivery and scale path.
**Implementation boundary:** application foundation and connector-neutral fixture development are approved. Production retailer connectors, live affiliate credentials, and production infrastructure remain gated by verified source permissions and later release review.

## Executive recommendation

Adopt a **single-repository modular monolith** with two coordinated deployables:

1. **Next.js + React + TypeScript** for the public, server-rendered web experience, SEO, accessibility, deal discovery, product pages, and the future account UI.
2. **ASP.NET Core REST API** for the domain, identity, saved products, alerts, ingestion orchestration, affiliate redirects, administration, and provider adapters.

Use **PostgreSQL as the system of record**, PostgreSQL full-text search plus `pg_trgm` for the MVP, and **Hangfire with PostgreSQL storage** for durable retries and scheduled work. Host the web service and a separately scalable worker service from the same application image on **DigitalOcean App Platform in Toronto**, with **managed PostgreSQL in Toronto** and optional **DigitalOcean Spaces in Toronto** for permitted project-owned assets or feed staging. Use Cloudflare's free DNS/TLS/CDN baseline where it does not interfere with affiliate redirect semantics.

This is one deployable product boundary, not a microservice estate. The separation between web, API, and worker is operational: it preserves low cost while allowing independent scaling when the traffic or ingestion workload justifies it.

## Architecture principles

- Evidence, freshness, product identity, and source permissions are domain concepts, not presentation details.
- Affiliate commission never changes organic deal quality, eligibility, or ranking.
- Anonymous discovery is first-class; account creation is required only for saved products and alerts.
- A source is not automatically permitted because it is technically reachable. Every connector is gated by merchant/network approval, terms, and a policy record.
- API/feed first. Crawling is not an MVP strategy and is prohibited for sources whose policies forbid extraction.
- Deterministic product matching comes before fuzzy matching; uncertain matches are quarantined for review.
- One relational database is the initial consistency boundary. Add infrastructure only after a measured trigger.
- Canadian hosting is a regional preference and a checkpoint question; it is not a claim that every third-party service, email, CDN edge, or affiliate network processes data only in Canada.
- The checkpoint approved the architecture direction. Implementation still requires the constraints and gates in the integration documents, especially for merchant-specific production connectors.

## Proposed logical architecture

```text
Browser (desktop/mobile)
        |
        v
Cloudflare DNS/TLS/CDN baseline
        |
        +--> Next.js public web (SSR/SSG, SEO, accessible UI)
        |         |
        |         +--> read API / BFF calls
        |         +--> internal redirect CTA to /go/{listingId}
        |
        +--> ASP.NET Core API (modular monolith)
                  |
                  +--> PostgreSQL (domain, identity, jobs, audit)
                  +--> Hangfire PostgreSQL storage
                  +--> Resend transactional email (proposed)
                  +--> approved affiliate/network adapters
                  +--> permitted retailer feeds/APIs
                  +--> observability and audit sinks
                  ^
                  |
        ASP.NET Core worker service (same image, separate App Platform component)
        imports, normalization, matching, freshness, alerts, retries, reconciliation
```

The public UI never trusts a client-provided destination URL. The API resolves an allowlisted `RetailerListing` to an approved affiliate destination and records a privacy-conscious click event before redirecting.

## Approved public routing contract

The MVP should present the browser with one public site boundary:

```text
https://canadadeals.ca/       -> Next.js web
https://canadadeals.ca/api/*  -> ASP.NET Core API
https://canadadeals.ca/go/*   -> ASP.NET Core safe affiliate handoff
```

The exact DNS and provider reverse-proxy configuration is intentionally deferred until deployment experimentation. The application contracts must nevertheless assume same-site browser/API routing so cookies, CSRF protection, CORS, security headers, and frontend calls remain simple. A separate API origin is not required for MVP.

## Application modules

The backend remains one bounded application with explicit modules:

- **Catalog:** product, brand, category, canonical identifiers, approved product content.
- **Retailer Listings:** merchant offers, availability, shipping region, current permitted price state, evidence references.
- **Price Truth:** observations, freshness, history availability, confidence, comparison rules.
- **Deal Evaluation:** deal quality inputs and explanation; no commission input.
- **Discovery/Search:** query parsing, filters, sort, category facets, pagination.
- **Accounts:** ASP.NET Core Identity, consent, saved products, target-price alerts.
- **Ingestion:** connector lifecycle, fetch, normalization, idempotency, retry, quarantine, policy enforcement.
- **Matching:** deterministic identifiers, candidate matches, manual review, merge/split audit.
- **Affiliate Handoff:** approved link generation/revalidation, safe redirect, disclosure metadata, click telemetry.
- **Administration:** owner-only reversible Category/Store lifecycle, editorial offers and banners, source policy, connector health, moderation of match decisions, import retry, and audit trail. Category/store deactivation is evaluated by public queries and handoffs rather than cascading destructive updates.

The Next.js application should mirror these user-facing capabilities but should not duplicate domain rules. The API remains authoritative for product identity, price state, eligibility, alerts, disclosures, and redirect safety.

## Approved technology decisions

| Concern | Proposed choice | Why now | Trigger to revisit |
|---|---|---|---|
| Public frontend | Next.js, React, TypeScript | SSR/SSG, SEO, accessible component ecosystem, strong public-web performance path | measured rendering bottleneck or a product decision for a different client |
| Backend | ASP.NET Core REST API | fits the approved .NET backend role, typed domain code, mature PostgreSQL and identity support | team skill or throughput changes materially |
| Repository | one monorepo with separate web/API/worker projects | coordinated contracts, lower operational overhead, clear ownership | independent release cadence or team boundaries require split repos |
| Application style | modular monolith with vertical slices | simplest consistent transaction boundary for MVP | independent scaling, deployment, or ownership pressure |
| Database | managed PostgreSQL | relational integrity, JSON where needed, search extensions, low initial cost | sustained load, history volume, or read/write isolation trigger |
| Search | PostgreSQL FTS + `pg_trgm` | no new service; enough for the initial catalog and filters | search p95 >300 ms, catalog >1M products, or relevance/facet needs exceed PostgreSQL |
| Jobs | Hangfire backed by PostgreSQL | durable recurring jobs, retries, dashboard, no Redis/Kafka dependency | queue throughput or isolation requires a managed queue |
| Authentication | ASP.NET Core Identity with secure cookie sessions and email confirmation | no external identity bill or regional dependency for MVP | social login demand, abuse load, or security review requires managed identity |
| Email | Resend transactional email | simple API and low-volume free tier; alerts are P1 not a core marketplace | deliverability, residency, or volume requirements change |
| Hosting | DigitalOcean App Platform, Toronto; managed PostgreSQL, Toronto | small-team operations, low starting cost, Canadian region availability | workload or compliance requires Azure controls or multi-region design |
| Edge | Cloudflare Free baseline | DNS, TLS, CDN and DDoS baseline at zero cost | WAF/SLA/advanced edge controls become necessary |
| Assets | built-in first-party SVGs plus bounded PostgreSQL-backed reviewed banner and Product Image libraries; optional Spaces for future scale | durable low-volume owner uploads without ephemeral container writes or new MVP infrastructure; same-origin delivery simplifies CSP and rights revocation | asset volume/traffic justifies object storage/CDN or permitted retailer feeds require it |

Product imagery is an independent Product-owned entity, not a retailer listing URL. The first implementation accepts only owner-reviewed internal bytes with signature/dimension/size validation, SHA-256 identity, placement and validity gates, and audited reversible state. Public reads select only the newest active eligible image and serve it through an opaque same-origin endpoint with ETag and `nosniff`; unknown, pending, expired, blocked, and archived records fail closed. Optional source-listing and merchant-policy references reserve the future connector boundary, but no connector may populate or publish those images until merchant-specific display and caching rights are verified.

## Option scoring

Scores are 1 (poor) to 5 (strong). They are screening judgements, not benchmark results. Weighted dimensions follow the architecture brief: Canada/region 15%, cost 20%, scalability 10%, developer productivity 10%, SEO 10%, accessibility 5%, operability 10%, security 5%, integration ease 10%, ecosystem 2.5%, flexibility 2.5%.

### Frontend

| Option | Canada/region | Cost | Scale | Productivity | SEO | A11y | Ops | Security | Integrations | Ecosystem | Flexibility | Weighted result |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Next.js + React | 4 | 4 | 4 | 5 | 5 | 4 | 4 | 4 | 4 | 5 | 5 | **4.30 / 5** |
| ASP.NET MVC/Razor | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 3 | 3.90 / 5 |
| Blazor | 4 | 4 | 4 | 4 | 3 | 4 | 4 | 4 | 4 | 4 | 4 | 3.80 / 5 |
| SPA-only React | 4 | 4 | 4 | 5 | 2 | 4 | 4 | 4 | 4 | 5 | 5 | 3.85 / 5 |

### Backend

| Option | Canada/region | Cost | Scale | Productivity | SEO boundary | A11y boundary | Ops | Security | Integrations | Ecosystem | Flexibility | Weighted result |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| ASP.NET Core | 4 | 4 | 5 | 5 | 4 | 4 | 4 | 5 | 5 | 5 | 5 | **4.55 / 5** |
| Node.js/NestJS | 4 | 4 | 5 | 5 | 4 | 4 | 4 | 4 | 5 | 5 | 5 | 4.40 / 5 |
| FastAPI | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 4 | 5 | 4.05 / 5 |
| Spring Boot | 4 | 3 | 5 | 3 | 4 | 4 | 3 | 5 | 5 | 5 | 4 | 4.00 / 5 |

### Database

| Option | Canada/region | Cost | Scale | Productivity | SEO boundary | A11y boundary | Ops | Security | Integrations | Ecosystem | Flexibility | Weighted result |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Managed PostgreSQL | 5 | 5 | 5 | 5 | 4 | 4 | 5 | 5 | 5 | 5 | 5 | **4.85 / 5** |
| SQL Server | 4 | 2 | 5 | 4 | 4 | 4 | 4 | 5 | 4 | 5 | 4 | 4.00 / 5 |
| MySQL | 5 | 5 | 4 | 4 | 4 | 4 | 5 | 4 | 4 | 5 | 4 | 4.45 / 5 |

These scores support the recommendation; they do not constitute a Human Checkpoint approval.

## Public performance and reliability targets

Initial targets are budgets for implementation and testing, not guarantees:

- public page TTFB p75 below 500 ms for cached/SSR paths;
- LCP p75 below 2.5 s, INP p75 below 200 ms, CLS below 0.1 for representative mobile traffic;
- search p95 below 300 ms for a catalog up to approximately 100,000 canonical products;
- ingestion jobs are idempotent and safe to retry;
- alert delivery is at-least-once with a deduplication key and a visible delivery state;
- public stale data is disclosed rather than silently presented as current.

## Security and privacy baseline

- HTTPS, secure and HttpOnly cookies, SameSite policy, anti-forgery protection, CSP, rate limits, secret manager/environment secrets, and dependency scanning.
- Public redirect accepts only an internal listing identifier; destination hosts and paths are allowlisted from approved connector records.
- Admin actions require an internal role, are audited, and cannot bypass source policy without an explicit reason and expiry.
- Click telemetry stores opaque IDs and placement/context, not email, full IP, or arbitrary query strings. Retention is minimized and documented.
- Email alerts require consent, unsubscribe handling, and suppression after repeated failures.
- No claim of Canadian-only processing is made for Resend, Cloudflare, or affiliate networks without a separate legal/vendor review.

## Backup, recovery, and observability

Proposed MVP objectives are RPO <=24 hours and RTO 4-8 hours. Use managed PostgreSQL backups with a 7-14 day retention target, verify a restore weekly in a non-production environment, and document the actual region/retention setting before launch. Cross-region disaster recovery is not assumed until it is confirmed with the provider.

Emit structured logs and metrics for fetch duration, source status, rows received, rows accepted/rejected, match confidence, stale offers, alert sends, redirect failures, and policy violations. Add error tracking only after data-scrubbing rules are defined.

## Scale path and explicit triggers

| Stage | Expected shape | Add only when the trigger is observed |
|---|---|---|
| MVP / ~1k MAU | one web component, one worker component, PostgreSQL 1 GiB, PostgreSQL search, no Redis | none; optimize queries and indexes first |
| Growth / ~10k MAU | scale web and worker independently, PostgreSQL 2-4 GiB, CDN/cache for public content | sustained CPU, memory, queue age, or DB saturation |
| Catalog scale / ~100k MAU or >1M products | dedicated search (Meilisearch/Typesense/OpenSearch), read model, history partitioning/archive | search latency/relevance/facet trigger |
| Network scale / multiple high-volume sources | managed queue or event bus, dedicated ingestion workers, stronger provider isolation | ingestion throughput, provider back-pressure, or failure isolation |
| Large traffic / ~1M MAU | multi-region edge, read replicas, partitioned historical storage, stronger DR | measurable availability, traffic, or recovery requirement |

Do not introduce Kubernetes, Kafka, Redis, microservices, or a dedicated search cluster at MVP solely for anticipated scale.

## Risks requiring checkpoint attention

1. DigitalOcean Toronto availability is documented for App Platform and managed PostgreSQL, but account/plan creation availability and exact pricing must be checked immediately before provisioning.
2. Affiliate approvals and merchant feed rights are not yet granted. The product can be designed around source-neutral contracts, but a launch retailer set cannot be called final until contracts and terms are recorded.
3. Amazon product content, price display, caching, historical storage, comparison, and mobile use carry policy constraints. Amazon is therefore a gated candidate, not an automatically approved connector.
4. Third-party email/CDN/affiliate services may process data outside Canada. Privacy/legal review is required before production personal-data flows.

## Evidence and verification notes

- DigitalOcean Toronto region and service availability: [regional availability](https://docs.digitalocean.com/platform/regional-availability/) (VERIFIED 2026-08-11).
- DigitalOcean App Platform pricing: [official pricing details](https://docs.digitalocean.com/products/app-platform/details/pricing/) (VERIFIED 2026-08-11).
- DigitalOcean managed database pricing: [official managed database pricing](https://www.digitalocean.com/pricing/managed-databases) (VERIFIED 2026-08-11).
- DigitalOcean Spaces pricing: [official Spaces pricing](https://docs.digitalocean.com/products/spaces/details/pricing/) (VERIFIED 2026-08-11).
- Azure Canada regions/data geography for the fallback path: [Azure regions overview](https://learn.microsoft.com/en-us/azure/reliability/regions-overview) (VERIFIED 2026-08-11).
- Cloudflare Free baseline: [Free plan](https://www.cloudflare.com/plans/free/) (VERIFIED 2026-08-11).
- Resend plan assumptions: [pricing](https://resend.com/pricing) and [pricing explanation](https://resend.com/docs/knowledge-base/what-is-resend-pricing) (VERIFIED 2026-08-11).

All technology, provider, price, and retention statements are either verified against the linked source on the date above, explicitly labelled as a proposed assumption, or listed as requiring re-verification before provisioning.
