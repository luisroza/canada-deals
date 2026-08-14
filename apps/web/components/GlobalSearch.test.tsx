import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { GlobalSearch } from "./GlobalSearch";

const router = vi.hoisted(() => ({ push: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => router }));

afterEach(() => {
  cleanup();
  router.push.mockReset();
  vi.unstubAllGlobals();
});

describe("GlobalSearch", () => {
  it("offers accessible product and category suggestions from the existing discovery endpoint", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        items: [{ productId: "product-a", productTitle: "Northstar 55-inch QLED TV", brand: "Northstar", retailer: "Demo North", detailsPath: "/products/northstar-55-qled-tv" }],
        facets: { categories: [{ key: "electronics", label: "Electronics" }], retailers: [] },
      }),
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<GlobalSearch />);

    const input = screen.getByRole("combobox", { name: "Search products, models, or categories" });
    fireEvent.change(input, { target: { value: "northstar" } });

    expect(await screen.findByRole("listbox", { name: "Search suggestions" })).toBeVisible();
    expect(screen.getByRole("option", { name: /Northstar 55-inch QLED TV/ })).toBeVisible();
    expect(screen.getByRole("option", { name: /Electronics/ })).toBeVisible();
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/deals?search=northstar&pageSize=6", expect.objectContaining({ cache: "no-store" }));
    await waitFor(() => expect(document.querySelector(".global-search-status")).toHaveTextContent("2 suggestions available"));
  });

  it("supports keyboard selection without hiding the normal search submission", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ items: [{ productId: "a", productTitle: "Fixture TV", brand: "Fixture", retailer: "Demo", detailsPath: "/products/fixture-tv" }], facets: { categories: [], retailers: [] } }),
    }));
    render(<GlobalSearch />);
    const input = screen.getByRole("combobox");
    fireEvent.change(input, { target: { value: "fixture" } });
    await screen.findByRole("option", { name: /Fixture TV/ });
    fireEvent.keyDown(input, { key: "ArrowDown" });
    fireEvent.submit(input.closest("form")!);
    expect(router.push).toHaveBeenCalledWith("/products/fixture-tv");
  });
});
