"use client";

import Link from "next/link";
import { FormEvent, useEffect, useRef, useState } from "react";
import { getPriceAlerts, getSession, PriceAlert, removePriceAlert, upsertPriceAlert } from "../lib/account";

type Props = {
  productId: string;
  productTitle: string;
  currentPrice: number | null;
  currency: string;
  returnTo: string;
};

function formatPrice(value: number, currency = "CAD") {
  return new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(value);
}

export function TargetPriceAlertControl({ productId, productTitle, currentPrice, currency, returnTo }: Props) {
  const [loading, setLoading] = useState(true);
  const [authenticated, setAuthenticated] = useState(false);
  const [emailConfirmed, setEmailConfirmed] = useState(false);
  const [alert, setAlert] = useState<PriceAlert | null>(null);
  const [editing, setEditing] = useState(false);
  const [target, setTarget] = useState(currentPrice ? Math.max(0.01, Math.floor(currentPrice * 0.9 * 100) / 100).toFixed(2) : "");
  const [consent, setConsent] = useState(false);
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const accountQuery = `returnTo=${encodeURIComponent(returnTo)}`;

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const session = await getSession();
        if (!active) return;
        setAuthenticated(session.isAuthenticated);
        setEmailConfirmed(session.emailConfirmed);
        if (session.isAuthenticated) {
          const alerts = await getPriceAlerts();
          if (!active) return;
          const existing = alerts.find((item) => item.productId === productId && item.status === "ACTIVE") ?? null;
          setAlert(existing);
          if (existing) setTarget(existing.targetPrice.toFixed(2));
        }
      } catch {
        if (active) setError("Price-alert state could not be loaded. Product evidence remains available.");
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [productId]);

  useEffect(() => {
    if (editing) headingRef.current?.focus();
  }, [editing]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const value = Number(target);
    if (!Number.isFinite(value) || value <= 0 || value > 1_000_000 || !/^\d+(\.\d{1,2})?$/.test(target)) {
      setError("Enter a CAD target from $0.01 to $1,000,000.00, with at most two decimal places.");
      return;
    }
    if (!consent) {
      setError("Confirm that you want this target-price email alert.");
      return;
    }

    setPending(true);
    setError(null);
    setMessage(null);
    try {
      await upsertPriceAlert(productId, value);
      const updated = (await getPriceAlerts()).find((item) => item.productId === productId && item.status === "ACTIVE") ?? null;
      setAlert(updated);
      setEditing(false);
      setConsent(false);
      setMessage(`Alert active at ${formatPrice(value)}. We'll evaluate fresh, verified offers at or below your target.`);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "The price alert could not be saved.");
    } finally {
      setPending(false);
    }
  }

  async function remove() {
    setPending(true);
    setError(null);
    setMessage(null);
    try {
      await removePriceAlert(productId);
      setAlert(null);
      setEditing(false);
      setMessage("Target-price alert removed. The product remains in your saved list.");
    } catch {
      setError("The price alert could not be removed. Please try again.");
    } finally {
      setPending(false);
    }
  }

  if (loading) return <section className="alert-control" aria-label={`Target-price alert for ${productTitle}`}><p role="status">Loading price-alert state…</p></section>;

  return (
    <section className="alert-control" aria-labelledby={`alert-heading-${productId}`}>
      <h2 id={`alert-heading-${productId}`}>Target-price alert</h2>
      {!authenticated ? (
        <div className="account-boundary">
          <p>Sign in to set a personal CAD target. Discovery and price evidence remain public.</p>
          <div className="inline-actions"><Link className="button button-primary" href={`/account/sign-in?${accountQuery}`}>Sign in</Link><Link className="button button-secondary" href={`/account/register?${accountQuery}`}>Create account</Link></div>
        </div>
      ) : !emailConfirmed ? (
        <div className="notice"><strong>Email confirmation required.</strong><p>An alert cannot become active until your account email is confirmed. No notification will be queued.</p></div>
      ) : alert && !editing ? (
        <div className="alert-summary">
          <p><strong>Active target: {formatPrice(alert.targetPrice, alert.currency)}</strong></p>
          <p>We evaluate only fresh, policy-permitted, safely matched offers. A continuous below-target condition sends at most one notification. This is not marketing or a weekly digest.</p>
          <div className="inline-actions"><button className="button button-secondary" type="button" onClick={() => setEditing(true)} disabled={pending}>Edit target</button><button className="button button-text" type="button" onClick={remove} disabled={pending}>{pending ? "Removing…" : "Remove alert"}</button></div>
        </div>
      ) : (
        <form className="alert-form" onSubmit={submit} noValidate>
          <h3 ref={headingRef} tabIndex={-1}>{alert ? "Edit your target" : "Choose your target"}</h3>
          <label htmlFor={`target-${productId}`}>Target price (CAD)</label>
          <div className="target-input"><span aria-hidden="true">$</span><input id={`target-${productId}`} name="targetPrice" type="text" inputMode="decimal" value={target} onChange={(event) => setTarget(event.target.value)} aria-describedby={`target-help-${productId}`} /></div>
          <p className="field-hint" id={`target-help-${productId}`}>{currentPrice === null ? "Current verified price is unavailable." : `Current verified price: ${formatPrice(currentPrice, currency)}.`} Equality qualifies.</p>
          <label className="consent-check"><input type="checkbox" checked={consent} onChange={(event) => setConsent(event.target.checked)} /> <span>I agree to receive an email when a fresh, verified offer reaches this target.</span></label>
          <p className="field-hint">This consent is only for this target-price alert—not marketing or a weekly digest. Creating an alert also saves this product.</p>
          <div className="inline-actions"><button className="button button-primary" type="submit" disabled={pending}>{pending ? "Saving…" : alert ? "Update alert" : "Set alert"}</button>{alert && <button className="button button-text" type="button" onClick={() => setEditing(false)} disabled={pending}>Cancel</button>}</div>
        </form>
      )}
      {message && <p className="report-success" role="status">{message}</p>}
      {error && <p className="field-error" role="alert">{error}</p>}
      <p className="delivery-boundary"><strong>Delivery status:</strong> email delivery is captured in Development/Test. Production email delivery is not yet configured.</p>
    </section>
  );
}
