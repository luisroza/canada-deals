"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FormEvent, KeyboardEvent, useEffect, useId, useRef, useState } from "react";
import type { DiscoveryResponse } from "../lib/api";

type Suggestion = {
  key: string;
  label: string;
  context: string;
  href: string;
};

function suggestionsFrom(response: DiscoveryResponse): Suggestion[] {
  const products = response.items.slice(0, 5).map((item) => ({
    key: `product-${item.productId}`,
    label: item.productTitle,
    context: `${item.brand} · ${item.retailer}`,
    href: item.detailsPath,
  }));
  const categories = response.facets.categories.slice(0, 3).map((item) => ({
    key: `category-${item.key}`,
    label: item.label,
    context: "Browse category",
    href: `/?category=${encodeURIComponent(item.key)}`,
  }));
  return [...products, ...categories];
}

export function GlobalSearch() {
  const router = useRouter();
  const instanceId = useId().replace(/:/g, "");
  const containerRef = useRef<HTMLDivElement>(null);
  const [query, setQuery] = useState("");
  const [suggestions, setSuggestions] = useState<Suggestion[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const [hydrated, setHydrated] = useState(false);
  const listboxId = `global-search-suggestions-${instanceId}`;

  useEffect(() => { setHydrated(true); }, []);

  useEffect(() => {
    const normalized = query.trim();
    if (normalized.length < 2) {
      setSuggestions([]);
      setOpen(false);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    const timeout = window.setTimeout(async () => {
      setLoading(true);
      try {
        const response = await fetch(`/api/v1/deals?search=${encodeURIComponent(normalized)}&pageSize=6`, {
          cache: "no-store",
          signal: controller.signal,
        });
        if (!response.ok) throw new Error("Suggestions unavailable");
        const next = suggestionsFrom(await response.json() as DiscoveryResponse);
        setSuggestions(next);
        setActiveIndex(-1);
        setOpen(true);
      } catch (error) {
        if ((error as Error).name !== "AbortError") {
          setSuggestions([]);
          setOpen(true);
        }
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }, 220);

    return () => {
      window.clearTimeout(timeout);
      controller.abort();
    };
  }, [query]);

  function submit(event: FormEvent<HTMLFormElement>) {
    if (activeIndex >= 0 && suggestions[activeIndex]) {
      event.preventDefault();
      router.push(suggestions[activeIndex].href);
      setOpen(false);
    }
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "ArrowDown" && suggestions.length) {
      event.preventDefault();
      setOpen(true);
      setActiveIndex((current) => current >= suggestions.length - 1 ? 0 : current + 1);
    } else if (event.key === "ArrowUp" && suggestions.length) {
      event.preventDefault();
      setOpen(true);
      setActiveIndex((current) => current <= 0 ? suggestions.length - 1 : current - 1);
    } else if (event.key === "Escape") {
      setOpen(false);
      setActiveIndex(-1);
    }
  }

  return (
    <div
      className="global-search"
      ref={containerRef}
      onBlur={(event) => {
        if (!containerRef.current?.contains(event.relatedTarget as Node | null)) setOpen(false);
      }}
    >
      <form action="/" method="get" role="search" onSubmit={submit}>
        <label className="visually-hidden" htmlFor="global-search-input">Search products, models, or categories</label>
        <div className="global-search-row">
          <input
            id="global-search-input"
            name="search"
            type="search"
            disabled={!hydrated}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onFocus={() => query.trim().length >= 2 && setOpen(true)}
            onKeyDown={onKeyDown}
            placeholder="Search products or model numbers"
            autoComplete="off"
            role="combobox"
            aria-autocomplete="list"
            aria-expanded={open}
            aria-controls={listboxId}
            aria-activedescendant={activeIndex >= 0 ? `${listboxId}-${activeIndex}` : undefined}
          />
          <button className="global-search-submit" type="submit" aria-label="Search site" disabled={!hydrated}>Search</button>
        </div>
      </form>
      <span className="visually-hidden global-search-status" aria-live="polite" aria-atomic="true">
        {loading ? "Loading suggestions" : open && query.trim().length >= 2 ? `${suggestions.length} suggestions available` : ""}
      </span>
      {open && query.trim().length >= 2 && (
        <div className="search-suggestions-panel">
          {suggestions.length > 0 ? (
            <ul id={listboxId} role="listbox" aria-label="Search suggestions">
              {suggestions.map((suggestion, index) => (
                <li key={suggestion.key} id={`${listboxId}-${index}`} role="option" aria-selected={activeIndex === index}>
                  <Link href={suggestion.href} onMouseEnter={() => setActiveIndex(index)} onClick={() => setOpen(false)}>
                    <span>{suggestion.label}</span><small>{suggestion.context}</small>
                  </Link>
                </li>
              ))}
            </ul>
          ) : !loading ? <p>No matching suggestions. Search to see all results.</p> : null}
          <Link className="search-all-results" href={`/?search=${encodeURIComponent(query.trim())}`} onClick={() => setOpen(false)}>View all results</Link>
        </div>
      )}
    </div>
  );
}
