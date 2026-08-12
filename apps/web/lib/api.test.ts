import { afterEach, describe, expect, it, vi } from "vitest";
import { getDeals } from "./api";

describe("getDeals", () => {
  afterEach(() => vi.restoreAllMocks());

  it("serializes search, filters, sort, and pagination without hidden ranking inputs", async () => {
    const fetchMock = vi.spyOn(globalThis, "fetch").mockResolvedValue(new Response(JSON.stringify({ items: [] }), { status: 200 }));
    await getDeals({ search: "NS55QLED-2026", retailer: "demo-north", minPrice: "500", sort: "relevance", page: "2" });

    const url = String(fetchMock.mock.calls[0][0]);
    expect(url).toContain("search=NS55QLED-2026");
    expect(url).toContain("retailer=demo-north");
    expect(url).toContain("minPrice=500");
    expect(url).toContain("sort=relevance");
    expect(url).toContain("page=2");
    expect(url).not.toMatch(/commission|affiliate|saved|alert/i);
  });
});
