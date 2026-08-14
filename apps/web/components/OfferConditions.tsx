import type { RetailerOffer } from "../lib/api";
import { availabilityLabel, conditionLabel, sellerLabel } from "../lib/offerPresentation";

function checkedLabel(observedAt: string | null) {
  return observedAt
    ? new Date(observedAt).toLocaleString("en-CA")
    : "Observation time unavailable";
}

export function OfferConditions({ offer }: { offer: RetailerOffer }) {
  const headingId = `offer-conditions-${offer.listingId}`;

  return <section className="offer-conditions" aria-labelledby={headingId}>
    <h3 id={headingId}>Offer conditions</h3>
    <dl>
      <div><dt>Seller</dt><dd>{sellerLabel(offer.seller, offer.retailer)}</dd></div>
      <div><dt>Availability</dt><dd>{availabilityLabel(offer.availabilityState)}</dd></div>
      <div><dt>Item condition</dt><dd>{conditionLabel(offer.conditionState)}</dd></div>
      <div><dt>Region</dt><dd>{offer.regionAvailabilityContext ?? "Region not provided by source"}</dd></div>
      <div><dt>Shipping</dt><dd>{offer.shippingContext ?? "Shipping terms not provided by source"}</dd></div>
      <div><dt>Last checked</dt><dd>{checkedLabel(offer.observedAt)}</dd></div>
    </dl>
    <p className="condition-boundary"><strong>Coupon and eligibility:</strong> No verified requirement was supplied by this source. Confirm the final price and any membership, payment, or coupon conditions at the retailer.</p>
    <p className="condition-boundary"><strong>Offer expiry:</strong> No verified expiry was supplied. Freshness reflects our latest observation, not a retailer guarantee.</p>
  </section>;
}
