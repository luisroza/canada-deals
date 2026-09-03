import type { Metadata } from "next";
import { DiscoveryExperience } from "../components/DiscoveryExperience";
import { StoreBannerRail } from "../components/StoreBannerRail";
import { getDeals, getStoreBanners, type DiscoveryParams, type DiscoveryResponse, type StoreBannerData } from "../lib/api";
import { absoluteUrl } from "../lib/seo";

type RawParams = Record<string, string | string[] | undefined>;

function normalize(raw: RawParams): DiscoveryParams {
  const value = (key: string) => typeof raw[key] === "string" ? raw[key] as string : undefined;
  return { search: value("search") ?? value("q"), category: value("category"), retailer: value("retailer"), sort: value("sort"), page: value("page"), pageSize: value("pageSize") };
}

export async function generateMetadata({ searchParams }: { searchParams: Promise<RawParams> }): Promise<Metadata> {
  const params = normalize(await searchParams);
  const narrowed = Object.values(params).some(Boolean);
  const title = params.search ? `Search results for ${params.search} | Deal North` : "Deal North";
  const description = "Discover Canadian online offers with clear deal prices, freshness, and product evidence.";
  return {
    title,
    description,
    alternates: { canonical: absoluteUrl("/") },
    openGraph: { title, description, type: "website", url: absoluteUrl("/"), siteName: "Deal North", locale: "en_CA" },
    robots: narrowed ? { index: false, follow: true } : undefined,
  };
}

export default async function Home({ searchParams }: { searchParams: Promise<RawParams> }) {
  const params = normalize(await searchParams);
  let result: DiscoveryResponse | undefined;
  let storeBanners: StoreBannerData[] = [];
  let error = false;
  try { [result, storeBanners] = await Promise.all([getDeals(params), getStoreBanners()]); } catch { error = true; }

  return <>
    <section className="hero home-hero"><p className="eyebrow">Canadian deals, checked carefully</p><h1>Find the right deal. Fast.</h1><p className="lede">Discover Canadian deals by category or store — and save your favourites for later.</p></section>
    {result && <StoreBannerRail banners={storeBanners} />}
    <DiscoveryExperience initialParams={params} initialResult={result} initialError={error} />
  </>;
}
