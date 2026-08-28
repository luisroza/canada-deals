import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { DiscoveryControls } from "./DiscoveryControls";

const categories = [{ key: "electronics", label: "Electronics" }];
const retailers = [{ key: "demo-north", label: "Demo North" }];

afterEach(cleanup);

describe("DiscoveryControls", () => {
  function controls(params: Parameters<typeof DiscoveryControls>[0]["params"], resultCount = 2, overrides: Partial<Parameters<typeof DiscoveryControls>[0]> = {}) {
    return <DiscoveryControls
      params={params}
      categories={categories}
      retailers={retailers}
      resultCount={resultCount}
      pending={false}
      onApply={vi.fn().mockResolvedValue(true)}
      onRemove={vi.fn().mockResolvedValue(true)}
      onClear={vi.fn().mockResolvedValue(true)}
      {...overrides}
    />;
  }

  it("exposes only the approved category and store filters", () => {
    render(controls({ category: "electronics", retailer: "demo-north" }));

    expect(screen.getByLabelText("Category")).toHaveValue("electronics");
    expect(screen.getByLabelText("Store")).toHaveValue("demo-north");
    expect(screen.getByRole("button", { name: "Show deals" })).toBeVisible();
    expect(screen.queryByLabelText("Minimum price")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Freshness")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("Comparison confidence")).not.toBeInTheDocument();
  });

  it("preserves search and sort while clearing category and store", () => {
    render(controls({ search: "northstar", category: "electronics", retailer: "demo-north", sort: "recent" }, 1));

    expect(screen.getByRole("link", { name: "Clear" })).toHaveAttribute("href", "/?search=northstar&sort=recent");
    expect(screen.getByRole("link", { name: "Remove Category filter" })).toHaveAttribute("href", "/?search=northstar&retailer=demo-north&sort=recent");
    expect(screen.getByRole("link", { name: "Remove Store filter" })).toHaveAttribute("href", "/?search=northstar&category=electronics&sort=recent");
  });

  it("reports the result count beside the compact controls", () => {
    render(controls({}, 6));
    expect(screen.getByText("6").parentElement).toHaveTextContent("6 products");
  });

  it("clears both active filters through the in-place callback", async () => {
    const onClear = vi.fn().mockResolvedValue(true);
    render(controls({ category: "electronics", retailer: "demo-north" }, 1, { onClear }));

    fireEvent.click(screen.getByRole("link", { name: "Clear" }));

    await waitFor(() => expect(onClear).toHaveBeenCalledTimes(1));
    expect(screen.getByLabelText("Category")).toHaveValue("");
    expect(screen.getByLabelText("Store")).toHaveValue("");
  });
});
