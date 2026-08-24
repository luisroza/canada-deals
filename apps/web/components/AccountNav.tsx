"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useState } from "react";
import { getSession, signOut } from "../lib/account";

export function AccountNav() {
  const pathname = usePathname();
  const [authenticated, setAuthenticated] = useState(false);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    getSession().then((session) => setAuthenticated(session.isAuthenticated)).catch(() => {}).finally(() => setLoaded(true));
  }, [pathname]);

  async function logout() {
    await signOut();
    setAuthenticated(false);
    window.location.assign("/");
  }

  return <nav className="site-nav" aria-label="Account"><Link className="wishlist-link" href="/saved">Wishlist</Link>{loaded && (authenticated ? <button className="nav-button" type="button" onClick={logout}>Sign out</button> : <Link href="/account/sign-in">Sign in</Link>)}</nav>;
}
