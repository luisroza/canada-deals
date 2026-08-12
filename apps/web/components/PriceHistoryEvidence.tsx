import Link from "next/link";
import type { ProductHistory } from "../lib/api";
import { StateBadge } from "./StateBadge";

function money(amount: number | null, currency = "CAD") {
  return amount === null ? "Unavailable" : new Intl.NumberFormat("en-CA", { style: "currency", currency }).format(amount);
}

function date(value: string | null) {
  return value ? new Intl.DateTimeFormat("en-CA", { month: "short", day: "numeric", year: "numeric", timeZone: "UTC" }).format(new Date(value)) : "Unavailable";
}

function windowHref(slug: string, window: "30d" | "90d") {
  return `/products/${slug}?history=${window}`;
}

function HistoryChart({ history }: { history: ProductHistory }) {
  const width = 640;
  const height = 220;
  const inset = 28;
  const prices = history.points.map(point => point.lowestPrice);
  const minimum = Math.min(...prices);
  const maximum = Math.max(...prices);
  const range = Math.max(maximum - minimum, 1);
  const x = (index: number) => inset + index * ((width - inset * 2) / Math.max(history.points.length - 1, 1));
  const y = (price: number) => height - inset - ((price - minimum) / range) * (height - inset * 2);
  const segments = history.points.slice(1).map((point, index) => {
    const previous = history.points[index];
    const days = Math.round((new Date(point.observedDate).getTime() - new Date(previous.observedDate).getTime()) / 86_400_000);
    return { previous, point, index, isGap: days > (history.windowDays === 30 ? 10 : 21) };
  });

  return <div className="history-chart-wrap">
    <svg className="history-chart" viewBox={`0 0 ${width} ${height}`} role="img" aria-labelledby="history-chart-title history-chart-desc">
      <title id="history-chart-title">{`Observed product prices over ${history.windowDays} days`}</title>
      <desc id="history-chart-desc">{`${history.coverageSummary} Each point is the lowest qualifying observed price for that day. Dashed segments mark larger evidence gaps.`}</desc>
      <line x1={inset} y1={height - inset} x2={width - inset} y2={height - inset} className="chart-axis" />
      <line x1={inset} y1={inset} x2={inset} y2={height - inset} className="chart-axis" />
      {segments.map(segment => <line key={segment.point.observedDate} x1={x(segment.index)} y1={y(segment.previous.lowestPrice)} x2={x(segment.index + 1)} y2={y(segment.point.lowestPrice)} className={segment.isGap ? "chart-segment chart-gap" : "chart-segment"} />)}
      {history.points.map((point, index) => <circle key={point.observedDate} cx={x(index)} cy={y(point.lowestPrice)} r="5" className="chart-point"><title>{`${date(point.observedDate)}: ${money(point.lowestPrice, point.currency)}`}</title></circle>)}
      <text x={inset} y="18" className="chart-label">{money(maximum)}</text>
      <text x={inset} y={height - 7} className="chart-label">{money(minimum)}</text>
    </svg>
    <details className="history-data"><summary>View observed price data</summary><table><caption>Text equivalent of the Product history chart</caption><thead><tr><th scope="col">Date</th><th scope="col">Lowest observed</th><th scope="col">Observations</th></tr></thead><tbody>{history.points.map(point => <tr key={point.observedDate}><td>{date(point.observedDate)}</td><td>{money(point.lowestPrice, point.currency)}</td><td>{point.observationCount}</td></tr>)}</tbody></table></details>
  </div>;
}

export function PriceHistoryEvidence({ history, productSlug, currentPrice, currentFreshness, error = false }: { history: ProductHistory | null; productSlug: string; currentPrice: number | null; currentFreshness: string; error?: boolean }) {
  const selected = history?.window ?? "30d";
  return <section className="panel history-panel" aria-labelledby="history-heading">
    <div className="history-heading"><div><p className="eyebrow">Bounded observed evidence</p><h2 id="history-heading">Price history</h2></div><div className="history-range" aria-label="History range"><Link href={windowHref(productSlug, "30d")} aria-current={selected === "30d" ? "page" : undefined}>30 days</Link><Link href={windowHref(productSlug, "90d")} aria-current={selected === "90d" ? "page" : undefined}>90 days</Link></div></div>
    <p className="history-current"><strong>Current price:</strong> {money(currentPrice)} <span>· Freshness: {currentFreshness.toLowerCase()}</span></p>
    {error && <div className="error-state" role="alert"><strong>History temporarily unavailable.</strong> Current price and retailer evidence are still available.</div>}
    {!error && history && <>
      <div className="state-row"><StateBadge label={`${history.state.charAt(0)}${history.state.slice(1).toLowerCase()} history`} tone={history.state === "RELIABLE" ? "good" : history.state === "PARTIAL" ? "warning" : "neutral"} /></div>
      {history.state === "UNAVAILABLE" ? <div className="history-unavailable" data-testid="history-unavailable"><h3>Price history unavailable</h3><p>{history.coverageSummary}</p><p>{history.interpretation}</p></div> : <>
        <div className="history-summary" aria-live="polite">
          <p><strong>Lowest observed in the last {history.windowDays} days:</strong> {money(history.lowestObservedPrice)}</p>
          <p><strong>Tracking since:</strong> {date(history.trackingStart)}</p>
          <p><strong>Evidence:</strong> {history.observationCount} qualifying observations across {history.observedDayCount} observed days</p>
          <p>{history.coverageSummary}</p><p>{history.interpretation}</p>
        </div>
        <HistoryChart history={history} />
      </>}
      <p className="history-boundary">Product-level history uses the lowest safely matched, policy-permitted new-product price observed per day. Missing days are not invented. This bounded result is never a claim about the lowest price ever.</p>
    </>}
  </section>;
}

export function PriceHistoryEvidenceLoading({ productSlug, selected, currentPrice, currentFreshness }: { productSlug: string; selected: "30d" | "90d"; currentPrice: number | null; currentFreshness: string }) {
  return <section className="panel history-panel" aria-labelledby="history-heading" aria-busy="true">
    <div className="history-heading"><div><p className="eyebrow">Bounded observed evidence</p><h2 id="history-heading">Price history</h2></div><div className="history-range" aria-label="History range"><Link href={windowHref(productSlug, "30d")} aria-current={selected === "30d" ? "page" : undefined}>30 days</Link><Link href={windowHref(productSlug, "90d")} aria-current={selected === "90d" ? "page" : undefined}>90 days</Link></div></div>
    <p className="history-current"><strong>Current price:</strong> {money(currentPrice)} <span>Â· Freshness: {currentFreshness.toLowerCase()}</span></p>
    <p className="notice" role="status">Loading observed price historyâ€¦</p>
  </section>;
}
