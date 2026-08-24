"use client";

import { useEffect, useRef, useState } from "react";
import type { RetailerOffer } from "../lib/api";
import { publicHandoffPath } from "../lib/api";

export function retailerActionLabel(offer: Pick<RetailerOffer, "freshnessState" | "retailer">) {
  return offer.freshnessState === "STALE"
    ? `Check current price at ${offer.retailer}`
    : `Continue to ${offer.retailer}`;
}

export function RetailerAction({ offer, stickyOnMobile = false }: { offer: RetailerOffer; stickyOnMobile?: boolean }) {
  const sourceRef = useRef<HTMLAnchorElement>(null);
  const [showSticky, setShowSticky] = useState(false);
  const label = retailerActionLabel(offer);

  useEffect(() => {
    if (!stickyOnMobile || !sourceRef.current || typeof IntersectionObserver === "undefined") return;
    const observer = new IntersectionObserver(([entry]) => setShowSticky(!entry.isIntersecting), { threshold: 0.15 });
    observer.observe(sourceRef.current);
    return () => observer.disconnect();
  }, [stickyOnMobile]);

  if (!offer.handoffPath) return null;
  const href = publicHandoffPath(offer.handoffPath);

  return <>
    <a ref={sourceRef} className="button button-primary" href={href}>{label}</a>
    {stickyOnMobile && showSticky && <div className="mobile-retailer-bar" role="region" aria-label="Retailer action">
      <a className="button button-primary" href={href}>Check retailer price</a>
    </div>}
  </>;
}
