import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { StoreBannerData } from "../lib/api";
import { StoreBanner } from "./StoreBanner";
import { StoreBannerRail } from "./StoreBannerRail";

function banner(overrides: Partial<StoreBannerData> = {}): StoreBannerData {
  return {
    retailerKey: "demo-north",
    displayName: "Demo North",
    title: "Shop Demo North",
    subtitle: "Electronics and everyday tech",
    assetPath: "/store-banners/electronics-devices.svg",
    assetSource: "CANADADEALSORIGINAL",
    brandAssetPolicy: "UNKNOWN",
    affiliateStatus: "DISCOVERY_ONLY",
    href: "/?retailer=demo-north#deals",
    opensNewTab: false,
    ...overrides,
  };
}

describe("StoreBanner", () => {
  it("opens an active affiliate handoff in a protected sponsored new tab", () => {
    render(<StoreBanner banner={banner({ affiliateStatus: "ACTIVE_AFFILIATE", href: "/go/store/demo-north", opensNewTab: true })} />);
    const link = screen.getByRole("link", { name: /opens retailer website in a new tab/i });
    expect(link).toHaveAttribute("href", "/go/store/demo-north");
    expect(link).toHaveAttribute("target", "_blank");
    expect(link).toHaveAttribute("rel", "noopener noreferrer sponsored");
  });

  it("keeps a discovery-only store inside GreatDeals", () => {
    render(<StoreBanner banner={banner()} />);
    const link = screen.getByRole("link", { name: "Browse Demo North deals on GreatDeals.ca" });
    expect(link).toHaveAttribute("href", "/?retailer=demo-north#deals");
    expect(link).not.toHaveAttribute("target");
    expect(link).toHaveClass("store-banner-discovery");
    expect(link).toHaveProperty("tabIndex", 0);
  });

  it("uses first-party fallback art when the configured asset is missing", () => {
    const { container } = render(<StoreBanner banner={banner({ assetPath: null })} />);
    expect(container.querySelector("img")).toHaveAttribute("src", "/store-banners/marketplace-packages.svg");
  });

  it("does not render a disabled banner", () => {
    const { container } = render(<StoreBanner banner={banner({ affiliateStatus: "DISABLED" })} />);
    expect(container).toBeEmptyDOMElement();
  });

  it("fails closed when an active banner is given a raw external URL", () => {
    const { container } = render(<StoreBanner banner={banner({ affiliateStatus: "ACTIVE_AFFILIATE", href: "https://attacker.example", opensNewTab: true })} />);
    const link = container.querySelector("a");
    expect(link).toHaveAttribute("href", "/#deals");
    expect(link).not.toHaveAttribute("target");
    expect(link).not.toHaveAttribute("rel");
  });
});

describe("StoreBannerRail", () => {
  it("shows at most four enabled banners in supplied editorial order", () => {
    const banners = [
      banner({ retailerKey: "disabled", affiliateStatus: "DISABLED" }),
      ...Array.from({ length: 5 }, (_, index) => banner({
        retailerKey: `demo-${index + 1}`,
        displayName: `Demo ${index + 1}`,
        title: `Shop Demo ${index + 1}`,
        href: `/?retailer=demo-${index + 1}#deals`,
      })),
    ];
    const { container } = render(<StoreBannerRail banners={banners} />);
    expect(container.textContent).toMatch(/may earn a commission/i);
    expect([...container.querySelectorAll("a")].map((link) => link.textContent)).toEqual([
      expect.stringContaining("Shop Demo 1"),
      expect.stringContaining("Shop Demo 2"),
      expect.stringContaining("Shop Demo 3"),
      expect.stringContaining("Shop Demo 4"),
    ]);
    expect(container.textContent).not.toContain("Shop Demo 5");
    expect(container.textContent).toMatch(/never changes store order or deal ranking/i);
  });
});
