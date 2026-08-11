import type { Metadata } from "next";
import Link from "next/link";
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
        <header className="site-header"><div className="header-inner"><Link className="brand" href="/">Canada Deals</Link><nav className="site-nav" aria-label="Primary"><Link href="/">Deals</Link><Link href="/">How evidence works</Link></nav></div></header>
        <main id="main-content" className="page-shell">{children}</main>
        <footer><div className="header-inner">Fixture-backed foundation. No live retailer data or affiliate relationship is represented here.</div></footer>
      </body>
    </html>
  );
}
