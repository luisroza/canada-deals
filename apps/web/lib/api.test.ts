import { afterEach, describe, expect, it, vi } from "vitest";
import { getDeals, resolveApiBase } from "./api";

describe("resolveApiBase", () => {
  it("requires an explicit API origin for a production server", () => {
    expect(() => resolveApiBase(undefined, "production", false)).toThrow("API_BASE_URL is required in production.");
  });

  it("uses same-origin routes in a production browser bundle", () => {
    expect(resolveApiBase(undefined, "production", true)).toBe("");
  });

  it("preserves an explicitly configured server API origin", () => {
    expect(resolveApiBase("http://api:8080", "production", false)).toBe("http://api:8080");
  });
});

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
