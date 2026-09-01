import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SaveProductButton } from "./SaveProductButton";
import { WishlistProvider } from "./WishlistContext";

vi.mock("next/navigation", () => ({ usePathname: () => "/products/fixture-television" }));

const product = { listingId: "listing-1", productTitle: "Fixture television", returnTo: "/offers/listing-1" };

describe("SaveProductButton", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  function renderButton() {
    return render(<WishlistProvider><SaveProductButton {...product} /></WishlistProvider>);
  }

  it("explains the account boundary without hiding public product context", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isAuthenticated: false, email: null }) }));
    renderButton();

    fireEvent.click(await screen.findByRole("button", { name: "Save offer" }));

    expect(screen.getByRole("heading", { name: "Sign in to save this offer" }).parentElement).toHaveTextContent("account is needed only");
    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", expect.stringContaining("returnTo=%2Foffers%2Flisting-1"));
    expect(screen.getByRole("link", { name: "Create account" })).toBeVisible();
  });

  it("saves an authenticated product and exposes a text Saved state", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string, options?: RequestInit) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test" }) });
      if (path === "/api/v1/saved-offers" && !options?.method) return Promise.resolve({ ok: true, json: async () => [] });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      return Promise.resolve({ ok: true, status: 201, json: async () => ({ listingId: "listing-1", isSaved: true }) });
    });
    vi.stubGlobal("fetch", fetchMock);
    renderButton();

    fireEvent.click(await screen.findByRole("button", { name: "Save offer" }));

    expect(await screen.findByRole("button", { name: "Saved — remove" })).toHaveAttribute("aria-pressed", "true");
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/saved-offers/listing-1", expect.objectContaining({ method: "PUT", headers: expect.objectContaining({ "X-CSRF-TOKEN": "csrf" }) }));
  });

  it("loads an existing save and removes it", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string, options?: RequestInit) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test" }) });
      if (path === "/api/v1/saved-offers" && !options?.method) return Promise.resolve({ ok: true, json: async () => [{ listingId: "listing-1" }] });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      return Promise.resolve({ ok: true, status: 204, json: async () => null });
    });
    vi.stubGlobal("fetch", fetchMock);
    renderButton();

    fireEvent.click(await screen.findByRole("button", { name: "Saved — remove" }));

    expect(await screen.findByRole("button", { name: "Save offer" })).toHaveAttribute("aria-pressed", "false");
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/saved-offers/listing-1", expect.objectContaining({ method: "DELETE" }));
  });

  it("keeps the public page usable when session loading fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, status: 503 }));
    renderButton();

    expect(await screen.findByRole("alert")).toHaveTextContent("Public product details remain available");
    await waitFor(() => expect(screen.getByRole("button", { name: "Save offer" })).toBeDisabled());
  });
});
