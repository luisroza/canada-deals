"use client";

import { useEffect, useRef, useState } from "react";
import type { RetailerOffer } from "../lib/api";
import { publicHandoffHref } from "../lib/api";

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
    if (!stickyOnMobile || !sourceRef.current) return;
    const updateStickyVisibility = () => {
      const sourceBounds = sourceRef.current?.getBoundingClientRect();
      // Do not cover content while the primary action is visible or still below
      // the viewport. The compact action appears only after it was scrolled past.
      setShowSticky(Boolean(sourceBounds && sourceBounds.bottom < 0));
    };
    updateStickyVisibility();
    window.addEventListener("scroll", updateStickyVisibility, { passive: true });
    window.addEventListener("resize", updateStickyVisibility);
    return () => {
      window.removeEventListener("scroll", updateStickyVisibility);
      window.removeEventListener("resize", updateStickyVisibility);
    };
  }, [stickyOnMobile]);

  const href = publicHandoffHref(offer.handoffPath, offer.handoffUrl);
  if (!href) return null;
  const rel = "sponsored noopener";

  return <>
    <a ref={sourceRef} className="button button-primary" href={href} target="_blank" rel={rel} aria-description="Opens retailer website in a new tab.">{label} <span aria-hidden="true">↗</span></a>
    {stickyOnMobile && showSticky && <div className="mobile-retailer-bar" role="region" aria-label="Retailer action">
      <a className="button button-primary" href={href} target="_blank" rel={rel} aria-description="Opens retailer website in a new tab.">Check retailer price <span aria-hidden="true">↗</span></a>
    </div>}
  </>;
}
