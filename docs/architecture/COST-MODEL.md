# Canada Deals - Proposed Cost Model

**Status:** APPROVED COST DIRECTION - Human Architecture / Data Integration Checkpoint completed
**Currency:** USD source prices, converted for planning at an explicit assumption of **1 USD = 1.38 CAD**. Verify exchange rate and provider quotes before provisioning.
**Date checked:** 2026-08-11

## Recommended MVP footprint

| Component | Proposed footprint | USD planning amount | CAD planning amount |
|---|---|---:|---:|
| App Platform web | shared 1 GiB service | $12/mo | ~$17/mo |
| App Platform worker | shared 512 MiB service; same image | $5/mo | ~$7/mo |
| Managed PostgreSQL | basic 1 GiB / 1 vCPU | $15.15/mo | ~$21/mo |
| Spaces | optional, only for permitted owned assets/feed staging | $5/mo | ~$7/mo |
| Cloudflare | Free baseline | $0 | $0 |
| Resend | free tier while volume and daily limit fit; otherwise Pro | $0-$20/mo | ~$0-$28/mo |
| Monitoring/error tracking | start with provider logs/metrics; add only after scrubbing | $0-$10/mo | ~$0-$14/mo |
| **Estimated total with Spaces** | excludes domain, taxes, bandwidth overages, support, and one-time setup | **$37.15-$67.15/mo** | **~$51-$93/mo** |
| **Estimated total without Spaces** | preferred if no permitted project-owned asset requirement exists | **$32.15-$62.15/mo** | **~$44-$86/mo** |

The lower-cost option is the recommended starting budget range. The worker can be collapsed into the web process only as an explicit cost tradeoff; doing so increases noisy-neighbor and job-isolation risk and is not the baseline recommendation.

## Source price notes

- App Platform currently lists a shared 512 MiB plan at $5/month and a shared 1 GiB fixed plan at $10/month; the $12/month 1 GiB shared option is used here as the conservative web-service planning line. Jobs are billed only while they run; a continuously running worker is modelled as a service.
- Managed PostgreSQL basic 1 GiB / 1 vCPU is listed at $15.15/month. A 2 GiB tier is approximately $30.45/month if the MVP workload requires it.
- Spaces lists a $5/month base with included storage/bandwidth allowances; it is optional because retailer image caching may be restricted by source terms.
- Resend's published transactional free tier is 3,000 emails/month with a 100/day limit; the published Pro tier is $20/month for 50,000 emails/month. Recheck before launch.
- Cloudflare's Free plan is $0/month; paid features are deliberately excluded from the MVP baseline.

## Scenarios

### A - Minimum-cost single host (not recommended for production)

One Toronto virtual machine with application, worker, and database containers could minimize cash cost, but it creates a single failure domain, increases backup/patching responsibility, and weakens the recovery story. Use only for a disposable development environment, not for a public launch.

### B - Recommended MVP

DigitalOcean App Platform web + worker in Toronto, managed PostgreSQL in Toronto, optional Spaces, Cloudflare Free, and low-volume transactional email. This is the best balance of Canadian-region preference, small-team operations, predictable cost, and a path to separate scaling.

### C - Azure-aligned growth path

Azure Container Apps or App Service in Canada Central, managed PostgreSQL in Canada Central, Blob Storage, Azure Monitor, and an email provider. Azure is a credible path where enterprise controls, identity, support, or network policy justify the additional complexity. Exact monthly cost is workload-dependent and must be produced from the Azure calculator at the checkpoint; do not treat an unquoted range as a commitment.

### D - Scale-ready architecture

Keep the application contracts compatible with managed search, queue, read replicas, partitioned history, and multi-region edge services, but do not provision these systems before the triggers in `ARCHITECTURE.md` are met.

## FinOps guardrails

- Set provider budget alerts at $50, $75, and $100 USD/month and review actual cost weekly during MVP.
- Label services by `environment`, `owner`, `cost-centre`, and `data-classification` when the provider supports tags.
- Keep non-production databases and workers stopped or destroyed when not in use; use App Platform jobs for bounded batch experiments where practical.
- Put feed staging and backups on lifecycle policies; do not retain raw provider payloads by default.
- Avoid paid search, Redis, Kafka, Kubernetes, HA database standbys, and cross-region replicas until a measured requirement exists.
- Track cost per successful normalized listing, cost per active alert, and cost per million public page views. These are more useful than infrastructure cost alone.
- Recheck provider billing, tax, egress, storage, and exchange-rate assumptions immediately before provisioning; prices can change.

## Cost unknowns and exclusions

The estimate excludes domain registration, taxes, support plans, provider overages, affiliate network fees, legal review, paid analytics, user acquisition, transactional SMS, and any retailer-specific feed/license charge. Merchant/API approval may change the data acquisition cost and is a go/no-go input, not an implementation detail.

## Evidence

- [DigitalOcean App Platform pricing](https://docs.digitalocean.com/products/app-platform/details/pricing/) - VERIFIED 2026-08-11.
- [DigitalOcean managed databases pricing](https://www.digitalocean.com/pricing/managed-databases) - VERIFIED 2026-08-11.
- [DigitalOcean Spaces pricing](https://docs.digitalocean.com/products/spaces/details/pricing/) - VERIFIED 2026-08-11.
- [DigitalOcean regional availability](https://docs.digitalocean.com/platform/regional-availability/) - VERIFIED 2026-08-11.
- [Cloudflare Free plan](https://www.cloudflare.com/plans/free/) - VERIFIED 2026-08-11.
- [Resend pricing](https://resend.com/pricing) - VERIFIED 2026-08-11.
