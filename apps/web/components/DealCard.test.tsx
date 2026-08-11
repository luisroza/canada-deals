import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { DealCard } from "./DealCard";

const baseDeal = {
  listingId: "a",
  productSlug: "northstar-55-qled-tv",
  productTitle: "Northstar 55-inch QLED TV",
  brand: "Northstar Demo",
  category: "Electronics",
  retailer: "Demo North Electronics",
  currentPrice: 1099.99,
  currency: "CAD",
  freshnessState: "RECENT" as const,
  evidenceState: "STRONG" as const,
  evidenceExplanation: "Observed history is available for this permitted fixture source.",
  observedAt: "2026-08-11T12:00:00Z",
  matchState: "Same product confirmed",
  historyState: "RELIABLE" as const,
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
    expect(screen.getByText(/Safe comparison available/)).toBeInTheDocument();
  });

  it("renders stale and unavailable evidence honestly", () => {
    render(<DealCard deal={{ ...baseDeal, freshnessState: "STALE", evidenceState: "UNAVAILABLE", hasSafeComparison: false, matchState: "No safe comparison available" }} />);
    expect(screen.getByText("May be stale")).toBeInTheDocument();
    expect(screen.getByText("Evidence unavailable")).toBeInTheDocument();
    expect(screen.getByText("No safe comparison available")).toBeInTheDocument();
  });
});
