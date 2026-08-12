import type { Metadata } from "next";
import { SavedProductsView } from "../../components/SavedProductsView";

export const metadata: Metadata = { title: "Saved products | Canada Deals", robots: { index: false, follow: false } };

export default function SavedProductsPage() {
  return <><section className="hero"><p className="eyebrow">Your return list</p><h1>Saved products</h1><p className="lede">Revisit canonical products with current price truth, freshness, and evidence context.</p></section><SavedProductsView /></>;
}
