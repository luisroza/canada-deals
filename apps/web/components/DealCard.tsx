import Link from "next/link";
import type { DealCard as DealCardModel } from "../lib/api";
import { availabilityLabel } from "../lib/offerPresentation";
import { freshnessTone, StateBadge } from "./StateBadge";

function formatPrice(price: number | null, currency: string) {
  if (price === null) return "Price unavailable";
  return new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(price);
}

function humanEvidence(state: string) {
  if (state === "STRONG") return "Strong evidence";
  if (state === "PARTIAL") return "Partial evidence";
  return "Evidence unavailable";
}

export function DealCard({ deal }: { deal: DealCardModel }) {
  return (
    <article className="deal-card">
      <div className="deal-card-retailer"><span className="retailer-avatar" aria-hidden="true">{deal.retailer.slice(0, 1)}</span><strong>{deal.retailer}</strong><span>{deal.observedAt ? new Date(deal.observedAt).toLocaleDateString("en-CA", { month: "short", day: "numeric" }) : "Date unavailable"}</span></div>
      <Link className="deal-card-visual" href={deal.detailsPath} aria-label={`View ${deal.productTitle}`}>
        <span className="product-visual-mark" aria-hidden="true">{deal.category.slice(0, 2).toUpperCase()}</span>
        <span className="product-visual-copy">{deal.category}<small>Image shown when retailer rights permit</small></span>
        {deal.supportedSavingsPercent !== null && <strong className="discount-badge">-{Math.round(deal.supportedSavingsPercent)}%</strong>}
      </Link>
      <div className="deal-card-body">
        <p className="deal-card-context">{deal.brand} · {deal.category}</p>
        <h2><Link href={deal.detailsPath}>{deal.productTitle}</Link></h2>
        <div className="card-price-block">
          {deal.referencePrice !== null && <span className="reference-price">{formatPrice(deal.referencePrice, deal.currency)}</span>}
          <p className="price">{formatPrice(deal.currentPrice, deal.currency)}</p>
        </div>
        <div className="card-meta"><span>{availabilityLabel(deal.availabilityState)}</span><span>{deal.observedAt ? `Checked ${new Date(deal.observedAt).toLocaleString("en-CA", { dateStyle: "medium", timeStyle: "short" })}` : "Check time unavailable"}</span></div>
        <div className="state-row compact-states">
          <StateBadge label={humanEvidence(deal.evidenceState)} tone={deal.evidenceState === "STRONG" ? "good" : "neutral"} />
          <StateBadge label={deal.freshnessState === "RECENT" ? "Recently checked" : deal.freshnessState === "STALE" ? "May be stale" : deal.freshnessState.toLowerCase()} tone={freshnessTone(deal.freshnessState)} />
        </div>
        <div className="deal-card-footer">
          <Link className="card-details-link" href={deal.detailsPath}>View details</Link>
          {deal.handoffPath ? <a className="button button-primary" href={deal.handoffPath} rel="sponsored">Get deal</a> : <span className="button button-unavailable" aria-disabled="true">Link unavailable</span>}
        </div>
      </div>
    </article>
  );
}
