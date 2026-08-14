import type { Metadata } from "next";
import Link from "next/link";
import { AccountNav } from "../components/AccountNav";
import { GlobalSearch } from "../components/GlobalSearch";
import { MobileNav } from "../components/MobileNav";
import "./globals.css";

export const metadata: Metadata = {
  title: "Canada Deals - Evidence-led deal discovery",
  description: "Compare Canadian online offers with visible price evidence, freshness, and safe product matching.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en-CA">
      <body>
        <a className="skip-link" href="#main-content">Skip to content</a>
        <header className="site-header"><div className="header-inner"><Link className="brand" href="/">Canada Deals</Link><GlobalSearch /><AccountNav /></div></header>
        <main id="main-content" className="page-shell">{children}</main>
        <footer><div className="header-inner">Fixture-backed foundation. No live retailer data or affiliate relationship is represented here.</div></footer>
        <MobileNav />
      </body>
    </html>
  );
}
