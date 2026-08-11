# ADR-004: DigitalOcean App Platform in Toronto

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11

## Context

The MVP needs low operational overhead, a Canadian hosting region, separate web/worker scaling, managed database support, and a reversible path to Azure if enterprise requirements emerge.

## Options

DigitalOcean App Platform in Toronto, Azure Canada Central, a Toronto virtual machine, or a larger container platform.

## Decision

Use DigitalOcean App Platform in Toronto for the web and worker components and managed PostgreSQL in Toronto. Use Azure Canada Central as the documented growth/fallback path, not as an immediate second platform.

## Reasoning

DigitalOcean's regional availability documentation lists Toronto for App Platform and managed database services. The model is small-team friendly and the initial cost is transparent.

## Tradeoffs

Provider feature depth, enterprise controls, and multi-region recovery are more limited than a full Azure estate. Account-level availability and exact prices must be rechecked before provisioning.

## Migration path

Keep the application containerized, configuration externalized, and storage contracts provider-neutral. Move to Azure Container Apps/App Service and Azure PostgreSQL only when controls, support, workload, or data requirements justify it.
