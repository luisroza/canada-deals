import Link from "next/link";
import type { StoreBannerData } from "../lib/api";

const fallbackAsset = "/store-banners/marketplace-packages.svg";

export function StoreBanner({ banner, mobileActive = true, imagePriority = false }: { banner: StoreBannerData; mobileActive?: boolean; imagePriority?: boolean }) {
  if (banner.affiliateStatus === "DISABLED") return null;

  const active = banner.affiliateStatus === "ACTIVE_AFFILIATE" && banner.opensNewTab && banner.href.startsWith("/go/store/");
  const href = active
    ? banner.href
    : banner.href.startsWith("/?retailer=")
      ? banner.href
      : "/#deals";
  return (
    <Link
      className={`store-banner store-banner-${active ? "affiliate" : "discovery"}`}
      data-mobile-active={mobileActive ? "true" : "false"}
      href={href}
      aria-description={active ? "Opens retailer website in a new tab." : undefined}
      target={active ? "_blank" : undefined}
      rel={active ? "noopener noreferrer sponsored" : undefined}
      prefetch={false}
    >
      <img
        className="store-banner-art"
        src={banner.assetPath || fallbackAsset}
        alt=""
        width="640"
        height="360"
        loading={imagePriority ? "eager" : "lazy"}
        fetchPriority={imagePriority ? "high" : undefined}
        decoding="async"
      />
      <span className="store-banner-shade" aria-hidden="true" />
      <span className="store-banner-content">
        <small>{active ? "Retailer website" : "Browse by store"}</small>
        <strong>{banner.title}</strong>
        <span>{banner.subtitle}</span>
        <b>{active ? <>Visit retailer <span aria-hidden="true">↗</span></> : <>See store deals <span aria-hidden="true">→</span></>}</b>
      </span>
    </Link>
  );
}
