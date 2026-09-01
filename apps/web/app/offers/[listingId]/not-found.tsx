import Link from "next/link";

export default function OfferNotFound() {
  return <section className="product-not-found" aria-labelledby="offer-not-found-heading">
    <div className="product-not-found-art" aria-hidden="true"><span>?</span><small>Offer unavailable</small></div>
    <div className="product-not-found-content">
      <p className="eyebrow">Offer unavailable</p>
      <h1 id="offer-not-found-heading">We couldn’t find this offer</h1>
      <p className="lede">The promotion may have ended, or the store listing may no longer be available. Search for another offer or continue browsing current deals.</p>
      <form className="product-not-found-search" action="/" method="get" role="search">
        <label htmlFor="missing-offer-search">Search for another offer</label>
        <div><input id="missing-offer-search" name="search" type="search" required placeholder="Try a product name or model number" /><button className="button button-primary" type="submit">Search deals</button></div>
      </form>
      <div className="product-not-found-actions"><Link className="button button-secondary" href="/#deals">Browse current deals</Link><Link className="button button-text" href="/saved">View Wishlist</Link></div>
    </div>
  </section>;
}
