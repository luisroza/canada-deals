import Link from "next/link";

export default function ProductNotFound() {
  return (
    <section className="product-not-found" aria-labelledby="product-not-found-heading">
      <div className="product-not-found-art" aria-hidden="true">
        <span>?</span>
        <small>Product unavailable</small>
      </div>
      <div className="product-not-found-content">
        <p className="eyebrow">Product unavailable</p>
        <h1 id="product-not-found-heading">We couldn’t find this product</h1>
        <p className="lede">The link may be outdated, or the product may have been removed or renamed. Search by product name or model number, or continue browsing current deals.</p>
        <form className="product-not-found-search" action="/" method="get" role="search">
          <label htmlFor="missing-product-search">Search for another product</label>
          <div>
            <input id="missing-product-search" name="search" type="search" required placeholder="Try a product name or model number" />
            <button className="button button-primary" type="submit">Search deals</button>
          </div>
        </form>
        <div className="product-not-found-actions">
          <Link className="button button-secondary" href="/#deals">Browse current deals</Link>
          <Link className="button button-text" href="/saved">View Wishlist</Link>
        </div>
      </div>
    </section>
  );
}
