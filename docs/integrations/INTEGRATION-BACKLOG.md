# Canada Deals - Integration Backlog

**Status:** APPROVED GATE - Human Architecture / Data Integration Checkpoint completed
**Date:** 2026-08-11

## P0 - required before a first connector

| ID | Work item | Acceptance evidence | Gate |
|---|---|---|---|
| INT-001 | Approve canonical entity and policy model | checkpoint sign-off and `DATA-MODEL.md` version | architecture/data |
| INT-002 | Create merchant/network approval record format | reviewer, terms URL, fields, retention, expiry | legal/data |
| INT-003 | Confirm Best Buy Canada product feed/API and link rights | written program answer and sample permitted payload | merchant approval |
| INT-004 | Confirm Home Depot Canada feed/API and link rights | written program answer and sample permitted payload | merchant approval |
| INT-005 | Resolve Amazon Associates/Creators API policy gate | account eligibility, Partner Tag, quota, link integrity, comparison/history/image decisions | legal/network |
| INT-006 | Confirm Walmart/Rakuten fallback capabilities | advertiser partnership, catalog fields, deep link rules | network approval |
| INT-007 | Define identifier and matching test set | labelled GTIN/MPN/model fixtures and review states | data quality |
| INT-008 | Define ingestion idempotency/retry/DLQ contract | replay and duplicate scenarios documented | reliability |
| INT-009 | Define freshness tiers per approved source | quota-aware schedule and stale UX mapping | product/data |
| INT-010 | Define alert threshold and delivery semantics | consent, dedupe, unsubscribe, retry behavior | product/security |

## Connector-neutral foundation now authorized

The application foundation may implement canonical contracts, policy evaluation, matching, freshness, safe handoff boundaries, fixtures, and provider test adapters without merchant approval. Production connector work remains blocked until the source evidence below is committed.

`INT-001` is completed by DEC-010. `INT-002` through `INT-006` remain external approval and policy gates; they are not satisfied by synthetic fixtures.

Vertical Slice 9 implements the provider-neutral tracking-link boundary and deterministic Impact/CJ adapters without satisfying `INT-003` or `INT-004`. Best Buy/Impact and Home Depot/CJ remain operationally blocked by publisher approval, credentials, provider IDs, and controlled live evidence. Product feed/API rights remain completely separate and unresolved.

The Rakuten connector implementation completes the technical fixture-backed portion of `INT-006`, including scoped token reuse, read-only discovery, capability persistence, deep links, Product Search dry-run, source mapping, and policy-gated import. `INT-006` is still externally blocked: there is no secure credential set, live discovery evidence, approved merchant partnership, Canada Deals data entitlement, MerchantPolicy approval, or controlled live link/catalog result.

### Rakuten activation checklist

- [ ] Rotate the credential disclosed outside the approved secret store.
- [ ] Securely configure Client ID, Client Secret, and Publisher Account ID.
- [ ] Capture read-only advertiser/partnership evidence and exact MID.
- [ ] Confirm ACTIVE relationship, Canada relevance, Product Feed, and deep-link capability.
- [ ] Document permitted fields, metadata/price/history/image retention, cadence/quota, attribution, and termination behavior.
- [ ] Approve retailer and MerchantPolicy mapping, separately enabling affiliate and catalog use.
- [ ] Review bounded Product Search dry-run and identifier-conflict output.
- [ ] Approve and capture one controlled deep-link and one controlled persistence run.

## P1 - first reliable vertical slice

- Implement one approved feed/API adapter behind the connector contract.
- Normalize products, listings, current permitted price, availability, source timestamp, and policy state.
- Run deterministic matching and quarantine uncertain candidates.
- Generate an approved internal affiliate redirect with adjacent disclosure metadata.
- Publish evidence/freshness states to the public API.
- Add provider health, import counts, error rates, and audit events.
- Add contract tests using provider fixtures with no live credentials in CI.

## P2 - retention and coverage

- Add the second approved merchant connector.
- Add permitted bounded price history and interpretation states.
- Configure and validate the production email provider for the implemented Target Price Alert boundary and the still-gated account-confirmation lifecycle.
- Add manual admin review for match, policy, conflict, source disable, and import retry.
- Add reconciliation reports for source count, stale listings, duplicate matches, and click/link failures.

## Deferred / explicitly out of MVP

- silent scraping or bot-based extraction;
- broad retailer coverage without program/data rights;
- Amazon historical archive without written permission;
- image mirroring without explicit rights;
- commission-aware ranking or sponsored organic placement;
- community price submissions, reputation, cashback, mobile app, browser extension, AI shopping agent, and weekly digest (P2).

## Definition of done for a connector

The connector is not done when it fetches JSON. It is done when: the source is approved; policy fields are recorded; identifiers are mapped; schema and contract tests pass; retries are bounded; idempotency is demonstrated; ambiguous matches quarantine; prices/freshness display honestly; affiliate links are allowlisted and disclosed; observability and audit data exist; and rollback/source disable is tested.
