# ADR-008: Modular monolith with explicit web, API, and worker boundaries

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11

## Context

The product has a small initial team and several related concerns: public discovery, domain truth, ingestion, matching, alerts, affiliate handoff, and administration. Premature microservices would add operational cost before product/volume validation.

## Options

Microservices, a single undifferentiated application, or a modular monolith with separate runtime components.

## Decision

Use a modular monolith in one repository and one data model, with Next.js web, ASP.NET Core API, and a separately scalable ASP.NET worker component. Modules have explicit boundaries and internal contracts.

## Reasoning

This preserves consistency and speed while keeping the future extraction seams visible. Web and ingestion workloads can be scaled independently without introducing distributed transactions.

## Tradeoffs

The database is still a shared dependency and module boundaries can erode if code review is weak.

## Migration path

Extract only a module that has a measured independent scaling, ownership, or reliability need. Start with the ingestion worker because it already has an operational boundary.
