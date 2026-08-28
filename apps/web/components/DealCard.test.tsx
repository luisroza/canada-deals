import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DealCard } from "./DealCard";
import { WishlistProvider } from "./WishlistContext";

vi.mock("next/navigation", () => ({ usePathname: () => "/" }));

const baseDeal = {
  listingId: "a",
  productId: "product-a",
  productSlug: "northstar-55-qled-tv",
  productTitle: "Northstar 55-inch QLED TV",
  brand: "Northstar Demo",
  category: "Electronics",
  retailer: "Demo North Electronics",
  currentPrice: 1099.99,
  currency: "CAD",
  freshnessState: "RECENT" as const,
  evidenceState: "STRONG" as const,
  availabilityState: "AVAILABLE" as const,
  evidenceExplanation: "Observed history is available for this permitted fixture source.",
  observedAt: "2026-08-11T12:00:00Z",
  matchState: "Same product confirmed",
  historyState: "RELIABLE" as const,
  referencePrice: 1299.99,
  supportedSavingsPercent: 15.4,
  hasSafeComparison: true,
  detailsPath: "/products/northstar-55-qled-tv",
  handoffPath: "/go/a",
  handoffUrl: null,
  handoffMode: "INTERNAL_REDIRECT" as const,
  disclosure: "Demo fixture data.",
  productImage: { url: "/api/v1/product-images/image-a", width: 800, height: 800 },
};

describe("DealCard", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("uses a clean decision hierarchy and exposes the approved retailer handoff", () => {
    render(<DealCard deal={baseDeal} />);
    expect(screen.getByText("$1,099.99")).toBeInTheDocument();
    expect(screen.getByText("CAD")).toBeInTheDocument();
    expect(screen.getByText("15% below reference")).toBeInTheDocument();
    expect(screen.getByText(/Checked recently.*Strong evidence/)).toBeInTheDocument();
    expect(screen.queryByText("$1,299.99")).not.toBeInTheDocument();
    expect(screen.queryByText("Available online")).not.toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "View details" })).not.toBeInTheDocument();
    const retailerLink = screen.getByRole("link", { name: "Check retailer price at Demo North Electronics" });
    expect(retailerLink).toHaveAttribute("href", "/go/a");
    expect(retailerLink).toHaveAttribute("rel", "sponsored");
    expect(retailerLink).not.toHaveAttribute("target");
    expect(screen.getByRole("link", { name: baseDeal.productTitle })).toHaveAttribute("href", baseDeal.detailsPath);
    expect(screen.getByRole("img", { name: baseDeal.productTitle })).toHaveAttribute("src", baseDeal.productImage.url);
  });

  it("renders stale evidence and a missing retailer destination honestly", () => {
    render(<DealCard deal={{ ...baseDeal, freshnessState: "STALE", evidenceState: "UNAVAILABLE", handoffPath: null, handoffMode: "NONE" }} />);
    expect(screen.getByText(/May be stale.*Reference unavailable/)).toBeInTheDocument();
    expect(screen.getByText("Retailer link unavailable")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /check retailer price/i })).not.toBeInTheDocument();
  });

  it("preserves an owner-provided Amazon link for direct handoff", () => {
    render(<DealCard deal={{ ...baseDeal, retailer: "Amazon.ca", handoffPath: null, handoffUrl: "https://amzn.to/example", handoffMode: "DIRECT_PROVIDER" }} />);
    const retailerLink = screen.getByRole("link", { name: "Check retailer price at Amazon.ca" });
    expect(retailerLink).toHaveAttribute("href", "https://amzn.to/example");
    expect(retailerLink).toHaveAttribute("rel", "sponsored noopener");
    expect(screen.getByText("Paid link")).toBeInTheDocument();
  });

  it("offers a card-level Wishlist path that preserves the current discovery context", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isAuthenticated: false, email: null }) }));
    render(<WishlistProvider><DealCard deal={baseDeal} returnTo="/?category=electronics#deals" /></WishlistProvider>);
    expect(await screen.findByRole("link", { name: "Save Northstar 55-inch QLED TV to your Wishlist — sign in required" })).toHaveAttribute("href", "/account/sign-in?returnTo=%2F%3Fcategory%3Delectronics%23deals");
  });
});
