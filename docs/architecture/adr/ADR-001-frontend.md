# ADR-001: Next.js + React + TypeScript for the public frontend

**Status:** PROPOSED - awaiting Human Architecture Checkpoint
**Date:** 2026-08-11

## Context

The approved UX requires a search-first, evidence-led Canadian deals experience that works on desktop and mobile, is accessible, fast, and discoverable by search engines.

## Options

Next.js/React, ASP.NET MVC/Razor, Blazor, or SPA-only React were screened against SEO, accessibility, performance, developer fit, cost, and future client reuse.

## Decision

Use Next.js + React + TypeScript with server rendering/static generation for public pages and a progressively enhanced client UI for filters, saves, alerts, and comparison.

## Reasoning

It gives the product a strong SEO and performance path while keeping a clean API boundary for future mobile clients. It also supports accessible, component-based UX without making the public catalog dependent on a client-only boot.

## Tradeoffs

The team owns two language/runtime surfaces and must prevent duplicated business rules. Server/client boundaries and caching semantics require discipline.

## Migration path

Keep contracts in the ASP.NET Core API. A future mobile app consumes the same API; a future search read model does not require replacing the web shell.
