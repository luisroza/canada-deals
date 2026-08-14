"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import type { DiscoveryFacet, DiscoveryParams } from "../lib/api";

type Props = {
  params: DiscoveryParams;
  categories: DiscoveryFacet[];
  retailers: DiscoveryFacet[];
  resultCount: number;
};

const parameterLabels: Record<string, string> = {
  category: "Category", retailer: "Retailer", minPrice: "Minimum price", maxPrice: "Maximum price",
  hasReference: "Reference", freshness: "Freshness", match: "Match", availability: "Availability"
};

function hrefWithout(params: DiscoveryParams, key: keyof DiscoveryParams) {
  const next = new URLSearchParams();
  Object.entries(params).forEach(([name, value]) => {
    if (name !== key && name !== "page" && value) next.set(name, value);
  });
  return next.size ? `/?${next.toString()}` : "/";
}

function hrefWith(params: DiscoveryParams, key: keyof DiscoveryParams, value: string) {
  const next = new URLSearchParams();
  Object.entries(params).forEach(([name, current]) => {
    if (name !== "page" && current) next.set(name, current);
  });
  next.set(key, value);
  return `/?${next.toString()}`;
}

function hrefWithoutFilters(params: DiscoveryParams) {
  const next = new URLSearchParams();
  if (params.search) next.set("search", params.search);
  if (params.sort) next.set("sort", params.sort);
  return next.size ? `/?${next.toString()}` : "/";
}

export function DiscoveryControls({ params, categories, retailers, resultCount }: Props) {
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [isMobileViewport, setIsMobileViewport] = useState(true);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLElement>(null);
  const panelHeadingRef = useRef<HTMLHeadingElement>(null);
  const active = Object.entries(params).filter(([key, value]) => key in parameterLabels && value) as Array<[keyof DiscoveryParams, string]>;
  const effectiveSort = params.sort ?? (params.search ? "relevance" : "recent");
  const clearFiltersHref = hrefWithoutFilters(params);

  useEffect(() => {
    if (!window.matchMedia) return;
    const media = window.matchMedia("(max-width: 760px)");
    const updateViewport = () => setIsMobileViewport(media.matches);
    updateViewport();
    media.addEventListener("change", updateViewport);
    return () => media.removeEventListener("change", updateViewport);
  }, []);

  useEffect(() => {
    if (filtersOpen && isMobileViewport) panelHeadingRef.current?.focus();
  }, [filtersOpen, isMobileViewport]);

  useEffect(() => {
    if (!filtersOpen || !isMobileViewport) return;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        event.preventDefault();
        closeFilters();
        return;
      }
      if (event.key !== "Tab" || !panelRef.current) return;
      const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      )).filter((element) => element.getAttribute("aria-hidden") !== "true");
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      const activeElement = document.activeElement;
      if (!focusable.includes(activeElement as HTMLElement)) {
        event.preventDefault();
        (event.shiftKey ? last : first).focus();
      } else if (event.shiftKey && activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    }

    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [filtersOpen, isMobileViewport]);

  function openFilters() {
    setFiltersOpen(true);
  }

  function closeFilters() {
    setFiltersOpen(false);
    triggerRef.current?.focus();
  }

  return <section className="discovery-controls" aria-label="Search and filter deals">
    <form action="/" method="get" role="search" className="discovery-search-form">
      <label htmlFor="deal-search">Search a product or model number</label>
      <div className="search-row">
        <input id="deal-search" name="search" defaultValue={params.search} placeholder="Try Northstar, NS55QLED-2026, or cordless drill" />
        <button className="button button-primary" type="submit">Search deals</button>
      </div>
    </form>
    <form action="/" method="get" className="discovery-form">
      {params.search && <input type="hidden" name="search" value={params.search} />}
      <div className="control-toolbar">
        <button ref={triggerRef} className="button button-secondary filter-trigger" type="button" onClick={filtersOpen ? closeFilters : openFilters} aria-expanded={filtersOpen} aria-controls="filter-panel">
          <span>Filters</span>
          {active.length > 0 && <span className="filter-count" aria-label={`${active.length} active ${active.length === 1 ? "filter" : "filters"}`}>{active.length}</span>}
          <span className={`filter-chevron${filtersOpen ? " is-open" : ""}`} aria-hidden="true">⌄</span>
        </button>
        <div className="sort-controls">
          <label htmlFor="deal-sort">Sort by</label>
          <select id="deal-sort" name="sort" defaultValue={effectiveSort}>
            <option value="relevance">Relevance</option>
            <option value="recent">Recently checked</option>
            <option value="savings">Supported savings</option>
            <option value="price-asc">Lowest price</option>
          </select>
          <button className="button button-secondary" type="submit">Apply sort</button>
        </div>
      </div>

      <aside ref={panelRef} id="filter-panel" className={`filter-panel${filtersOpen ? " is-open" : ""}`} role={filtersOpen && isMobileViewport ? "dialog" : "region"} aria-modal={filtersOpen && isMobileViewport || undefined} aria-labelledby="filter-heading" hidden={!filtersOpen}>
        <div className="filter-heading-row">
          <div>
            <h2 id="filter-heading" ref={panelHeadingRef} tabIndex={-1}>Filter deals</h2>
            <p>Narrow the results while keeping your current search.</p>
          </div>
          <button className="button button-text filter-close" type="button" onClick={closeFilters}>Close filters</button>
        </div>
        <div className="filter-fields">
          <div className="filter-field">
            <label htmlFor="category">Category</label>
            <select id="category" name="category" defaultValue={params.category ?? ""}><option value="">All categories</option>{categories.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select>
          </div>
          <div className="filter-field">
            <label htmlFor="retailer">Retailer</label>
            <select id="retailer" name="retailer" defaultValue={params.retailer ?? ""}><option value="">All retailers</option>{retailers.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select>
          </div>
          <fieldset className="price-fields">
            <legend>Price range</legend>
            <div><label htmlFor="min-price">Minimum price</label><input id="min-price" name="minPrice" type="number" min="0" step="0.01" inputMode="decimal" defaultValue={params.minPrice} /></div>
            <div><label htmlFor="max-price">Maximum price</label><input id="max-price" name="maxPrice" type="number" min="0" step="0.01" inputMode="decimal" defaultValue={params.maxPrice} /></div>
          </fieldset>
          <div className="filter-field">
            <label htmlFor="freshness">Freshness</label>
            <select id="freshness" name="freshness" defaultValue={params.freshness ?? ""}><option value="">Any freshness</option><option value="recent">Checked within 6 hours</option><option value="aging">Checked 6–24 hours ago</option><option value="stale">Older than 24 hours</option><option value="unknown">Unknown</option></select>
          </div>
          <div className="filter-field">
            <label htmlFor="reference">Supported reference</label>
            <select id="reference" name="hasReference" defaultValue={params.hasReference ?? ""}><option value="">Any evidence state</option><option value="true">Has supported reference</option><option value="false">No supported reference</option></select>
          </div>
          <div className="filter-field">
            <label htmlFor="match">Comparison confidence</label>
            <select id="match" name="match" defaultValue={params.match ?? ""}><option value="">Any match state</option><option value="safe">Safe comparison</option><option value="review">Needs review</option><option value="none">No match</option></select>
          </div>
          <div className="filter-field">
            <label htmlFor="availability">Availability</label>
            <select id="availability" name="availability" defaultValue={params.availability ?? ""}><option value="">Any availability</option><option value="online">Available online</option><option value="unavailable">Unavailable</option><option value="unknown">Unknown</option></select>
          </div>
        </div>
        <div className="filter-actions"><Link className="button button-text" href={clearFiltersHref}>Clear filters</Link><button className="button button-primary" type="submit">Apply filters</button></div>
      </aside>
    </form>

    <div className="result-summary" role="status" aria-live="polite"><strong>{resultCount}</strong> {resultCount === 1 ? "product" : "products"} found</div>
    {categories.length > 0 && <nav className="category-shortcuts" aria-label="Browse deal categories"><span>Quick categories</span>{categories.slice(0, 6).map((item) => <Link key={item.key} href={hrefWith(params, "category", item.key)} aria-current={params.category === item.key ? "page" : undefined}>{item.label}</Link>)}</nav>}
    {active.length > 0 && <div className="active-filters" aria-label="Active filters">{active.map(([key, value]) => <Link className="filter-chip" key={key} href={hrefWithout(params, key)} aria-label={`Remove ${parameterLabels[key]} filter`}>{parameterLabels[key]}: {value} <span aria-hidden="true">×</span></Link>)}<Link href={clearFiltersHref} className="clear-filters">Clear filters</Link></div>}
  </section>;
}
