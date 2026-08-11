# ADR-003: Managed PostgreSQL as system of record

**Status:** PROPOSED - awaiting Human Architecture Checkpoint
**Date:** 2026-08-11

## Context

The system needs relational product identity, retailer listings, permitted price observations, alerts, account data, idempotency, audit records, and durable job state.

## Options

Managed PostgreSQL, MySQL, SQL Server, or a document-first database.

## Decision

Use managed PostgreSQL in the selected Canadian region. Use relational tables for core state, JSON only for bounded source metadata, and PostgreSQL FTS plus `pg_trgm` for MVP search.

## Reasoning

PostgreSQL provides integrity, indexing, transactional consistency, extensions for the initial search need, and a low-cost managed option in Toronto.

## Tradeoffs

Large history tables and search relevance may eventually need partitioning, read models, or a dedicated search service. Those are deferred.

## Migration path

Partition/archive price history when volume requires it. Add a search projection or managed search service when the documented latency/relevance triggers are met.
