"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { signIn, signOut } from "../lib/account";
import {
  AdminApiError,
  createAdminOffer,
  getAdminDashboard,
  updateAdminBanner,
  updateAdminOffer,
  updateAdminReport,
  type AdminBanner,
  type AdminBannerInput,
  type AdminDashboard,
  type AdminOffer,
  type AdminOfferInput,
} from "../lib/admin";

type AccessState = "loading" | "authorized" | "signed-out" | "forbidden" | "unavailable";
type Section = "overview" | "offers" | "banners" | "reports" | "audit";
type OfferForm = Omit<AdminOfferInput, "currentPrice" | "packQuantity" | "observedAt" | "fetchedAt" | "variantAttributes" | "externalIdentifiers"> & {
  currentPrice: string; packQuantity: string; observedAt: string; fetchedAt: string; variantAttributes: string; externalIdentifiers: string;
};

const assetPaths = [
  "/store-banners/electronics-devices.svg",
  "/store-banners/home-decor.svg",
  "/store-banners/marketplace-bags.svg",
  "/store-banners/marketplace-collection.svg",
  "/store-banners/marketplace-packages.svg",
  "/store-banners/office-workspace.svg",
  "/store-banners/pc-hardware.svg",
  "/store-banners/workstation.svg",
];

function localDateTime(value?: string | null) {
  const date = value ? new Date(value) : new Date();
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

function clean(value: string) { return value.trim() || null; }
function jsonText(value: Record<string, string>) { return JSON.stringify(value, null, 2); }

function offerToForm(offer: AdminOffer): OfferForm {
  return {
    slug: offer.slug, productTitle: offer.productTitle, brandId: offer.brandId, categoryId: offer.categoryId,
    modelNumber: offer.modelNumber, manufacturerPartNumber: offer.manufacturerPartNumber, gtin: offer.gtin,
    variantAttributes: jsonText(offer.variantAttributes), retailerId: offer.retailerId, merchantPolicyId: offer.merchantPolicyId,
    externalListingId: offer.externalListingId, retailerSku: offer.retailerSku, originalTitle: offer.originalTitle,
    productUrl: offer.productUrl, approvedAffiliateDestinationReference: offer.approvedAffiliateDestinationReference,
    seller: offer.seller, isMarketplaceSeller: offer.isMarketplaceSeller, conditionState: offer.conditionState,
    packQuantity: offer.packQuantity?.toString() ?? "", bundleContents: offer.bundleContents,
    regionAvailabilityContext: offer.regionAvailabilityContext, availabilityState: offer.availabilityState,
    shippingContext: offer.shippingContext, externalIdentifiers: jsonText(offer.externalIdentifiers),
    currentPrice: offer.currentPrice?.toFixed(2) ?? "", observedAt: localDateTime(offer.observedAt), fetchedAt: localDateTime(offer.fetchedAt),
    matchState: offer.matchState, isEnabled: offer.isEnabled, changeReason: null,
  };
}

function emptyOffer(dashboard: AdminDashboard): OfferForm {
  const now = localDateTime();
  return {
    slug: "", productTitle: "", brandId: dashboard.brands[0]?.id ?? "", categoryId: dashboard.categories[0]?.id ?? "",
    modelNumber: null, manufacturerPartNumber: null, gtin: null, variantAttributes: "{}",
    retailerId: dashboard.retailers.find(item => item.isEnabled)?.id ?? "", merchantPolicyId: dashboard.policies[0]?.id ?? "",
    externalListingId: "", retailerSku: null, originalTitle: "", productUrl: "", approvedAffiliateDestinationReference: null,
    seller: null, isMarketplaceSeller: false, conditionState: "NEW", packQuantity: "1", bundleContents: null,
    regionAvailabilityContext: "Canada", availabilityState: "AVAILABLE", shippingContext: null, externalIdentifiers: "{}",
    currentPrice: "", observedAt: now, fetchedAt: now, matchState: "CONFIRMED", isEnabled: false, changeReason: null,
  };
}

function bannerInput(banner: AdminBanner): AdminBannerInput {
  return {
    title: banner.title, subtitle: banner.subtitle, assetPath: banner.assetPath ?? assetPaths[0], assetSource: banner.assetSource,
    assetProvider: banner.assetProvider, assetEvidenceReference: banner.assetEvidenceReference, allowedPlacement: banner.allowedPlacement ?? "store_banner",
    effectiveAt: banner.effectiveAt ? localDateTime(banner.effectiveAt) : null, expiresAt: banner.expiresAt ? localDateTime(banner.expiresAt) : null,
    bannerOrder: banner.bannerOrder === 2147483647 ? 100 : banner.bannerOrder, isEnabled: banner.isEnabled, changeReason: null,
  };
}

export function AdminPanel() {
  const [access, setAccess] = useState<AccessState>("loading");
  const [dashboard, setDashboard] = useState<AdminDashboard | null>(null);
  const [section, setSection] = useState<Section>("overview");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const load = useCallback(async () => {
    try {
      const result = await getAdminDashboard();
      setDashboard(result); setAccess("authorized"); setError(null);
    } catch (caught) {
      if (caught instanceof AdminApiError && caught.status === 401) setAccess("signed-out");
      else if (caught instanceof AdminApiError && caught.status === 403) setAccess("forbidden");
      else { setAccess("unavailable"); setError(caught instanceof Error ? caught.message : "The panel is unavailable."); }
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  if (access === "loading") return <div className="admin-access"><p role="status">Loading the secure workspace…</p></div>;
  if (access !== "authorized" || !dashboard) return <AdminSignIn access={access} error={error} onAuthorized={load} />;

  async function logout() {
    setPending(true);
    try { await signOut(); setDashboard(null); setAccess("signed-out"); }
    finally { setPending(false); }
  }

  return (
    <div className="admin-panel-shell">
      <header className="admin-topbar">
        <div><span className="admin-brand-mark" aria-hidden="true">G</span><strong>GreatDeals.ca</strong><span>Admin</span></div>
        <div><Link href="/">View public site</Link><button type="button" className="button button-secondary" onClick={logout} disabled={pending}>Sign out</button></div>
      </header>
      <div className="admin-layout">
        <nav className="admin-nav" aria-label="Administration">
          {(["overview", "offers", "banners", "reports", "audit"] as Section[]).map(item => (
            <button key={item} type="button" aria-current={section === item ? "page" : undefined} onClick={() => { setSection(item); setMessage(null); setError(null); }}>
              {item[0].toUpperCase() + item.slice(1)}
            </button>
          ))}
        </nav>
        <div className="admin-main" id="admin-content">
          {message && <p className="admin-notice" role="status">{message}</p>}
          {error && <p className="field-error admin-error" role="alert">{error}</p>}
          {section === "overview" && <Overview dashboard={dashboard} navigate={setSection} />}
          {section === "offers" && <Offers dashboard={dashboard} refresh={load} notify={setMessage} reportError={setError} />}
          {section === "banners" && <Banners dashboard={dashboard} refresh={load} notify={setMessage} reportError={setError} />}
          {section === "reports" && <Reports dashboard={dashboard} refresh={load} notify={setMessage} reportError={setError} />}
          {section === "audit" && <Audit dashboard={dashboard} />}
        </div>
      </div>
    </div>
  );
}

function AdminSignIn({ access, error, onAuthorized }: { access: AccessState; error: string | null; onAuthorized: () => Promise<void> }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [pending, setPending] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setPending(true); setLocalError(null);
    try { await signIn(email, password); await onAuthorized(); }
    catch (caught) { setLocalError(caught instanceof Error ? caught.message : "Sign-in failed."); }
    finally { setPending(false); setPassword(""); }
  }

  async function resetSession() {
    setPending(true);
    try { await signOut(); window.location.reload(); }
    catch { window.location.reload(); }
  }

  return (
    <div className="admin-access">
      <section className="admin-login-card" aria-labelledby="admin-login-heading">
        <span className="admin-brand-mark" aria-hidden="true">G</span>
        <p className="eyebrow">Restricted workspace</p>
        <h1 id="admin-login-heading">GreatDeals.ca Admin</h1>
        <p>Use the owner administrator account. This route is not part of public navigation.</p>
        {access === "forbidden" ? (
          <div className="admin-denied" role="alert"><strong>This account is not authorized.</strong><p>Sign out, then use the owner administrator account.</p><button className="button button-secondary" type="button" onClick={resetSession} disabled={pending}>Use another account</button></div>
        ) : (
          <form onSubmit={submit} noValidate>
            <label htmlFor="admin-email">Email</label>
            <input id="admin-email" type="email" autoComplete="username" required maxLength={254} value={email} onChange={event => setEmail(event.target.value)} />
            <label htmlFor="admin-password">Password</label>
            <input id="admin-password" type="password" autoComplete="current-password" required maxLength={128} value={password} onChange={event => setPassword(event.target.value)} />
            {(localError || error) && <p className="field-error" role="alert">{localError ?? error}</p>}
            <button className="button button-primary" type="submit" disabled={pending}>{pending ? "Checking…" : "Sign in securely"}</button>
          </form>
        )}
        <Link href="/">Return to public site</Link>
      </section>
    </div>
  );
}

function Overview({ dashboard, navigate }: { dashboard: AdminDashboard; navigate: (section: Section) => void }) {
  const { counts } = dashboard;
  return <>
    <div className="admin-heading"><div><p className="eyebrow">Editorial operations</p><h1>Dashboard</h1><p>Publish deliberately. Affiliate, evidence, and asset-rights states remain derived by the backend.</p></div><button className="button button-primary" type="button" onClick={() => navigate("offers")}>Add offer</button></div>
    <section className="admin-stats" aria-label="Operational summary">
      <article><strong>{counts.publishedOffers}</strong><span>Enabled offers</span></article>
      <article><strong>{counts.draftOffers}</strong><span>Draft or disabled</span></article>
      <article><strong>{counts.enabledBanners}</strong><span>Ready banners</span></article>
      <article><strong>{counts.openReports}</strong><span>Open customer reports</span></article>
    </section>
    <section className="admin-card"><h2>Needs attention</h2>
      {counts.blockedOrExpiredBanners > 0 && <p><span className="status-chip status-warning">Banner</span> {counts.blockedOrExpiredBanners} banner assets are blocked or expired.</p>}
      {counts.openReports > 0 && <p><span className="status-chip status-warning">Reports</span> {counts.openReports} customer reports await review. <button className="button-text" type="button" onClick={() => navigate("reports")}>Open review queue</button></p>}
      {counts.blockedOrExpiredBanners === 0 && counts.openReports === 0 && <p>No immediate operational warnings.</p>}
    </section>
    <section className="admin-card"><h2>Publication rules</h2><ul><li>Offers remain drafts until enabled and permitted by their Merchant Policy.</li><li>Reference prices and evidence are never typed manually.</li><li>Banner tracking destinations remain provider-managed and cannot be pasted here.</li></ul></section>
  </>;
}

function Offers({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminOffer | "new" | null>(null);
  const [form, setForm] = useState<OfferForm | null>(null);
  const [pending, setPending] = useState(false);
  const [filter, setFilter] = useState("");

  const visible = useMemo(() => dashboard.offers.filter(offer => `${offer.productTitle} ${offer.retailer} ${offer.externalListingId}`.toLowerCase().includes(filter.toLowerCase())), [dashboard.offers, filter]);

  function open(value: AdminOffer | "new") { setSelected(value); setForm(value === "new" ? emptyOffer(dashboard) : offerToForm(value)); notify(null); reportError(null); }
  function field<K extends keyof OfferForm>(key: K, value: OfferForm[K]) { setForm(current => current ? { ...current, [key]: value } : current); }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!form) return; setPending(true); notify(null); reportError(null);
    try {
      const variants = JSON.parse(form.variantAttributes || "{}") as Record<string, string>;
      const identifiers = JSON.parse(form.externalIdentifiers || "{}") as Record<string, string>;
      const input: AdminOfferInput = {
        ...form, modelNumber: clean(form.modelNumber ?? ""), manufacturerPartNumber: clean(form.manufacturerPartNumber ?? ""), gtin: clean(form.gtin ?? ""),
        retailerSku: clean(form.retailerSku ?? ""), approvedAffiliateDestinationReference: clean(form.approvedAffiliateDestinationReference ?? ""), seller: clean(form.seller ?? ""),
        packQuantity: form.packQuantity ? Number(form.packQuantity) : null, bundleContents: clean(form.bundleContents ?? ""), regionAvailabilityContext: clean(form.regionAvailabilityContext ?? ""),
        shippingContext: clean(form.shippingContext ?? ""), currentPrice: Number(form.currentPrice), observedAt: new Date(form.observedAt).toISOString(), fetchedAt: new Date(form.fetchedAt).toISOString(),
        variantAttributes: variants, externalIdentifiers: identifiers, changeReason: clean(form.changeReason ?? ""),
      };
      if (selected === "new") await createAdminOffer(input); else if (selected) await updateAdminOffer(selected.listingId, input);
      await refresh(); setSelected(null); setForm(null); notify(selected === "new" ? "Offer saved. Drafts remain hidden until enabled." : "Offer updated and audited.");
    } catch (caught) { reportError(caught instanceof SyntaxError ? "Variant attributes and external identifiers must be valid JSON objects." : caught instanceof Error ? caught.message : "Offer could not be saved."); }
    finally { setPending(false); }
  }

  if (form) return <OfferEditor dashboard={dashboard} selected={selected} form={form} field={field} save={save} cancel={() => { setSelected(null); setForm(null); }} pending={pending} />;

  return <>
    <div className="admin-heading"><div><p className="eyebrow">Catalog operations</p><h1>Offers</h1><p>Create drafts, review readiness, publish, or reversibly deactivate.</p></div><button className="button button-primary" type="button" onClick={() => open("new")}>Add offer</button></div>
    <label className="admin-search">Search offers<input type="search" value={filter} onChange={event => setFilter(event.target.value)} placeholder="Product, retailer, or external ID" /></label>
    <div className="admin-table" role="region" aria-label="Administrative offers" tabIndex={0}>
      <table><thead><tr><th>Product</th><th>Retailer</th><th>Price</th><th>Status</th><th>Readiness</th><th><span className="sr-only">Actions</span></th></tr></thead>
        <tbody>{visible.map(offer => <tr key={offer.listingId}><td><strong>{offer.productTitle}</strong><small>{offer.externalListingId}</small></td><td>{offer.retailer}</td><td>{offer.currentPrice?.toLocaleString("en-CA", { style: "currency", currency: "CAD" })}</td><td><span className={`status-chip ${offer.isEnabled ? "status-ready" : ""}`}>{offer.isEnabled ? "Enabled" : "Draft / disabled"}</span></td><td>{offer.isPubliclyEligible ? "Ready" : "Blocked"}</td><td><button className="button button-secondary" type="button" onClick={() => open(offer)}>Edit</button></td></tr>)}</tbody></table>
      {visible.length === 0 && <p className="admin-empty">No offers match this search.</p>}
    </div>
  </>;
}

function OfferEditor({ dashboard, selected, form, field, save, cancel, pending }: { dashboard: AdminDashboard; selected: AdminOffer | "new" | null; form: OfferForm; field: <K extends keyof OfferForm>(key: K, value: OfferForm[K]) => void; save: (event: FormEvent<HTMLFormElement>) => Promise<void>; cancel: () => void; pending: boolean }) {
  const existing = selected !== "new" && selected !== null;
  return <form className="admin-editor" onSubmit={save} noValidate>
    <div className="admin-heading"><div><p className="eyebrow">{existing ? "Edit offer" : "New offer"}</p><h1>{form.productTitle || "Untitled offer"}</h1><p>{existing ? selected.readinessSummary : "New offers start as drafts."}</p></div><div className="admin-heading-actions">{existing && <Link className="button button-secondary" href={selected.previewPath} target="_blank">Public preview</Link>}<button className="button button-secondary" type="button" onClick={cancel}>Cancel</button><button className="button button-primary" type="submit" disabled={pending}>{pending ? "Saving…" : "Save offer"}</button></div></div>
    <div className="admin-editor-grid"><div className="admin-form-stack">
      <details open><summary>Product identity</summary><div className="admin-form-grid">
        <label className="span-2">Product title<input required maxLength={240} value={form.productTitle} onChange={e => field("productTitle", e.target.value)} /></label>
        <label>Slug<input required pattern="[a-z0-9-]+" value={form.slug} onChange={e => field("slug", e.target.value.toLowerCase())} /></label>
        <label>Brand<select value={form.brandId} onChange={e => field("brandId", e.target.value)}>{dashboard.brands.map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Category<select value={form.categoryId} onChange={e => field("categoryId", e.target.value)}>{dashboard.categories.map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Model number<input value={form.modelNumber ?? ""} onChange={e => field("modelNumber", e.target.value)} /></label>
        <label>MPN<input value={form.manufacturerPartNumber ?? ""} onChange={e => field("manufacturerPartNumber", e.target.value)} /></label>
        <label>GTIN<input value={form.gtin ?? ""} onChange={e => field("gtin", e.target.value)} /></label>
        <label className="span-2">Variant attributes (JSON)<textarea rows={5} value={form.variantAttributes} onChange={e => field("variantAttributes", e.target.value)} /></label>
      </div></details>
      <details open><summary>Retailer listing</summary><div className="admin-form-grid">
        <label>Retailer<select disabled={existing} value={form.retailerId} onChange={e => field("retailerId", e.target.value)}>{dashboard.retailers.filter(item => item.isEnabled).map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Merchant policy<select disabled={existing} value={form.merchantPolicyId} onChange={e => field("merchantPolicyId", e.target.value)}>{dashboard.policies.map(item => <option key={item.id} value={item.id}>{item.sourceKey} · price {item.priceStorage}</option>)}</select></label>
        <label>External listing ID<input required disabled={existing} value={form.externalListingId} onChange={e => field("externalListingId", e.target.value)} /></label>
        <label>Retailer SKU<input value={form.retailerSku ?? ""} onChange={e => field("retailerSku", e.target.value)} /></label>
        <label className="span-2">Original retailer title<input required value={form.originalTitle} onChange={e => field("originalTitle", e.target.value)} /></label>
        <label className="span-2">Product URL<input required type="url" placeholder="https://" value={form.productUrl} onChange={e => field("productUrl", e.target.value)} /></label>
        <label className="span-2">Approved destination reference<input type="url" placeholder="Optional; policy must permit affiliate links" value={form.approvedAffiliateDestinationReference ?? ""} onChange={e => field("approvedAffiliateDestinationReference", e.target.value)} /></label>
        <label>Seller<input value={form.seller ?? ""} onChange={e => field("seller", e.target.value)} /></label>
        <label>Condition<select value={form.conditionState} onChange={e => field("conditionState", e.target.value)}><option>NEW</option><option>REFURBISHED</option><option>USED</option><option>UNKNOWN</option></select></label>
        <label>Marketplace seller<select value={form.isMarketplaceSeller === null ? "unknown" : String(form.isMarketplaceSeller)} onChange={e => field("isMarketplaceSeller", e.target.value === "unknown" ? null : e.target.value === "true")}><option value="false">No</option><option value="true">Yes</option><option value="unknown">Unknown</option></select></label>
        <label>Pack quantity<input type="number" min="1" max="1000" value={form.packQuantity} onChange={e => field("packQuantity", e.target.value)} /></label>
        <label className="span-2">Bundle contents<textarea rows={3} value={form.bundleContents ?? ""} onChange={e => field("bundleContents", e.target.value)} /></label>
        <label className="span-2">External identifiers (JSON)<textarea rows={5} value={form.externalIdentifiers} onChange={e => field("externalIdentifiers", e.target.value)} /></label>
      </div></details>
      <details open><summary>Current offer facts</summary><div className="admin-form-grid">
        <label>Current price (CAD)<input required type="number" min="0.01" max="1000000" step="0.01" value={form.currentPrice} onChange={e => field("currentPrice", e.target.value)} /></label>
        <label>Availability<select value={form.availabilityState} onChange={e => field("availabilityState", e.target.value)}><option>AVAILABLE</option><option>UNAVAILABLE</option><option>UNKNOWN</option></select></label>
        <label>Observed at<input required type="datetime-local" value={form.observedAt} onChange={e => field("observedAt", e.target.value)} /></label>
        <label>Fetched at<input required type="datetime-local" value={form.fetchedAt} onChange={e => field("fetchedAt", e.target.value)} /></label>
        <label>Region<input value={form.regionAvailabilityContext ?? ""} onChange={e => field("regionAvailabilityContext", e.target.value)} /></label>
        <label>Shipping context<input value={form.shippingContext ?? ""} onChange={e => field("shippingContext", e.target.value)} /></label>
      </div></details>
    </div><aside className="admin-publication-card">
      <h2>Publication</h2><label className="admin-toggle"><input type="checkbox" checked={form.isEnabled} onChange={e => field("isEnabled", e.target.checked)} /><span>Enable public discovery</span></label>
      <p>Saving disabled creates a reversible draft. Enabling still requires an eligible Merchant Policy.</p>
      <label>Match decision<select value={form.matchState} onChange={e => field("matchState", e.target.value)}><option value="CONFIRMED">Same product confirmed</option><option value="POSSIBLEMATCHREVIEW">Review before comparing</option><option value="MANUALREVIEW">Manual review</option><option value="NOMATCH">No safe match</option></select></label>
      <label>Change reason<textarea rows={4} value={form.changeReason ?? ""} onChange={e => field("changeReason", e.target.value)} placeholder="Required when deactivating or changing match state" /></label>
      <div className="admin-readonly"><strong>Derived, not editable</strong><span>Freshness from timestamps</span><span>Evidence from policy</span><span>Affiliate handoff from approved active link</span><span>Reference price from permitted observations</span></div>
    </aside></div>
  </form>;
}

function Banners({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminBanner | null>(null);
  const [form, setForm] = useState<AdminBannerInput | null>(null);
  const [pending, setPending] = useState(false);
  function open(banner: AdminBanner) { setSelected(banner); setForm(bannerInput(banner)); notify(null); reportError(null); }
  function field<K extends keyof AdminBannerInput>(key: K, value: AdminBannerInput[K]) { setForm(current => current ? { ...current, [key]: value } : current); }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selected || !form) return; setPending(true); notify(null); reportError(null);
    try {
      const input = { ...form, effectiveAt: form.effectiveAt ? new Date(form.effectiveAt).toISOString() : null, expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null, changeReason: clean(form.changeReason ?? "") };
      await updateAdminBanner(selected.retailerId, input); await refresh(); setSelected(null); setForm(null); notify("Banner updated and audited. Public rights and affiliate state remain fail-closed.");
    } catch (caught) { reportError(caught instanceof Error ? caught.message : "Banner could not be saved."); }
    finally { setPending(false); }
  }
  if (selected && form) return <form className="admin-editor" onSubmit={save}><div className="admin-heading"><div><p className="eyebrow">Banner editor</p><h1>{selected.retailer}</h1><p>Affiliate destination: read-only and provider-managed.</p></div><div className="admin-heading-actions"><button className="button button-secondary" type="button" onClick={() => { setSelected(null); setForm(null); }}>Cancel</button><button className="button button-primary" type="submit" disabled={pending}>{pending ? "Saving…" : "Save banner"}</button></div></div>
    <div className="admin-editor-grid"><div className="admin-form-stack"><div className="admin-card admin-form-grid">
      <label className="span-2">Title<input required maxLength={120} value={form.title} onChange={e => field("title", e.target.value)} /></label>
      <label className="span-2">Subtitle<textarea required maxLength={180} rows={3} value={form.subtitle} onChange={e => field("subtitle", e.target.value)} /></label>
      <label>Reviewed asset<select value={form.assetPath ?? ""} onChange={e => field("assetPath", e.target.value)}>{assetPaths.map(path => <option key={path}>{path}</option>)}</select></label>
      <label>Asset source<select value={form.assetSource} onChange={e => field("assetSource", e.target.value)}><option value="CANADADEALSORIGINAL">GreatDeals original</option><option value="MERCHANTAPPROVEDAFFILIATEASSET">Merchant-approved affiliate asset</option></select></label>
      <label>Display order<input type="number" min="0" max="10000" value={form.bannerOrder} onChange={e => field("bannerOrder", Number(e.target.value))} /></label>
      <label>Provider<select disabled={form.assetSource === "CANADADEALSORIGINAL"} value={form.assetProvider ?? "RAKUTEN"} onChange={e => field("assetProvider", e.target.value)}><option value="RAKUTEN">Rakuten</option><option value="IMPACT">Impact</option><option value="CJ">CJ</option><option value="AMAZONCREATORS">Amazon Creators</option><option value="OTHER">Other</option></select></label>
      <label className="span-2">Redacted rights evidence<input disabled={form.assetSource === "CANADADEALSORIGINAL"} value={form.assetEvidenceReference ?? ""} onChange={e => field("assetEvidenceReference", e.target.value)} /></label>
      <label>Effective at<input disabled={form.assetSource === "CANADADEALSORIGINAL"} type="datetime-local" value={form.effectiveAt ?? ""} onChange={e => field("effectiveAt", e.target.value)} /></label>
      <label>Expires at<input disabled={form.assetSource === "CANADADEALSORIGINAL"} type="datetime-local" value={form.expiresAt ?? ""} onChange={e => field("expiresAt", e.target.value)} /></label>
      <label className="span-2">Change reason<textarea rows={3} value={form.changeReason ?? ""} onChange={e => field("changeReason", e.target.value)} /></label>
    </div></div><aside className="admin-publication-card"><h2>Banner preview</h2><div className="admin-banner-preview" style={{ backgroundImage: `linear-gradient(90deg,rgba(4,31,22,.94),rgba(4,31,22,.38)),url(${form.assetPath})` }}><small>{selected.retailer}</small><strong>{form.title}</strong><span>{form.subtitle}</span></div><label className="admin-toggle"><input type="checkbox" checked={form.isEnabled} onChange={e => field("isEnabled", e.target.checked)} /><span>Enable banner</span></label><div className="admin-readonly"><strong>Current derived state</strong><span>Visibility: {selected.visibilityState}</span><span>Rights: {selected.rightsState}</span><span>Brand policy: {selected.brandAssetPolicy}</span></div></aside></div></form>;

  return <><div className="admin-heading"><div><p className="eyebrow">Homepage merchandising</p><h1>Store banners</h1><p>Only reviewed first-party assets or documented merchant-approved assets can be enabled.</p></div></div><div className="admin-banner-grid">{dashboard.banners.map(banner => <article className="admin-banner-card" key={banner.retailerId}><div className="admin-banner-preview" style={{ backgroundImage: `linear-gradient(90deg,rgba(4,31,22,.94),rgba(4,31,22,.38)),url(${banner.assetPath ?? assetPaths[0]})` }}><small>{banner.retailer}</small><strong>{banner.title}</strong><span>{banner.subtitle}</span></div><div><span className={`status-chip ${banner.rightsState === "READY" ? "status-ready" : "status-warning"}`}>{banner.visibilityState}</span><p>{banner.assetSource === "CANADADEALSORIGINAL" ? "Original GreatDeals artwork" : "Merchant-approved asset"}</p><button className="button button-secondary" type="button" onClick={() => open(banner)}>Edit banner</button></div></article>)}</div></>;
}

function Reports({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [notes, setNotes] = useState<Record<string, string>>({});
  const [pending, setPending] = useState<string | null>(null);
  const ordered = [...dashboard.reports].sort((a, b) => (a.status === "OPEN" ? -1 : b.status === "OPEN" ? 1 : 0) || b.createdAt.localeCompare(a.createdAt));

  async function change(reportId: string, status: string) {
    const note = notes[reportId]?.trim();
    if (!note) { reportError("Add a short resolution note before changing report status."); return; }
    setPending(reportId); reportError(null); notify(null);
    try { await updateAdminReport(reportId, status, note); await refresh(); notify(`Report marked ${status.toLowerCase()} and audited.`); }
    catch (caught) { reportError(caught instanceof Error ? caught.message : "Report status could not be changed."); }
    finally { setPending(null); }
  }

  return <><div className="admin-heading"><div><p className="eyebrow">Trust operations</p><h1>Customer reports</h1><p>Review stale prices, wrong products or variants, expired offers, and unavailable retailer pages.</p></div></div>
    <div className="admin-report-list">{ordered.map(report => <article className="admin-card admin-report-card" key={report.reportId}>
      <div><span className={`status-chip ${report.status === "OPEN" ? "status-warning" : "status-ready"}`}>{report.status}</span><small>{new Date(report.createdAt).toLocaleString("en-CA")}</small></div>
      <h2>{report.listingTitle}</h2><p><strong>{report.retailer}</strong> · {report.reason.replaceAll("_", " ").toLowerCase()}</p>
      {report.customerNote && <blockquote>{report.customerNote}</blockquote>}
      <label>Resolution note<textarea rows={2} maxLength={300} value={notes[report.reportId] ?? ""} onChange={event => setNotes(current => ({ ...current, [report.reportId]: event.target.value }))} /></label>
      <div className="admin-report-actions"><button className="button button-secondary" type="button" disabled={pending === report.reportId} onClick={() => change(report.reportId, "REVIEWED")}>Mark reviewed</button><button className="button button-primary" type="button" disabled={pending === report.reportId} onClick={() => change(report.reportId, "RESOLVED")}>Resolve</button><button className="button button-secondary" type="button" disabled={pending === report.reportId} onClick={() => change(report.reportId, "DISMISSED")}>Dismiss</button></div>
    </article>)}{ordered.length === 0 && <div className="admin-card admin-empty">No customer reports have been submitted.</div>}</div>
  </>;
}

function Audit({ dashboard }: { dashboard: AdminDashboard }) {
  return <><div className="admin-heading"><div><p className="eyebrow">Accountability</p><h1>Recent audit</h1><p>Administrative mutations record the actor, entity, action, reason, and time without storing credentials.</p></div></div><div className="admin-table" role="region" aria-label="Recent audit events" tabIndex={0}><table><thead><tr><th>When</th><th>Entity</th><th>Action</th><th>Summary</th></tr></thead><tbody>{dashboard.recentAudit.map(item => <tr key={item.id}><td>{new Date(item.createdAt).toLocaleString("en-CA")}</td><td>{item.entityType}<small>{item.entityId}</small></td><td>{item.action}</td><td>{item.summary}</td></tr>)}</tbody></table>{dashboard.recentAudit.length === 0 && <p className="admin-empty">No administrative changes have been recorded yet.</p>}</div></>;
}
