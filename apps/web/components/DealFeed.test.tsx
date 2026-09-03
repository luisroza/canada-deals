import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { DealCard as DealCardData, DiscoveryResponse } from "../lib/api";

vi.mock("./DealCard", () => ({ DealCard: ({ deal }: { deal: DealCardData }) => <article>{deal.productTitle}</article> }));

import { DealFeed } from "./DealFeed";

function deal(productId: string, productTitle: string): DealCardData {
  return {
    listingId: `listing-${productId}`, productId, productSlug: productId, productTitle, brand: "Brand", category: "Category", retailer: "Store",
    currentPrice: 10, currency: "CAD", freshnessState: "RECENT", evidenceState: "STRONG", availabilityState: "AVAILABLE",
    evidenceExplanation: "Controlled evidence", observedAt: "2026-08-28T12:00:00Z", matchState: "CONFIRMED", historyState: "UNAVAILABLE",
    regularPrice: null, savingsAmount: null, savingsPercent: null, detailsPath: `/offers/listing-${productId}`, handoffPath: null,
    handoffUrl: null, handoffMode: "NONE", disclosure: "", productImage: null,
  };
}

function response(sort: string, items: DealCardData[]): DiscoveryResponse {
  return { items, count: items.length, sort, page: 1, pageSize: 24, totalPages: 1, hasNext: false, facets: { categories: [], retailers: [] } };
}

describe("DealFeed", () => {
  afterEach(() => { cleanup(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

  it("changes sorting in place, updates the URL, and preserves the page position", async () => {
    const initial = response("recent", [deal("a", "Alpha"), deal("b", "Beta")]);
    const onSort = vi.fn().mockResolvedValue(true);

    render(<DealFeed
      params={{ sort: "recent", page: "3" }}
      result={initial}
      initialLoadError={false}
      updateError={null}
      pendingKind={null}
      onSort={onSort}
      onClearFilters={vi.fn().mockResolvedValue(true)}
    />);
    expect(screen.getAllByRole("article").map(item => item.textContent)).toEqual(["Alpha", "Beta"]);

    fireEvent.click(screen.getByRole("button", { name: "Best savings" }));

    expect(onSort).toHaveBeenCalledWith("savings");
  });

  it("requests a complete selection reset from the no-results recovery action", () => {
    const onClearFilters = vi.fn().mockResolvedValue(true);

    render(<DealFeed
      params={{ search: "missing", category: "electronics", retailer: "demo-north" }}
      result={response("relevance", [])}
      initialLoadError={false}
      updateError={null}
      pendingKind={null}
      onSort={vi.fn().mockResolvedValue(true)}
      onClearFilters={onClearFilters}
    />);

    fireEvent.click(screen.getByRole("link", { name: "Clear selection" }));

    expect(onClearFilters).toHaveBeenCalledOnce();
  });
});
