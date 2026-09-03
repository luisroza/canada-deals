import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { BrandLogo } from "./BrandLogo";

describe("BrandLogo", () => {
  afterEach(cleanup);

  it("exposes one accessible Deal North identity and a decorative compact mark", () => {
    const { container } = render(<BrandLogo compact />);

    expect(screen.getByRole("img", { name: "Deal North" })).toHaveAttribute(
      "src",
      "/brand/deal-north-logo.png",
    );
    expect(container.querySelector(".brand-logo-mark")).toHaveAttribute("aria-hidden", "true");
    expect(container.querySelector(".brand-logo-mobile-name")).toHaveTextContent("Deal North");
    expect(container.querySelector(".brand-logo")).toHaveClass("brand-logo-compact");
  });
});
