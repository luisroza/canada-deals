import type { Metadata } from "next";
import Link from "next/link";
import { DealCard } from "../components/DealCard";
import { DiscoveryControls } from "../components/DiscoveryControls";
import { getDeals, type DiscoveryParams } from "../lib/api";
import { absoluteUrl } from "../lib/seo";

type RawParams = Record<string, string | string[] | undefined>;

function normalize(raw: RawParams): DiscoveryParams {
  const value = (key: string) => typeof raw[key] === "string" ? raw[key] as string : undefined;
  return { search: value("search") ?? value("q"), category: value("category"), retailer: value("retailer"), minPrice: value("minPrice"), maxPrice: value("maxPrice"), hasReference: value("hasReference"), freshness: value("freshness"), match: value("match"), availability: value("availability"), sort: value("sort"), page: value("page"), pageSize: value("pageSize") };
}

function pageHref(params: DiscoveryParams, page: number) {
  const query = new URLSearchParams();
  Object.entries({ ...params, page: String(page) }).forEach(([key, value]) => { if (value) query.set(key, value); });
  return `/?${query.toString()}`;
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
  let error = false;
  try { result = await getDeals(params); } catch { error = true; }
  const effectiveSort = result?.sort ?? (params.search ? "relevance" : "recent");

  return <>
    <section className="hero"><p className="eyebrow">Canadian price-truth layer</p><h1>Deals with strong evidence.</h1><p className="lede">Find current CAD offers, understand when they were checked, and compare only listings we can safely identify as the same product.</p></section>
    {result && <DiscoveryControls params={params} categories={result.facets.categories} retailers={result.facets.retailers} resultCount={result.count} />}
    <div className="trust-strip" aria-label="What we show"><div className="trust-item"><strong>Current CAD price</strong><span>What the source last observed</span></div><div className="trust-item"><strong>Freshness</strong><span>When the offer was checked</span></div><div className="trust-item"><strong>Evidence</strong><span>What the available history supports</span></div><div className="trust-item"><strong>Safe matching</strong><span>Variants stay out when uncertain</span></div></div>
    <section aria-labelledby="deal-feed-heading">
      <div className="section-heading"><div><p className="eyebrow">{params.search ? `Results for “${params.search}”` : "Curated discovery feed"}</p><h2 id="deal-feed-heading">{effectiveSort === "relevance" ? "Most relevant" : effectiveSort === "savings" ? "Strongest supported savings" : effectiveSort === "price-asc" ? "Lowest current price" : "Most recently checked"}</h2></div></div>
      {error && <div className="error-state" role="alert">Deals are temporarily unavailable. Check that the API and local PostgreSQL are running.</div>}
      {!error && result?.items.length === 0 && <div className="empty-state"><h3>No products match these controls.</h3><p>Try another product name, model number, or remove one or more filters.</p><Link className="button button-secondary" href="/">Clear search and filters</Link></div>}
      {!error && result && <div className="deal-grid">{result.items.map(deal => <DealCard key={deal.productId} deal={deal} />)}</div>}
      {!error && result && result.totalPages > 1 && <nav className="pagination" aria-label="Search result pages">{result.page > 1 && <Link className="button button-secondary" href={pageHref(params, result.page - 1)}>Previous</Link>}<span>Page {result.page} of {result.totalPages}</span>{result.hasNext && <Link className="button button-secondary" href={pageHref(params, result.page + 1)}>Next</Link>}</nav>}
    </section>
  </>;
}
