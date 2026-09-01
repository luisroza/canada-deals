import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { DealCard as DealCardData, DiscoveryResponse } from "../lib/api";

vi.mock("./DealCard", () => ({ DealCard: ({ deal }: { deal: DealCardData }) => <article>{deal.productTitle}</article> }));

import { DiscoveryExperience } from "./DiscoveryExperience";

const categories = [{ key: "electronics", label: "Electronics" }];
const retailers = [{ key: "demo-north", label: "Demo North" }];

function deal(productId: string, productTitle: string): DealCardData {
  return {
    listingId: `listing-${productId}`, productId, productSlug: productId, productTitle, brand: "Brand", category: "Electronics", retailer: "Demo North",
    currentPrice: 10, currency: "CAD", freshnessState: "RECENT", evidenceState: "STRONG", availabilityState: "AVAILABLE",
    evidenceExplanation: "Controlled evidence", observedAt: "2026-08-28T12:00:00Z", matchState: "CONFIRMED", historyState: "UNAVAILABLE",
    regularPrice: null, savingsAmount: null, savingsPercent: null, detailsPath: `/offers/listing-${productId}`, handoffPath: null,
    handoffUrl: null, handoffMode: "NONE", disclosure: "", productImage: null,
  };
}

function response(items: DealCardData[]): DiscoveryResponse {
  return { items, count: items.length, sort: "recent", page: 1, pageSize: 24, totalPages: 1, hasNext: false, facets: { categories, retailers } };
}

describe("DiscoveryExperience", () => {
  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    window.history.replaceState(null, "", "/");
  });

  it("applies and clears filters without navigation while preserving scroll position", async () => {
    const initial = response([deal("a", "Alpha"), deal("b", "Beta")]);
    const filtered = response([deal("a", "Alpha")]);
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify(filtered), { status: 200, headers: { "Content-Type": "application/json" } }))
      .mockResolvedValueOnce(new Response(JSON.stringify(initial), { status: 200, headers: { "Content-Type": "application/json" } }));
    vi.stubGlobal("fetch", fetchMock);
    const pushState = vi.spyOn(window.history, "pushState").mockImplementation(() => undefined);
    const scrollTo = vi.spyOn(window, "scrollTo").mockImplementation(() => undefined);
    Object.defineProperty(window, "scrollX", { configurable: true, value: 0 });
    Object.defineProperty(window, "scrollY", { configurable: true, value: 640 });

    render(<DiscoveryExperience initialParams={{ sort: "recent" }} initialResult={initial} initialError={false} />);

    fireEvent.change(screen.getByLabelText("Category"), { target: { value: "electronics" } });
    fireEvent.change(screen.getByLabelText("Store"), { target: { value: "demo-north" } });
    fireEvent.click(screen.getByRole("button", { name: "Show deals" }));

    await waitFor(() => expect(screen.getAllByRole("article").map(item => item.textContent)).toEqual(["Alpha"]));
    expect(pushState).toHaveBeenLastCalledWith(null, "", "/?sort=recent&category=electronics&retailer=demo-north");
    expect(scrollTo).toHaveBeenLastCalledWith(0, 640);
    expect(screen.getByText("1").parentElement).toHaveTextContent("1 product");

    fireEvent.click(screen.getByRole("link", { name: "Clear" }));

    await waitFor(() => expect(screen.getAllByRole("article").map(item => item.textContent)).toEqual(["Alpha", "Beta"]));
    expect(screen.getByLabelText("Category")).toHaveValue("");
    expect(screen.getByLabelText("Store")).toHaveValue("");
    expect(screen.queryByRole("link", { name: "Clear" })).not.toBeInTheDocument();
    expect(pushState).toHaveBeenLastCalledWith(null, "", "/?sort=recent");
    expect(scrollTo).toHaveBeenLastCalledWith(0, 640);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
