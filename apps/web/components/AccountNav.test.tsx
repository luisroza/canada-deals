import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AccountNav } from "./AccountNav";
import { WishlistProvider } from "./WishlistContext";

vi.mock("next/navigation", () => ({ usePathname: () => "/" }));

describe("AccountNav", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("shows the authenticated Wishlist count after one shared load", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "shopper@example.test" }) });
      return Promise.resolve({ ok: true, json: async () => [{ listingId: "one" }, { listingId: "two" }] });
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<WishlistProvider><AccountNav /></WishlistProvider>);

    expect(await screen.findByRole("link", { name: "Wishlist (2)" })).toHaveAttribute("href", "/saved");
    expect(screen.getByRole("button", { name: "Sign out" })).toBeVisible();
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
