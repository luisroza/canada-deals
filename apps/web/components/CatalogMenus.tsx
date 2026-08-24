"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import type { DiscoveryFacet } from "../lib/api";

type Props = {
  categories: DiscoveryFacet[];
  retailers: DiscoveryFacet[];
};

function CatalogMenu({ label, items, parameter, isOpen, onOpenChange }: {
  label: string;
  items: DiscoveryFacet[];
  parameter: "category" | "retailer";
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  return (
    <details className="catalog-menu" open={isOpen} onToggle={(event) => {
      if (event.currentTarget.open !== isOpen) onOpenChange(event.currentTarget.open);
    }}>
      <summary>{label}<span aria-hidden="true">⌄</span></summary>
      <div className={`catalog-menu-panel catalog-menu-panel-${parameter}`}>
        <p className="catalog-menu-title">Browse by {label.toLowerCase()}</p>
        <div className="catalog-menu-grid">
          {items.map((item) => (
            <Link
              className={`catalog-menu-item catalog-menu-item-${parameter}`}
              key={item.key}
              href={`/?${parameter}=${encodeURIComponent(item.key)}#deals`}
              onClick={() => onOpenChange(false)}
            >
              {parameter === "retailer" && <span className="catalog-menu-mark" aria-hidden="true">{item.label.slice(0, 1)}</span>}
              <span className="catalog-menu-label">{item.label}</span>
            </Link>
          ))}
        </div>
      </div>
    </details>
  );
}

export function CatalogMenus({ categories, retailers }: Props) {
  const pathname = usePathname();
  const [openMenu, setOpenMenu] = useState<"category" | "retailer" | null>(null);

  useEffect(() => {
    setOpenMenu(null);
  }, [pathname]);

  return (
    <nav className="catalog-nav" aria-label="Browse deals">
      <Link className="catalog-nav-home" href="/">Deals</Link>
      <CatalogMenu label="Categories" items={categories} parameter="category" isOpen={openMenu === "category"} onOpenChange={(open) => setOpenMenu(open ? "category" : null)} />
      <CatalogMenu label="Stores" items={retailers} parameter="retailer" isOpen={openMenu === "retailer"} onOpenChange={(open) => setOpenMenu(open ? "retailer" : null)} />
      <Link className="catalog-nav-wishlist" href="/saved">Wishlist</Link>
    </nav>
  );
}
