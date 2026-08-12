import type { RetailerOffer } from "../lib/api";
import { publicHandoffPath } from "../lib/api";
import { freshnessTone, StateBadge } from "./StateBadge";

export function OfferCard({ offer, related = false }: { offer: RetailerOffer; related?: boolean }) {
  return (
    <article className={`offer-card${related ? " offer-related" : ""}`}>
      <div>
        <p className="eyebrow">{offer.retailer}</p>
        <h3>{offer.title}</h3>
        <p className="price">{offer.currentPrice === null ? "Price unavailable" : new Intl.NumberFormat("en-CA", { style: "currency", currency: offer.currency }).format(offer.currentPrice)}</p>
      </div>
      <div className="state-row">
        <StateBadge label={offer.matchState} tone={offer.isSafeComparison ? "good" : "warning"} />
        <StateBadge label={offer.freshnessState === "STALE" ? "May be stale" : offer.freshnessState.toLowerCase()} tone={freshnessTone(offer.freshnessState)} />
      </div>
      <p className="offer-meta">{offer.evidenceState.toLowerCase()} evidence · {offer.historyState.toLowerCase()} history</p>
      {!related && offer.handoffPath && <a className="button button-primary" href={publicHandoffPath(offer.handoffPath)}>Continue to {offer.retailer}</a>}
    </article>
  );
}
