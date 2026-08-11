# ADR-009: Containerized delivery with gated CI/CD direction

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11

## Context

The repository needs repeatable local development and a safe path from pull request to staging and production. This phase must not create a production pipeline or infrastructure.

## Options

Provider-native source deployment, containerized builds with GitHub Actions, or a larger platform delivery system.

## Decision

Use containerized web/API/worker builds from the monorepo. The proposed flow is pull request checks -> unit/integration/contract/frontend/accessibility/security checks -> staging -> human approval -> production. CI/CD implementation remains deferred.

## Reasoning

Container boundaries keep local, CI, and App Platform runtime assumptions aligned and leave an Azure migration path. Gated promotion protects the human checkpoints and affiliate/data policy work.

## Tradeoffs

The team must maintain Dockerfiles, dependency scanning, secrets, migrations, and release rollback discipline. None should be added until the checkpoint approves the foundation phase.

## Migration path

Start with one pipeline and environment-specific configuration. Add blue/green, canary, or multi-region promotion only when availability and change volume justify it.
