import Link from "next/link";
import type { DealCard as DealCardModel } from "../lib/api";
import { WishlistCardButton } from "./WishlistCardButton";
import { ProductVisual } from "./ProductVisual";

function formatPrice(price: number | null, currency: string) {
  if (price === null) return "Price unavailable";
  return new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(price);
}

function humanEvidence(state: string) {
  if (state === "STRONG") return "Strong evidence";
  if (state === "PARTIAL") return "Partial evidence";
  return "Reference unavailable";
}

function humanFreshness(state: string) {
  if (state === "RECENT") return "Checked recently";
  if (state === "AGING") return "Checked earlier";
  if (state === "STALE") return "May be stale";
  return "Check time unavailable";
}

export function DealCard({ deal, returnTo = deal.detailsPath }: { deal: DealCardModel; returnTo?: string }) {
  const handoffHref = deal.handoffUrl ?? deal.handoffPath;
  return (
    <article className="deal-card">
      <div className="deal-card-media">
        <div className="deal-card-visual"><ProductVisual image={deal.productImage} title={deal.productTitle} category={deal.category} /></div>
        <div className="deal-card-wishlist"><WishlistCardButton productId={deal.productId} productTitle={deal.productTitle} returnTo={returnTo} /></div>
      </div>
      <div className="deal-card-body">
        <p className="deal-card-retailer-name">{deal.retailer}</p>
        <h2><Link href={deal.detailsPath}>{deal.productTitle}</Link></h2>
        <div className="deal-card-price-row"><p className="price">{formatPrice(deal.currentPrice, deal.currency)}</p>{deal.currentPrice !== null && <span>{deal.currency}</span>}</div>
        {deal.supportedSavingsPercent !== null && deal.supportedSavingsPercent > 0 && <p className="deal-card-savings">{Math.round(deal.supportedSavingsPercent)}% below reference</p>}
        <p className={`deal-card-confidence${deal.freshnessState === "STALE" ? " confidence-warning" : ""}`}>{humanFreshness(deal.freshnessState)} <span aria-hidden="true">·</span> {humanEvidence(deal.evidenceState)}</p>
        <div className="deal-card-footer">
          {handoffHref ? <><a className="button button-primary" href={handoffHref} rel={deal.handoffMode === "DIRECT_PROVIDER" ? "sponsored noopener" : "sponsored"} aria-label={`Check retailer price at ${deal.retailer}`}>Check retailer price <span aria-hidden="true">↗</span></a>{deal.handoffMode === "DIRECT_PROVIDER" && <small className="deal-card-paid-link">Paid link</small>}</> : <p className="deal-card-link-unavailable">Retailer link unavailable</p>}
        </div>
      </div>
    </article>
  );
}
