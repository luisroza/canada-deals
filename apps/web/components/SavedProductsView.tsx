"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { freshnessTone, StateBadge } from "./StateBadge";
import { useWishlist } from "./WishlistContext";

function formatPrice(price: number | null, currency: string) {
  return price === null ? "Current price unavailable" : new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(price);
}

export function SavedProductsView() {
  const wishlist = useWishlist();
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [category, setCategory] = useState("");
  const [retailer, setRetailer] = useState("");
  const [sort, setSort] = useState("saved-desc");

  const categories = useMemo(() => [...new Set(wishlist.items.map((item) => item.category))].sort(), [wishlist.items]);
  const retailers = useMemo(() => [...new Set(wishlist.items.map((item) => item.retailer).filter((value): value is string => Boolean(value)))].sort(), [wishlist.items]);
  const visibleItems = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase("en-CA");
    return wishlist.items
      .filter((item) => !normalizedQuery || [item.productTitle, item.brand, item.category, item.retailer ?? ""].some((value) => value.toLocaleLowerCase("en-CA").includes(normalizedQuery)))
      .filter((item) => !category || item.category === category)
      .filter((item) => !retailer || item.retailer === retailer)
      .sort((left, right) => {
        if (sort === "price-asc") return (left.currentPrice ?? Number.MAX_SAFE_INTEGER) - (right.currentPrice ?? Number.MAX_SAFE_INTEGER);
        if (sort === "name") return left.productTitle.localeCompare(right.productTitle, "en-CA");
        if (sort === "retailer") return (left.retailer ?? "").localeCompare(right.retailer ?? "", "en-CA");
        return new Date(right.savedAt).getTime() - new Date(left.savedAt).getTime();
      });
  }, [category, query, retailer, sort, wishlist.items]);
  const hasFilters = Boolean(query || category || retailer || sort !== "saved-desc");

  async function remove(productId: string) {
    setError(null);
    try {
      await wishlist.toggle(productId);
    } catch {
      setError("The saved product could not be removed. Please try again.");
    }
  }

  function clearFilters() {
    setQuery("");
    setCategory("");
    setRetailer("");
    setSort("saved-desc");
  }

  if (wishlist.loading) return <p role="status">Loading saved products…</p>;
  if (wishlist.authenticated === false) return <section className="account-boundary"><h2>Sign in to see your wishlist</h2><p>Browsing remains public. Sign in only when you want to keep products for later.</p><div className="inline-actions"><Link className="button button-primary" href="/account/sign-in?returnTo=%2Fsaved">Sign in</Link><Link className="button button-secondary" href="/account/register?returnTo=%2Fsaved">Create account</Link></div></section>;
  if (wishlist.loadError) return <section className="error-state" role="alert"><h2>Wishlist unavailable</h2><p>{wishlist.loadError}</p><button className="button button-primary" type="button" onClick={() => void wishlist.retry()}>Try again</button></section>;

  return (
    <>
      {error && <p className="field-error" role="alert">{error}</p>}
      {wishlist.items.length === 0 ? (
        <section className="notice"><h2>Your wishlist is empty.</h2><p>Browse current offers and save a product when you want to return to it.</p><Link className="button button-primary" href="/">Browse deals</Link></section>
      ) : (
        <>
          <form className="wishlist-controls" onSubmit={(event) => event.preventDefault()}>
            <div><label htmlFor="wishlist-search">Search your Wishlist</label><input id="wishlist-search" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Product, brand, category, or store" /></div>
            <div><label htmlFor="wishlist-category">Category</label><select id="wishlist-category" value={category} onChange={(event) => setCategory(event.target.value)}><option value="">All categories</option>{categories.map((value) => <option key={value} value={value}>{value}</option>)}</select></div>
            <div><label htmlFor="wishlist-retailer">Store</label><select id="wishlist-retailer" value={retailer} onChange={(event) => setRetailer(event.target.value)}><option value="">All stores</option>{retailers.map((value) => <option key={value} value={value}>{value}</option>)}</select></div>
            <div><label htmlFor="wishlist-sort">Sort by</label><select id="wishlist-sort" value={sort} onChange={(event) => setSort(event.target.value)}><option value="saved-desc">Recently saved</option><option value="price-asc">Lowest current price</option><option value="name">Product name</option><option value="retailer">Store name</option></select></div>
            <button className="button button-secondary" type="button" disabled={!hasFilters} onClick={clearFilters}>Clear</button>
          </form>
          <p className="wishlist-result-count" role="status" aria-live="polite">{visibleItems.length} of {wishlist.items.length} saved {wishlist.items.length === 1 ? "product" : "products"}</p>
          {visibleItems.length === 0 ? <section className="notice"><h2>No saved products match.</h2><p>Try another product, category, or store.</p><button className="button button-primary" type="button" onClick={clearFilters}>Clear Wishlist filters</button></section> : <div className="saved-grid">
          {visibleItems.map((item) => <article className="deal-card saved-product-card" key={item.productId}>
              <p className="eyebrow">{item.category} · {item.brand}</p>
              <h2><Link href={item.detailsPath}>{item.productTitle}</Link></h2>
              <p className="price">{formatPrice(item.currentPrice, item.currency)}</p>
              <p className="card-meta">{item.retailer ?? "Retailer context unavailable"}</p>
              <p className="saved-at">Saved {new Intl.DateTimeFormat("en-CA", { dateStyle: "medium" }).format(new Date(item.savedAt))}</p>
              <div className="state-row">
                <StateBadge label={`Evidence: ${item.evidenceState.toLowerCase()}`} tone={item.evidenceState === "STRONG" ? "good" : "neutral"} />
                <StateBadge label={item.freshnessState === "RECENT" ? "Checked recently" : `Freshness: ${item.freshnessState.toLowerCase()}`} tone={freshnessTone(item.freshnessState)} />
              </div>
              <div className="inline-actions"><Link className="button button-secondary" href={item.detailsPath}>View current offer</Link><button className="button button-text" type="button" disabled={wishlist.isPending(item.productId)} onClick={() => remove(item.productId)}>{wishlist.isPending(item.productId) ? "Removing…" : "Remove from wishlist"}</button></div>
            </article>
          )}
          </div>}
        </>
      )}
    </>
  );
}
