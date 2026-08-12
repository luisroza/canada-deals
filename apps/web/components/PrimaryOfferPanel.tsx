import type { RetailerOffer } from "../lib/api";
import { availabilityLabel } from "../lib/offerPresentation";
import { RetailerAction } from "./RetailerAction";
import { freshnessTone, StateBadge } from "./StateBadge";

function formatPrice(price: number | null, currency: string) {
  return price === null ? "Price unavailable" : new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(price);
}

function observationCopy(offer: RetailerOffer) {
  if (!offer.observedAt) return "Observation time unavailable";
  return `Checked ${new Date(offer.observedAt).toLocaleString("en-CA")}`;
}

export function PrimaryOfferPanel({ offer }: { offer: RetailerOffer }) {
  const stale = offer.freshnessState === "STALE";

  return <section className="primary-offer" aria-labelledby="current-offer-heading">
    <p className="eyebrow">Current retailer offer</p>
    <h2 id="current-offer-heading">Current observed price</h2>
    <p className="price primary-price">{formatPrice(offer.currentPrice, offer.currency)}</p>
    <p className="product-meta"><strong>{offer.retailer}</strong> · {availabilityLabel(offer.availabilityState)} · {observationCopy(offer)}</p>
    <div className="state-row">
      <StateBadge label={offer.matchState} tone={offer.isSafeComparison ? "good" : "warning"} />
      <StateBadge label={stale ? "May be stale" : offer.freshnessState.toLowerCase()} tone={freshnessTone(offer.freshnessState)} />
    </div>
    {stale && <p className="stale-guidance"><strong>This observed price may have changed.</strong> Verify the current price and availability at the retailer before deciding.</p>}
    <RetailerAction offer={offer} stickyOnMobile />
    <p className="disclosure">{offer.disclosure}</p>
  </section>;
}
