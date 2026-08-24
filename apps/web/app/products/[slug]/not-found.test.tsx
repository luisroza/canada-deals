import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import ProductNotFound from "./not-found";

describe("ProductNotFound", () => {
  it("explains the unavailable product and provides accessible recovery paths without exposing a generic 404", () => {
    const { container } = render(<ProductNotFound />);

    expect(screen.getByRole("heading", { level: 1, name: "We couldn’t find this product" })).toBeInTheDocument();
    expect(screen.getByRole("search")).toHaveAttribute("action", "/");
    expect(screen.getByRole("searchbox", { name: "Search for another product" })).toHaveAttribute("name", "search");
    expect(screen.getByRole("button", { name: "Search deals" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Browse current deals" })).toHaveAttribute("href", "/#deals");
    expect(screen.getByRole("link", { name: "View Wishlist" })).toHaveAttribute("href", "/saved");
    expect(container).not.toHaveTextContent("404");
  });
});
