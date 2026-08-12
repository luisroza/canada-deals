import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PrimaryOfferPanel } from "./PrimaryOfferPanel";

describe("PrimaryOfferPanel", () => {
  it("puts the current price, freshness, retailer action, and disclosure in one decision block", () => {
    render(<PrimaryOfferPanel offer={{ listingId: "x", retailer: "Demo North Electronics", title: "QLED TV", currentPrice: 1099.99, currency: "CAD", freshnessState: "STALE", evidenceState: "STRONG", matchState: "Same product confirmed", historyState: "RELIABLE", availabilityState: "AVAILABLE", observedAt: "2026-08-10T12:00:00Z", handoffPath: "/go/x", disclosure: "Demo fixture data.", isSafeComparison: true }} />);

    expect(screen.getByRole("heading", { name: "Current observed price" })).toBeInTheDocument();
    expect(screen.getByText("$1,099.99")).toBeInTheDocument();
    expect(screen.getByText("May be stale")).toBeInTheDocument();
    expect(screen.getByText(/Available online/)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Check current price at Demo North Electronics" })).toBeInTheDocument();
    expect(screen.getByText("Demo fixture data.")).toBeInTheDocument();
  });
});
