import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ReportIssueForm } from "./ReportIssueForm";

describe("ReportIssueForm", () => {
  afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
  });

  it("opens, submits a reason and optional note, and announces confirmation", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ message: "Thanks. Your report was attached to this listing for review." }),
    });
    vi.stubGlobal("fetch", fetchMock);
    render(<ReportIssueForm listingId="listing-1" listingLabel="Demo retailer: Fixture product" />);

    fireEvent.click(screen.getByRole("button", { name: "Report stale or wrong" }));
    fireEvent.click(screen.getByRole("radio", { name: "Price changed" }));
    fireEvent.change(screen.getByLabelText("Optional note"), { target: { value: "Retailer now shows $549." } });
    fireEvent.click(screen.getByRole("button", { name: "Send report" }));

    expect(await screen.findByRole("status")).toHaveTextContent("attached to this listing for review");
    expect(fetchMock).toHaveBeenCalledWith("/api/v1/listings/listing-1/reports", expect.objectContaining({ method: "POST" }));
  });

  it("requires a controlled reason before submitting", () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    render(<ReportIssueForm listingId="listing-1" listingLabel="Fixture product" />);

    fireEvent.click(screen.getByRole("button", { name: "Report stale or wrong" }));
    fireEvent.click(screen.getByRole("button", { name: "Send report" }));

    expect(screen.getByRole("alert")).toHaveTextContent("Choose what is wrong");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("preserves the reason and note when submission fails", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue({ ok: false, json: async () => ({}) }));
    render(<ReportIssueForm listingId="listing-1" listingLabel="Fixture product" />);

    fireEvent.click(screen.getByRole("button", { name: "Report stale or wrong" }));
    fireEvent.click(screen.getByRole("radio", { name: "Wrong variant" }));
    fireEvent.change(screen.getByLabelText("Optional note"), { target: { value: "The battery count is different." } });
    fireEvent.click(screen.getByRole("button", { name: "Send report" }));

    await waitFor(() => expect(screen.getByRole("alert")).toHaveTextContent("please try again"));
    expect(screen.getByRole("radio", { name: "Wrong variant" })).toBeChecked();
    expect(screen.getByLabelText("Optional note")).toHaveValue("The battery count is different.");
  });
});
