# Production email delivery boundary

Vertical Slice 7 implements transactional account-confirmation and Target Price Alert email behind the provider-neutral `ITransactionalEmailSender` boundary. Resend is the first production adapter. Development and Test use a deterministic PostgreSQL capture adapter and make no network calls.

## Configuration

Configuration is read from `Email` and production fails during startup unless every required value is present and valid.

| Key | Production rule |
| --- | --- |
| `Email:Enabled` | must be `true` |
| `Email:Provider` | must be `Resend` |
| `Email:ApiKey` | required secret; deployment secret store only |
| `Email:FromAddress` | required address on the verified sending domain |
| `Email:FromDisplayName` | defaults to `Canada Deals` |
| `Email:PublicOrigin` | canonical HTTPS origin only; links never derive from request Host or forwarded-host headers |
| `Email:WebhookSigningSecret` | required `whsec_...` secret; deployment secret store only |
| `Email:ConfirmationTokenHours` | explicit Identity email-token lifetime; defaults to 24, bounded to 1–72 |
| `Email:MaxDeliveryAttempts` | bounded attempts; defaults to 4, bounded to 1–10 |
| `Email:EmergencyStop` | when `true`, Resend delivery is suppressed before any provider network call; use for deployment and incident containment |

`Email:AutoConfirmDevelopmentAccounts=true` preserves fast ordinary local development. Set it to `false` to exercise the captured-email confirmation journey. This option has no production bypass because production startup requires the live provider configuration.

## Account confirmation

Registration persists an unconfirmed Identity user and a durable `AccountConfirmationDelivery`. The supported Identity token is UTF-8/base64url encoded for the query string. The message contains `/account/confirm-email?userId=...&code=...` on the configured public origin. Confirmation is a CSRF-protected POST and has explicit `CONFIRMED`, `ALREADY_CONFIRMED`, and `INVALID_OR_EXPIRED` outcomes.

`POST /api/v1/account/resend-confirmation` always returns the same `202` response for missing, confirmed, and unconfirmed addresses. It shares the authentication rate-limit policy. Each valid resend creates a new durable delivery/token; it never discloses whether an account exists.

## Provider acceptance, delivery, and retries

The Resend adapter calls `POST /emails` directly so the boundary owns the provider message ID, response classification, and `Idempotency-Key`. Each key is derived from the immutable persisted delivery ID (`account-confirmation/{id}` or `price-alert/{id}`), never an in-memory attempt.

For alert delivery, an attempt is committed before the network call. If the process stops after provider acceptance but before the result commit, the next run reuses the same key. A transient result schedules a delayed Hangfire retry; retries are bounded and use persisted `NextAttemptAt`, `429` honors `Retry-After`, and other transient failures use bounded exponential delay. A still-ambiguous attempt is suppressed before Resend's 24-hour idempotency window can expire, avoiding a blind duplicate. Permanent 4xx failures do not retry. Account-confirmation failures remain durable and are retried through the generic, rate-limited resend action rather than an invisible account-email loop.

`ProviderAccepted` means Resend returned a provider message ID. It is not presented as delivery. Only a verified `email.delivered` webhook sets `Delivered`.

## Webhook and suppression behavior

`POST /api/v1/webhooks/email/resend` reads the exact raw request body and verifies `svix-id`, `svix-timestamp`, and `svix-signature` using HMAC-SHA256, constant-time comparison, and a five-minute timestamp tolerance. The secret is never logged. `(Provider, EventId)` is unique, and a PostgreSQL advisory lock makes duplicate concurrent delivery replay-safe.

Processed lifecycle events are `email.sent`, `email.delivered`, `email.failed`, `email.bounced`, `email.complained`, and `email.suppressed`. Open/click tracking events are deliberately ignored. Provider event timestamps prevent older out-of-order events from regressing state. A later bounce, complaint, or suppression creates/updates a normalized application suppression record; subsequent account-confirmation and alert sends to that address are suppressed.

An event that arrives before the provider-acceptance database commit is retained by provider message ID. When the acceptance result is committed, the latest retained event is reconciled immediately, closing the webhook-before-commit race.

## Templates and privacy

Both account-confirmation and alert messages have HTML and plain-text bodies. They contain no marketing copy, pixel, click tracker, remote image, Weekly Digest consent, password-recovery flow, or MFA flow. Alert copy reports the qualifying observed CAD price and target, warns that price and fulfillment details can change, links to Product evidence, and links to `/saved` to manage or disable the alert.

## Operational activation checklist

1. Verify a dedicated sending subdomain and configure the provider-required SPF and DKIM records.
2. Create a least-privilege production API key and store it with the webhook signing secret in the deployment secret store.
3. Set a sender on the exact verified domain and configure the canonical production HTTPS origin.
4. Configure the Resend webhook endpoint and subscribe only to the lifecycle events listed above.
5. Send provider test-address checks for delivered, bounced, complained, and suppressed outcomes, then verify webhook state and application suppression.
6. Run one controlled real-mail confirmation and one controlled alert delivery; verify provider acceptance, webhook delivery, content, links, and logs.
7. Record DNS/provider/account evidence without committing secrets.

Official behavior was reverified on 2026-08-12 against the Resend .NET/send, idempotency, rate-limit, webhook verification/event, test-address, and domain documentation, plus the ASP.NET Core Identity account-confirmation documentation.

## Current provider status

`PRODUCTION EMAIL PROVIDER IMPLEMENTED — OPERATIONAL VALIDATION BLOCKED`.

No Resend API key, verified sender domain/address, webhook secret, or production public origin was available in this workspace. Deterministic implementation and validation are complete; DNS/provider acceptance and real-mail smoke tests remain an external release gate.

Slice 8 adds the executable operational procedure in `docs/operations/PRODUCTION-RUNBOOK.md`. The App Spec leaves `Email__EmergencyStop=true` until the approved controlled validation begins.
