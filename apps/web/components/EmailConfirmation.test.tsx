import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { EmailConfirmation } from "./EmailConfirmation";

describe("EmailConfirmation", () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it("announces a confirmed account and offers sign in", async () => {
    vi.stubGlobal("fetch", vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ status: "CONFIRMED", message: "Confirmed" }) }));

    render(<EmailConfirmation userId="1d45ea9d-aea6-4cc4-bd6b-edfa89fbb99f" code="safe-code" />);

    expect(await screen.findByText("Email confirmed.")).toBeVisible();
    expect(screen.getByRole("link", { name: "Sign in" })).toHaveAttribute("href", "/account/sign-in");
  });

  it("renders the already-confirmed state idempotently", async () => {
    vi.stubGlobal("fetch", vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: true, status: 200, json: async () => ({ status: "ALREADY_CONFIRMED", message: "Already" }) }));

    render(<EmailConfirmation userId="1d45ea9d-aea6-4cc4-bd6b-edfa89fbb99f" code="safe-code" />);

    expect(await screen.findByText("Email already confirmed.")).toBeVisible();
  });

  it("shows invalid-link recovery and preserves a generic resend response", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: true, status: 202, json: async () => ({ message: "If an unconfirmed account exists for that address, a confirmation email has been sent.", isAuthenticated: false }) });
    vi.stubGlobal("fetch", fetchMock);
    render(<EmailConfirmation />);

    expect(screen.getByRole("alert")).toHaveTextContent("invalid or has expired");
    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "person@example.test" } });
    fireEvent.click(screen.getByRole("button", { name: "Send a new confirmation email" }));

    expect(await screen.findByRole("status")).toHaveTextContent("If an unconfirmed account exists");
    await waitFor(() => expect(fetchMock).toHaveBeenLastCalledWith("/api/v1/account/resend-confirmation", expect.objectContaining({ method: "POST" })));
  });

  it("does not expose technical details for a temporary failure", async () => {
    vi.stubGlobal("fetch", vi.fn()
      .mockResolvedValueOnce({ ok: true, json: async () => ({ requestToken: "csrf" }) })
      .mockResolvedValueOnce({ ok: false, status: 500, json: async () => ({ detail: "internal" }) }));
    render(<EmailConfirmation userId="1d45ea9d-aea6-4cc4-bd6b-edfa89fbb99f" code="safe-code" />);

    expect(await screen.findByRole("alert")).toHaveTextContent("temporarily unavailable");
    expect(screen.queryByText("internal")).not.toBeInTheDocument();
  });
});
