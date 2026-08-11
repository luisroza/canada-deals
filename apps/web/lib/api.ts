export type DealCard = {
  listingId: string;
  productSlug: string;
  productTitle: string;
  brand: string;
  category: string;
  retailer: string;
  currentPrice: number | null;
  currency: string;
  freshnessState: "RECENT" | "AGING" | "STALE" | "UNKNOWN";
  evidenceState: "STRONG" | "PARTIAL" | "UNAVAILABLE" | "UNKNOWN";
  evidenceExplanation: string;
  observedAt: string | null;
  matchState: string;
  historyState: "RELIABLE" | "PARTIAL" | "UNAVAILABLE";
  hasSafeComparison: boolean;
  detailsPath: string;
  handoffPath: string;
  disclosure: string;
};

export type RetailerOffer = {
  listingId: string;
  retailer: string;
  title: string;
  currentPrice: number | null;
  currency: string;
  freshnessState: string;
  evidenceState: string;
  matchState: string;
  historyState: string;
  observedAt: string | null;
  handoffPath: string;
  disclosure: string;
  isSafeComparison: boolean;
};

export type ProductDetail = {
  productSlug: string;
  productTitle: string;
  brand: string;
  category: string;
  variantAttributes: Record<string, string>;
  primaryOffer: RetailerOffer;
  safeComparisons: RetailerOffer[];
  relatedListingsForReview: RetailerOffer[];
  historySummary: string;
  evidenceSummary: string;
};

type DiscoveryResponse = { items: DealCard[]; count: number; sort: string };

const apiBase = process.env.API_BASE_URL ?? "http://localhost:5099";

export function publicHandoffPath(path: string) {
  const origin = process.env.NEXT_PUBLIC_API_ORIGIN ?? "";
  return `${origin}${path}`;
}

export async function getDeals(search?: string): Promise<DiscoveryResponse> {
  const query = search ? `?search=${encodeURIComponent(search)}` : "";
  const response = await fetch(`${apiBase}/api/v1/deals${query}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Deals could not be loaded.");
  return response.json() as Promise<DiscoveryResponse>;
}

export async function getProduct(slug: string): Promise<ProductDetail | null> {
  const response = await fetch(`${apiBase}/api/v1/products/${encodeURIComponent(slug)}`, { cache: "no-store" });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Product could not be loaded.");
  return response.json() as Promise<ProductDetail>;
}
