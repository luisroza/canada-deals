import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { OfferCard } from "./OfferCard";

describe("OfferCard", () => {
  it("does not present a possible variant as a safe handoff", () => {
    render(<OfferCard offer={{ listingId: "x", retailer: "Demo Market Lab", title: "Tool-only drill", currentPrice: 89.99, currency: "CAD", freshnessState: "RECENT", evidenceState: "PARTIAL", matchState: "Review before comparing", historyState: "PARTIAL", availabilityState: "AVAILABLE", observedAt: null, handoffPath: "/go/x", disclosure: "Demo", isSafeComparison: false }} related />);
    expect(screen.getByText("Review before comparing")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /continue to/i })).not.toBeInTheDocument();
  });

  it("turns a stale handoff into an explicit price-verification action", () => {
    render(<OfferCard offer={{ listingId: "x", retailer: "Demo North Electronics", title: "QLED TV", currentPrice: 1099.99, currency: "CAD", freshnessState: "STALE", evidenceState: "STRONG", matchState: "Same product confirmed", historyState: "RELIABLE", availabilityState: "AVAILABLE", observedAt: "2026-08-10T12:00:00Z", handoffPath: "/go/x", disclosure: "Demo fixture data.", isSafeComparison: true }} />);
    expect(screen.getByRole("link", { name: "Check current price at Demo North Electronics" })).toHaveAttribute("href", "/go/x");
    expect(screen.getByText(/Price may have changed/)).toBeInTheDocument();
    expect(screen.getByText("Demo fixture data.")).toBeInTheDocument();
  });
});
