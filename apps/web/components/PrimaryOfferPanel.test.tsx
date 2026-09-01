import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PrimaryOfferPanel } from "./PrimaryOfferPanel";

describe("PrimaryOfferPanel", () => {
  it("puts the current price, freshness, retailer action, and disclosure in one decision block", () => {
    render(<PrimaryOfferPanel offer={{ listingId: "x", retailer: "Demo North Electronics", title: "QLED TV", currentPrice: 1099.99, regularPrice: 1299.99, savingsAmount: 200, savingsPercent: 15.38, currency: "CAD", freshnessState: "STALE", evidenceState: "STRONG", matchState: "Offer identity verified", historyState: "RELIABLE", availabilityState: "AVAILABLE", seller: "Demo North Electronics", conditionState: "NEW", regionAvailabilityContext: "Canada", shippingContext: "Shipping calculated at checkout", observedAt: "2026-08-10T12:00:00Z", handoffPath: "/go/x", handoffUrl: null, handoffMode: "INTERNAL_REDIRECT", disclosure: "Demo fixture data." }} secondaryAction={<button type="button">Save offer</button>} />);

    expect(screen.getByRole("heading", { name: "Current deal price" })).toBeInTheDocument();
    expect(screen.getByText("$1,099.99")).toBeInTheDocument();
    expect(screen.getByText("May be stale")).toBeInTheDocument();
    expect(screen.getAllByText(/Available online/).length).toBeGreaterThan(0);
    const retailerLink = screen.getByRole("link", { name: "Check current price at Demo North Electronics" });
    expect(retailerLink).toHaveAttribute("href", "/go/x");
    expect(retailerLink).toHaveAttribute("target", "_blank");
    expect(retailerLink).toHaveAttribute("rel", "sponsored noopener");
    expect(retailerLink).toHaveAttribute("aria-description", "Opens retailer website in a new tab.");
    expect(screen.getByText(/Regular price/)).toHaveTextContent("$1,299.99");
    expect(screen.getByText(/You save/)).toHaveTextContent("$200.00 (15%)");
    expect(screen.getByRole("button", { name: "Save offer" })).toBeInTheDocument();
    expect(screen.getByText("Demo fixture data.")).toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Offer conditions" })).not.toBeInTheDocument();
  });
});
