import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DealCard } from "./DealCard";

const baseDeal = {
  listingId: "a",
  productId: "product-a",
  productSlug: "northstar-55-qled-tv",
  productTitle: "Northstar 55-inch QLED TV",
  brand: "Northstar Demo",
  category: "Electronics",
  retailer: "Demo North Electronics",
  currentPrice: 1099.99,
  currency: "CAD",
  freshnessState: "RECENT" as const,
  evidenceState: "STRONG" as const,
  availabilityState: "AVAILABLE" as const,
  evidenceExplanation: "Observed history is available for this permitted fixture source.",
  observedAt: "2026-08-11T12:00:00Z",
  matchState: "Same product confirmed",
  historyState: "RELIABLE" as const,
  referencePrice: 1299.99,
  supportedSavingsPercent: 15.4,
  hasSafeComparison: true,
  detailsPath: "/products/northstar-55-qled-tv",
  handoffPath: "/go/a",
  disclosure: "Demo fixture data.",
};

describe("DealCard", () => {
  it("shows price, evidence, freshness, and safe comparison context", () => {
    render(<DealCard deal={baseDeal} />);
    expect(screen.getByText("$1,099.99")).toBeInTheDocument();
    expect(screen.getByText("Strong evidence")).toBeInTheDocument();
    expect(screen.getByText("Checked recently")).toBeInTheDocument();
    expect(screen.getByText("Available online")).toBeInTheDocument();
    expect(screen.getByText("Current observed price")).toBeInTheDocument();
    expect(screen.getByText(/Observed at/)).toHaveTextContent("Demo North Electronics");
    expect(screen.getByText(/Safe comparison available/)).toBeInTheDocument();
    expect(screen.getByText(/15.4% below supported reference/)).toBeInTheDocument();
  });

  it("renders stale and unavailable evidence honestly", () => {
    render(<DealCard deal={{ ...baseDeal, freshnessState: "STALE", evidenceState: "UNAVAILABLE", hasSafeComparison: false, matchState: "No safe comparison available" }} />);
    expect(screen.getByText("May be stale")).toBeInTheDocument();
    expect(screen.getByText("Evidence unavailable")).toBeInTheDocument();
    expect(screen.getByText("No safe comparison available")).toBeInTheDocument();
  });
});
