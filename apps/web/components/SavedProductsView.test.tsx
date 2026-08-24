import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SavedProductsView } from "./SavedProductsView";
import { WishlistProvider } from "./WishlistContext";

vi.mock("next/navigation", () => ({ usePathname: () => "/saved" }));

const savedProduct = {
  productId: "product-1", productSlug: "fixture-tv", productTitle: "Fixture television", brand: "Fixture Brand",
  category: "Electronics", currentPrice: 499.99, currency: "CAD", freshnessState: "RECENT", evidenceState: "STRONG",
  historyState: "RELIABLE", retailer: "Fixture Retailer", savedAt: "2026-08-11T19:30:00Z", detailsPath: "/products/fixture-tv",
};
const secondSavedProduct = {
  ...savedProduct,
  productId: "product-2",
  productSlug: "fixture-drill",
  productTitle: "Fixture cordless drill",
  category: "Tools",
  currentPrice: 129.99,
  retailer: "Demo Tool Store",
  savedAt: "2026-08-10T19:30:00Z",
  detailsPath: "/products/fixture-drill",
};

describe("SavedProductsView", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  function renderView() {
    return render(<WishlistProvider><SavedProductsView /></WishlistProvider>);
  }

  it("shows an understandable signed-out wishlist boundary", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isAuthenticated: false, email: null }) }));
    renderView();
    expect(await screen.findByRole("heading", { name: "Sign in to see your wishlist" })).toBeVisible();
    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", expect.stringContaining("returnTo=%2Fsaved"));
  });

  it("shows a useful authenticated empty state", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) })
      .mockResolvedValue({ ok: true, json: async () => [] });
    vi.stubGlobal("fetch", fetchMock);
    renderView();
    expect(await screen.findByRole("heading", { name: "Your wishlist is empty." })).toBeVisible();
  });

  it("loads only saved products and removes an item from the wishlist", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test" }) });
      if (path === "/api/v1/saved-products") return Promise.resolve({ ok: true, json: async () => [savedProduct] });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      return Promise.resolve({ ok: true, status: 204, json: async () => null });
    });
    vi.stubGlobal("fetch", fetchMock);
    renderView();

    expect(await screen.findByRole("heading", { name: "Fixture television" })).toBeVisible();
    expect(fetchMock).not.toHaveBeenCalledWith("/api/v1/price-alerts", expect.anything());
    fireEvent.click(screen.getByRole("button", { name: "Remove from wishlist" }));
    expect(await screen.findByRole("heading", { name: "Your wishlist is empty." })).toBeVisible();
  });

  it("searches, filters, and sorts the Wishlist without another API request", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test" }) });
      return Promise.resolve({ ok: true, json: async () => [savedProduct, secondSavedProduct] });
    });
    vi.stubGlobal("fetch", fetchMock);
    const { container } = renderView();

    expect(await screen.findByText("2 of 2 saved products")).toBeVisible();
    fireEvent.change(screen.getByLabelText("Search your Wishlist"), { target: { value: "drill" } });
    expect(screen.getByRole("heading", { name: "Fixture cordless drill" })).toBeVisible();
    expect(screen.queryByRole("heading", { name: "Fixture television" })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Clear" }));
    fireEvent.change(screen.getByLabelText("Sort by"), { target: { value: "price-asc" } });
    const titles = [...container.querySelectorAll(".saved-product-card h2")].map((heading) => heading.textContent);
    expect(titles).toEqual(["Fixture cordless drill", "Fixture television"]);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("announces loading errors", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 503 }));
    renderView();
    expect(await screen.findByRole("alert")).toHaveTextContent("couldn’t load your Wishlist");
  });
});
