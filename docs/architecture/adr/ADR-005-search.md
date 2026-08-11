# ADR-005: PostgreSQL search for MVP

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11

## Context

The UX requires search, filters, freshness, retailer comparison, and safe same-product matching. The initial catalog is expected to be modest and cost-sensitive.

## Options

PostgreSQL FTS + `pg_trgm`, Meilisearch, Typesense, OpenSearch, or a hosted search provider.

## Decision

Start with PostgreSQL FTS, `pg_trgm`, carefully designed indexes, and a query/read model. Do not provision a dedicated search service for MVP.

## Reasoning

It keeps transactional truth and search close, avoids another bill and failure mode, and is sufficient for the initial catalog when measured and indexed.

## Tradeoffs

Relevance tuning, typo tolerance, and high-cardinality facets will eventually be less capable than a dedicated engine.

## Migration path

Introduce a search projection and Meilisearch/Typesense/OpenSearch only after search p95 exceeds 300 ms, the catalog passes approximately 1M products, or relevance/facet requirements cannot be met with PostgreSQL.
