import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { OfferCard } from "../../../components/OfferCard";
import { PrimaryOfferPanel } from "../../../components/PrimaryOfferPanel";
import { ReportIssueForm } from "../../../components/ReportIssueForm";
import { SaveProductButton } from "../../../components/SaveProductButton";
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
  const jsonLd = { "@context": "https://schema.org", "@type": "Product", name: product.productTitle, brand: product.brand, category: product.category, url: canonical, offers: product.primaryOffer.currentPrice ? { "@type": "Offer", priceCurrency: product.primaryOffer.currency, price: product.primaryOffer.currentPrice, url: canonical, ...(availability ? { availability } : {}) } : undefined };

  return <div className="product-page">
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
    <p><Link href="/">← Back to deals</Link></p>
    <section className="product-hero"><p className="eyebrow">{product.category} · {product.brand}</p><h1>{product.productTitle}</h1><p className="product-meta">Verify the exact product and current offer before buying.</p><PrimaryOfferPanel offer={product.primaryOffer} /><div className="retention-actions wishlist-only"><SaveProductButton productId={product.productId} productTitle={product.productTitle} returnTo={`/products/${product.productSlug}`} /></div></section>
    <div className="product-layout">
      <div>
        <section className="panel" aria-labelledby="evidence-heading"><h2 id="evidence-heading">Offer confidence</h2><p>{product.evidenceSummary}</p><p>Price tracking and target-price alerts are not part of the current product. Use the visible check time and verify the final price at the retailer.</p></section>
        <section className="panel" aria-labelledby="comparison-heading"><h2 id="comparison-heading">Safe retailer comparison</h2><p className="product-meta">Only confirmed same-product matches appear here. A lower price is not useful if the variant is different.</p><div className="offer-stack"><OfferCard offer={product.primaryOffer} />{product.safeComparisons.map((offer) => <OfferCard key={offer.listingId} offer={offer} />)}</div>{product.safeComparisons.length === 0 && <div className="notice">No safe comparison available. We found no other listing we can confidently identify as the same product.</div>}</section>
        {product.relatedListingsForReview.length > 0 && <section className="panel" aria-labelledby="related-heading"><h2 id="related-heading">Possible related listings</h2><p>These may differ by model, size, bundle, seller, or condition. They are not included in the primary comparison.</p><div className="offer-stack">{product.relatedListingsForReview.map((offer) => <OfferCard key={offer.listingId} offer={offer} related />)}</div></section>}
      </div>
      <aside className="panel" aria-labelledby="details-heading"><h2 id="details-heading">Product details</h2><ul className="variant-list">{Object.entries(product.variantAttributes).map(([key, value]) => <li key={key}><strong>{key}:</strong> {value}</li>)}</ul><p className="disclosure">{product.primaryOffer.disclosure}</p><ReportIssueForm listingId={product.primaryOffer.listingId} listingLabel={`${product.primaryOffer.retailer}: ${product.primaryOffer.title}`} /></aside>
    </div>
  </div>;
}
