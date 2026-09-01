import Link from "next/link";
import type { ProductDetail } from "../lib/api";
import { absoluteUrl } from "../lib/seo";
import { schemaAvailability } from "../lib/offerPresentation";
import { OfferConditions } from "./OfferConditions";
import { PrimaryOfferPanel } from "./PrimaryOfferPanel";
import { ProductVisual } from "./ProductVisual";
import { ReportIssueForm } from "./ReportIssueForm";
import { SaveProductButton } from "./SaveProductButton";

export function OfferDetailPage({ product, canonicalPath }: { product: ProductDetail; canonicalPath: string }) {
  const canonical = absoluteUrl(canonicalPath);
  const availability = schemaAvailability(product.primaryOffer);
  const variantAttributes = Object.entries(product.variantAttributes);
  const jsonLd = {
    "@context": "https://schema.org",
    "@type": "Product",
    name: product.productTitle,
    brand: product.brand,
    category: product.category,
    url: canonical,
    image: product.productImage ? absoluteUrl(product.productImage.url) : undefined,
    offers: product.primaryOffer.currentPrice ? {
      "@type": "Offer",
      priceCurrency: product.primaryOffer.currency,
      price: product.primaryOffer.currentPrice,
      seller: { "@type": "Organization", name: product.primaryOffer.retailer },
      url: canonical,
      ...(availability ? { availability } : {}),
    } : undefined,
  };

  return <div className="product-page">
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
    <p className="product-back-link"><Link href="/">← Back to deals</Link></p>
    <section className="product-hero">
      <div className="product-identity">
        <p className="eyebrow">{product.category} · {product.brand}</p>
        <h1>{product.productTitle}</h1>
        <p className="product-meta">Offer from <strong>{product.primaryOffer.retailer}</strong></p>
        {variantAttributes.length > 0 && <ul className="variant-list product-key-attributes">{variantAttributes.map(([key, value]) => <li key={key}><strong>{key}:</strong> {value}</li>)}</ul>}
      </div>
      <div className="product-detail-visual"><ProductVisual image={product.productImage} title={product.productTitle} category={product.category} className="product-detail-image" /></div>
      <div className="product-offer-summary">
        <PrimaryOfferPanel offer={product.primaryOffer} secondaryAction={<SaveProductButton listingId={product.primaryOffer.listingId} productTitle={product.productTitle} returnTo={canonicalPath} />} />
      </div>
    </section>
    <div className="product-layout">
      <main className="product-main">
        <section className="panel product-evidence" aria-labelledby="evidence-heading"><p className="eyebrow">Offer confidence</p><h2 id="evidence-heading">What we know about this offer</h2><p>{product.evidenceSummary}</p><p className="product-meta">This offer stands on its own. Confirm the final price, availability, seller, and product details at the store before buying.</p></section>
      </main>
      <aside className="product-sidebar" aria-label="Offer details and corrections">
        <section className="panel product-offer-details"><OfferConditions offer={product.primaryOffer} /></section>
        {variantAttributes.length > 0 && <section className="panel" aria-labelledby="details-heading"><h2 id="details-heading">Product details</h2><ul className="variant-list">{variantAttributes.map(([key, value]) => <li key={key}><strong>{key}:</strong> {value}</li>)}</ul></section>}
        <section className="panel product-report"><ReportIssueForm listingId={product.primaryOffer.listingId} listingLabel={`${product.primaryOffer.retailer}: ${product.primaryOffer.title}`} /></section>
      </aside>
    </div>
  </div>;
}
