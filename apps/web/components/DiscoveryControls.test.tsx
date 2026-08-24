import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { DiscoveryControls } from "./DiscoveryControls";

const categories = [{ key: "electronics", label: "Electronics" }];
const retailers = [{ key: "demo-north", label: "Demo North" }];

afterEach(cleanup);

describe("DiscoveryControls", () => {
  it("exposes only the approved category and store filters", () => {
    render(<DiscoveryControls params={{ category: "electronics", retailer: "demo-north" }} categories={categories} retailers={retailers} resultCount={2} />);

    expect(screen.getByLabelText("Category")).toHaveValue("electronics");
    expect(screen.getByLabelText("Store")).toHaveValue("demo-north");
    expect(screen.getByRole("button", { name: "Show deals" })).toBeVisible();
    expect(screen.queryByLabelText("Minimum price")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Freshness")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Comparison confidence")).not.toBeInTheDocument();
  });

  it("preserves search and sort while clearing category and store", () => {
    render(<DiscoveryControls params={{ search: "northstar", category: "electronics", retailer: "demo-north", sort: "recent" }} categories={categories} retailers={retailers} resultCount={1} />);

    expect(screen.getByRole("link", { name: "Clear" })).toHaveAttribute("href", "/?search=northstar&sort=recent");
    expect(screen.getByRole("link", { name: "Remove Category filter" })).toHaveAttribute("href", "/?search=northstar&retailer=demo-north&sort=recent");
    expect(screen.getByRole("link", { name: "Remove Store filter" })).toHaveAttribute("href", "/?search=northstar&category=electronics&sort=recent");
  });

  it("reports the result count beside the compact controls", () => {
    render(<DiscoveryControls params={{}} categories={categories} retailers={retailers} resultCount={6} />);
    expect(screen.getByText("6").parentElement).toHaveTextContent("6 products");
  });
});
