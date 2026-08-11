import type { Metadata } from "next";
import { DealCard } from "../components/DealCard";
import { SearchForm } from "../components/SearchForm";
import { getDeals } from "../lib/api";

export const metadata: Metadata = { title: "Deals with strong evidence | Canada Deals" };

export default async function Home({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const params = await searchParams;
  const query = params.q?.trim() ?? "";
  let result;
  let error = false;
  try { result = await getDeals(query); } catch { error = true; }

  return <>
    <section className="hero">
      <p className="eyebrow">Canadian price-truth layer</p>
      <h1>Deals with strong evidence.</h1>
      <p className="lede">Find current CAD offers, understand when they were checked, and compare only listings we can safely identify as the same product.</p>
    </section>
    <div className="trust-strip" aria-label="What we show">
      <div className="trust-item"><strong>Current CAD price</strong><span>What the fixture source observed</span></div>
      <div className="trust-item"><strong>Freshness</strong><span>When the offer was checked</span></div>
      <div className="trust-item"><strong>Evidence</strong><span>What the available history supports</span></div>
      <div className="trust-item"><strong>Safe matching</strong><span>Variants stay out when uncertain</span></div>
    </div>
    <SearchForm initialQuery={query} />
    <section aria-labelledby="deal-feed-heading">
      <div className="section-heading"><div><p className="eyebrow">{query ? `Results for “${query}”` : "Fixture discovery feed"}</p><h2 id="deal-feed-heading">Most recently checked</h2></div>{result && <span className="card-meta">{result.count} offers</span>}</div>
      {error && <div className="error-state" role="alert">Deals are temporarily unavailable. Check that the API and local PostgreSQL are running.</div>}
      {!error && result?.items.length === 0 && <div className="notice">No deals match this search. Try a product name or model number.</div>}
      {!error && result && <div className="deal-grid">{result.items.map((deal) => <DealCard key={deal.listingId} deal={deal} />)}</div>}
    </section>
  </>;
}
