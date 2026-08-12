import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { OfferCard } from "../../../components/OfferCard";
import { PriceHistoryEvidence, PriceHistoryEvidenceLoading } from "../../../components/PriceHistoryEvidence";
import { ReportIssueForm } from "../../../components/ReportIssueForm";
import { SaveProductButton } from "../../../components/SaveProductButton";
import { TargetPriceAlertControl } from "../../../components/TargetPriceAlertControl";
import { getProduct, getProductHistory } from "../../../lib/api";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const product = await getProduct(slug);
  return product ? { title: `${product.productTitle} | Canada Deals`, description: product.evidenceSummary } : { title: "Product not found | Canada Deals" };
}

async function ProductHistorySection({ slug, requestedHistory, currentPrice, currentFreshness }: { slug: string; requestedHistory: "30d" | "90d"; currentPrice: number | null; currentFreshness: string }) {
  let history = null;
  let historyError = false;
  try { history = await getProductHistory(slug, requestedHistory); } catch { historyError = true; }
  return <PriceHistoryEvidence history={history} productSlug={slug} currentPrice={currentPrice} currentFreshness={currentFreshness} error={historyError} />;
}

export default async function ProductPage({ params, searchParams }: { params: Promise<{ slug: string }>; searchParams: Promise<{ history?: string }> }) {
  const { slug } = await params;
  const requestedHistory = (await searchParams).history === "90d" ? "90d" : "30d";
  const product = await getProduct(slug);
  if (!product) notFound();

  const jsonLd = { "@context": "https://schema.org", "@type": "Product", name: product.productTitle, brand: product.brand, category: product.category, offers: product.primaryOffer.currentPrice ? { "@type": "Offer", priceCurrency: product.primaryOffer.currency, price: product.primaryOffer.currentPrice, availability: "https://schema.org/InStock" } : undefined };

  return <>
    <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }} />
    <p><Link href="/">← Back to deals</Link></p>
    <section className="product-hero"><p className="eyebrow">{product.category} · {product.brand}</p><h1>{product.productTitle}</h1><p className="product-meta">Verify the exact variant before comparing. This page makes evidence and unknowns visible.</p><SaveProductButton productId={product.productId} productTitle={product.productTitle} returnTo={`/products/${product.productSlug}`} /><TargetPriceAlertControl productId={product.productId} productTitle={product.productTitle} currentPrice={product.primaryOffer.currentPrice} currency={product.primaryOffer.currency} returnTo={`/products/${product.productSlug}`} /></section>
    <div className="product-layout">
      <div>
        <section className="panel" aria-labelledby="evidence-heading"><h2 id="evidence-heading">Price evidence</h2><p>{product.evidenceSummary}</p><p>{product.historySummary}</p></section>
        <Suspense fallback={<PriceHistoryEvidenceLoading productSlug={product.productSlug} selected={requestedHistory} currentPrice={product.primaryOffer.currentPrice} currentFreshness={product.primaryOffer.freshnessState} />}>
          <ProductHistorySection slug={product.productSlug} requestedHistory={requestedHistory} currentPrice={product.primaryOffer.currentPrice} currentFreshness={product.primaryOffer.freshnessState} />
        </Suspense>
        <section className="panel" aria-labelledby="comparison-heading"><h2 id="comparison-heading">Safe retailer comparison</h2><p className="product-meta">Only confirmed same-product matches appear here. A lower price is not useful if the variant is different.</p><div className="offer-stack"><OfferCard offer={product.primaryOffer} />{product.safeComparisons.map((offer) => <OfferCard key={offer.listingId} offer={offer} />)}</div>{product.safeComparisons.length === 0 && <div className="notice">No safe comparison available. We found no other listing we can confidently identify as the same product.</div>}</section>
        {product.relatedListingsForReview.length > 0 && <section className="panel" aria-labelledby="related-heading"><h2 id="related-heading">Possible related listings</h2><p>These may differ by model, size, bundle, seller, or condition. They are not included in the primary comparison.</p><div className="offer-stack">{product.relatedListingsForReview.map((offer) => <OfferCard key={offer.listingId} offer={offer} related />)}</div></section>}
      </div>
      <aside className="panel" aria-labelledby="details-heading"><h2 id="details-heading">Product details</h2><ul className="variant-list">{Object.entries(product.variantAttributes).map(([key, value]) => <li key={key}><strong>{key}:</strong> {value}</li>)}</ul><p className="disclosure">{product.primaryOffer.disclosure}</p><ReportIssueForm listingId={product.primaryOffer.listingId} listingLabel={`${product.primaryOffer.retailer}: ${product.primaryOffer.title}`} /></aside>
    </div>
  </>;
}
