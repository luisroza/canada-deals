"use client";

import { useCallback, useEffect, useRef, useState, type TouchEvent } from "react";
import type { StoreBannerData } from "../lib/api";
import { StoreBanner } from "./StoreBanner";

type CarouselMeasurements = {
  clientWidth: number;
  scrollWidth: number;
  scrollLeft: number;
  itemWidth: number;
  gap: number;
  itemCount: number;
};

function isMobileCarousel() {
  return window.matchMedia?.("(max-width: 760px)").matches ?? window.innerWidth <= 760;
}

export function calculateStoreCarousel(measurements: CarouselMeasurements) {
  const { clientWidth, scrollWidth, scrollLeft, itemWidth, gap, itemCount } = measurements;
  const step = Math.max(1, itemWidth + gap);
  const itemsPerPage = Math.max(1, Math.floor((clientWidth + gap + 1) / step));
  const pages = Math.max(1, Math.ceil(itemCount / itemsPerPage));
  const maximumScroll = Math.max(0, scrollWidth - clientWidth);
  const calculatedPage = Math.round(scrollLeft / (step * itemsPerPage)) + 1;
  const page = maximumScroll > 0 && scrollLeft >= maximumScroll - 2
    ? pages
    : Math.min(pages, Math.max(1, calculatedPage));
  return {
    page,
    pages,
    itemsPerPage,
    step,
    maximumScroll,
    canPrevious: scrollLeft > 2,
    canNext: scrollLeft < maximumScroll - 2,
  };
}

export function StoreBannerRail({ banners }: { banners: StoreBannerData[] }) {
  const visibleBanners = banners
    .filter((banner) => banner.affiliateStatus !== "DISABLED");
  const railRef = useRef<HTMLDivElement>(null);
  const touchStartXRef = useRef<number | null>(null);
  const [mobileIndex, setMobileIndex] = useState(0);
  const [carousel, setCarousel] = useState({ page: 1, pages: 1, canPrevious: false, canNext: false });

  const updateCarousel = useCallback(() => {
    const rail = railRef.current;
    if (!rail) return;
    if (isMobileCarousel()) {
      rail.scrollLeft = 0;
      const page = Math.min(Math.max(1, mobileIndex + 1), Math.max(1, visibleBanners.length));
      setCarousel({
        page,
        pages: Math.max(1, visibleBanners.length),
        canPrevious: page > 1,
        canNext: page < visibleBanners.length,
      });
      return;
    }
    const firstBanner = rail.firstElementChild as HTMLElement | null;
    const gap = Number.parseFloat(window.getComputedStyle(rail).columnGap) || 0;
    const itemWidth = firstBanner?.getBoundingClientRect().width ?? rail.clientWidth;
    setCarousel(calculateStoreCarousel({
      clientWidth: rail.clientWidth,
      scrollWidth: rail.scrollWidth,
      scrollLeft: rail.scrollLeft,
      itemWidth,
      gap,
      itemCount: rail.children.length,
    }));
  }, [mobileIndex, visibleBanners.length]);

  useEffect(() => {
    setMobileIndex((current) => Math.min(current, Math.max(0, visibleBanners.length - 1)));
  }, [visibleBanners.length]);

  useEffect(() => {
    const rail = railRef.current;
    if (!rail) return;
    updateCarousel();
    rail.addEventListener("scroll", updateCarousel, { passive: true });
    window.addEventListener("resize", updateCarousel);
    const resizeObserver = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(updateCarousel);
    resizeObserver?.observe(rail);
    return () => {
      rail.removeEventListener("scroll", updateCarousel);
      window.removeEventListener("resize", updateCarousel);
      resizeObserver?.disconnect();
    };
  }, [updateCarousel, visibleBanners.length]);

  function move(direction: -1 | 1) {
    const rail = railRef.current;
    if (!rail) return;
    if (isMobileCarousel()) {
      setMobileIndex((current) => Math.min(visibleBanners.length - 1, Math.max(0, current + direction)));
      return;
    }
    const firstBanner = rail.firstElementChild as HTMLElement | null;
    const gap = Number.parseFloat(window.getComputedStyle(rail).columnGap) || 0;
    const metrics = calculateStoreCarousel({
      clientWidth: rail.clientWidth,
      scrollWidth: rail.scrollWidth,
      scrollLeft: rail.scrollLeft,
      itemWidth: firstBanner?.getBoundingClientRect().width ?? rail.clientWidth,
      gap,
      itemCount: rail.children.length,
    });
    const targetPage = Math.min(metrics.pages, Math.max(1, carousel.page + direction));
    const targetLeft = Math.min(metrics.maximumScroll, (targetPage - 1) * metrics.step * metrics.itemsPerPage);
    const reduceMotion = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ?? false;
    rail.scrollTo({ left: targetLeft, behavior: reduceMotion ? "auto" : "smooth" });
  }

  function handleTouchStart(event: TouchEvent<HTMLDivElement>) {
    touchStartXRef.current = event.changedTouches[0]?.clientX ?? null;
  }

  function handleTouchEnd(event: TouchEvent<HTMLDivElement>) {
    const startX = touchStartXRef.current;
    touchStartXRef.current = null;
    if (startX === null || !isMobileCarousel()) return;
    const endX = event.changedTouches[0]?.clientX;
    if (endX === undefined) return;
    const distance = endX - startX;
    if (Math.abs(distance) < 44) return;
    move(distance < 0 ? 1 : -1);
  }

  if (visibleBanners.length === 0) return null;

  return (
    <section className="store-banners" aria-labelledby="store-banners-heading">
      <div className="store-banners-heading">
        <div>
          <p className="eyebrow">Stores</p>
          <h2 id="store-banners-heading">Browse deals by store</h2>
        </div>
        <div className="store-carousel-controls" aria-label="Store banner carousel controls">
          <span role="status" aria-live="polite">{visibleBanners.length} {visibleBanners.length === 1 ? "store" : "stores"} · Page {carousel.page} of {carousel.pages}</span>
          <button type="button" onClick={() => move(-1)} disabled={!carousel.canPrevious} aria-label="Previous store banners">←</button>
          <button type="button" onClick={() => move(1)} disabled={!carousel.canNext} aria-label="Next store banners">→</button>
        </div>
      </div>
      <div
        className="store-banner-rail"
        ref={railRef}
        aria-label="Store banners"
        onTouchStart={handleTouchStart}
        onTouchEnd={handleTouchEnd}
      >
        {visibleBanners.map((banner, index) => (
          <StoreBanner banner={banner} mobileActive={index === mobileIndex} imagePriority={index === 0} key={banner.retailerKey} />
        ))}
      </div>
    </section>
  );
}
