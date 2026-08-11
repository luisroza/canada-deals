# ADR-002: ASP.NET Core REST API

**Status:** PROPOSED - awaiting Human Architecture Checkpoint
**Date:** 2026-08-11

## Context

The product needs domain rules for price truth, source policy, matching, alerts, affiliate handoff, administration, and reliable ingestion. The backend role is explicitly .NET-oriented and must not redesign approved architecture during implementation.

## Options

ASP.NET Core, Node.js/NestJS, FastAPI, and Spring Boot were screened for typed domain work, integrations, PostgreSQL, security, operations, and team fit.

## Decision

Use ASP.NET Core as a REST API in a modular monolith. Organize code by business module and vertical slice; keep provider DTOs at adapter boundaries.

## Reasoning

It matches the approved implementation role, has mature identity and PostgreSQL support, and provides a strong path for background work and testable domain services.

## Tradeoffs

The frontend and backend use different ecosystems. The API must publish stable contracts and avoid leaking persistence models.

## Migration path

Extract a module only after independent deployment or scaling is measured as necessary. The REST contracts remain the extraction seam.
