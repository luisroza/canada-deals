import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { DiscoveryControls } from "./DiscoveryControls";

const facets = [{ key: "electronics", label: "Electronics" }];

afterEach(cleanup);

describe("DiscoveryControls", () => {
  it("restores URL-backed controls and exposes removable active filters", () => {
    render(<DiscoveryControls params={{ search: "northstar", category: "electronics", freshness: "recent" }} categories={facets} retailers={[{ key: "demo-north", label: "Demo North" }]} resultCount={2} />);

    expect(screen.getByLabelText("Search a product or model number")).toHaveValue("northstar");
    expect(screen.getByLabelText("Category")).toHaveValue("electronics");
    expect(screen.getByLabelText("Freshness")).toHaveValue("recent");
    expect(screen.getByRole("status")).toHaveTextContent("2 products found");
    expect(screen.getByRole("link", { name: "Remove Category filter" })).toHaveAttribute("href", "/?search=northstar&freshness=recent");
  });

  it("opens an accessible mobile filter dialog and returns focus on close", () => {
    render(<DiscoveryControls params={{}} categories={facets} retailers={[]} resultCount={6} />);
    const trigger = screen.getByRole("button", { name: "Filters" });
    fireEvent.click(trigger);

    expect(screen.getByRole("dialog", { name: "Filter deals" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Filter deals" })).toHaveFocus();
    fireEvent.click(screen.getByRole("button", { name: "Close" }));
    expect(trigger).toHaveFocus();
  });

  it("keeps the approved sorting and filter controls explicit", () => {
    render(<DiscoveryControls params={{ sort: "savings" }} categories={facets} retailers={[]} resultCount={1} />);
    expect(screen.getByLabelText("Sort")).toHaveValue("savings");
    expect(screen.getByLabelText("Supported reference")).toBeInTheDocument();
    expect(screen.getByLabelText("Comparison confidence")).toBeInTheDocument();
    expect(screen.getByLabelText("Availability")).toBeInTheDocument();
  });
});
