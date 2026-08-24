"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useWishlist } from "./WishlistContext";

const items = [
  { label: "Home", href: "/", match: (path: string) => path === "/" },
  { label: "Categories", href: "/#filters", match: () => false },
  { label: "Search", href: "/#global-search-input", match: () => false },
  { label: "Wishlist", href: "/saved", match: (path: string) => path.startsWith("/saved") },
  { label: "Account", href: "/account/sign-in", match: (path: string) => path.startsWith("/account") },
];

export function MobileNav() {
  const pathname = usePathname();
  const wishlist = useWishlist();
  return (
    <nav className="mobile-nav" aria-label="Mobile primary navigation">
      {items.map((item) => <Link key={item.label} href={item.href} aria-current={item.match(pathname) ? "page" : undefined}>{item.label}{item.label === "Wishlist" && wishlist.authenticated && wishlist.count > 0 ? <span className="mobile-wishlist-count" aria-label={`${wishlist.count} saved products`}>{wishlist.count}</span> : null}</Link>)}
    </nav>
  );
}
