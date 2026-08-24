import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CatalogMenus } from "./CatalogMenus";

vi.mock("next/navigation", () => ({ usePathname: () => "/" }));

afterEach(cleanup);

describe("CatalogMenus", () => {
  it("uses a consistent icon and label grid for store names of different lengths", () => {
    render(<CatalogMenus categories={[]} retailers={[
      { key: "short", label: "Short Store" },
      { key: "long", label: "A Much Longer Canadian Store Name" },
    ]} />);

    const shortStore = screen.getByRole("link", { name: "Short Store" });
    const longStore = screen.getByRole("link", { name: "A Much Longer Canadian Store Name" });
    expect(shortStore).toHaveClass("catalog-menu-item-retailer");
    expect(longStore).toHaveClass("catalog-menu-item-retailer");
    expect(shortStore.querySelector(".catalog-menu-label")).toHaveTextContent("Short Store");
    expect(longStore.querySelector(".catalog-menu-label")).toHaveTextContent("A Much Longer Canadian Store Name");
    expect(shortStore.querySelector(".catalog-menu-mark")).toHaveTextContent("S");
    expect(longStore.querySelector(".catalog-menu-mark")).toHaveTextContent("A");
  });

  it("keeps only one browse menu open at a time", async () => {
    const { container } = render(<CatalogMenus
      categories={[{ key: "electronics", label: "Electronics" }]}
      retailers={[{ key: "demo-store", label: "Demo Store" }]}
    />);
    const menus = container.querySelectorAll("details.catalog-menu");

    fireEvent.click(screen.getByText("Categories"));
    await waitFor(() => expect(menus[0]).toHaveAttribute("open"));
    expect(menus[1]).not.toHaveAttribute("open");

    fireEvent.click(screen.getByText("Stores"));
    await waitFor(() => expect(menus[1]).toHaveAttribute("open"));
    expect(menus[0]).not.toHaveAttribute("open");
  });
});
