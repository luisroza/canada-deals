import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { OfferCard } from "../../../components/OfferCard";
import { OfferConditions } from "../../../components/OfferConditions";
import { PrimaryOfferPanel } from "../../../components/PrimaryOfferPanel";
import { ReportIssueForm } from "../../../components/ReportIssueForm";
import { SaveProductButton } from "../../../components/SaveProductButton";
import { ProductVisual } from "../../../components/ProductVisual";
import { getProduct } from "../../../lib/api";
import { schemaAvailability } from "../../../lib/offerPresentation";
import { absoluteUrl } from "../../../lib/seo";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const product = await getProduct(slug);
  if (!product) return { title: "Product not found | Canada Deals", robots: { index: false, follow: false } };
  const title = `${product.productTitle} | Canada Deals`;
  const canonical = absoluteUrl(`/products/${product.productSlug}`);
  return {
    title,
    description: product.evidenceSummary,
    alternates: { canonical },
    openGraph: { title, description: product.evidenceSummary, type: "website", url: canonical, siteName: "Canada Deals", locale: "en_CA" },
  };
}

export default async function ProductPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const product = await getProduct(slug);
  if (!product) notFound();

  const canonical = absoluteUrl(`/products/${product.productSlug}`);
  const availability = schemaAvailability(product.primaryOffer);
  const variantAttributes = Object.entries(product.variantAttributes);
  const jsonLd = { "@context": "https://schema.org", "@type": "Product", name: product.productTitle, brand: product.brand, category: product.category, url: canonical, image: product.productImage ? absoluteUrl(product.productImage.url) : undefined, offers: product.primaryOffer.currentPrice ? { "@type": "Offer", priceCurrency: product.primaryOffer.currency, price: product.primaryOffer.currentPrice, url: canonical, ...(availability ? { availability } : {}) } : undefined };

  return <div className="product-page">
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
    <p className="product-back-link"><Link href="/">← Back to deals</Link></p>
    <section className="product-hero">
      <div className="product-identity">
        <p className="eyebrow">{product.category} · {product.brand}</p>
        <h1>{product.productTitle}</h1>
        {variantAttributes.length > 0 && <ul className="variant-list product-key-attributes">{variantAttributes.map(([key, value]) => <li key={key}><strong>{key}:</strong> {value}</li>)}</ul>}
      </div>
      <div className="product-detail-visual"><ProductVisual image={product.productImage} title={product.productTitle} category={product.category} className="product-detail-image" /></div>
      <div className="product-offer-summary">
        <PrimaryOfferPanel offer={product.primaryOffer} secondaryAction={<SaveProductButton productId={product.productId} productTitle={product.productTitle} returnTo={`/products/${product.productSlug}`} />} />
      </div>
    </section>
    <div className="product-layout">
      <main className="product-main">
        <section className="panel product-evidence" aria-labelledby="evidence-heading"><p className="eyebrow">Confidence</p><h2 id="evidence-heading">What we know about this offer</h2><p>{product.evidenceSummary}</p><p className="product-meta">Use the visible check time and confirm the final price, availability, and product details at the retailer.</p></section>
        <section className="panel" aria-labelledby="comparison-heading"><p className="eyebrow">Same-product matches</p><h2 id="comparison-heading">Compare retailer offers</h2><p className="product-meta">Only confirmed same-product matches appear here. Different variants, bundles, sellers, or conditions stay outside this comparison.</p>{product.safeComparisons.length > 0 ? <div className="offer-stack"><OfferCard offer={product.primaryOffer} />{product.safeComparisons.map((offer) => <OfferCard key={offer.listingId} offer={offer} />)}</div> : <div className="notice"><strong>No other confirmed retailer offer.</strong><span> We found no additional listing that we can confidently identify as the same product.</span></div>}</section>
        {product.relatedListingsForReview.length > 0 && <section className="panel" aria-labelledby="related-heading"><h2 id="related-heading">Possible related listings</h2><p>These may differ by model, size, bundle, seller, or condition. They are not included in the primary comparison.</p><div className="offer-stack">{product.relatedListingsForReview.map((offer) => <OfferCard key={offer.listingId} offer={offer} related />)}</div></section>}
      </main>
      <aside className="product-sidebar" aria-label="Offer details and corrections">
        <section className="panel product-offer-details"><OfferConditions offer={product.primaryOffer} /></section>
        {variantAttributes.length > 0 && <section className="panel" aria-labelledby="details-heading"><h2 id="details-heading">Product details</h2><ul className="variant-list">{variantAttributes.map(([key, value]) => <li key={key}><strong>{key}:</strong> {value}</li>)}</ul></section>}
        <section className="panel product-report"><ReportIssueForm listingId={product.primaryOffer.listingId} listingLabel={`${product.primaryOffer.retailer}: ${product.primaryOffer.title}`} /></section>
      </aside>
    </div>
  </div>;
}
