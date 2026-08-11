# ADR-007: ASP.NET Core Identity with secure cookie sessions

**Status:** APPROVED - Human Architecture / Data Integration Checkpoint
**Date:** 2026-08-11

## Context

Anonymous discovery is required. Save Product and Target Price Alert require an account, consent, email confirmation, and secure session handling. A separate identity vendor would add cost and a data-processing dependency.

## Options

ASP.NET Core Identity, Auth0/Clerk, a custom token system, or a hosted regional identity service.

## Decision

Use ASP.NET Core Identity with secure cookie sessions, email confirmation, password reset, rate limits, and an internal admin role. Social login is deferred.

The browser/API MVP topology is same-site: the public web is served at `/`, the API is routed under `/api/*` on the same public origin, and safe retailer handoff is routed under `/go/*`. The exact reverse-proxy/DNS implementation is deferred to deployment work; no separate API origin is required for MVP.

## Reasoning

It keeps identity state in the managed Canadian database boundary and avoids a custom authentication protocol or another required vendor for MVP.

## Tradeoffs

The team owns account UX, abuse controls, and operational recovery. Email delivery still relies on a third-party provider and requires privacy review.

## Migration path

Keep user identifiers and authorization behind an application service. Add social or managed identity only after a concrete adoption, security, or support requirement.
