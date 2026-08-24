"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { signOut } from "../lib/account";
import { useWishlist } from "./WishlistContext";

export function AccountNav() {
  const pathname = usePathname();
  const wishlist = useWishlist();

  async function logout() {
    await signOut();
    window.location.assign("/");
  }

  const wishlistLabel = wishlist.authenticated && wishlist.count > 0 ? `Wishlist (${wishlist.count})` : "Wishlist";
  return <nav className="site-nav" aria-label="Account"><Link className="wishlist-link" href="/saved">{wishlistLabel}</Link>{!wishlist.loading && (wishlist.authenticated ? <button className="nav-button" type="button" onClick={logout}>Sign out</button> : <Link href={`/account/sign-in?returnTo=${encodeURIComponent(pathname)}`}>Sign in</Link>)}</nav>;
}
