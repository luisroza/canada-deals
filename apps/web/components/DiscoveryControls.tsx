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

export function DiscoveryControls({ params, categories, retailers, resultCount }: Props) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const panelRef = useRef<HTMLElement>(null);
  const panelHeadingRef = useRef<HTMLHeadingElement>(null);
  const active = Object.entries(params).filter(([key, value]) => key in parameterLabels && value) as Array<[keyof DiscoveryParams, string]>;

  useEffect(() => {
    if (mobileOpen) panelHeadingRef.current?.focus();
  }, [mobileOpen]);

  useEffect(() => {
    if (!mobileOpen) return;
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
      const active = document.activeElement;
      if (!focusable.includes(active as HTMLElement)) {
        event.preventDefault();
        (event.shiftKey ? last : first).focus();
      } else if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    }

    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [mobileOpen]);

  function openFilters() {
    setMobileOpen(true);
  }

  function closeFilters() {
    setMobileOpen(false);
    triggerRef.current?.focus();
  }

  return <section className="discovery-controls" aria-label="Search and filter deals">
    <form action="/" method="get" role="search" className="discovery-form">
      <label htmlFor="deal-search">Search a product or model number</label>
      <div className="search-row">
        <input id="deal-search" name="search" defaultValue={params.search} placeholder="Try Northstar, NS55QLED-2026, or cordless drill" />
        <button className="button button-primary" type="submit">Search deals</button>
      </div>
      <div className="control-toolbar">
        <button ref={triggerRef} className="button button-secondary mobile-filter-trigger" type="button" onClick={openFilters} aria-expanded={mobileOpen} aria-controls="filter-panel">
          Filters{active.length ? ` (${active.length})` : ""}
        </button>
        <label htmlFor="deal-sort">Sort</label>
        <select id="deal-sort" name="sort" defaultValue={params.sort ?? ""}>
          <option value="">Recommended default</option>
          <option value="relevance">Relevance</option>
          <option value="recent">Recently checked</option>
          <option value="savings">Supported savings</option>
          <option value="price-asc">Lowest price</option>
        </select>
        <button className="button button-secondary" type="submit">Apply</button>
      </div>

      <aside ref={panelRef} id="filter-panel" className={`filter-panel${mobileOpen ? " is-open" : ""}`} role={mobileOpen ? "dialog" : undefined} aria-modal={mobileOpen || undefined} aria-labelledby="filter-heading">
        <div className="filter-heading-row">
          <h2 id="filter-heading" ref={panelHeadingRef} tabIndex={-1}>Filter deals</h2>
          <button className="button button-text mobile-filter-close" type="button" onClick={closeFilters}>Close</button>
        </div>
        <div className="filter-fields">
          <label htmlFor="category">Category</label>
          <select id="category" name="category" defaultValue={params.category ?? ""}><option value="">All categories</option>{categories.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select>
          <label htmlFor="retailer">Retailer</label>
          <select id="retailer" name="retailer" defaultValue={params.retailer ?? ""}><option value="">All retailers</option>{retailers.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select>
          <div className="price-fields"><div><label htmlFor="min-price">Minimum price</label><input id="min-price" name="minPrice" type="number" min="0" step="0.01" defaultValue={params.minPrice} /></div><div><label htmlFor="max-price">Maximum price</label><input id="max-price" name="maxPrice" type="number" min="0" step="0.01" defaultValue={params.maxPrice} /></div></div>
          <label htmlFor="reference">Supported reference</label>
          <select id="reference" name="hasReference" defaultValue={params.hasReference ?? ""}><option value="">Any evidence state</option><option value="true">Has supported reference</option><option value="false">No supported reference</option></select>
          <label htmlFor="freshness">Freshness</label>
          <select id="freshness" name="freshness" defaultValue={params.freshness ?? ""}><option value="">Any freshness</option><option value="recent">Checked within 6 hours</option><option value="aging">Checked 6–24 hours ago</option><option value="stale">Older than 24 hours</option><option value="unknown">Unknown</option></select>
          <label htmlFor="match">Comparison confidence</label>
          <select id="match" name="match" defaultValue={params.match ?? ""}><option value="">Any match state</option><option value="safe">Safe comparison</option><option value="review">Needs review</option><option value="none">No match</option></select>
          <label htmlFor="availability">Availability</label>
          <select id="availability" name="availability" defaultValue={params.availability ?? ""}><option value="">Any availability</option><option value="online">Available online</option><option value="unavailable">Unavailable</option><option value="unknown">Unknown</option></select>
        </div>
        <div className="filter-actions"><button className="button button-primary" type="submit">Apply filters</button><Link className="button button-text" href="/">Clear all</Link></div>
      </aside>
    </form>

    <div className="result-summary" role="status" aria-live="polite"><strong>{resultCount}</strong> {resultCount === 1 ? "product" : "products"} found</div>
    {active.length > 0 && <div className="active-filters" aria-label="Active filters">{active.map(([key, value]) => <Link className="filter-chip" key={key} href={hrefWithout(params, key)} aria-label={`Remove ${parameterLabels[key]} filter`}>{parameterLabels[key]}: {value} <span aria-hidden="true">×</span></Link>)}<Link href="/" className="clear-filters">Clear all</Link></div>}
  </section>;
}
