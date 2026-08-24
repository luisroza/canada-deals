import type { Metadata } from "next";
import Link from "next/link";
import { DealCard } from "../components/DealCard";
import { DiscoveryControls } from "../components/DiscoveryControls";
import { StoreBannerRail } from "../components/StoreBannerRail";
import { getDeals, getStoreBanners, type DiscoveryParams, type StoreBannerData } from "../lib/api";
import { absoluteUrl } from "../lib/seo";

type RawParams = Record<string, string | string[] | undefined>;

function normalize(raw: RawParams): DiscoveryParams {
  const value = (key: string) => typeof raw[key] === "string" ? raw[key] as string : undefined;
  return { search: value("search") ?? value("q"), category: value("category"), retailer: value("retailer"), sort: value("sort"), page: value("page"), pageSize: value("pageSize") };
}

function pageHref(params: DiscoveryParams, page: number) {
  const query = new URLSearchParams();
  Object.entries({ ...params, page: String(page) }).forEach(([key, value]) => { if (value) query.set(key, value); });
  return `/?${query.toString()}`;
}

function sortHref(params: DiscoveryParams, sort: string) {
  const query = new URLSearchParams();
  Object.entries({ ...params, sort, page: undefined }).forEach(([key, value]) => { if (value) query.set(key, value); });
  return `/?${query.toString()}`;
}

function currentDiscoveryHref(params: DiscoveryParams) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => { if (value) query.set(key, value); });
  return query.size > 0 ? `/?${query.toString()}#deals` : "/#deals";
}

export async function generateMetadata({ searchParams }: { searchParams: Promise<RawParams> }): Promise<Metadata> {
  const params = normalize(await searchParams);
  const narrowed = Object.values(params).some(Boolean);
  const title = params.search ? `Search results for ${params.search} | Canada Deals` : "Deals with strong evidence | Canada Deals";
  const description = "Compare Canadian online offers with visible price evidence, freshness, and safe product matching.";
  return {
    title,
    description,
    alternates: { canonical: absoluteUrl("/") },
    openGraph: { title, description, type: "website", url: absoluteUrl("/"), siteName: "Canada Deals", locale: "en_CA" },
    robots: narrowed ? { index: false, follow: true } : undefined,
  };
}

export default async function Home({ searchParams }: { searchParams: Promise<RawParams> }) {
  const params = normalize(await searchParams);
  let result;
  let storeBanners: StoreBannerData[] = [];
  let error = false;
  try { [result, storeBanners] = await Promise.all([getDeals(params), getStoreBanners()]); } catch { error = true; }
  const effectiveSort = result?.sort ?? (params.search ? "relevance" : "recent");

  return <>
    <section className="hero home-hero"><p className="eyebrow">Canadian deals, checked carefully</p><h1>Find the right deal. Fast.</h1><p className="lede">Browse current CAD offers by category or store, then verify the exact product before you buy.</p></section>
    {result && <StoreBannerRail banners={storeBanners} />}
    {result && <DiscoveryControls params={params} categories={result.facets.categories} retailers={result.facets.retailers} resultCount={result.count} />}
    <section id="deals" aria-labelledby="deal-feed-heading">
      <div className="section-heading"><div><p className="eyebrow">{params.search ? `Results for “${params.search}”` : "Current offers"}</p><h2 id="deal-feed-heading">{effectiveSort === "relevance" ? "Most relevant deals" : effectiveSort === "savings" ? "Biggest verified savings" : effectiveSort === "price-asc" ? "Lowest prices" : "Recently checked deals"}</h2></div><span className="feed-count">{result?.count ?? 0} products</span></div>
      <nav className="feed-modes" aria-label="Deal feed views">
        {params.search && <Link href={sortHref(params, "relevance")} aria-current={effectiveSort === "relevance" ? "page" : undefined}>Most relevant</Link>}
        <Link href={sortHref(params, "recent")} aria-current={effectiveSort === "recent" ? "page" : undefined}>Latest</Link>
        <Link href={sortHref(params, "savings")} aria-current={effectiveSort === "savings" ? "page" : undefined}>Best savings</Link>
        <Link href={sortHref(params, "price-asc")} aria-current={effectiveSort === "price-asc" ? "page" : undefined}>Lowest price</Link>
      </nav>
      {error && <div className="error-state" role="alert">Deals are temporarily unavailable. Check that the API and local PostgreSQL are running.</div>}
      {!error && result?.items.length === 0 && <div className="empty-state"><h3>No products match this selection.</h3><p>Try another search, category, or store.</p><Link className="button button-secondary" href="/">Clear selection</Link></div>}
      {!error && result && <div className="deal-grid">{result.items.map(deal => <DealCard key={deal.productId} deal={deal} returnTo={currentDiscoveryHref(params)} />)}</div>}
      {!error && result && result.totalPages > 1 && <nav className="pagination" aria-label="Search result pages">{result.page > 1 && <Link className="button button-secondary" href={pageHref(params, result.page - 1)}>Previous</Link>}<span>Page {result.page} of {result.totalPages}</span>{result.hasNext && <Link className="button button-secondary" href={pageHref(params, result.page + 1)}>Next</Link>}</nav>}
    </section>
  </>;
}
