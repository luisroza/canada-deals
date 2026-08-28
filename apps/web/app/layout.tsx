import type { Metadata } from "next";
import Link from "next/link";
import { AccountNav } from "../components/AccountNav";
import { CatalogMenus } from "../components/CatalogMenus";
import { GlobalSearch } from "../components/GlobalSearch";
import { MobileNav } from "../components/MobileNav";
import { WishlistProvider } from "../components/WishlistContext";
import { getDeals, type DiscoveryFacet } from "../lib/api";
import "./globals.css";

export const metadata: Metadata = {
  title: "Canada Deals - Evidence-led deal discovery",
  description: "Compare Canadian online offers with visible price evidence, freshness, and safe product matching.",
};

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  let categories: DiscoveryFacet[] = [];
  let retailers: DiscoveryFacet[] = [];
  try {
    const navigation = await getDeals({ pageSize: "1" });
    categories = navigation.facets.categories;
    retailers = navigation.facets.retailers;
  } catch {
    // Page-level error states still explain API availability; navigation stays usable.
  }

  return (
    <html lang="en-CA">
      <body>
        <WishlistProvider>
          <a className="skip-link" href="#main-content">Skip to content</a>
          <header className="site-header">
            <div className="header-inner header-primary"><Link className="brand" href="/">GreatDeals<span>.ca</span></Link><GlobalSearch /><AccountNav /></div>
            <div className="catalog-bar"><div className="header-inner"><CatalogMenus categories={categories} retailers={retailers} /></div></div>
          </header>
          <main id="main-content" className="page-shell">{children}</main>
          <footer><div className="header-inner">Prices reflect our latest source check. We may earn a commission from some retailer links. As an Amazon Associate I earn from qualifying purchases.</div></footer>
          <MobileNav />
        </WishlistProvider>
      </body>
    </html>
  );
}
