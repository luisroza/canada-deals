import type { StoreBannerData } from "../lib/api";
import { StoreBanner } from "./StoreBanner";

export function StoreBannerRail({ banners }: { banners: StoreBannerData[] }) {
  const visibleBanners = banners
    .filter((banner) => banner.affiliateStatus !== "DISABLED")
    .slice(0, 4);

  if (visibleBanners.length === 0) return null;

  return (
    <section className="store-banners" aria-labelledby="store-banners-heading">
      <div className="store-banners-heading">
        <div>
          <p className="eyebrow">Stores</p>
          <h2 id="store-banners-heading">Browse deals by store</h2>
        </div>
        <p>We may earn a commission when you shop through some of these links.</p>
      </div>
      <div className="store-banner-rail">
        {visibleBanners.map((banner) => <StoreBanner banner={banner} key={banner.retailerKey} />)}
      </div>
      <p className="store-banner-boundary">Affiliate status never changes store order or deal ranking. Stores without an approved destination remain inside GreatDeals.ca.</p>
    </section>
  );
}
