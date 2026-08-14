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

  it("keeps filters collapsed until requested and returns focus on close", () => {
    render(<DiscoveryControls params={{}} categories={facets} retailers={[]} resultCount={6} />);
    const trigger = screen.getByRole("button", { name: "Filters" });
    expect(trigger).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByRole("dialog", { name: "Filter deals" })).not.toBeInTheDocument();
    fireEvent.click(trigger);

    expect(trigger).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("dialog", { name: "Filter deals" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Filter deals" })).toHaveFocus();
    fireEvent.click(screen.getByRole("button", { name: "Close filters" }));
    expect(trigger).toHaveFocus();
  });

  it("closes the mobile filter dialog with Escape and contains keyboard focus", () => {
    render(<DiscoveryControls params={{}} categories={facets} retailers={[]} resultCount={6} />);
    const trigger = screen.getByRole("button", { name: "Filters" });
    fireEvent.click(trigger);
    const dialog = screen.getByRole("dialog", { name: "Filter deals" });
    const close = screen.getByRole("button", { name: "Close filters" });
    const apply = screen.getByRole("button", { name: "Apply filters" });

    apply.focus();
    fireEvent.keyDown(document, { key: "Tab" });
    expect(close).toHaveFocus();
    close.focus();
    fireEvent.keyDown(document, { key: "Tab", shiftKey: true });
    expect(apply).toHaveFocus();

    fireEvent.keyDown(document, { key: "Escape" });
    expect(screen.queryByRole("dialog", { name: "Filter deals" })).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });

  it("keeps the approved sorting and filter controls explicit", () => {
    render(<DiscoveryControls params={{ sort: "savings" }} categories={facets} retailers={[]} resultCount={1} />);
    expect(screen.getByLabelText("Sort by")).toHaveValue("savings");
    expect(screen.getByLabelText("Supported reference")).toBeInTheDocument();
    expect(screen.getByLabelText("Comparison confidence")).toBeInTheDocument();
    expect(screen.getByLabelText("Availability")).toBeInTheDocument();
  });

  it("uses transparent sort defaults and exposes catalog-backed category shortcuts", () => {
    render(<DiscoveryControls params={{}} categories={facets} retailers={[]} resultCount={6} />);
    expect(screen.getByLabelText("Sort by")).toHaveValue("recent");
    expect(screen.queryByRole("option", { name: "Recommended default" })).not.toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Browse deal categories" })).toContainElement(screen.getByRole("link", { name: "Electronics" }));
  });

  it("clears filters without discarding the current search or sort", () => {
    render(<DiscoveryControls params={{ search: "northstar", category: "electronics", sort: "savings" }} categories={facets} retailers={[]} resultCount={1} />);
    fireEvent.click(screen.getByRole("button", { name: /Filters/ }));

    expect(screen.getAllByRole("link", { name: "Clear filters" })).toHaveLength(2);
    screen.getAllByRole("link", { name: "Clear filters" }).forEach((link) => {
      expect(link).toHaveAttribute("href", "/?search=northstar&sort=savings");
    });
  });
});
