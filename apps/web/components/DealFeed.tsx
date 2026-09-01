"use client";

import Link from "next/link";
import type { DiscoveryParams, DiscoveryResponse } from "../lib/api";
import { DealCard } from "./DealCard";

const sortOptions = [
  { value: "relevance", label: "Most relevant", searchOnly: true },
  { value: "recent", label: "Latest", searchOnly: false },
  { value: "savings", label: "Best savings", searchOnly: false },
  { value: "price-asc", label: "Lowest price", searchOnly: false },
] as const;

function discoveryHref(params: DiscoveryParams) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => { if (value) query.set(key, value); });
  return query.size > 0 ? `/?${query.toString()}` : "/";
}

function pageHref(params: DiscoveryParams, page: number) {
  return discoveryHref({ ...params, page: String(page) });
}

function currentDiscoveryHref(params: DiscoveryParams) {
  return `${discoveryHref(params)}#deals`;
}

function feedHeading(sort: string) {
  if (sort === "relevance") return "Most relevant deals";
  if (sort === "savings") return "Biggest verified savings";
  if (sort === "price-asc") return "Lowest prices";
  return "Recently checked deals";
}

export function DealFeed({ params, result, initialLoadError, updateError, pendingKind, onSort, onClearFilters }: {
  params: DiscoveryParams;
  result?: DiscoveryResponse;
  initialLoadError: boolean;
  updateError: "filters" | "sort" | null;
  pendingKind: "filters" | "sort" | "history" | null;
  onSort: (sort: string) => Promise<boolean>;
  onClearFilters: () => Promise<boolean>;
}) {
  const effectiveSort = result?.sort ?? (params.search ? "relevance" : "recent");
  const selectedSort = pendingKind === "sort" ? (params.sort ?? (params.search ? "relevance" : "recent")) : effectiveSort;

  function changeSort(sort: string) {
    if (sort === selectedSort || pendingKind) return;
    void onSort(sort);
  }

  return <section id="deals" aria-labelledby="deal-feed-heading" aria-busy={pendingKind ? "true" : undefined}>
    <div className="section-heading"><div><p className="eyebrow">{params.search ? `Results for “${params.search}”` : "Current offers"}</p><h2 id="deal-feed-heading">{feedHeading(effectiveSort)}</h2></div><span className="feed-count">{result?.count ?? 0} products</span></div>
    <nav className="feed-modes" aria-label="Deal feed views">
      {sortOptions.filter(option => !option.searchOnly || params.search).map(option => <button key={option.value} type="button" aria-current={selectedSort === option.value ? "page" : undefined} disabled={Boolean(pendingKind)} onClick={() => changeSort(option.value)}>{option.label}</button>)}
    </nav>
    {pendingKind === "sort" && <p className="sr-only" role="status" aria-live="polite">Updating deal order…</p>}
    {updateError && <div className="error-state feed-update-error" role="alert">{updateError === "filters" ? "The filters could not be applied. Your current results are unchanged." : "The deal order could not be updated. Your current results are unchanged."}</div>}
    {initialLoadError && <div className="error-state" role="alert">Deals are temporarily unavailable. Check that the API and local PostgreSQL are running.</div>}
    {!initialLoadError && result?.items.length === 0 && <div className="empty-state"><h3>No products match this selection.</h3><p>Try another search, category, or store.</p><Link className="button button-secondary" href="/" onClick={event => { event.preventDefault(); if (!pendingKind) void onClearFilters(); }}>Clear selection</Link></div>}
    {!initialLoadError && result && <div className="deal-grid">{result.items.map(deal => <DealCard key={deal.listingId} deal={deal} returnTo={currentDiscoveryHref(params)} />)}</div>}
    {!initialLoadError && result && result.totalPages > 1 && <nav className="pagination" aria-label="Search result pages">{result.page > 1 && <Link className="button button-secondary" href={pageHref(params, result.page - 1)}>Previous</Link>}<span>Page {result.page} of {result.totalPages}</span>{result.hasNext && <Link className="button button-secondary" href={pageHref(params, result.page + 1)}>Next</Link>}</nav>}
  </section>;
}
