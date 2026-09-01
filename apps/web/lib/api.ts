export type DealCard = {
  listingId: string;
  productId: string;
  productSlug: string;
  productTitle: string;
  brand: string;
  category: string;
  retailer: string;
  currentPrice: number | null;
  currency: string;
  freshnessState: "RECENT" | "AGING" | "STALE" | "UNKNOWN";
  evidenceState: "STRONG" | "PARTIAL" | "UNAVAILABLE" | "UNKNOWN";
  availabilityState: "AVAILABLE" | "UNAVAILABLE" | "UNKNOWN";
  evidenceExplanation: string;
  observedAt: string | null;
  matchState: string;
  historyState: "RELIABLE" | "PARTIAL" | "UNAVAILABLE";
  regularPrice: number | null;
  savingsAmount: number | null;
  savingsPercent: number | null;
  detailsPath: string;
  handoffPath: string | null;
  handoffUrl: string | null;
  handoffMode: "INTERNAL_REDIRECT" | "DIRECT_PROVIDER" | "NONE";
  disclosure: string;
  productImage: ProductImageData | null;
};

export type ProductImageData = { url: string; width: number; height: number };

export type RetailerOffer = {
  listingId: string;
  retailer: string;
  title: string;
  currentPrice: number | null;
  regularPrice: number | null;
  savingsAmount: number | null;
  savingsPercent: number | null;
  currency: string;
  freshnessState: string;
  evidenceState: string;
  matchState: string;
  historyState: string;
  availabilityState: "AVAILABLE" | "UNAVAILABLE" | "UNKNOWN";
  seller: string | null;
  conditionState: "NEW" | "USED" | "REFURBISHED" | "UNKNOWN";
  regionAvailabilityContext: string | null;
  shippingContext: string | null;
  observedAt: string | null;
  handoffPath: string | null;
  handoffUrl: string | null;
  handoffMode: "INTERNAL_REDIRECT" | "DIRECT_PROVIDER" | "NONE";
  disclosure: string;
};

export type ProductDetail = {
  productId: string;
  productSlug: string;
  productTitle: string;
  brand: string;
  category: string;
  variantAttributes: Record<string, string>;
  primaryOffer: RetailerOffer;
  evidenceSummary: string;
  productImage: ProductImageData | null;
};

export type ProductHistoryPoint = {
  observedDate: string;
  lowestPrice: number;
  currency: string;
  observationCount: number;
};

export type ProductHistory = {
  productId: string;
  productSlug: string;
  window: "30d" | "90d";
  windowDays: 30 | 90;
  state: "RELIABLE" | "PARTIAL" | "UNAVAILABLE";
  trackingStart: string | null;
  observationStart: string | null;
  observationEnd: string | null;
  lowestObservedPrice: number | null;
  highestObservedPrice: number | null;
  observationCount: number;
  observedDayCount: number;
  largestGapDays: number | null;
  coverageSummary: string;
  interpretation: string;
  points: ProductHistoryPoint[];
};

export type DiscoveryFacet = { key: string; label: string };

export type StoreBannerData = {
  retailerKey: string;
  displayName: string;
  title: string;
  subtitle: string;
  assetPath: string | null;
  assetSource: "CANADADEALSORIGINAL" | "MERCHANTAPPROVEDAFFILIATEASSET";
  brandAssetPolicy: "UNKNOWN" | "ALLOWED" | "DENIED";
  affiliateStatus: "ACTIVE_AFFILIATE" | "DISCOVERY_ONLY" | "DISABLED";
  href: string;
  opensNewTab: boolean;
};

export type DiscoveryResponse = {
  items: DealCard[];
  count: number;
  sort: string;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  facets: { categories: DiscoveryFacet[]; retailers: DiscoveryFacet[] };
};

export type DiscoveryParams = {
  search?: string;
  category?: string;
  retailer?: string;
  minPrice?: string;
  maxPrice?: string;
  hasReference?: string;
  freshness?: string;
  match?: string;
  availability?: string;
  sort?: string;
  page?: string;
  pageSize?: string;
};

export type SavedOffer = {
  listingId: string;
  productId: string;
  productSlug: string;
  productTitle: string;
  brand: string;
  category: string;
  currentPrice: number | null;
  regularPrice: number | null;
  savingsAmount: number | null;
  savingsPercent: number | null;
  currency: string;
  freshnessState: string;
  evidenceState: string;
  historyState: string;
  retailer: string | null;
  savedAt: string;
  detailsPath: string;
  productImage: ProductImageData | null;
};

export function resolveApiBase(configuredApiBase: string | undefined, environment: string | undefined, browserRuntime: boolean) {
  if (environment === "production" && !configuredApiBase && !browserRuntime) {
    throw new Error("API_BASE_URL is required in production.");
  }
  return configuredApiBase ?? (browserRuntime ? "" : "http://localhost:5099");
}

const apiBase = resolveApiBase(process.env.API_BASE_URL, process.env.NODE_ENV, typeof window !== "undefined");

export function publicHandoffPath(path: string) {
  return path;
}

export function publicHandoffHref(handoffPath: string | null, handoffUrl: string | null) {
  return handoffUrl ?? handoffPath;
}

export async function getDeals(params: DiscoveryParams = {}): Promise<DiscoveryResponse> {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value?.trim()) query.set(key, value.trim());
  });
  const suffix = query.size ? `?${query.toString()}` : "";
  const response = await fetch(`${apiBase}/api/v1/deals${suffix}`, { cache: "no-store" });
  if (!response.ok) throw new Error("Deals could not be loaded.");
  return response.json() as Promise<DiscoveryResponse>;
}

export async function getStoreBanners(): Promise<StoreBannerData[]> {
  const response = await fetch(`${apiBase}/api/v1/store-banners`, { cache: "no-store" });
  if (!response.ok) throw new Error("Store banners could not be loaded.");
  return response.json() as Promise<StoreBannerData[]>;
}

export async function getProduct(slug: string): Promise<ProductDetail | null> {
  const response = await fetch(`${apiBase}/api/v1/products/${encodeURIComponent(slug)}`, { cache: "no-store" });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Product could not be loaded.");
  return response.json() as Promise<ProductDetail>;
}

export async function getOffer(listingId: string): Promise<ProductDetail | null> {
  const response = await fetch(`${apiBase}/api/v1/offers/${encodeURIComponent(listingId)}`, { cache: "no-store" });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Offer could not be loaded.");
  return response.json() as Promise<ProductDetail>;
}

export async function getProductHistory(slug: string, window: "30d" | "90d"): Promise<ProductHistory | null> {
  const response = await fetch(`${apiBase}/api/v1/products/${encodeURIComponent(slug)}/history?window=${window}`, { cache: "no-store" });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error("Product history could not be loaded.");
  return response.json() as Promise<ProductHistory>;
}
