"use client";

import Link from "next/link";
import { FormEvent, useEffect, useState } from "react";
import type { DiscoveryFacet, DiscoveryParams } from "../lib/api";

type Props = {
  params: DiscoveryParams;
  categories: DiscoveryFacet[];
  retailers: DiscoveryFacet[];
  resultCount: number;
  pending: boolean;
  onApply: (filters: Pick<DiscoveryParams, "category" | "retailer">) => Promise<boolean>;
  onRemove: (key: "category" | "retailer") => Promise<boolean>;
  onClear: () => Promise<boolean>;
};

const parameterLabels: Record<string, string> = {
  category: "Category", retailer: "Store"
};

function hrefWithout(params: DiscoveryParams, key: keyof DiscoveryParams) {
  const next = new URLSearchParams();
  Object.entries(params).forEach(([name, value]) => {
    if (name !== key && name !== "page" && value) next.set(name, value);
  });
  return next.size ? `/?${next.toString()}` : "/";
}

function hrefWithoutFilters(params: DiscoveryParams) {
  const next = new URLSearchParams();
  if (params.search) next.set("search", params.search);
  if (params.sort) next.set("sort", params.sort);
  return next.size ? `/?${next.toString()}` : "/";
}

export function DiscoveryControls({ params, categories, retailers, resultCount, pending, onApply, onRemove, onClear }: Props) {
  const [category, setCategory] = useState(params.category ?? "");
  const [retailer, setRetailer] = useState(params.retailer ?? "");
  const [hydrated, setHydrated] = useState(false);
  const active = Object.entries(params).filter(([key, value]) => key in parameterLabels && value) as Array<[keyof DiscoveryParams, string]>;
  const clearFiltersHref = hrefWithoutFilters(params);
  const inactive = pending || !hydrated;

  useEffect(() => { setHydrated(true); }, []);

  useEffect(() => {
    setCategory(params.category ?? "");
    setRetailer(params.retailer ?? "");
  }, [params.category, params.retailer]);

  async function apply(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const updated = await onApply({ category: category || undefined, retailer: retailer || undefined });
    if (!updated) {
      setCategory(params.category ?? "");
      setRetailer(params.retailer ?? "");
    }
  }

  async function clear() {
    setCategory("");
    setRetailer("");
    const updated = await onClear();
    if (!updated) {
      setCategory(params.category ?? "");
      setRetailer(params.retailer ?? "");
    }
  }

  async function remove(key: "category" | "retailer") {
    if (key === "category") setCategory("");
    else setRetailer("");
    const updated = await onRemove(key);
    if (!updated) {
      setCategory(params.category ?? "");
      setRetailer(params.retailer ?? "");
    }
  }

  return <section id="filters" className="discovery-controls" aria-label="Filter deals">
    <form action="/" method="get" className="discovery-form compact-filter-form" onSubmit={apply} aria-busy={pending ? "true" : undefined}>
      {params.search && <input type="hidden" name="search" value={params.search} />}
      {params.sort && <input type="hidden" name="sort" value={params.sort} />}
      <div className="compact-filter-heading"><div><p className="eyebrow">Quick filters</p><h2>What are you looking for?</h2></div><span><strong>{resultCount}</strong> {resultCount === 1 ? "product" : "products"}</span></div>
      <div className="compact-filter-fields">
        <div className="filter-field"><label htmlFor="category">Category</label><select id="category" name="category" value={category} disabled={inactive} onChange={event => setCategory(event.target.value)}><option value="">All categories</option>{categories.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select></div>
        <div className="filter-field"><label htmlFor="retailer">Store</label><select id="retailer" name="retailer" value={retailer} disabled={inactive} onChange={event => setRetailer(event.target.value)}><option value="">All stores</option>{retailers.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select></div>
        <button className="button button-primary" type="submit" disabled={inactive}>{pending ? "Updating…" : "Show deals"}</button>
        {active.length > 0 && <Link className="button button-text" href={clearFiltersHref} aria-disabled={inactive || undefined} tabIndex={inactive ? -1 : undefined} onClick={event => { event.preventDefault(); if (!inactive) void clear(); }}>Clear</Link>}
      </div>
    </form>
    {active.length > 0 && <div className="active-filters" aria-label="Active filters">{active.map(([key, value]) => <Link className="filter-chip" key={key} href={hrefWithout(params, key)} aria-disabled={inactive || undefined} tabIndex={inactive ? -1 : undefined} aria-label={`Remove ${parameterLabels[key]} filter`} onClick={event => { event.preventDefault(); if (!inactive) void remove(key as "category" | "retailer"); }}>{parameterLabels[key]}: {value} <span aria-hidden="true">×</span></Link>)}</div>}
    {pending && <p className="sr-only" role="status" aria-live="polite">Updating filtered deals…</p>}
  </section>;
}
