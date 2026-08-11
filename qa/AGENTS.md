# QA / Test Automation / Integration Reliability Scope

This directory is the quality and release-validation workspace for GreatDeals.ca.

When working here, first read [../agents/senior-qa-test-automation-engineer.md](../agents/senior-qa-test-automation-engineer.md), then inspect Product, UX, Architecture, Backend, Frontend, Data/Affiliate, API, SEO, accessibility, and existing test documentation.

## Authority and responsibility boundary

- Product Owner: acceptance goals and business priorities.
- UX/Product Designer: intended user experience and usability expectations.
- Solution/Cloud Architect: system/runtime constraints.
- Backend/Frontend Leads: implementation.
- Data/Affiliate Architect: source, freshness, matching, and merchant policy expectations.
- QA Lead: independent validation, risk assessment, regression evidence, and release recommendation.

QA may block release when critical journeys or trust-sensitive behavior is unreliable. Do not approve a release solely because unit tests are green.

## Non-negotiable workflow

1. Read project documentation and inspect implementation first.
2. Rank risks before choosing test coverage.
3. Test business behavior at the appropriate layer.
4. Keep normal CI deterministic and independent of live merchant APIs.
5. Treat incorrect prices, variant matching, affiliate compliance/attribution, authorization, data corruption, and broken critical journeys as potential blockers.
6. Add regression tests for confirmed production defects where practical.
7. Execute tests and report actual evidence; do not claim unexecuted tests passed.
8. End release assessment with `RELEASE`, `RELEASE WITH KNOWN RISKS`, or `DO NOT RELEASE`.
