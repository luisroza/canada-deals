import type { MetadataRoute } from "next";
import { getDeals } from "../lib/api";
import { absoluteUrl } from "../lib/seo";

export const dynamic = "force-dynamic";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const routes: MetadataRoute.Sitemap = [{ url: absoluteUrl("/"), changeFrequency: "hourly", priority: 1 }];
  try {
    const deals = await getDeals({ pageSize: "48" });
    routes.push(...deals.items.map((deal) => ({
      url: absoluteUrl(deal.detailsPath),
      changeFrequency: "daily" as const,
      priority: 0.8,
    })));
  } catch {
    // Keep the homepage discoverable if the catalog is temporarily unavailable.
  }
  return routes;
}
