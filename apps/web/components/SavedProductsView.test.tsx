import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SavedProductsView } from "./SavedProductsView";

const savedProduct = {
  productId: "product-1", productSlug: "fixture-tv", productTitle: "Fixture television", brand: "Fixture Brand",
  category: "Electronics", currentPrice: 499.99, currency: "CAD", freshnessState: "RECENT", evidenceState: "STRONG",
  historyState: "RELIABLE", retailer: "Fixture Retailer", savedAt: "2026-08-11T19:30:00Z", detailsPath: "/products/fixture-tv",
};

describe("SavedProductsView", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("shows an understandable signed-out boundary", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isAuthenticated: false, email: null }) }));
    render(<SavedProductsView />);
    expect(await screen.findByRole("heading", { name: "Sign in to see saved products" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", expect.stringContaining("returnTo=%2Fsaved"));
  });

  it("shows a useful authenticated empty state", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) })
      .mockResolvedValue({ ok: true, json: async () => [] });
    vi.stubGlobal("fetch", fetchMock);
    render(<SavedProductsView />);
    expect(await screen.findByRole("heading", { name: "No saved products yet." })).toBeVisible();
    expect(screen.getByRole("link", { name: "Browse deals" })).toHaveAttribute("href", "/");
  });

  it("renders trust context and removes a persisted item", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test" }) });
      if (path === "/api/v1/saved-products") return Promise.resolve({ ok: true, json: async () => [savedProduct] });
      if (path === "/api/v1/price-alerts") return Promise.resolve({ ok: true, json: async () => [] });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      return Promise.resolve({ ok: true, status: 204, json: async () => null });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<SavedProductsView />);

    expect(await screen.findByRole("heading", { name: "Fixture television" })).toBeVisible();
    expect(screen.getByText("Evidence: strong")).toBeVisible();
    fireEvent.click(screen.getByRole("button", { name: "Remove saved product" }));
    expect(await screen.findByRole("heading", { name: "No saved products yet." })).toBeVisible();
  });

  it("announces loading errors", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 503 }));
    render(<SavedProductsView />);
    expect(await screen.findByRole("alert")).toHaveTextContent("could not be loaded");
  });

  it("shows an active target and a direct remove control", async () => {
    const activeAlert = { productId: "product-1", productSlug: "fixture-tv", productTitle: "Fixture television", targetPrice: 450, currency: "CAD", status: "ACTIVE", targetVersion: 1, consentGrantedAt: "2026-08-11T00:00:00Z", consentVersion: "target-price-email-v1", lastEvaluatedAt: null, lastTriggeredAt: null };
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) });
      if (path === "/api/v1/saved-products") return Promise.resolve({ ok: true, json: async () => [savedProduct] });
      if (path === "/api/v1/price-alerts") return Promise.resolve({ ok: true, json: async () => [activeAlert] });
      return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<SavedProductsView />);
    expect(await screen.findByText(/Target alert:/)).toHaveTextContent("$450.00");
    expect(screen.getByRole("button", { name: "Remove alert" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Edit target on product" })).toHaveAttribute("href", "/products/fixture-tv");
  });
});
