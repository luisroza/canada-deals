import { fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import type { StoreBannerData } from "../lib/api";
import { StoreBanner } from "./StoreBanner";
import { calculateStoreCarousel, StoreBannerRail } from "./StoreBannerRail";

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
  it("calculates one banner per phone page and four per desktop page", () => {
    expect(calculateStoreCarousel({ clientWidth: 390, scrollWidth: 1970, scrollLeft: 0, itemWidth: 390, gap: 5, itemCount: 5 })).toMatchObject({ pages: 5, itemsPerPage: 1, page: 1, canNext: true });
    expect(calculateStoreCarousel({ clientWidth: 1200, scrollWidth: 1503, scrollLeft: 0, itemWidth: 291, gap: 12, itemCount: 5 })).toMatchObject({ pages: 2, itemsPerPage: 4, page: 1, canNext: true });
  });

  it("tracks the final mobile page without creating an extra page for gaps", () => {
    expect(calculateStoreCarousel({ clientWidth: 390, scrollWidth: 1970, scrollLeft: 1580, itemWidth: 390, gap: 5, itemCount: 5 })).toMatchObject({ pages: 5, page: 5, canNext: false, canPrevious: true });
    expect(calculateStoreCarousel({ clientWidth: 1200, scrollWidth: 1503, scrollLeft: 303, itemWidth: 291, gap: 12, itemCount: 5 })).toMatchObject({ pages: 2, page: 2, canNext: false, canPrevious: true });
  });

  it("keeps every enabled banner in editorial order inside the four-column carousel", () => {
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
    expect(container.textContent).not.toMatch(/may earn a commission/i);
    expect([...container.querySelectorAll("a")].map((link) => link.textContent)).toEqual([
      expect.stringContaining("Shop Demo 1"),
      expect.stringContaining("Shop Demo 2"),
      expect.stringContaining("Shop Demo 3"),
      expect.stringContaining("Shop Demo 4"),
      expect.stringContaining("Shop Demo 5"),
    ]);
    expect(screen.getByRole("button", { name: "Previous store banners" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Next store banners" })).toBeDisabled();
    expect(screen.getByRole("status")).toHaveTextContent("5 stores");
    expect(container.textContent).not.toMatch(/never changes store order or deal ranking/i);
  });

  it("shows one discrete banner at a time on phones and supports buttons and swipe", async () => {
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      value: (query: string) => ({
        matches: query === "(max-width: 760px)",
        media: query,
        onchange: null,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        addListener: () => undefined,
        removeListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
    const banners = Array.from({ length: 3 }, (_, index) => banner({
      retailerKey: `mobile-${index + 1}`,
      displayName: `Mobile ${index + 1}`,
      title: `Shop Mobile ${index + 1}`,
      href: `/?retailer=mobile-${index + 1}#deals`,
    }));

    const { container } = render(<StoreBannerRail banners={banners} />);
    const mobileCarousel = within(container);
    await waitFor(() => expect(mobileCarousel.getByRole("status")).toHaveTextContent("Page 1 of 3"));
    const cards = [...container.querySelectorAll(".store-banner")];
    expect(cards.map((card) => card.getAttribute("data-mobile-active"))).toEqual(["true", "false", "false"]);

    fireEvent.click(mobileCarousel.getByRole("button", { name: "Next store banners" }));
    await waitFor(() => expect(mobileCarousel.getByRole("status")).toHaveTextContent("Page 2 of 3"));
    expect(cards.map((card) => card.getAttribute("data-mobile-active"))).toEqual(["false", "true", "false"]);

    const rail = container.querySelector(".store-banner-rail")!;
    fireEvent.touchStart(rail, { changedTouches: [{ clientX: 300 }] });
    fireEvent.touchEnd(rail, { changedTouches: [{ clientX: 200 }] });
    await waitFor(() => expect(mobileCarousel.getByRole("status")).toHaveTextContent("Page 3 of 3"));
    expect(cards.map((card) => card.getAttribute("data-mobile-active"))).toEqual(["false", "false", "true"]);
  });
});
