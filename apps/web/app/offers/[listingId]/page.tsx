import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { OfferDetailPage } from "../../../components/OfferDetailPage";
import { getOffer } from "../../../lib/api";
import { absoluteUrl } from "../../../lib/seo";

export async function generateMetadata({ params }: { params: Promise<{ listingId: string }> }): Promise<Metadata> {
  const { listingId } = await params;
  const product = await getOffer(listingId);
  if (!product) return { title: "Offer not found | GreatDeals.ca", robots: { index: false, follow: false } };
  const title = `${product.productTitle} at ${product.primaryOffer.retailer} | GreatDeals.ca`;
  const canonical = absoluteUrl(`/offers/${product.primaryOffer.listingId}`);
  return {
    title,
    description: product.evidenceSummary,
    alternates: { canonical },
    openGraph: { title, description: product.evidenceSummary, type: "website", url: canonical, siteName: "GreatDeals.ca", locale: "en_CA" },
  };
}

export default async function PublicOfferPage({ params }: { params: Promise<{ listingId: string }> }) {
  const { listingId } = await params;
  const product = await getOffer(listingId);
  if (!product) notFound();
  return <OfferDetailPage product={product} canonicalPath={`/offers/${product.primaryOffer.listingId}`} />;
}
