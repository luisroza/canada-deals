export function SearchForm({ initialQuery = "" }: { initialQuery?: string }) {
  return (
    <form className="search-form" action="/" method="get" role="search">
      <label htmlFor="deal-search">Search a product or model number</label>
      <div className="search-row">
        <input id="deal-search" name="q" defaultValue={initialQuery} placeholder="Try Northstar, NS55QLED-2026, or cordless drill" />
        <button className="button button-primary" type="submit">Search deals</button>
      </div>
    </form>
  );
}
