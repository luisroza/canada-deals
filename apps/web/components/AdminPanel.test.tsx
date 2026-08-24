import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AdminPanel } from "./AdminPanel";

describe("AdminPanel", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify({ detail: "Authentication required" }), {
      status: 401,
      headers: { "Content-Type": "application/json" },
    })));
  });

  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

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
      brands: [], categories: [], retailers: [], managedCategories: [], managedRetailers: [], policies: [], offers: [], bannerAssets: [], reports: [], recentAudit: [],
      banners: [banner("11111111-1111-1111-1111-111111111111", "North Store", true), banner("22222222-2222-2222-2222-222222222222", "Home Store", false)],
    };
    vi.stubGlobal("fetch", vi.fn().mockImplementation((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/api/v1/admin/dashboard")) return Promise.resolve(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } }));
      return Promise.resolve(new Response(JSON.stringify({ requestToken: "csrf" }), { status: 200, headers: { "Content-Type": "application/json" } }));
    }));

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Banners" }));
    expect(screen.getByText(/1 public/i)).toBeInTheDocument();
    expect(screen.getAllByLabelText("Active in homepage carousel")).toHaveLength(2);
    fireEvent.click(screen.getAllByRole("button", { name: "Edit banner" })[0]);
    expect(screen.getByRole("heading", { name: "Public preview" })).toBeInTheDocument();
    expect(screen.getByLabelText(/Artwork from reviewed library/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Upload artwork/i)).toHaveAttribute("accept", "image/png,image/jpeg,image/webp");
    expect(screen.getByLabelText(/Artwork provenance/i)).toBeInTheDocument();
  });

  it("provides separate accessible category and store management screens", async () => {
    const dashboard = {
      counts: { publishedOffers: 3, draftOffers: 0, enabledBanners: 1, blockedOrExpiredBanners: 0, openReports: 0 },
      brands: [], categories: [], retailers: [], policies: [], offers: [], bannerAssets: [], banners: [], reports: [], recentAudit: [],
      managedCategories: [{ id: "cat-1", name: "Electronics", slug: "electronics", isEnabled: true, productCount: 7, publishedOfferCount: 3 }],
      managedRetailers: [{ id: "store-1", name: "Example Store", key: "example-store", countryCode: "CA", isEnabled: true, listingCount: 5, publishedOfferCount: 3, hasBannerProfile: true, isBannerActive: true, affiliateProgramCount: 1 }],
    };
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response(JSON.stringify(dashboard), { status: 200, headers: { "Content-Type": "application/json" } })));

    render(<AdminPanel />);
    fireEvent.click(await screen.findByRole("button", { name: "Categories" }));
    expect(screen.getByRole("heading", { name: "Categories" })).toBeInTheDocument();
    expect(screen.getByText("Electronics")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit category" }));
    expect(screen.getByDisplayValue("electronics")).toHaveAttribute("readonly");
    expect(screen.queryByRole("button", { name: /delete/i })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Stores" }));
    expect(screen.getByRole("heading", { name: "Stores" })).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Edit store" }));
    expect(screen.getByDisplayValue("example-store")).toHaveAttribute("readonly");
    expect(screen.getByDisplayValue("Canada (CA)")).toHaveAttribute("readonly");
    expect(screen.getByText(/activation never approves data rights/i)).toBeInTheDocument();
  });
});
