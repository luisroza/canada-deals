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
  disclosure: "Demo fixture data.",
  productImage: { url: "/api/v1/product-images/image-a", width: 800, height: 800 },
};

describe("DealCard", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("uses a compact deal hierarchy and exposes the approved affiliate handoff", () => {
    render(<DealCard deal={baseDeal} />);
    expect(screen.getByText("$1,099.99")).toBeInTheDocument();
    expect(screen.getByText("$1,299.99")).toBeInTheDocument();
    expect(screen.getByText("-15%")).toBeInTheDocument();
    expect(screen.getByText("Strong evidence")).toBeInTheDocument();
    expect(screen.getByText("Recently checked")).toBeInTheDocument();
    expect(screen.getByText("Available online")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Get deal" })).toHaveAttribute("href", "/go/a");
    expect(screen.getByRole("link", { name: "View details" })).toHaveAttribute("href", baseDeal.detailsPath);
    expect(screen.getByRole("img", { name: baseDeal.productTitle })).toHaveAttribute("src", baseDeal.productImage.url);
  });

  it("renders stale evidence and a missing affiliate destination honestly", () => {
    render(<DealCard deal={{ ...baseDeal, freshnessState: "STALE", evidenceState: "UNAVAILABLE", handoffPath: null }} />);
    expect(screen.getByText("May be stale")).toBeInTheDocument();
    expect(screen.getByText("Evidence unavailable")).toBeInTheDocument();
    expect(screen.getByText("Link unavailable")).toHaveAttribute("aria-disabled", "true");
  });

  it("offers a card-level Wishlist path that preserves the current discovery context", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isAuthenticated: false, email: null }) }));
    render(<WishlistProvider><DealCard deal={baseDeal} returnTo="/?category=electronics#deals" /></WishlistProvider>);
    expect(await screen.findByRole("link", { name: /sign in to save northstar 55-inch qled tv/i })).toHaveAttribute("href", "/account/sign-in?returnTo=%2F%3Fcategory%3Delectronics%23deals");
  });
});
