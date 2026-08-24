import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { WishlistCardButton } from "./WishlistCardButton";

const { mockUseWishlist } = vi.hoisted(() => ({ mockUseWishlist: vi.fn() }));

vi.mock("./WishlistContext", () => ({ useWishlist: mockUseWishlist }));

const productTitle = "Northstar 55-inch QLED TV";

function wishlistState(overrides: Record<string, unknown> = {}) {
  return {
    authenticated: true,
    loading: false,
    loadError: null,
    items: [],
    count: 0,
    isSaved: () => false,
    isPending: () => false,
    toggle: vi.fn(),
    retry: vi.fn(),
    ...overrides,
  };
}

describe("WishlistCardButton accessible names", () => {
  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it.each([
    ["loading", wishlistState({ loading: true, authenticated: null }), `Save — loading Wishlist state for ${productTitle}`],
    ["signed out", wishlistState({ authenticated: false }), `Save ${productTitle} to your Wishlist — sign in required`],
    ["unavailable", wishlistState({ authenticated: null }), `Save — Wishlist unavailable for ${productTitle}`],
  ])("includes the visible Save label while %s", (_state, value, accessibleName) => {
    mockUseWishlist.mockReturnValue(value);
    render(<WishlistCardButton productId="product-a" productTitle={productTitle} returnTo="/#deals" />);
    expect(screen.getByRole(value.authenticated === false ? "link" : "button", { name: accessibleName })).toHaveTextContent("Save");
  });

  it.each([
    ["unsaved", false, false, `Save ${productTitle} to your Wishlist`, "Save"],
    ["saved", true, false, `Saved — remove ${productTitle} from your Wishlist`, "Saved"],
    ["pending save", false, true, `Wait — saving ${productTitle} to your Wishlist`, "Wait"],
    ["pending remove", true, true, `Wait — removing ${productTitle} from your Wishlist`, "Wait"],
  ])("includes the visible label for the authenticated %s state", (_state, saved, pending, accessibleName, visibleLabel) => {
    mockUseWishlist.mockReturnValue(wishlistState({ isSaved: () => saved, isPending: () => pending }));
    render(<WishlistCardButton productId="product-a" productTitle={productTitle} returnTo="/#deals" />);
    expect(screen.getByRole("button", { name: accessibleName })).toHaveTextContent(visibleLabel);
  });
});
