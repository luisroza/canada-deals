"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import type { SavedProduct } from "../lib/api";
import { getSavedProducts, getSession, unsaveProduct } from "../lib/account";
import { freshnessTone, StateBadge } from "./StateBadge";

function formatPrice(price: number | null, currency: string) {
  return price === null ? "Current price unavailable" : new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(price);
}

export function SavedProductsView() {
  const [loading, setLoading] = useState(true);
  const [authenticated, setAuthenticated] = useState<boolean | null>(null);
  const [items, setItems] = useState<SavedProduct[]>([]);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const session = await getSession();
        if (!active) return;
        setAuthenticated(session.isAuthenticated);
        if (session.isAuthenticated) {
          const savedProducts = await getSavedProducts();
          if (active) setItems(savedProducts);
        }
      } catch {
        if (active) setError("Saved products could not be loaded. Please try again.");
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, []);

  async function remove(productId: string) {
    setPendingId(productId);
    setError(null);
    try {
      await unsaveProduct(productId);
      setItems((current) => current.filter((item) => item.productId !== productId));
    } catch {
      setError("The saved product could not be removed. Please try again.");
    } finally {
      setPendingId(null);
    }
  }

  if (loading) return <p role="status">Loading saved products…</p>;
  if (authenticated === false) return <section className="account-boundary"><h2>Sign in to see your wishlist</h2><p>Browsing remains public. Sign in only when you want to keep products for later.</p><div className="inline-actions"><Link className="button button-primary" href="/account/sign-in?returnTo=%2Fsaved">Sign in</Link><Link className="button button-secondary" href="/account/register?returnTo=%2Fsaved">Create account</Link></div></section>;

  return (
    <>
      {error && <p className="field-error" role="alert">{error}</p>}
      {items.length === 0 ? (
        <section className="notice"><h2>Your wishlist is empty.</h2><p>Browse current offers and save a product when you want to return to it.</p><Link className="button button-primary" href="/">Browse deals</Link></section>
      ) : (
        <div className="saved-grid">
          {items.map((item) => <article className="deal-card saved-product-card" key={item.productId}>
              <p className="eyebrow">{item.category} · {item.brand}</p>
              <h2><Link href={item.detailsPath}>{item.productTitle}</Link></h2>
              <p className="price">{formatPrice(item.currentPrice, item.currency)}</p>
              <p className="card-meta">{item.retailer ?? "Retailer context unavailable"}</p>
              <div className="state-row">
                <StateBadge label={`Evidence: ${item.evidenceState.toLowerCase()}`} tone={item.evidenceState === "STRONG" ? "good" : "neutral"} />
                <StateBadge label={item.freshnessState === "RECENT" ? "Checked recently" : `Freshness: ${item.freshnessState.toLowerCase()}`} tone={freshnessTone(item.freshnessState)} />
              </div>
              <div className="inline-actions"><Link className="button button-secondary" href={item.detailsPath}>View current offer</Link><button className="button button-text" type="button" disabled={pendingId === item.productId} onClick={() => remove(item.productId)}>{pendingId === item.productId ? "Removing…" : "Remove from wishlist"}</button></div>
            </article>
          )}
        </div>
      )}
    </>
  );
}
