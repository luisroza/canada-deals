import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { OfferCard } from "./OfferCard";

describe("OfferCard", () => {
  it("does not present a possible variant as a safe handoff", () => {
    render(<OfferCard offer={{ listingId: "x", retailer: "Demo Market Lab", title: "Tool-only drill", currentPrice: 89.99, currency: "CAD", freshnessState: "RECENT", evidenceState: "PARTIAL", matchState: "Review before comparing", historyState: "PARTIAL", observedAt: null, handoffPath: "/go/x", disclosure: "Demo", isSafeComparison: false }} related />);
    expect(screen.getByText("Review before comparing")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: /continue to/i })).not.toBeInTheDocument();
  });
});
