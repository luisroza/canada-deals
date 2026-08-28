import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AdminPanel, suggestProductTitleFromRetailerUrl } from "./AdminPanel";

describe("AdminPanel", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: "Authentication required" }), {
      status: 401,
      headers: { "Content-Type": "application/json" },
    })));
  });

  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("derives a reviewable title suggestion from a descriptive Amazon Product URL", () => {
    expect(suggestProductTitleFromRetailerUrl("https://www.amazon.ca/SONGMICS-Standing-Multifunctional-Storage-ULSA055B02/dp/B08KGHSS9J/?tag=example-20"))
      .toBe("SONGMICS Standing Multifunctional Storage ULSA055B02");
    expect(suggestProductTitleFromRetailerUrl("https://amzn.to/example")).toBeNull();
  });

  it("shows a dedicated owner login without public registration", async () => {
    render(<AdminPanel />);
    expect(await screen.findByRole("heading", { name: "GreatDeals.ca Admin" })).toBeInTheDocument();
    expect(screen.getByLabelText("Email")).toHaveAttribute("autocomplete", "username");
    expect(screen.getByLabelText("Password")).toHaveAttribute("autocomplete", "current-password");
    expect(screen.queryByText(/create account/i)).not.toBeInTheDocument();
  });

  it("lets the owner select carousel banners and exposes the reviewed artwork workflow", async () => {
    const banner = (id: string, retailer: string, enabled: boolean) => ({
      retailerId: id, retailerKey: retailer.toLowerCase(), retailer, profileId: id, title: `Shop ${retailer}`, subtitle: "Current store deals",
      assetPath: "/store-banners/electronics-devices.svg", assetSource: "CANADADEALSORIGINAL", brandAssetPolicy: "UNKNOWN",
      assetProvider: null, allowedPlacement: "store_banner", bannerOrder: enabled ? 10 : 20, isEnabled: enabled,
      assetEvidenceReference: null, effectiveAt: null, expiresAt: null, visibilityState: enabled ? "ENABLED" : "DISABLED", rightsState: "READY",
      isInPublicCarousel: enabled, publicPosition: enabled ? 1 : null, publicArtworkState: "CONFIGURED",
      publicEligibilityReason: enabled ? "Active in the homepage carousel." : "Banner is inactive.",
    });
    const dashboard = {
      counts: { publishedOffers: 0, draftOffers: 0, enabledBanners: 1, blockedOrExpiredBanners: 0, openReports: 0 },
      brands: [], categories: [], retailers: [], products: [], managedBrands: [], managedCategories: [], managedRetailers: [], policies: [], offers: [], productImages: [], bannerAssets: [], reports: [], recentAudit: [],
      banners: [banner("11111111-1111-1111-1111-111111111111", "North Store", true), banner("22222222-2222-2222-2222-222222222222", "Home Store", false)],
    };
    vi.stubGlobal("fetch", vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/v1/admin/dashboard")) return Promise.resolve(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } }));
      return Promise.resolve(new Response(JSON.stringify({ requestToken: "csrf" }), { status: 200, headers: { "Content-Type": "application/json" } }));
    }));

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Banners" }));
    const selectionSummary = screen.getByLabelText("Carousel selection summary");
    const selectionMetrics = selectionSummary.querySelectorAll(":scope > div");
    expect(selectionMetrics).toHaveLength(3);
    expect(selectionMetrics[0].querySelector("dt")).toHaveTextContent("Selected");
    expect(selectionMetrics[0].querySelector("dd")).toHaveTextContent("1");
    expect(selectionMetrics[1].querySelector("dt")).toHaveTextContent("Currently public");
    expect(selectionMetrics[1].querySelector("dd")).toHaveTextContent("1");
    expect(selectionMetrics[2].querySelector("dt")).toHaveTextContent("Need attention");
    expect(selectionMetrics[2].querySelector("dd")).toHaveTextContent("0");
    expect(screen.getByText(/1 public/i)).toBeInTheDocument();
    expect(screen.getAllByLabelText("Active in homepage carousel")).toHaveLength(2);
    fireEvent.click(screen.getAllByRole("button", { name: "Edit banner" })[0]);
    expect(screen.getByRole("heading", { name: "Public preview" })).toBeInTheDocument();
    expect(screen.getByLabelText(/Artwork from reviewed library/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Upload artwork/i)).toHaveAttribute("accept", "image/png,image/jpeg,image/webp");
    expect(screen.getByLabelText(/Artwork provenance/i)).toBeInTheDocument();
    expect(screen.getByText(/Carousel membership is controlled only/i)).toBeInTheDocument();
    expect(screen.queryByLabelText("Active in homepage carousel")).not.toBeInTheDocument();
  });

  it("provides separate accessible category and store management screens", async () => {
    const dashboard = {
      counts: { publishedOffers: 3, draftOffers: 0, enabledBanners: 1, blockedOrExpiredBanners: 0, openReports: 0 },
      brands: [], categories: [], retailers: [], products: [], policies: [], offers: [], productImages: [], bannerAssets: [], banners: [], reports: [], recentAudit: [],
      managedBrands: [{ id: "brand-1", name: "NorthStar", slug: "northstar", isEnabled: true, productCount: 7, publishedOfferCount: 3 }],
      managedCategories: [{ id: "cat-1", name: "Electronics", slug: "electronics", isEnabled: true, productCount: 7, publishedOfferCount: 3 }],
      managedRetailers: [{ id: "store-1", name: "Example Store", key: "example-store", countryCode: "CA", isEnabled: true, listingCount: 5, publishedOfferCount: 3, hasBannerProfile: true, isBannerActive: true, affiliateProgramCount: 1 }],
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } })));

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Catalog" }));
    expect(screen.getByRole("heading", { name: "Categories" })).toBeInTheDocument();
    expect(screen.getByText("Electronics")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit category" }));
    expect(screen.getByDisplayValue("electronics")).toHaveAttribute("readonly");
    expect(screen.queryByRole("button", { name: /delete/i })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("tab", { name: /Brands/ }));
    expect(screen.getByRole("heading", { name: "Brands" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit brand" }));
    expect(screen.getByDisplayValue("northstar")).toHaveAttribute("readonly");

    fireEvent.click(screen.getByRole("button", { name: "Stores" }));
    expect(screen.getByRole("heading", { name: "Stores" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit store" }));
    expect(screen.getByDisplayValue("example-store")).toHaveAttribute("readonly");
    expect(screen.getByDisplayValue("Canada (CA)")).toHaveAttribute("readonly");
    expect(screen.getByText(/activation never approves data rights/i)).toBeInTheDocument();
  });

  it("guides offer entry through product reuse, essentials, validity, and advanced details", async () => {
    const dashboard = {
      counts: { publishedOffers: 1, draftOffers: 0, enabledBanners: 0, blockedOrExpiredBanners: 0, openReports: 0 },
      brands: [{ id: "brand-1", key: "northstar", label: "NorthStar", isEnabled: true }],
      categories: [{ id: "cat-1", key: "electronics", label: "Electronics", isEnabled: true }],
      retailers: [{ id: "store-1", key: "store", label: "Store", isEnabled: true }],
      products: [{ id: "product-1", slug: "quiet-headphones", title: "Quiet Headphones", brandId: "brand-1", brand: "NorthStar", categoryId: "cat-1", category: "Electronics", modelNumber: "QH-1", manufacturerPartNumber: null, gtin: null, variantAttributes: {} }],
      policies: [{ id: "policy-1", sourceKey: "controlled", priceStorage: "ALLOWED", priceHistory: "NOT_APPLICABLE", affiliateLinks: "UNKNOWN", requiredAttribution: "" }],
      managedBrands: [], managedCategories: [], managedRetailers: [], offers: [], productImages: [], bannerAssets: [], banners: [], reports: [], recentAudit: [],
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } })));

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Offers" }));
    fireEvent.click(screen.getByRole("button", { name: "Add offer" }));
    expect(screen.getByText("Create a new Product")).toBeInTheDocument();
    expect(screen.getByText("Add an offer to an existing Product")).toBeInTheDocument();
    expect(screen.getByLabelText(/Offer valid until/i)).toBeInTheDocument();
    expect(screen.getByText("Advanced Product identity and matching")).toBeInTheDocument();
    const advancedSource = screen.getByText("Advanced source and retailer details").closest("details");
    expect(advancedSource).not.toHaveAttribute("open");
    fireEvent.click(screen.getByText("Advanced source and retailer details"));
    expect(advancedSource).toHaveAttribute("open");
    expect(screen.getByLabelText(/^Provider-generated link destination/)).toBeInTheDocument();
    expect(screen.getByLabelText(/^Retailer link/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Validate and fill" })).toBeDisabled();

    fireEvent.click(screen.getByText("Add an offer to an existing Product"));
    expect(screen.getByLabelText("Existing Product")).toHaveValue("product-1");
    expect(screen.getByLabelText(/^Slug/)).toHaveAttribute("readonly");
  });

  it("fills Product identity after validating an Amazon Canada short link", async () => {
    const dashboard = {
      counts: { publishedOffers: 0, draftOffers: 0, enabledBanners: 0, blockedOrExpiredBanners: 0, openReports: 0 },
      brands: [{ id: "brand-levoit", key: "levoit", label: "Levoit", isEnabled: true }],
      categories: [{ id: "cat-home", key: "home", label: "Home", isEnabled: true }],
      retailers: [{ id: "store-amazon", key: "amazon-ca", label: "Amazon.ca", isEnabled: true }],
      products: [], policies: [{ id: "policy-amazon", sourceKey: "amazon-creators-api", priceStorage: "ALLOWED", priceHistory: "DENIED", affiliateLinks: "ALLOWED", requiredAttribution: "Amazon Associate" }],
      managedBrands: [], managedCategories: [], managedRetailers: [], offers: [], productImages: [], bannerAssets: [], banners: [], reports: [], recentAudit: [],
    };
    const inspection = {
      provider: "AMAZON_CREATORS", handoffMode: "DIRECT_PROVIDER", status: "READY", trackingHost: "amzn.to", destinationHost: "www.amazon.ca",
      resolvedProductUrl: "https://www.amazon.ca/Levoit-Smart-Humidifiers-Bedroom-Large/dp/B0DMNJNFW8?tag=canadadeal-20",
      externalProductId: "B0DMNJNFW8", canonicalProductUrl: "https://www.amazon.ca/dp/B0DMNJNFW8", partnerTag: "canadadeal-20",
      matchedRetailerId: "store-amazon", matchedRetailer: "Amazon.ca",
      brandCandidate: { name: "Levoit", slug: "levoit", normalizedKey: "levoit", source: "CATALOG_URL_PREFIX", confidence: "EXACT", matchStatus: "MATCHED_EXISTING", matchedBrandId: "brand-levoit", matchedBrandName: "Levoit", matchedBrandIsEnabled: true },
      inspectedAt: new Date().toISOString(), warnings: [],
    };
    vi.stubGlobal("fetch", vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/v1/admin/dashboard")) return Promise.resolve(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } }));
      if (url.includes("/api/v1/account/antiforgery")) return Promise.resolve(new Response(JSON.stringify({ requestToken: "csrf" }), { status: 200, headers: { "Content-Type": "application/json" } }));
      if (url.includes("/api/v1/admin/affiliate-links/inspect")) return Promise.resolve(new Response(JSON.stringify(inspection), { status: 200, headers: { "Content-Type": "application/json" } }));
      return Promise.resolve(new Response(null, { status: 404 }));
    }));

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Offers" }));
    fireEvent.click(screen.getByRole("button", { name: "Add offer" }));
    fireEvent.change(screen.getByLabelText(/^Retailer link/), { target: { value: "https://amzn.to/example" } });
    fireEvent.click(screen.getByRole("button", { name: "Validate and fill" }));

    expect(await screen.findByLabelText(/^Product title/)).toHaveValue("Levoit Smart Humidifiers Bedroom Large");
    expect(screen.getByLabelText(/^Slug/)).toHaveValue("levoit-smart-humidifiers-bedroom-large");
    expect(screen.getByLabelText(/^Brand/)).toHaveValue("brand-levoit");
    expect(screen.getByLabelText(/^Amazon Partner Tag/)).toHaveValue("canadadeal-20");
    expect(screen.getByLabelText(/^Canonical retailer Product page/)).toHaveValue("https://www.amazon.ca/dp/B0DMNJNFW8");
    expect(screen.getByText("B0DMNJNFW8")).toBeInTheDocument();
  });

  it("fills a new brand candidate but waits for explicit save confirmation before creating it", async () => {
    const dashboard = {
      counts: { publishedOffers: 0, draftOffers: 0, enabledBanners: 0, blockedOrExpiredBanners: 0, openReports: 0 },
      brands: [] as Array<{ id: string; key: string; label: string; isEnabled: boolean }>,
      categories: [{ id: "cat-home", key: "home", label: "Home", isEnabled: true }],
      retailers: [{ id: "store-amazon", key: "amazon-ca", label: "Amazon.ca", isEnabled: true }],
      products: [], policies: [{ id: "policy-amazon", sourceKey: "amazon-creators-api", priceStorage: "ALLOWED", priceHistory: "DENIED", affiliateLinks: "ALLOWED", requiredAttribution: "Amazon Associate" }],
      managedBrands: [], managedCategories: [], managedRetailers: [], offers: [], productImages: [], bannerAssets: [], banners: [], reports: [], recentAudit: [],
    };
    const inspection = {
      provider: "AMAZON_CREATORS", handoffMode: "DIRECT_PROVIDER", status: "READY", trackingHost: "amzn.to", destinationHost: "www.amazon.ca",
      resolvedProductUrl: "https://www.amazon.ca/SONGMICS-Standing-Storage/dp/B08KGHSS9J?tag=canadadeal-20",
      externalProductId: "B08KGHSS9J", canonicalProductUrl: "https://www.amazon.ca/dp/B08KGHSS9J", partnerTag: "canadadeal-20",
      matchedRetailerId: "store-amazon", matchedRetailer: "Amazon.ca",
      brandCandidate: { name: "SONGMICS", slug: "songmics", normalizedKey: "songmics", source: "URL_PATH", confidence: "LOW", matchStatus: "NEW_CANDIDATE", matchedBrandId: null, matchedBrandName: null, matchedBrandIsEnabled: null },
      inspectedAt: new Date().toISOString(), warnings: [],
    };
    const fetchMock = vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/v1/admin/dashboard")) return Promise.resolve(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } }));
      if (url.includes("/api/v1/account/antiforgery")) return Promise.resolve(new Response(JSON.stringify({ requestToken: "csrf" }), { status: 200, headers: { "Content-Type": "application/json" } }));
      if (url.includes("/api/v1/admin/affiliate-links/inspect")) return Promise.resolve(new Response(JSON.stringify(inspection), { status: 200, headers: { "Content-Type": "application/json" } }));
      return Promise.resolve(new Response(null, { status: 404 }));
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Offers" }));
    fireEvent.click(screen.getByRole("button", { name: "Add offer" }));
    expect(screen.getByLabelText(/^Brand/)).toHaveValue("");
    fireEvent.change(screen.getByLabelText(/^Retailer link/), { target: { value: "https://amzn.to/songmics" } });
    fireEvent.click(screen.getByRole("button", { name: "Validate and fill" }));

    expect(await screen.findByLabelText("New brand name")).toHaveValue("SONGMICS");
    expect(screen.getByLabelText("Brand slug")).toHaveValue("songmics");
    expect(screen.getByText(/No catalog record has been created yet/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole("checkbox", { name: /Create, activate, and use this brand/i }));
    expect(screen.getByRole("checkbox", { name: /Create, activate, and use this brand/i })).toBeChecked();
    expect(fetchMock.mock.calls.some(([input, init]) => String(input).endsWith("/api/v1/admin/brands") && (init as RequestInit | undefined)?.method === "POST")).toBe(false);
  });
});
