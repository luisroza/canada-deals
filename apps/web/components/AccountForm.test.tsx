import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { AccountForm } from "./AccountForm";

const router = vi.hoisted(() => ({ push: vi.fn(), refresh: vi.fn() }));
vi.mock("next/navigation", () => ({ useRouter: () => router }));

describe("AccountForm", () => {
  afterEach(() => {
    cleanup();
    router.push.mockReset();
    router.refresh.mockReset();
    vi.unstubAllGlobals();
  });

  it("labels account fields and returns to the preserved product after Development registration", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: true, status: 201, json: async () => ({ message: "Account created.", isAuthenticated: true }) });
    vi.stubGlobal("fetch", fetchMock);
    render(<AccountForm mode="register" returnTo="/products/fixture-tv" />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "shopper@example.test" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "SecurePass42" } });
    fireEvent.click(screen.getByRole("button", { name: "Create account" }));

    await waitFor(() => expect(router.push).toHaveBeenCalledWith("/products/fixture-tv"));
    expect(screen.getByLabelText("Password")).toHaveAttribute("autocomplete", "new-password");
    expect(fetchMock).toHaveBeenLastCalledWith("/api/v1/account/register", expect.objectContaining({ method: "POST", headers: expect.objectContaining({ "X-CSRF-TOKEN": "csrf" }) }));
  });

  it("announces a generic sign-in error without losing entered fields", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: false, status: 401, json: async () => ({ detail: "Invalid email or password." }) });
    vi.stubGlobal("fetch", fetchMock);
    render(<AccountForm mode="sign-in" returnTo="/saved" />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "shopper@example.test" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "IncorrectPass42" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Invalid email or password");
    expect(screen.getByLabelText("Email")).toHaveValue("shopper@example.test");
    expect(router.push).not.toHaveBeenCalled();
  });

  it("offers a resend action when production registration requires confirmation", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: true, status: 201, json: async () => ({ message: "Account created. Confirm your email before signing in.", isAuthenticated: false }) })
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf-2" }) })
      .mockResolvedValueOnce({ ok: true, status: 202, json: async () => ({ message: "If an unconfirmed account exists for that address, a confirmation email has been sent.", isAuthenticated: false }) });
    vi.stubGlobal("fetch", fetchMock);
    render(<AccountForm mode="register" returnTo="/saved" />);
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "shopper@example.test" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "SecurePass42" } });
    fireEvent.click(screen.getByRole("button", { name: "Create account" }));
    fireEvent.click(await screen.findByRole("button", { name: "Resend confirmation email" }));
    expect(await screen.findByRole("status")).toHaveTextContent("If an unconfirmed account exists");
  });
});
