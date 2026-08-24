"use client";

import Link from "next/link";
import type { DiscoveryFacet, DiscoveryParams } from "../lib/api";

type Props = {
  params: DiscoveryParams;
  categories: DiscoveryFacet[];
  retailers: DiscoveryFacet[];
  resultCount: number;
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

export function DiscoveryControls({ params, categories, retailers, resultCount }: Props) {
  const active = Object.entries(params).filter(([key, value]) => key in parameterLabels && value) as Array<[keyof DiscoveryParams, string]>;
  const clearFiltersHref = hrefWithoutFilters(params);

  return <section id="filters" className="discovery-controls" aria-label="Filter deals">
    <form action="/" method="get" className="discovery-form compact-filter-form">
      {params.search && <input type="hidden" name="search" value={params.search} />}
      {params.sort && <input type="hidden" name="sort" value={params.sort} />}
      <div className="compact-filter-heading"><div><p className="eyebrow">Quick filters</p><h2>What are you looking for?</h2></div><span><strong>{resultCount}</strong> {resultCount === 1 ? "product" : "products"}</span></div>
      <div className="compact-filter-fields">
        <div className="filter-field"><label htmlFor="category">Category</label><select id="category" name="category" defaultValue={params.category ?? ""}><option value="">All categories</option>{categories.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select></div>
        <div className="filter-field"><label htmlFor="retailer">Store</label><select id="retailer" name="retailer" defaultValue={params.retailer ?? ""}><option value="">All stores</option>{retailers.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select></div>
        <button className="button button-primary" type="submit">Show deals</button>
        {active.length > 0 && <Link className="button button-text" href={clearFiltersHref}>Clear</Link>}
      </div>
    </form>
    {active.length > 0 && <div className="active-filters" aria-label="Active filters">{active.map(([key, value]) => <Link className="filter-chip" key={key} href={hrefWithout(params, key)} aria-label={`Remove ${parameterLabels[key]} filter`}>{parameterLabels[key]}: {value} <span aria-hidden="true">×</span></Link>)}</div>}
  </section>;
}
