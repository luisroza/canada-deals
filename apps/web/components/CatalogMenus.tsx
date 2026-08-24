"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef } from "react";
import type { DiscoveryFacet } from "../lib/api";

type Props = {
  categories: DiscoveryFacet[];
  retailers: DiscoveryFacet[];
};

function CatalogMenu({ label, items, parameter }: { label: string; items: DiscoveryFacet[]; parameter: "category" | "retailer" }) {
  const detailsRef = useRef<HTMLDetailsElement>(null);

  return (
    <details className="catalog-menu" ref={detailsRef}>
      <summary>{label}<span aria-hidden="true">⌄</span></summary>
      <div className={`catalog-menu-panel catalog-menu-panel-${parameter}`}>
        <p className="catalog-menu-title">Browse by {label.toLowerCase()}</p>
        <div className="catalog-menu-grid">
          {items.map((item) => (
            <Link
              key={item.key}
              href={`/?${parameter}=${encodeURIComponent(item.key)}#deals`}
              onClick={() => detailsRef.current?.removeAttribute("open")}
            >
              {parameter === "retailer" && <span className="catalog-menu-mark" aria-hidden="true">{item.label.slice(0, 1)}</span>}
              <span>{item.label}</span>
            </Link>
          ))}
        </div>
      </div>
    </details>
  );
}

export function CatalogMenus({ categories, retailers }: Props) {
  const pathname = usePathname();

  useEffect(() => {
    document.querySelectorAll<HTMLDetailsElement>(".catalog-menu[open]").forEach((item) => item.removeAttribute("open"));
  }, [pathname]);

  return (
    <nav className="catalog-nav" aria-label="Browse deals">
      <Link className="catalog-nav-home" href="/">Deals</Link>
      <CatalogMenu label="Categories" items={categories} parameter="category" />
      <CatalogMenu label="Stores" items={retailers} parameter="retailer" />
      <Link className="catalog-nav-wishlist" href="/saved">Wishlist</Link>
    </nav>
  );
}
