"use client";

import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { getDeals, type DiscoveryParams, type DiscoveryResponse } from "../lib/api";
import { DealFeed } from "./DealFeed";
import { DiscoveryControls } from "./DiscoveryControls";

function paramsFromLocation(): DiscoveryParams {
  const query = new URLSearchParams(window.location.search);
  const value = (key: keyof DiscoveryParams) => query.get(key) ?? undefined;
  return {
    search: value("search") ?? query.get("q") ?? undefined,
    category: value("category"), retailer: value("retailer"), minPrice: value("minPrice"), maxPrice: value("maxPrice"),
    hasReference: value("hasReference"), freshness: value("freshness"), match: value("match"), availability: value("availability"),
    sort: value("sort"), page: value("page"), pageSize: value("pageSize"),
  };
}

function discoveryHref(params: DiscoveryParams) {
  const query = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => { if (value) query.set(key, value); });
  return query.size > 0 ? `/?${query.toString()}` : "/";
}

type UpdateKind = "filters" | "sort" | "history";

export function DiscoveryExperience({ initialParams, initialResult, initialError }: {
  initialParams: DiscoveryParams;
  initialResult?: DiscoveryResponse;
  initialError: boolean;
}) {
  const [params, setParams] = useState(initialParams);
  const [result, setResult] = useState(initialResult);
  const [initialLoadError, setInitialLoadError] = useState(initialError);
  const [updateError, setUpdateError] = useState<Exclude<UpdateKind, "history"> | null>(null);
  const [pendingKind, setPendingKind] = useState<UpdateKind | null>(null);
  const requestVersion = useRef(0);
  const pendingScroll = useRef<{ left: number; top: number } | null>(null);

  useLayoutEffect(() => {
    if (!pendingScroll.current) return;
    const target = pendingScroll.current;
    pendingScroll.current = null;
    window.scrollTo(target.left, target.top);
  }, [params, result]);

  const load = useCallback(async (
    nextParams: DiscoveryParams,
    updateHistory: boolean,
    preserveScroll: boolean,
    kind: UpdateKind,
  ) => {
    const version = ++requestVersion.current;
    if (preserveScroll) pendingScroll.current = { left: window.scrollX, top: window.scrollY };
    setPendingKind(kind);
    setUpdateError(null);

    try {
      const nextResult = await getDeals(nextParams);
      if (version !== requestVersion.current) return false;

      setResult(nextResult);
      setParams(nextParams);
      setInitialLoadError(false);
      if (updateHistory) window.history.pushState(null, "", discoveryHref(nextParams));
      return true;
    } catch {
      if (version === requestVersion.current) {
        pendingScroll.current = null;
        if (kind !== "history") setUpdateError(kind);
      }
      return false;
    } finally {
      if (version === requestVersion.current) setPendingKind(null);
    }
  }, []);

  useEffect(() => {
    const restoreFromHistory = () => void load(paramsFromLocation(), false, false, "history");
    window.addEventListener("popstate", restoreFromHistory);
    return () => window.removeEventListener("popstate", restoreFromHistory);
  }, [load]);

  function applyFilters(filters: Pick<DiscoveryParams, "category" | "retailer">) {
    return load({ ...params, ...filters, page: undefined }, true, true, "filters");
  }

  function removeFilter(key: "category" | "retailer") {
    return applyFilters({
      category: key === "category" ? undefined : params.category,
      retailer: key === "retailer" ? undefined : params.retailer,
    });
  }

  function clearFilters() {
    return applyFilters({ category: undefined, retailer: undefined });
  }

  function clearSelection() {
    return load({}, true, true, "filters");
  }

  function changeSort(sort: string) {
    if (pendingKind) return Promise.resolve(false);
    return load({ ...params, sort, page: undefined }, true, false, "sort");
  }

  return <>
    {result && <DiscoveryControls
      params={params}
      categories={result.facets.categories}
      retailers={result.facets.retailers}
      resultCount={result.count}
      pending={Boolean(pendingKind)}
      onApply={applyFilters}
      onRemove={removeFilter}
      onClear={clearFilters}
    />}
    <DealFeed
      params={params}
      result={result}
      initialLoadError={initialLoadError}
      updateError={updateError}
      pendingKind={pendingKind}
      onSort={changeSort}
      onClearFilters={clearSelection}
    />
  </>;
}
