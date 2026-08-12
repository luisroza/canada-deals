import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import type { ProductHistory } from "../lib/api";
import { PriceHistoryEvidence, PriceHistoryEvidenceLoading } from "./PriceHistoryEvidence";

const reliable: ProductHistory = {
  productId: "p1", productSlug: "northstar-55-qled-tv", window: "90d", windowDays: 90, state: "RELIABLE",
  trackingStart: "2026-05-20T00:00:00Z", observationStart: "2026-05-20T00:00:00Z", observationEnd: "2026-08-10T00:00:00Z",
  lowestObservedPrice: 1049.99, highestObservedPrice: 1299.99, observationCount: 12, observedDayCount: 11, largestGapDays: 12,
  coverageSummary: "Reliable history — 11 observed days span 83 days, with no gap longer than 12 days.",
  interpretation: "The observed evidence supports a bounded price trend for this selected period.",
  points: [
    { observedDate: "2026-05-20T00:00:00Z", lowestPrice: 1299.99, currency: "CAD", observationCount: 1 },
    { observedDate: "2026-08-10T00:00:00Z", lowestPrice: 1049.99, currency: "CAD", observationCount: 2 }
  ]
};

afterEach(cleanup);

describe("PriceHistoryEvidence", () => {
  it("renders summary first, selected range, chart semantics, and a text-equivalent table", () => {
    render(<PriceHistoryEvidence history={reliable} productSlug={reliable.productSlug} currentPrice={1099.99} currentFreshness="RECENT" />);
    expect(screen.getByText(/Lowest observed in the last 90 days/).closest("p")).toHaveTextContent("$1,049.99");
    expect(screen.getByRole("link", { name: "90 days" })).toHaveAttribute("aria-current", "page");
    expect(screen.getByRole("img", { name: /Observed product prices over 90 days/ })).toBeInTheDocument();
    expect(screen.getByText("View observed price data")).toBeInTheDocument();
    expect(screen.getByText(/never a claim about the lowest price ever/i)).toBeInTheDocument();
  });

  it("shows sparse real observations as partial without continuous-coverage language", () => {
    render(<PriceHistoryEvidence history={{ ...reliable, state: "PARTIAL", coverageSummary: "Partial history — 2 observed days span 8 days, so gaps limit stronger conclusions.", interpretation: "These are real observations, but they do not imply continuous monitoring between points." }} productSlug={reliable.productSlug} currentPrice={1099.99} currentFreshness="RECENT" />);
    expect(screen.getByText("Partial history")).toBeInTheDocument();
    expect(screen.getByText(/do not imply continuous monitoring/i)).toBeInTheDocument();
  });

  it("does not render a fake chart for unavailable history", () => {
    render(<PriceHistoryEvidence history={{ ...reliable, state: "UNAVAILABLE", lowestObservedPrice: null, highestObservedPrice: null, observationCount: 0, observedDayCount: 0, points: [], coverageSummary: "Price history unavailable — no qualifying permitted observations exist in this period." }} productSlug="no-history" currentPrice={249.99} currentFreshness="AGING" />);
    expect(screen.getByRole("heading", { name: "Price history unavailable" })).toBeInTheDocument();
    expect(screen.queryByRole("img")).not.toBeInTheDocument();
    expect(screen.getByText(/Current price:/).closest("p")).toHaveTextContent("$249.99");
  });

  it("distinguishes a technical error from an unavailable evidence state", () => {
    render(<PriceHistoryEvidence history={null} productSlug="p" currentPrice={100} currentFreshness="STALE" error />);
    expect(screen.getByRole("alert")).toHaveTextContent("History temporarily unavailable");
    expect(screen.getByText(/Freshness:/)).toHaveTextContent("stale");
  });

  it("keeps current evidence visible in the contained streaming fallback", () => {
    render(<PriceHistoryEvidenceLoading productSlug="p" selected="90d" currentPrice={100} currentFreshness="RECENT" />);
    expect(screen.getByRole("status")).toHaveTextContent("Loading observed price history");
    expect(screen.getByText(/Current price:/).closest("p")).toHaveTextContent("$100.00");
    expect(screen.getByRole("link", { name: "90 days" })).toHaveAttribute("aria-current", "page");
  });
});
