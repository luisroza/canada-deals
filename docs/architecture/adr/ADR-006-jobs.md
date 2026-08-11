# ADR-006: Hangfire with PostgreSQL storage for background jobs

**Status:** PROPOSED - awaiting Human Architecture Checkpoint
**Date:** 2026-08-11

## Context

Imports, normalization, matching, freshness updates, alert evaluation, email delivery, retries, and reconciliation need durable scheduling and idempotent execution without adding Redis or Kafka to MVP.

## Options

ASP.NET `BackgroundService` only, Hangfire with PostgreSQL storage, App Platform jobs only, or a managed queue/event bus.

## Decision

Use Hangfire backed by the same managed PostgreSQL. Run the web and worker as separate App Platform components from the same image, with worker concurrency deliberately limited at MVP.

## Reasoning

It gives persistence, recurring schedules, retries, and operational visibility while retaining one data boundary and no broker bill.

## Tradeoffs

Job state shares the database and can contend with product traffic. It is not a substitute for a high-throughput event platform.

## Migration path

Move ingestion to a managed queue and separate workers when queue age, provider back-pressure, database contention, or failure isolation crosses an agreed threshold.
