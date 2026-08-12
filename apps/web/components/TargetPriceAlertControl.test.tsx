import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TargetPriceAlertControl } from "./TargetPriceAlertControl";

const product = { productId: "product-1", productTitle: "Fixture television", currentPrice: 500, currency: "CAD", returnTo: "/products/fixture-tv" };

describe("TargetPriceAlertControl", () => {
  afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

  it("preserves public discovery and return context for signed-out visitors", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: true, json: async () => ({ isAuthenticated: false, email: null, emailConfirmed: false }) }));
    render(<TargetPriceAlertControl {...product} />);
    expect(await screen.findByText(/Discovery and price evidence remain public/)).toBeVisible();
    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", expect.stringContaining("returnTo=%2Fproducts%2Ffixture-tv"));
  });

  it("honestly blocks activation for an unconfirmed email", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: false }) });
      return Promise.resolve({ ok: true, json: async () => [] });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<TargetPriceAlertControl {...product} />);
    expect(await screen.findByText("Email confirmation required.")).toBeVisible();
    expect(screen.getByText(/No notification will be queued/)).toBeVisible();
  });

  it("requires explicit alert-only consent and persists a valid target", async () => {
    let alerts: unknown[] = [];
    const fetchMock = vi.fn().mockImplementation((path: string, options?: RequestInit) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) });
      if (path === "/api/v1/price-alerts" && !options?.method) return Promise.resolve({ ok: true, json: async () => alerts });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      if (path === "/api/v1/price-alerts/product-1" && options?.method === "PUT") {
        alerts = [{ productId: "product-1", productSlug: "fixture-tv", productTitle: "Fixture television", targetPrice: 450, currency: "CAD", status: "ACTIVE", targetVersion: 1, consentGrantedAt: "2026-08-11T00:00:00Z", consentVersion: "target-price-email-v1", lastEvaluatedAt: null, lastTriggeredAt: null }];
        return Promise.resolve({ ok: true, status: 201, json: async () => ({ targetPrice: 450 }) });
      }
      return Promise.resolve({ ok: true, json: async () => ({}) });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<TargetPriceAlertControl {...product} />);

    fireEvent.click(await screen.findByRole("button", { name: "Set alert" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Confirm that you want");
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: "Set alert" }));

    expect(await screen.findByText(/Active target:/)).toHaveTextContent("$450.00");
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/price-alerts/product-1", expect.objectContaining({ method: "PUT", headers: expect.objectContaining({ "X-CSRF-TOKEN": "csrf" }) }));
    expect(screen.getByText(/not marketing or a weekly digest/i)).toBeVisible();
  });

  it("shows and removes an existing target without removing the saved product", async () => {
    const existing = [{ productId: "product-1", productSlug: "fixture-tv", productTitle: "Fixture television", targetPrice: 425, currency: "CAD", status: "ACTIVE", targetVersion: 2, consentGrantedAt: "2026-08-11T00:00:00Z", consentVersion: "target-price-email-v1", lastEvaluatedAt: null, lastTriggeredAt: null }];
    const fetchMock = vi.fn().mockImplementation((path: string, options?: RequestInit) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) });
      if (path === "/api/v1/price-alerts" && !options?.method) return Promise.resolve({ ok: true, json: async () => existing });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      return Promise.resolve({ ok: true, status: 204, json: async () => null });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<TargetPriceAlertControl {...product} />);
    expect(await screen.findByText(/Active target:/)).toHaveTextContent("$425.00");
    fireEvent.click(screen.getByRole("button", { name: "Remove alert" }));
    expect(await screen.findByRole("status")).toHaveTextContent("product remains in your saved list");
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith("/api/v1/price-alerts/product-1", expect.objectContaining({ method: "DELETE" })));
  });

  it("validates the target range and decimal precision before mutation", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) });
      return Promise.resolve({ ok: true, json: async () => [] });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<TargetPriceAlertControl {...product} />);
    const input = await screen.findByLabelText("Target price (CAD)");
    fireEvent.change(input, { target: { value: "10.001" } });
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: "Set alert" }));
    expect(screen.getByRole("alert")).toHaveTextContent("at most two decimal places");
    expect(fetchMock).not.toHaveBeenCalledWith("/api/v1/price-alerts/product-1", expect.anything());
  });

  it("lets an active user enter edit mode", async () => {
    const existing = [{ productId: "product-1", productSlug: "fixture-tv", productTitle: "Fixture television", targetPrice: 425, currency: "CAD", status: "ACTIVE", targetVersion: 1, consentGrantedAt: "2026-08-11T00:00:00Z", consentVersion: "target-price-email-v1", lastEvaluatedAt: null, lastTriggeredAt: null }];
    const fetchMock = vi.fn().mockImplementation((path: string) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) });
      return Promise.resolve({ ok: true, json: async () => existing });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<TargetPriceAlertControl {...product} />);
    fireEvent.click(await screen.findByRole("button", { name: "Edit target" }));
    expect(screen.getByRole("heading", { name: "Edit your target" })).toHaveFocus();
    expect(screen.getByRole("button", { name: "Update alert" })).toBeVisible();
    expect(screen.getByLabelText("Target price (CAD)")).toHaveValue("425.00");
  });

  it("announces a delivery-boundary failure and remains retryable", async () => {
    const fetchMock = vi.fn().mockImplementation((path: string, options?: RequestInit) => {
      if (path.endsWith("/me")) return Promise.resolve({ ok: true, json: async () => ({ isAuthenticated: true, email: "a@example.test", emailConfirmed: true }) });
      if (path === "/api/v1/price-alerts" && !options?.method) return Promise.resolve({ ok: true, json: async () => [] });
      if (path.endsWith("/antiforgery")) return Promise.resolve({ ok: true, json: async () => ({ requestToken: "csrf" }) });
      return Promise.resolve({ ok: false, status: 503, json: async () => ({ detail: "Alert storage is temporarily unavailable." }) });
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<TargetPriceAlertControl {...product} />);
    await screen.findByRole("button", { name: "Set alert" });
    fireEvent.click(screen.getByRole("checkbox"));
    fireEvent.click(screen.getByRole("button", { name: "Set alert" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("temporarily unavailable");
    expect(screen.getByRole("button", { name: "Set alert" })).toBeEnabled();
  });
});
