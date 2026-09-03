import type { Metadata } from "next";
import { SavedProductsView } from "../../components/SavedProductsView";

export const metadata: Metadata = { title: "Wishlist | Deal North", robots: { index: false, follow: false } };

export default function SavedProductsPage() {
  return <><section className="hero"><p className="eyebrow">Your shortlist</p><h1>Wishlist</h1><p className="lede">Keep individual store offers you want to revisit. We do not send price alerts or compare prices across stores.</p></section><SavedProductsView /></>;
}
