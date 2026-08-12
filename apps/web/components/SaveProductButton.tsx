"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { getSavedProducts, getSession, saveProduct, unsaveProduct } from "../lib/account";

export function SaveProductButton({ productId, productTitle, returnTo }: { productId: string; productTitle: string; returnTo: string }) {
  const [loading, setLoading] = useState(true);
  const [authenticated, setAuthenticated] = useState(false);
  const [saved, setSaved] = useState(false);
  const [showBoundary, setShowBoundary] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const boundaryHeadingRef = useRef<HTMLHeadingElement>(null);
  const accountQuery = `returnTo=${encodeURIComponent(returnTo)}`;
  const boundaryId = `save-boundary-${productId}`;

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const session = await getSession();
        if (!active) return;
        setAuthenticated(session.isAuthenticated);
        if (session.isAuthenticated) {
          const products = await getSavedProducts();
          if (active) setSaved(products.some((item) => item.productId === productId));
        }
      } catch {
        if (active) setError("Saved state could not be loaded. Public product details remain available.");
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [productId]);

  useEffect(() => {
    if (showBoundary) boundaryHeadingRef.current?.focus();
  }, [showBoundary]);

  async function toggle() {
    if (!authenticated) {
      setShowBoundary(true);
      return;
    }
    setPending(true);
    setError(null);
    try {
      if (saved) await unsaveProduct(productId);
      else await saveProduct(productId);
      setSaved(!saved);
    } catch {
      setError(saved ? "This product could not be removed. Please try again." : "This product could not be saved. Please try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="save-control" aria-label={`Save ${productTitle}`}>
      <button ref={triggerRef} className="button button-secondary" type="button" onClick={toggle} disabled={loading || pending} aria-pressed={authenticated ? saved : undefined} aria-expanded={!authenticated ? showBoundary : undefined} aria-controls={!authenticated ? boundaryId : undefined}>
        {loading ? "Loading saved state…" : pending ? "Updating…" : saved ? "Saved — remove" : "Save product"}
      </button>
      {showBoundary && !authenticated && (
        <div id={boundaryId} className="save-boundary" aria-labelledby={`${boundaryId}-heading`}>
          <h2 ref={boundaryHeadingRef} tabIndex={-1} id={`${boundaryId}-heading`}>Sign in to save this product</h2>
          <p>Your product stays public. An account is needed only to keep this saved list across visits.</p>
          <div className="inline-actions">
            <Link className="button button-primary" href={`/account/sign-in?${accountQuery}`}>Sign in</Link>
            <Link className="button button-secondary" href={`/account/register?${accountQuery}`}>Create account</Link>
            <button className="button button-text" type="button" onClick={() => { setShowBoundary(false); requestAnimationFrame(() => triggerRef.current?.focus()); }}>Cancel</button>
          </div>
        </div>
      )}
      {error && <p className="field-error" role="alert">{error}</p>}
    </section>
  );
}
