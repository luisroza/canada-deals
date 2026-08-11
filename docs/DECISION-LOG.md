# Decision Log

Only record decisions that have actually been approved. Proposed options and research findings belong in the relevant research documents until a checkpoint approves them.

## Current decisions

### DEC-000 - Project repository

- **Status:** Confirmed
- **Decision:** Use the `luisroza/canada-deals` GitHub repository for the Canada Deals project.
- **Scope:** Repository context only; no application technology has been selected.
- **Date:** 2026-08-11

### DEC-001 - Initial development phase

- **Status:** Confirmed by project bootstrap
- **Decision:** Establish the repository foundation and complete Product Owner / Canadian market research before UX, architecture, integrations, or application code.
- **Rationale:** Major product and technical decisions should be evidence-based and reviewed before implementation.
- **Date:** 2026-08-11

### DEC-002 - Product research draft completed

- **Status:** Draft; not approved
- **Decision:** Complete the Product Owner / Canadian market research and produce a proposed product definition, MVP, roadmap, and backlog before moving to UX.
- **Proposed direction:** A Canadian price-truth layer for planned online purchases, initially focused on electronics plus home improvement/tools.
- **Important constraint:** This is a recommendation for the Human Product Checkpoint, not an approved product or technology decision.
- **Evidence:** `docs/product/PRODUCT-RESEARCH.md`, `docs/product/PRODUCT.md`, `docs/product/MVP.md`, `docs/product/ROADMAP.md`, and `docs/product/PRODUCT-BACKLOG.md`.
- **Date:** 2026-08-11

### DEC-003 - Separate repository governance from specialized role instructions

- **Status:** Confirmed by governance refactor
- **Decision:** Keep concise repository-wide behavior rules in the root `AGENTS.md`; keep the detailed Product Owner / Market Research specification in `agents/product-owner.md`; use specialized role files or clearly marked placeholders for other agents.
- **Rationale:** Every agent needs the global workflow and safety rules, while role-specific research and implementation requirements should remain scoped to the responsible agent.
- **Constraints:** Preserve human checkpoints, avoid technology selection, and do not begin product research or application implementation as part of this refactor.
- **Date:** 2026-08-11

### DEC-004 - Product discovery recommendations

- **Status:** PROPOSED; awaiting Human Product Checkpoint
- **Recommendation:** Validate a Canadian price-truth workflow for planned online purchases, initially focused on electronics plus home improvement/tools.
- **Proposed audience:** Canadian shoppers planning meaningful online purchases, with expert deal hunters as a secondary tester group.
- **Proposed MVP retailers:** Amazon.ca, Best Buy Canada, and Home Depot Canada; Walmart Canada is the fallback candidate. All require Data/Affiliate validation.
- **Proposed monetization:** approved affiliate links with transparent disclosure; affiliate commission must not silently influence organic Deal Quality or ranking.
- **Proposed retention:** saved products and target-price email alerts, followed by a low-noise digest if validated.
- **Important constraint:** These are product recommendations, not approved decisions. No frontend, backend, database, cloud, hosting, search, or integration technology has been selected.
- **Evidence:** `docs/product/PRODUCT-RESEARCH.md`, `docs/product/PRODUCT.md`, `docs/product/MVP.md`, `docs/product/ROADMAP.md`, and `docs/product/PRODUCT-BACKLOG.md`.
- **Date:** 2026-08-11
