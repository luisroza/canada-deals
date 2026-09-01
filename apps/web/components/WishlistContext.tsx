"use client";

import { usePathname } from "next/navigation";
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import type { SavedOffer } from "../lib/api";
import { getSavedOffers, getSession, saveOffer, unsaveOffer } from "../lib/account";

type WishlistContextValue = {
  authenticated: boolean | null;
  loading: boolean;
  loadError: string | null;
  items: SavedOffer[];
  count: number;
  isSaved: (listingId: string) => boolean;
  isPending: (listingId: string) => boolean;
  toggle: (listingId: string) => Promise<boolean>;
  retry: () => Promise<void>;
};

const WishlistContext = createContext<WishlistContextValue | null>(null);
const unavailableWishlist: WishlistContextValue = {
  authenticated: null,
  loading: false,
  loadError: "Wishlist context is unavailable.",
  items: [],
  count: 0,
  isSaved: () => false,
  isPending: () => false,
  toggle: async () => { throw new Error("Wishlist context is unavailable."); },
  retry: async () => {},
};

export function WishlistProvider({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const requestId = useRef(0);
  const [authenticated, setAuthenticated] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [items, setItems] = useState<SavedOffer[]>([]);
  const [savedIds, setSavedIds] = useState<Set<string>>(new Set());
  const [pendingIds, setPendingIds] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    const currentRequest = ++requestId.current;
    setLoading(true);
    setLoadError(null);
    try {
      const session = await getSession();
      if (currentRequest !== requestId.current) return;
      setAuthenticated(session.isAuthenticated);
      if (!session.isAuthenticated) {
        setItems([]);
        setSavedIds(new Set());
        return;
      }

      const savedProducts = await getSavedOffers();
      if (currentRequest !== requestId.current) return;
      setItems(savedProducts);
      setSavedIds(new Set(savedProducts.map((item) => item.listingId)));
    } catch {
      if (currentRequest !== requestId.current) return;
      setLoadError("We couldn’t load your Wishlist. Try again.");
    } finally {
      if (currentRequest === requestId.current) setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load, pathname]);

  const toggle = useCallback(async (listingId: string) => {
    if (!authenticated) throw new Error("Sign in to use your Wishlist.");
    if (pendingIds.has(listingId)) return savedIds.has(listingId);

    const wasSaved = savedIds.has(listingId);
    setPendingIds((current) => new Set(current).add(listingId));
    try {
      if (wasSaved) {
        await unsaveOffer(listingId);
        setSavedIds((current) => { const next = new Set(current); next.delete(listingId); return next; });
        setItems((current) => current.filter((item) => item.listingId !== listingId));
      } else {
        await saveOffer(listingId);
        setSavedIds((current) => new Set(current).add(listingId));
      }
      return !wasSaved;
    } finally {
      setPendingIds((current) => { const next = new Set(current); next.delete(listingId); return next; });
    }
  }, [authenticated, pendingIds, savedIds]);

  const value = useMemo<WishlistContextValue>(() => ({
    authenticated,
    loading,
    loadError,
    items,
    count: savedIds.size,
    isSaved: (listingId) => savedIds.has(listingId),
    isPending: (listingId) => pendingIds.has(listingId),
    toggle,
    retry: load,
  }), [authenticated, items, load, loadError, loading, pendingIds, savedIds, toggle]);

  return <WishlistContext.Provider value={value}>{children}</WishlistContext.Provider>;
}

export function useWishlist() {
  return useContext(WishlistContext) ?? unavailableWishlist;
}
