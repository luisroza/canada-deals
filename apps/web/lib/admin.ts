export type AdminReferenceOption = { id: string; key: string; label: string; isEnabled: boolean };
export type AdminBrand = { id: string; name: string; slug: string; isEnabled: boolean; productCount: number; publishedOfferCount: number };
export type AdminCategory = { id: string; name: string; slug: string; isEnabled: boolean; productCount: number; publishedOfferCount: number };
export type AdminRetailer = { id: string; name: string; key: string; countryCode: string; isEnabled: boolean; listingCount: number; publishedOfferCount: number; hasBannerProfile: boolean; isBannerActive: boolean; affiliateProgramCount: number };
export type AdminProductReference = {
  id: string; slug: string; title: string; brandId: string; brand: string; categoryId: string; category: string;
  modelNumber: string | null; manufacturerPartNumber: string | null; gtin: string | null; variantAttributes: Record<string, string>;
};
export type AdminPolicyOption = { id: string; sourceKey: string; priceStorage: string; priceHistory: string; affiliateLinks: string; requiredAttribution: string };
export type AdminOffer = {
  listingId: string; productId: string; slug: string; productTitle: string; brandId: string; brand: string; categoryId: string; category: string;
  modelNumber: string | null; manufacturerPartNumber: string | null; gtin: string | null; variantAttributes: Record<string, string>;
  retailerId: string; retailer: string; merchantPolicyId: string; merchantPolicy: string; externalListingId: string; retailerSku: string | null;
  originalTitle: string; productUrl: string; approvedAffiliateDestinationReference: string | null; seller: string | null; isMarketplaceSeller: boolean | null;
  affiliateTrackingUrl: string | null; affiliateProvider: string | null; affiliateHandoffMode: string | null; affiliateLinkStatus: string | null;
  affiliateLinkReadiness: string; affiliatePartnerTag: string | null; affiliateRelationshipEvidenceReference: string | null;
  conditionState: string; packQuantity: number | null; bundleContents: string | null; regionAvailabilityContext: string | null; availabilityState: string;
  shippingContext: string | null; externalIdentifiers: Record<string, string>; observedAt: string | null; fetchedAt: string | null; offerValidFrom: string | null; offerValidUntil: string | null;
  currentPrice: number | null; regularPrice: number | null; regularPriceObservedAt: string | null; regularPriceEvidenceReference: string | null;
  currency: string; matchState: string; evidenceState: string; historyState: string; isEnabled: boolean; isPubliclyEligible: boolean; readinessSummary: string; previewPath: string;
};
export type AdminBanner = {
  retailerId: string; retailerKey: string; retailer: string; profileId: string | null; title: string; subtitle: string; assetPath: string | null;
  assetSource: string; brandAssetPolicy: string; assetProvider: string | null; allowedPlacement: string | null; bannerOrder: number; isEnabled: boolean;
  assetEvidenceReference: string | null; effectiveAt: string | null; expiresAt: string | null; visibilityState: string; rightsState: string;
  isInPublicCarousel: boolean; publicPosition: number | null; publicArtworkState: string; publicEligibilityReason: string;
};
export type AdminBannerAsset = { id: string; fileName: string; contentType: string; sizeBytes: number; assetPath: string; createdAt: string };
export type AdminProductImage = {
  id: string; productId: string; productTitle: string; fileName: string; contentType: string; sizeBytes: number; width: number; height: number;
  previewPath: string; publicPath: string; origin: string; state: string; rightsEvidenceReference: string; allowedPlacements: string;
  effectiveAt: string | null; expiresAt: string | null; lastValidatedAt: string; createdAt: string; isPubliclyVisible: boolean;
};
export type AdminAudit = { id: string; action: string; entityType: string; entityId: string; summary: string; createdAt: string };
export type AdminReport = { reportId: string; listingId: string; retailer: string; listingTitle: string; reason: string; customerNote: string | null; status: string; createdAt: string; updatedAt: string };
export type AdminDashboard = {
  counts: { publishedOffers: number; draftOffers: number; enabledBanners: number; blockedOrExpiredBanners: number; openReports: number };
  brands: AdminReferenceOption[]; categories: AdminReferenceOption[]; retailers: AdminReferenceOption[]; products: AdminProductReference[]; policies: AdminPolicyOption[];
  managedBrands: AdminBrand[]; managedCategories: AdminCategory[]; managedRetailers: AdminRetailer[];
  offers: AdminOffer[]; productImages: AdminProductImage[]; bannerAssets: AdminBannerAsset[]; banners: AdminBanner[]; reports: AdminReport[]; recentAudit: AdminAudit[];
};

export type AdminOfferInput = {
  productId: string | null;
  slug: string; productTitle: string; brandId: string | null; newBrandName: string | null; newBrandSlug: string | null; confirmBrandCreation: boolean;
  categoryId: string; modelNumber: string | null; manufacturerPartNumber: string | null; gtin: string | null;
  variantAttributes: Record<string, string>; retailerId: string; merchantPolicyId: string; externalListingId: string; retailerSku: string | null;
  originalTitle: string; productUrl: string; approvedAffiliateDestinationReference: string | null; seller: string | null; isMarketplaceSeller: boolean | null;
  affiliateTrackingUrl: string | null; affiliatePartnerTag: string | null; affiliateRelationshipEvidenceReference: string | null; affiliateRelationshipConfirmed: boolean;
  conditionState: string; packQuantity: number | null; bundleContents: string | null; regionAvailabilityContext: string | null; availabilityState: string;
  shippingContext: string | null; externalIdentifiers: Record<string, string>; currentPrice: number; regularPrice: number | null;
  regularPriceObservedAt: string | null; regularPriceEvidenceReference: string | null; observedAt: string; fetchedAt: string; offerValidFrom: string | null; offerValidUntil: string | null; matchState: string;
  isEnabled: boolean; changeReason: string | null;
};

export type AdminBrandCandidate = {
  name: string; slug: string; normalizedKey: string; source: string; confidence: string; matchStatus: string;
  matchedBrandId: string | null; matchedBrandName: string | null; matchedBrandIsEnabled: boolean | null;
};

export type AdminAffiliateLinkInspection = {
  provider: string; handoffMode: string; status: string; trackingHost: string; destinationHost: string | null;
  resolvedProductUrl: string | null; externalProductId: string | null; canonicalProductUrl: string | null; partnerTag: string | null;
  matchedRetailerId: string | null; matchedRetailer: string | null; brandCandidate: AdminBrandCandidate | null; inspectedAt: string; warnings: string[];
};

export type AdminOfferCreated = { listingId: string; productId: string; brandId: string; previewPath: string };
export type AdminBrandCreated = { brandId: string };

export type AdminBannerInput = {
  title: string; subtitle: string; assetPath: string | null; assetSource: string; assetProvider: string | null; assetEvidenceReference: string | null;
  allowedPlacement: string | null; effectiveAt: string | null; expiresAt: string | null; bannerOrder: number; changeReason: string | null;
};

export class AdminApiError extends Error {
  constructor(message: string, public readonly status: number) { super(message); }
}

async function token() {
  const response = await fetch("/api/v1/account/antiforgery", { cache: "no-store", credentials: "same-origin" });
  if (!response.ok) throw new AdminApiError("Security validation could not be started.", response.status);
  return (await response.json() as { requestToken: string }).requestToken;
}

async function parseError(response: Response) {
  const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;
  const validation = problem?.errors ? Object.values(problem.errors).flat()[0] : null;
  return new AdminApiError(validation ?? problem?.detail ?? "The administrative request could not be completed.", response.status);
}

async function mutation(path: string, method: "POST" | "PUT", body: unknown) {
  const response = await fetch(path, {
    method,
    cache: "no-store",
    credentials: "same-origin",
    headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": await token() },
    body: JSON.stringify(body),
  });
  if (!response.ok) throw await parseError(response);
  return response.status === 204 ? null : response.json();
}

export async function getAdminDashboard() {
  const response = await fetch("/api/v1/admin/dashboard", { cache: "no-store", credentials: "same-origin" });
  if (!response.ok) throw await parseError(response);
  return response.json() as Promise<AdminDashboard>;
}

export function createAdminOffer(input: AdminOfferInput) { return mutation("/api/v1/admin/offers", "POST", input) as Promise<AdminOfferCreated>; }
export function updateAdminOffer(listingId: string, input: AdminOfferInput) { return mutation(`/api/v1/admin/offers/${encodeURIComponent(listingId)}`, "PUT", input); }
export function inspectAdminAffiliateLink(url: string) { return mutation("/api/v1/admin/affiliate-links/inspect", "POST", { url }) as Promise<AdminAffiliateLinkInspection>; }
export function createAdminBrand(name: string, slug: string) { return mutation("/api/v1/admin/brands", "POST", { name, slug }) as Promise<AdminBrandCreated>; }
export function updateAdminBrand(brandId: string, name: string, isEnabled: boolean, changeReason: string | null) { return mutation(`/api/v1/admin/brands/${encodeURIComponent(brandId)}`, "PUT", { name, isEnabled, changeReason }); }
export function createAdminCategory(name: string, slug: string) { return mutation("/api/v1/admin/categories", "POST", { name, slug }); }
export function updateAdminCategory(categoryId: string, name: string, isEnabled: boolean, changeReason: string | null) { return mutation(`/api/v1/admin/categories/${encodeURIComponent(categoryId)}`, "PUT", { name, isEnabled, changeReason }); }
export function createAdminRetailer(name: string, key: string) { return mutation("/api/v1/admin/retailers", "POST", { name, key }); }
export function updateAdminRetailer(retailerId: string, name: string, isEnabled: boolean, changeReason: string | null) { return mutation(`/api/v1/admin/retailers/${encodeURIComponent(retailerId)}`, "PUT", { name, isEnabled, changeReason }); }
export function updateAdminBanner(retailerId: string, input: AdminBannerInput) { return mutation(`/api/v1/admin/banners/${encodeURIComponent(retailerId)}`, "PUT", input); }
export function updateAdminBannerSelection(activeRetailerIds: string[], changeReason: string | null) {
  return mutation("/api/v1/admin/banners/selection", "PUT", { activeRetailerIds, changeReason });
}
export async function uploadAdminBannerAsset(file: File) {
  const body = new FormData();
  body.append("file", file);
  const response = await fetch("/api/v1/admin/banner-assets", {
    method: "POST",
    cache: "no-store",
    credentials: "same-origin",
    headers: { "X-CSRF-TOKEN": await token() },
    body,
  });
  if (!response.ok) throw await parseError(response);
  return response.json() as Promise<AdminBannerAsset>;
}
export async function uploadAdminProductImage(productId: string, input: { file: File; rightsEvidenceReference: string; allowedPlacements: string; effectiveAt: string | null; expiresAt: string | null; activate: boolean }) {
  const body = new FormData();
  body.append("file", input.file);
  body.append("rightsEvidenceReference", input.rightsEvidenceReference);
  body.append("allowedPlacements", input.allowedPlacements);
  if (input.effectiveAt) body.append("effectiveAt", new Date(input.effectiveAt).toISOString());
  if (input.expiresAt) body.append("expiresAt", new Date(input.expiresAt).toISOString());
  body.append("activate", String(input.activate));
  const response = await fetch(`/api/v1/admin/products/${encodeURIComponent(productId)}/images`, {
    method: "POST", cache: "no-store", credentials: "same-origin", headers: { "X-CSRF-TOKEN": await token() }, body,
  });
  if (!response.ok) throw await parseError(response);
  return response.json() as Promise<AdminProductImage>;
}
export function activateAdminProductImage(imageId: string, changeReason: string) {
  return mutation(`/api/v1/admin/product-images/${encodeURIComponent(imageId)}/activate`, "POST", { changeReason });
}
export function archiveAdminProductImage(imageId: string, changeReason: string) {
  return mutation(`/api/v1/admin/product-images/${encodeURIComponent(imageId)}/archive`, "POST", { changeReason });
}
export function updateAdminReport(reportId: string, status: string, resolutionNote: string) { return mutation(`/api/v1/admin/reports/${encodeURIComponent(reportId)}/status`, "PUT", { status, resolutionNote }); }
