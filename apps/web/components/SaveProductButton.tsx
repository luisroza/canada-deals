"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useWishlist } from "./WishlistContext";

export function SaveProductButton({ listingId, productTitle, returnTo }: { listingId: string; productTitle: string; returnTo: string }) {
  const wishlist = useWishlist();
  const [showBoundary, setShowBoundary] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const boundaryHeadingRef = useRef<HTMLHeadingElement>(null);
  const accountQuery = `returnTo=${encodeURIComponent(returnTo)}`;
  const boundaryId = `save-boundary-${listingId}`;

  useEffect(() => {
    if (showBoundary) boundaryHeadingRef.current?.focus();
  }, [showBoundary]);

  async function toggle() {
    if (!wishlist.authenticated) {
      setShowBoundary(true);
      return;
    }
    setError(null);
    try {
      await wishlist.toggle(listingId);
    } catch {
      setError(wishlist.isSaved(listingId) ? "This offer could not be removed. Please try again." : "This offer could not be saved. Please try again.");
    }
  }

  const saved = wishlist.isSaved(listingId);
  const pending = wishlist.isPending(listingId);
  const loading = wishlist.loading;

  return (
    <section className="save-control" aria-label={`Save ${productTitle}`}>
      <button ref={triggerRef} className="button button-secondary" type="button" onClick={toggle} disabled={loading || pending || Boolean(wishlist.loadError && wishlist.authenticated === null)} aria-pressed={wishlist.authenticated ? saved : undefined} aria-expanded={!wishlist.authenticated ? showBoundary : undefined} aria-controls={!wishlist.authenticated ? boundaryId : undefined}>
        {loading ? "Loading saved state…" : pending ? "Updating…" : saved ? "Saved — remove" : "Save offer"}
      </button>
      {showBoundary && !wishlist.authenticated && (
        <div id={boundaryId} className="save-boundary" aria-labelledby={`${boundaryId}-heading`}>
          <h2 ref={boundaryHeadingRef} tabIndex={-1} id={`${boundaryId}-heading`}>Sign in to save this offer</h2>
          <p>The offer stays public. An account is needed only to keep this Wishlist across visits.</p>
          <div className="inline-actions">
            <Link className="button button-primary" href={`/account/sign-in?${accountQuery}`}>Sign in</Link>
            <Link className="button button-secondary" href={`/account/register?${accountQuery}`}>Create account</Link>
            <button className="button button-text" type="button" onClick={() => { setShowBoundary(false); requestAnimationFrame(() => triggerRef.current?.focus()); }}>Cancel</button>
          </div>
        </div>
      )}
      {wishlist.loadError && <p className="field-error" role="alert">Saved state could not be loaded. Public product details remain available.</p>}
      {error && <p className="field-error" role="alert">{error}</p>}
    </section>
  );
}
