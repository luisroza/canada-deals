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
      <div className="deal-card-context"><span>{deal.category}</span><span>{deal.brand}</span></div>
      <div className="deal-card-heading">
        <div>
          <h2><Link href={deal.detailsPath}>{deal.productTitle}</Link></h2>
          <p className="retailer-line">Observed at <strong>{deal.retailer}</strong></p>
        </div>
        <div className="card-price-block"><span>Current observed price</span><p className="price">{formatPrice(deal.currentPrice, deal.currency)}</p></div>
      </div>
      <div className="card-meta"><span>{availabilityLabel(deal.availabilityState)}</span><span>{deal.observedAt ? `Checked ${new Date(deal.observedAt).toLocaleString("en-CA")}` : "Observation time unavailable"}</span></div>
      {deal.supportedSavingsPercent !== null && deal.referencePrice !== null && <p className="savings-proof">
        {deal.supportedSavingsPercent}% below supported reference of {formatPrice(deal.referencePrice, deal.currency)}
      </p>}
      <div className="state-row">
        <StateBadge label={humanEvidence(deal.evidenceState)} tone={deal.evidenceState === "STRONG" ? "good" : "neutral"} />
        <StateBadge label={deal.freshnessState === "RECENT" ? "Checked recently" : deal.freshnessState === "STALE" ? "May be stale" : `Freshness: ${deal.freshnessState.toLowerCase()}`} tone={freshnessTone(deal.freshnessState)} />
      </div>
      <p className="explanation">{deal.evidenceExplanation}</p>
      <p className="match-copy">{deal.matchState}{deal.hasSafeComparison ? " · Safe comparison available" : ""}</p>
      <div className="deal-card-footer">
        <span className="observed">Evidence and conditions before retailer handoff</span>
        <Link className="button button-secondary" href={deal.detailsPath}>Inspect evidence</Link>
      </div>
    </article>
  );
}
