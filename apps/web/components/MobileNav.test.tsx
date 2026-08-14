import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { MobileNav } from "./MobileNav";

const navigation = vi.hoisted(() => ({ pathname: "/saved" }));
vi.mock("next/navigation", () => ({ usePathname: () => navigation.pathname }));

afterEach(cleanup);

describe("MobileNav", () => {
  it("exposes the approved five destinations and identifies the current route", () => {
    render(<MobileNav />);
    const nav = screen.getByRole("navigation", { name: "Mobile primary navigation" });
    expect(nav).toContainElement(screen.getByRole("link", { name: "Home" }));
    expect(nav).toContainElement(screen.getByRole("link", { name: "Deals" }));
    expect(nav).toContainElement(screen.getByRole("link", { name: "Search" }));
    expect(screen.getByRole("link", { name: "Saved" })).toHaveAttribute("aria-current", "page");
    expect(nav).toContainElement(screen.getByRole("link", { name: "Account" }));
  });
});
