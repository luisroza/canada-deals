"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const items = [
  { label: "Home", href: "/", match: (path: string) => path === "/" },
  { label: "Deals", href: "/#deals", match: () => false },
  { label: "Search", href: "/#global-search-input", match: () => false },
  { label: "Saved", href: "/saved", match: (path: string) => path.startsWith("/saved") },
  { label: "Account", href: "/account/sign-in", match: (path: string) => path.startsWith("/account") },
];

export function MobileNav() {
  const pathname = usePathname();
  return (
    <nav className="mobile-nav" aria-label="Mobile primary navigation">
      {items.map((item) => <Link key={item.label} href={item.href} aria-current={item.match(pathname) ? "page" : undefined}>{item.label}</Link>)}
    </nav>
  );
}
