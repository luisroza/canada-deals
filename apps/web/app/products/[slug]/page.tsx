import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { redirect } from "next/navigation";
import { getProduct } from "../../../lib/api";
import { absoluteUrl } from "../../../lib/seo";

export async function generateMetadata({ params }: { params: Promise<{ slug: string }> }): Promise<Metadata> {
  const { slug } = await params;
  const product = await getProduct(slug);
  if (!product) return { title: "Product not found | GreatDeals.ca", robots: { index: false, follow: false } };
  const title = `${product.productTitle} | GreatDeals.ca`;
  const canonical = absoluteUrl(`/offers/${product.primaryOffer.listingId}`);
  return {
    title,
    description: product.evidenceSummary,
    alternates: { canonical },
    openGraph: { title, description: product.evidenceSummary, type: "website", url: canonical, siteName: "GreatDeals.ca", locale: "en_CA" },
  };
}

export default async function ProductPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const product = await getProduct(slug);
  if (!product) notFound();

  redirect(`/offers/${product.primaryOffer.listingId}`);
}
