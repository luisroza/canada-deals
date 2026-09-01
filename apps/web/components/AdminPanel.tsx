"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { signIn, signOut } from "../lib/account";
import {
  AdminApiError,
  activateAdminProductImage,
  archiveAdminProductImage,
  createAdminBrand,
  createAdminCategory,
  createAdminOffer,
  createAdminRetailer,
  getAdminDashboard,
  inspectAdminAffiliateLink,
  updateAdminBannerSelection,
  updateAdminBanner,
  updateAdminBrand,
  updateAdminCategory,
  updateAdminOffer,
  updateAdminReport,
  updateAdminRetailer,
  uploadAdminBannerAsset,
  uploadAdminProductImage,
  type AdminBanner,
  type AdminBannerInput,
  type AdminBrand,
  type AdminDashboard,
  type AdminAffiliateLinkInspection,
  type AdminCategory,
  type AdminOffer,
  type AdminOfferInput,
  type AdminRetailer,
} from "../lib/admin";

type AccessState = "loading" | "authorized" | "signed-out" | "forbidden" | "unavailable";
type Section = "overview" | "offers" | "catalog" | "stores" | "banners" | "reports" | "audit";
type OfferForm = Omit<AdminOfferInput, "currentPrice" | "regularPrice" | "packQuantity" | "observedAt" | "fetchedAt" | "regularPriceObservedAt" | "offerValidFrom" | "offerValidUntil" | "variantAttributes" | "externalIdentifiers"> & {
  currentPrice: string; regularPrice: string; packQuantity: string; observedAt: string; fetchedAt: string; regularPriceObservedAt: string; offerValidFrom: string; offerValidUntil: string; variantAttributes: string; externalIdentifiers: string;
};
type NewProductImageDraft = {
  file: File | null;
  rightsEvidenceReference: string;
  effectiveAt: string;
  expiresAt: string;
  activate: boolean;
};

const builtInAssets = [
  { path: "/store-banners/electronics-devices.svg", label: "Electronics devices" },
  { path: "/store-banners/home-decor.svg", label: "Home decor" },
  { path: "/store-banners/marketplace-bags.svg", label: "Marketplace bags" },
  { path: "/store-banners/marketplace-collection.svg", label: "Marketplace collection" },
  { path: "/store-banners/marketplace-packages.svg", label: "Marketplace packages" },
  { path: "/store-banners/office-workspace.svg", label: "Office workspace" },
  { path: "/store-banners/pc-hardware.svg", label: "PC hardware" },
  { path: "/store-banners/workstation.svg", label: "Workstation" },
];

function localDateTime(value?: string | null) {
  const date = value ? new Date(value) : new Date();
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

function clean(value: string) { return value.trim() || null; }
function slugify(value: string) { return value.toLowerCase().trim().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, ""); }
function jsonText(value: Record<string, string>) { return JSON.stringify(value, null, 2); }
export function suggestProductTitleFromRetailerUrl(value: string) {
  try {
    const uri = new URL(value);
    const parts = uri.pathname.split("/").filter(Boolean);
    const productMarker = parts.findIndex(part => part.toLowerCase() === "dp" || part.toLowerCase() === "product");
    if (productMarker < 1) return null;
    const candidate = decodeURIComponent(parts[productMarker - 1]).replace(/[-_]+/g, " ").replace(/\s+/g, " ").trim();
    if (candidate.length < 4 || /^[A-Z0-9]{10}$/i.test(candidate)) return null;
    return candidate.slice(0, 240);
  } catch { return null; }
}

function emptyProductImageDraft(): NewProductImageDraft {
  return { file: null, rightsEvidenceReference: "Owner-created or licensed asset reviewed by the account owner", effectiveAt: "", expiresAt: "", activate: true };
}

function offerToForm(offer: AdminOffer): OfferForm {
  return {
    productId: offer.productId, slug: offer.slug, productTitle: offer.productTitle, brandId: offer.brandId,
    newBrandName: null, newBrandSlug: null, confirmBrandCreation: false, categoryId: offer.categoryId,
    modelNumber: offer.modelNumber, manufacturerPartNumber: offer.manufacturerPartNumber, gtin: offer.gtin,
    variantAttributes: jsonText(offer.variantAttributes), retailerId: offer.retailerId, merchantPolicyId: offer.merchantPolicyId,
    externalListingId: offer.externalListingId, retailerSku: offer.retailerSku, originalTitle: offer.originalTitle,
    productUrl: offer.productUrl, approvedAffiliateDestinationReference: offer.approvedAffiliateDestinationReference,
    affiliateTrackingUrl: offer.affiliateTrackingUrl, affiliatePartnerTag: offer.affiliatePartnerTag,
    affiliateRelationshipEvidenceReference: offer.affiliateRelationshipEvidenceReference,
    affiliateRelationshipConfirmed: offer.affiliateLinkStatus === "ACTIVE",
    seller: offer.seller, isMarketplaceSeller: offer.isMarketplaceSeller, conditionState: offer.conditionState,
    packQuantity: offer.packQuantity?.toString() ?? "", bundleContents: offer.bundleContents,
    regionAvailabilityContext: offer.regionAvailabilityContext, availabilityState: offer.availabilityState,
    shippingContext: offer.shippingContext, externalIdentifiers: jsonText(offer.externalIdentifiers),
    currentPrice: offer.currentPrice?.toFixed(2) ?? "", regularPrice: offer.regularPrice?.toFixed(2) ?? "",
    observedAt: localDateTime(offer.observedAt), fetchedAt: localDateTime(offer.fetchedAt),
    regularPriceObservedAt: offer.regularPriceObservedAt ? localDateTime(offer.regularPriceObservedAt) : "",
    regularPriceEvidenceReference: offer.regularPriceEvidenceReference,
    offerValidFrom: offer.offerValidFrom ? localDateTime(offer.offerValidFrom) : "", offerValidUntil: offer.offerValidUntil ? localDateTime(offer.offerValidUntil) : "",
    matchState: offer.matchState, isEnabled: offer.isEnabled, changeReason: null,
  };
}

function emptyOffer(dashboard: AdminDashboard): OfferForm {
  const now = localDateTime();
  return {
    productId: null, slug: "", productTitle: "", brandId: null, newBrandName: null, newBrandSlug: null, confirmBrandCreation: false,
    categoryId: dashboard.categories.find(item => item.isEnabled)?.id ?? "",
    modelNumber: null, manufacturerPartNumber: null, gtin: null, variantAttributes: "{}",
    retailerId: dashboard.retailers.find(item => item.isEnabled)?.id ?? "", merchantPolicyId: dashboard.policies[0]?.id ?? "",
    externalListingId: "", retailerSku: null, originalTitle: "", productUrl: "", approvedAffiliateDestinationReference: null,
    affiliateTrackingUrl: null, affiliatePartnerTag: null, affiliateRelationshipEvidenceReference: null, affiliateRelationshipConfirmed: false,
    seller: null, isMarketplaceSeller: false, conditionState: "UNKNOWN", packQuantity: "1", bundleContents: null,
    regionAvailabilityContext: "Canada", availabilityState: "UNKNOWN", shippingContext: null, externalIdentifiers: "{}",
    currentPrice: "", regularPrice: "", regularPriceObservedAt: "", regularPriceEvidenceReference: null,
    observedAt: now, fetchedAt: now, offerValidFrom: "", offerValidUntil: "", matchState: "MANUALREVIEW", isEnabled: false, changeReason: null,
  };
}

function bannerInput(banner: AdminBanner): AdminBannerInput {
  return {
    title: banner.title, subtitle: banner.subtitle, assetPath: banner.assetPath ?? builtInAssets[0].path, assetSource: banner.assetSource,
    assetProvider: banner.assetProvider, assetEvidenceReference: banner.assetEvidenceReference, allowedPlacement: banner.allowedPlacement ?? "store_banner",
    effectiveAt: banner.effectiveAt ? localDateTime(banner.effectiveAt) : null, expiresAt: banner.expiresAt ? localDateTime(banner.expiresAt) : null,
    bannerOrder: banner.bannerOrder === 2147483647 ? 100 : banner.bannerOrder, changeReason: null,
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
          {(["overview", "offers", "catalog", "stores", "banners", "reports", "audit"] as Section[]).map(item => (
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
          {section === "catalog" && <CatalogSettings dashboard={dashboard} refresh={load} notify={setMessage} reportError={setError} />}
          {section === "stores" && <Stores dashboard={dashboard} refresh={load} notify={setMessage} reportError={setError} />}
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
    <section className="admin-card"><h2>Publication rules</h2><ul><li>Offers remain drafts until enabled and permitted by their Merchant Policy.</li><li>A regular price is optional, must exceed the deal price, and requires reviewed source evidence.</li><li>Banner tracking destinations remain provider-managed and cannot be pasted here.</li></ul></section>
  </>;
}

type EntityFilter = "all" | "active" | "inactive" | "public" | "empty";

function CatalogSettings({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [area, setArea] = useState<"categories" | "brands">("categories");
  return <>
    <div className="admin-catalog-tabs" role="tablist" aria-label="Catalog settings">
      <button role="tab" aria-selected={area === "categories"} type="button" onClick={() => setArea("categories")}><strong>Categories</strong><span>Public discovery structure</span></button>
      <button role="tab" aria-selected={area === "brands"} type="button" onClick={() => setArea("brands")}><strong>Brands</strong><span>Advanced Product identity</span></button>
    </div>
    {area === "categories"
      ? <Categories dashboard={dashboard} refresh={refresh} notify={notify} reportError={reportError} />
      : <Brands dashboard={dashboard} refresh={refresh} notify={notify} reportError={reportError} />}
  </>;
}

function Brands({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminBrand | "new" | null>(null);
  const [name, setName] = useState(""); const [slug, setSlug] = useState(""); const [enabled, setEnabled] = useState(false); const [reason, setReason] = useState("");
  const [search, setSearch] = useState(""); const [filter, setFilter] = useState<EntityFilter>("all"); const [pending, setPending] = useState(false);
  const visible = dashboard.managedBrands.filter(brand => {
    const matchesSearch = `${brand.name} ${brand.slug}`.toLowerCase().includes(search.toLowerCase());
    const matchesFilter = filter === "all" || (filter === "active" && brand.isEnabled) || (filter === "inactive" && !brand.isEnabled) ||
      (filter === "public" && brand.publishedOfferCount > 0) || (filter === "empty" && brand.productCount === 0);
    return matchesSearch && matchesFilter;
  });
  const activeCount = dashboard.managedBrands.filter(brand => brand.isEnabled).length;

  function open(value: AdminBrand | "new") { setSelected(value); setName(value === "new" ? "" : value.name); setSlug(value === "new" ? "" : value.slug); setEnabled(value === "new" ? false : value.isEnabled); setReason(""); notify(null); reportError(null); }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (selected !== "new" && selected?.isEnabled && !enabled && !reason.trim()) { reportError("Add a reason before deactivating this brand."); return; }
    setPending(true); notify(null); reportError(null);
    try { if (selected === "new") await createAdminBrand(name.trim(), slug); else if (selected) await updateAdminBrand(selected.id, name.trim(), enabled, clean(reason)); await refresh(); setSelected(null); notify(selected === "new" ? "Brand created inactive. Review it before using it on new Products." : "Brand updated and audited."); }
    catch (caught) { reportError(caught instanceof Error ? caught.message : "Brand could not be saved."); } finally { setPending(false); }
  }

  if (selected) {
    const existing = selected !== "new" ? selected : null;
    return <form className="admin-editor" onSubmit={save} noValidate><div className="admin-heading"><div><p className="eyebrow">{existing ? "Edit brand" : "New brand"}</p><h1>{name || "Untitled brand"}</h1><p>Brands support Product identity, search, and catalog organization. Existing Products remain intact when a brand is inactive.</p></div><div className="admin-heading-actions"><button className="button button-secondary" type="button" onClick={() => setSelected(null)}>Cancel</button><button className="button button-primary" type="submit" disabled={pending}>{pending ? "Saving…" : "Save brand"}</button></div></div>
      <div className="admin-editor-grid"><section className="admin-card admin-entity-form"><h2>Brand details</h2><label>Brand name<input required maxLength={120} value={name} onChange={event => { setName(event.target.value); if (!existing) setSlug(slugify(event.target.value)); }} /></label><label>Brand slug<input required maxLength={140} pattern="[a-z0-9-]+" readOnly={Boolean(existing)} value={slug} onChange={event => setSlug(slugify(event.target.value))} /><span className="field-help">{existing ? "Immutable after creation to preserve catalog identity." : "Lowercase letters, numbers, and hyphens."}</span></label>{existing && <div className="admin-readonly"><strong>Current impact</strong><span>{existing.productCount} products</span><span>{existing.publishedOfferCount} public offers</span><span>Product and Wishlist records are preserved</span></div>}</section>
        <aside className="admin-publication-card"><h2>Catalog status</h2><label className="admin-toggle"><input type="checkbox" checked={enabled} onChange={event => setEnabled(event.target.checked)} /><span>Available for public Products</span></label>{existing && existing.isEnabled && !enabled && <div className="admin-impact-warning" role="note"><strong>Before deactivating</strong><p>This hides associated offers from public discovery without deleting Products, offers, or Wishlists.</p></div>}<label>Change reason<textarea rows={4} maxLength={300} value={reason} onChange={event => setReason(event.target.value)} placeholder="Required when deactivating" /></label>{!existing && <p className="field-help">New brands always begin inactive.</p>}</aside></div></form>;
  }

  return <><div className="admin-heading"><div><p className="eyebrow">Catalog identity</p><h1>Brands</h1><p><strong>{activeCount} active</strong> · {dashboard.managedBrands.length - activeCount} inactive. Manage Product brands without editing database records directly.</p></div><button className="button button-primary" type="button" onClick={() => open("new")}>Add brand</button></div><EntityToolbar search={search} setSearch={setSearch} filter={filter} setFilter={setFilter} placeholder="Name or brand slug" includeEmpty />
    <div className="admin-entity-grid">{visible.map(brand => <article className="admin-entity-card" key={brand.id}><div><span className={`status-chip ${brand.isEnabled ? "status-ready" : ""}`}>{brand.isEnabled ? "Active" : "Inactive"}</span><code>{brand.slug}</code></div><h2>{brand.name}</h2><dl><div><dt>Products</dt><dd>{brand.productCount}</dd></div><div><dt>Public offers</dt><dd>{brand.publishedOfferCount}</dd></div></dl><button className="button button-secondary" type="button" onClick={() => open(brand)}>Edit brand</button></article>)}</div>{visible.length === 0 && <p className="admin-card admin-empty">No brands match these controls.</p>}</>;
}

function Categories({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminCategory | "new" | null>(null);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [enabled, setEnabled] = useState(false);
  const [reason, setReason] = useState("");
  const [search, setSearch] = useState("");
  const [filter, setFilter] = useState<EntityFilter>("all");
  const [pending, setPending] = useState(false);
  const visible = dashboard.managedCategories.filter(category => {
    const matchesSearch = `${category.name} ${category.slug}`.toLowerCase().includes(search.toLowerCase());
    const matchesFilter = filter === "all" || (filter === "active" && category.isEnabled) || (filter === "inactive" && !category.isEnabled) ||
      (filter === "public" && category.publishedOfferCount > 0) || (filter === "empty" && category.productCount === 0);
    return matchesSearch && matchesFilter;
  });
  const activeCount = dashboard.managedCategories.filter(category => category.isEnabled).length;

  function open(value: AdminCategory | "new") {
    setSelected(value); setName(value === "new" ? "" : value.name); setSlug(value === "new" ? "" : value.slug);
    setEnabled(value === "new" ? false : value.isEnabled); setReason(""); notify(null); reportError(null);
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (selected !== "new" && selected?.isEnabled && !enabled && !reason.trim()) { reportError("Add a reason before deactivating this category."); return; }
    setPending(true); notify(null); reportError(null);
    try {
      if (selected === "new") await createAdminCategory(name.trim(), slug);
      else if (selected) await updateAdminCategory(selected.id, name.trim(), enabled, clean(reason));
      await refresh(); setSelected(null); notify(selected === "new" ? "Category created inactive. Review it before activation." : "Category updated and audited.");
    } catch (caught) { reportError(caught instanceof Error ? caught.message : "Category could not be saved."); }
    finally { setPending(false); }
  }

  if (selected) {
    const existing = selected !== "new" ? selected : null;
    return <form className="admin-editor" onSubmit={save} noValidate><div className="admin-heading"><div><p className="eyebrow">{existing ? "Edit category" : "New category"}</p><h1>{name || "Untitled category"}</h1><p>Categories control public discovery. Products and wishlist records are preserved when a category is inactive.</p></div><div className="admin-heading-actions"><button className="button button-secondary" type="button" onClick={() => setSelected(null)}>Cancel</button><button className="button button-primary" type="submit" disabled={pending}>{pending ? "Saving…" : "Save category"}</button></div></div>
      <div className="admin-editor-grid"><section className="admin-card admin-entity-form"><h2>Category details</h2><label>Category name<input required maxLength={120} value={name} onChange={event => { setName(event.target.value); if (!existing) setSlug(slugify(event.target.value)); }} /></label><label>URL slug<input aria-label="URL slug" required maxLength={140} pattern="[a-z0-9-]+" readOnly={Boolean(existing)} value={slug} onChange={event => setSlug(slugify(event.target.value))} /><span className="field-help">{existing ? "Immutable after creation to preserve URLs." : "Lowercase letters, numbers, and hyphens."}</span></label>{existing && <div className="admin-readonly"><strong>Current impact</strong><span>{existing.productCount} products</span><span>{existing.publishedOfferCount} public offers</span><span>Wishlist records are preserved</span></div>}</section>
        <aside className="admin-publication-card"><h2>Public status</h2><label className="admin-toggle"><input type="checkbox" checked={enabled} onChange={event => setEnabled(event.target.checked)} /><span>Active in public discovery</span></label>{existing && existing.isEnabled && !enabled && <div className="admin-impact-warning" role="note"><strong>Before deactivating</strong><p>This hides the category and associated offers from discovery. Products, offers and wishlists are not deleted.</p></div>}<label>Change reason<textarea rows={4} maxLength={300} value={reason} onChange={event => setReason(event.target.value)} placeholder="Required when deactivating" /></label>{!existing && <p className="field-help">New categories always begin inactive.</p>}</aside></div></form>;
  }

  return <><div className="admin-heading"><div><p className="eyebrow">Catalog structure</p><h1>Categories</h1><p><strong>{activeCount} active</strong> · {dashboard.managedCategories.length - activeCount} inactive. Manage discovery without deleting products or wishlists.</p></div><button className="button button-primary" type="button" onClick={() => open("new")}>Add category</button></div><EntityToolbar search={search} setSearch={setSearch} filter={filter} setFilter={setFilter} placeholder="Name or slug" includeEmpty />
    <div className="admin-entity-grid">{visible.map(category => <article className="admin-entity-card" key={category.id}><div><span className={`status-chip ${category.isEnabled ? "status-ready" : ""}`}>{category.isEnabled ? "Active" : "Inactive"}</span><code>{category.slug}</code></div><h2>{category.name}</h2><dl><div><dt>Products</dt><dd>{category.productCount}</dd></div><div><dt>Public offers</dt><dd>{category.publishedOfferCount}</dd></div></dl><button className="button button-secondary" type="button" onClick={() => open(category)}>Edit category</button></article>)}</div>{visible.length === 0 && <p className="admin-card admin-empty">No categories match these controls.</p>}</>;
}

function Stores({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminRetailer | "new" | null>(null);
  const [name, setName] = useState(""); const [key, setKey] = useState(""); const [enabled, setEnabled] = useState(false); const [reason, setReason] = useState("");
  const [search, setSearch] = useState(""); const [filter, setFilter] = useState<EntityFilter>("all"); const [pending, setPending] = useState(false);
  const visible = dashboard.managedRetailers.filter(store => {
    const matchesSearch = `${store.name} ${store.key}`.toLowerCase().includes(search.toLowerCase());
    const matchesFilter = filter === "all" || (filter === "active" && store.isEnabled) || (filter === "inactive" && !store.isEnabled) ||
      (filter === "public" && store.publishedOfferCount > 0) || (filter === "empty" && store.listingCount === 0);
    return matchesSearch && matchesFilter;
  });
  const activeCount = dashboard.managedRetailers.filter(store => store.isEnabled).length;

  function open(value: AdminRetailer | "new") { setSelected(value); setName(value === "new" ? "" : value.name); setKey(value === "new" ? "" : value.key); setEnabled(value === "new" ? false : value.isEnabled); setReason(""); notify(null); reportError(null); }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (selected !== "new" && selected?.isEnabled && !enabled && !reason.trim()) { reportError("Add a reason before deactivating this store."); return; }
    setPending(true); notify(null); reportError(null);
    try { if (selected === "new") await createAdminRetailer(name.trim(), key); else if (selected) await updateAdminRetailer(selected.id, name.trim(), enabled, clean(reason)); await refresh(); setSelected(null); notify(selected === "new" ? "Store created inactive. Affiliate, data and banner gates remain separate." : "Store updated and audited."); }
    catch (caught) { reportError(caught instanceof Error ? caught.message : "Store could not be saved."); } finally { setPending(false); }
  }

  if (selected) {
    const existing = selected !== "new" ? selected : null;
    return <form className="admin-editor" onSubmit={save} noValidate><div className="admin-heading"><div><p className="eyebrow">{existing ? "Edit store" : "New store"}</p><h1>{name || "Untitled store"}</h1><p>Store activation never approves data rights, artwork, connectors, or affiliate destinations.</p></div><div className="admin-heading-actions"><button className="button button-secondary" type="button" onClick={() => setSelected(null)}>Cancel</button><button className="button button-primary" type="submit" disabled={pending}>{pending ? "Saving…" : "Save store"}</button></div></div>
      <div className="admin-editor-grid"><section className="admin-card admin-entity-form"><h2>Store details</h2><label>Store name<input required maxLength={160} value={name} onChange={event => { setName(event.target.value); if (!existing) setKey(slugify(event.target.value)); }} /></label><label>Store key<input aria-label="Store key" required maxLength={80} pattern="[a-z0-9-]+" readOnly={Boolean(existing)} value={key} onChange={event => setKey(slugify(event.target.value))} /><span className="field-help">{existing ? "Immutable after creation to preserve handoff and integration identity." : "Lowercase letters, numbers, and hyphens."}</span></label><label>Market<input readOnly value="Canada (CA)" /></label>{existing && <div className="admin-readonly"><strong>Current impact</strong><span>{existing.listingCount} retained offers</span><span>{existing.publishedOfferCount} public offers</span><span>{existing.hasBannerProfile ? existing.isBannerActive ? "Banner active" : "Banner configured but inactive" : "Banner not configured"}</span><span>{existing.affiliateProgramCount} affiliate programs</span></div>}</section>
        <aside className="admin-publication-card"><h2>Operational status</h2><label className="admin-toggle"><input type="checkbox" checked={enabled} onChange={event => setEnabled(event.target.checked)} /><span>Active store</span></label>{existing && existing.isEnabled && !enabled && <div className="admin-impact-warning" role="note"><strong>Before deactivating</strong><p>This hides public offers and banners and blocks store and product handoffs. Offers, programs, links and history are preserved.</p></div>}<label>Change reason<textarea rows={4} maxLength={300} value={reason} onChange={event => setReason(event.target.value)} placeholder="Required when deactivating" /></label>{!existing && <p className="field-help">New stores always begin inactive. Every publication capability remains independently gated.</p>}</aside></div></form>;
  }

  return <><div className="admin-heading"><div><p className="eyebrow">Retail operations</p><h1>Stores</h1><p><strong>{activeCount} active</strong> · {dashboard.managedRetailers.length - activeCount} inactive. Operational state is separate from affiliate and artwork approval.</p></div><button className="button button-primary" type="button" onClick={() => open("new")}>Add store</button></div><EntityToolbar search={search} setSearch={setSearch} filter={filter} setFilter={setFilter} placeholder="Name or store key" includeEmpty />
    <div className="admin-entity-grid">{visible.map(store => <article className="admin-entity-card" key={store.id}><div><span className={`status-chip ${store.isEnabled ? "status-ready" : ""}`}>{store.isEnabled ? "Active" : "Inactive"}</span><code>{store.key} · {store.countryCode}</code></div><h2>{store.name}</h2><dl><div><dt>Offers</dt><dd>{store.listingCount}</dd></div><div><dt>Public</dt><dd>{store.publishedOfferCount}</dd></div><div><dt>Banner</dt><dd>{store.hasBannerProfile ? store.isBannerActive ? "Active" : "Inactive" : "Not set"}</dd></div><div><dt>Affiliate programs</dt><dd>{store.affiliateProgramCount}</dd></div></dl><button className="button button-secondary" type="button" onClick={() => open(store)}>Edit store</button></article>)}</div>{visible.length === 0 && <p className="admin-card admin-empty">No stores match these controls.</p>}</>;
}

function EntityToolbar({ search, setSearch, filter, setFilter, placeholder, includeEmpty }: { search: string; setSearch: (value: string) => void; filter: EntityFilter; setFilter: (value: EntityFilter) => void; placeholder: string; includeEmpty?: boolean }) {
  const filters: EntityFilter[] = includeEmpty ? ["all", "active", "inactive", "public", "empty"] : ["all", "active", "inactive", "public"];
  return <div className="admin-entity-toolbar"><label className="admin-search">Search<input type="search" value={search} onChange={event => setSearch(event.target.value)} placeholder={placeholder} /></label><div className="admin-banner-filters" role="group" aria-label="Filter records">{filters.map(value => <button type="button" key={value} aria-pressed={filter === value} onClick={() => setFilter(value)}>{value === "public" ? "With public offers" : value[0].toUpperCase() + value.slice(1)}</button>)}</div></div>;
}

function Offers({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminOffer | "new" | null>(null);
  const [form, setForm] = useState<OfferForm | null>(null);
  const [newProductImage, setNewProductImage] = useState<NewProductImageDraft>(emptyProductImageDraft);
  const [pending, setPending] = useState(false);
  const [filter, setFilter] = useState("");

  const visible = useMemo(() => dashboard.offers.filter(offer => `${offer.productTitle} ${offer.retailer} ${offer.externalListingId}`.toLowerCase().includes(filter.toLowerCase())), [dashboard.offers, filter]);

  function open(value: AdminOffer | "new") { setSelected(value); setForm(value === "new" ? emptyOffer(dashboard) : offerToForm(value)); setNewProductImage(emptyProductImageDraft()); notify(null); reportError(null); }
  function field<K extends keyof OfferForm>(key: K, value: OfferForm[K]) { setForm(current => current ? { ...current, [key]: value } : current); }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!form) return; setPending(true); notify(null); reportError(null);
    try {
      const submitter = (event.nativeEvent as SubmitEvent).submitter as HTMLButtonElement | null;
      const requestedEnabled = submitter?.value === "publish" ? true : submitter?.value === "draft" ? false : form.isEnabled;
      const confirmedNewBrand = Boolean(form.newBrandName?.trim() && form.newBrandSlug?.trim() && form.confirmBrandCreation);
      if (!form.brandId && !confirmedNewBrand) throw new Error("Select an existing brand or confirm the proposed new brand before saving the offer.");
      if (selected === "new" && newProductImage.file && !newProductImage.rightsEvidenceReference.trim())
        throw new Error("Add the image rights evidence reference before saving the offer.");
      const variants = JSON.parse(form.variantAttributes || "{}") as Record<string, string>;
      const identifiers = JSON.parse(form.externalIdentifiers || "{}") as Record<string, string>;
      const input: AdminOfferInput = {
        ...form, brandId: form.brandId || null, newBrandName: clean(form.newBrandName ?? ""), newBrandSlug: clean(form.newBrandSlug ?? ""),
        modelNumber: clean(form.modelNumber ?? ""), manufacturerPartNumber: clean(form.manufacturerPartNumber ?? ""), gtin: clean(form.gtin ?? ""),
        retailerSku: clean(form.retailerSku ?? ""), approvedAffiliateDestinationReference: clean(form.approvedAffiliateDestinationReference ?? ""), seller: clean(form.seller ?? ""),
        affiliateTrackingUrl: clean(form.affiliateTrackingUrl ?? ""), affiliatePartnerTag: clean(form.affiliatePartnerTag ?? ""),
        affiliateRelationshipEvidenceReference: clean(form.affiliateRelationshipEvidenceReference ?? ""), isEnabled: requestedEnabled,
        packQuantity: form.packQuantity ? Number(form.packQuantity) : null, bundleContents: clean(form.bundleContents ?? ""), regionAvailabilityContext: clean(form.regionAvailabilityContext ?? ""),
        shippingContext: clean(form.shippingContext ?? ""), currentPrice: Number(form.currentPrice), regularPrice: form.regularPrice ? Number(form.regularPrice) : null,
        regularPriceObservedAt: form.regularPriceObservedAt ? new Date(form.regularPriceObservedAt).toISOString() : null,
        regularPriceEvidenceReference: clean(form.regularPriceEvidenceReference ?? ""), observedAt: new Date(form.observedAt).toISOString(), fetchedAt: new Date(form.fetchedAt).toISOString(),
        offerValidFrom: form.offerValidFrom ? new Date(form.offerValidFrom).toISOString() : null, offerValidUntil: form.offerValidUntil ? new Date(form.offerValidUntil).toISOString() : null,
        variantAttributes: variants, externalIdentifiers: identifiers, changeReason: clean(form.changeReason ?? ""),
      };
      if (selected === "new") {
        const hasNewImage = Boolean(newProductImage.file);
        const created = await createAdminOffer(hasNewImage ? { ...input, isEnabled: false } : input);
        if (newProductImage.file) {
          try {
            await uploadAdminProductImage(created.productId, {
              file: newProductImage.file,
              rightsEvidenceReference: newProductImage.rightsEvidenceReference.trim(),
              allowedPlacements: "DEAL_CARD,PRODUCT_PAGE,WISHLIST",
              effectiveAt: clean(newProductImage.effectiveAt),
              expiresAt: clean(newProductImage.expiresAt),
              activate: newProductImage.activate,
            });
          } catch (imageError) {
            await refresh(); setSelected(null); setForm(null); setNewProductImage(emptyProductImageDraft());
            reportError(`The offer was saved as a draft, but the image could not be attached: ${imageError instanceof Error ? imageError.message : "image upload failed"}`);
            return;
          }
          if (requestedEnabled) {
            try {
              await updateAdminOffer(created.listingId, {
                ...input,
                productId: created.productId,
                brandId: created.brandId,
                newBrandName: null,
                newBrandSlug: null,
                confirmBrandCreation: false,
                isEnabled: true,
                changeReason: input.changeReason ?? "Published after reviewed Product image upload.",
              });
            } catch (publishError) {
              await refresh(); setSelected(null); setForm(null); setNewProductImage(emptyProductImageDraft());
              reportError(`The offer and image were saved as a draft, but publishing failed: ${publishError instanceof Error ? publishError.message : "publication failed"}`);
              return;
            }
          }
        }
      } else if (selected) await updateAdminOffer(selected.listingId, input);
      await refresh(); setSelected(null); setForm(null); notify(requestedEnabled ? "Offer published and available in public discovery." : "Draft saved. This offer is not public.");
    } catch (caught) { reportError(caught instanceof SyntaxError ? "External identifiers must be a valid JSON object." : caught instanceof Error ? caught.message : "Offer could not be saved."); }
    finally { setPending(false); }
  }

  if (form) return <OfferEditor dashboard={dashboard} selected={selected} form={form} field={field} save={save} cancel={() => { setSelected(null); setForm(null); setNewProductImage(emptyProductImageDraft()); }} pending={pending} refresh={refresh} notify={notify} reportError={reportError} openExisting={open} newProductImage={newProductImage} setNewProductImage={setNewProductImage} />;

  return <>
    <div className="admin-heading"><div><p className="eyebrow">Catalog operations</p><h1>Offers</h1><p>Create drafts, review readiness, publish, or reversibly deactivate.</p></div><button className="button button-primary" type="button" onClick={() => open("new")}>Add offer</button></div>
    <label className="admin-search">Search offers<input type="search" value={filter} onChange={event => setFilter(event.target.value)} placeholder="Product, retailer, or external ID" /></label>
    <div className="admin-table" role="region" aria-label="Administrative offers" tabIndex={0}>
      <table><thead><tr><th>Product</th><th>Retailer</th><th>Deal / regular price</th><th>Status</th><th>Readiness</th><th><span className="sr-only">Actions</span></th></tr></thead>
        <tbody>{visible.map(offer => <tr key={offer.listingId}><td><strong>{offer.productTitle}</strong><small>{offer.externalListingId}</small></td><td>{offer.retailer}</td><td><strong>{offer.currentPrice?.toLocaleString("en-CA", { style: "currency", currency: "CAD" })}</strong>{offer.regularPrice && <small>Regular {offer.regularPrice.toLocaleString("en-CA", { style: "currency", currency: "CAD" })}</small>}</td><td><span className={`status-chip ${offer.isEnabled ? "status-ready" : ""}`}>{offer.isEnabled ? "Enabled" : "Draft / disabled"}</span></td><td>{offer.isPubliclyEligible ? "Ready" : "Blocked"}</td><td><button className="button button-secondary" type="button" onClick={() => open(offer)}>Edit</button></td></tr>)}</tbody></table>
      {visible.length === 0 && <p className="admin-empty">No offers match this search.</p>}
    </div>
  </>;
}

function OfferEditor({ dashboard, selected, form, field, save, cancel, pending, refresh, notify, reportError, openExisting, newProductImage, setNewProductImage }: { dashboard: AdminDashboard; selected: AdminOffer | "new" | null; form: OfferForm; field: <K extends keyof OfferForm>(key: K, value: OfferForm[K]) => void; save: (event: FormEvent<HTMLFormElement>) => Promise<void>; cancel: () => void; pending: boolean; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void; openExisting: (value: AdminOffer | "new") => void; newProductImage: NewProductImageDraft; setNewProductImage: (value: NewProductImageDraft) => void }) {
  const [brandCreatorOpen, setBrandCreatorOpen] = useState(false);
  const existing = selected !== "new" && selected !== null;
  const reusingProduct = selected === "new" && form.productId !== null;
  const linkReady = Boolean(form.affiliateTrackingUrl?.trim() && form.affiliatePartnerTag?.trim() && form.affiliateRelationshipEvidenceReference?.trim() && form.affiliateRelationshipConfirmed);
  const hasReviewedBrand = Boolean(form.brandId || (form.newBrandName?.trim() && form.newBrandSlug?.trim() && form.confirmBrandCreation));
  const cardReady = Boolean(form.productTitle.trim() && hasReviewedBrand && form.categoryId && form.productUrl.trim() && form.externalListingId.trim() && Number(form.currentPrice) > 0);
  const currentStep = !linkReady ? 1 : !cardReady ? 2 : 3;
  const previewImage = form.productId ? dashboard.productImages.find(image => image.productId === form.productId && image.isPubliclyVisible) : null;
  const publishBlockedByPendingImage = selected === "new" && Boolean(newProductImage.file) && !newProductImage.activate;

  function useProduct(productId: string | null) {
    if (!productId) {
      field("productId", null); field("slug", ""); field("productTitle", "");
      field("brandId", null); field("newBrandName", null); field("newBrandSlug", null); field("confirmBrandCreation", false);
      field("categoryId", dashboard.categories.find(item => item.isEnabled)?.id ?? "");
      field("modelNumber", null); field("manufacturerPartNumber", null); field("gtin", null); field("variantAttributes", "{}");
      return;
    }
    const product = dashboard.products.find(item => item.id === productId);
    if (!product) return;
    field("productId", product.id); field("slug", product.slug); field("productTitle", product.title); field("brandId", product.brandId);
    field("newBrandName", null); field("newBrandSlug", null); field("confirmBrandCreation", false);
    field("categoryId", product.categoryId); field("modelNumber", product.modelNumber); field("manufacturerPartNumber", product.manufacturerPartNumber);
    field("gtin", product.gtin); field("variantAttributes", jsonText(product.variantAttributes));
    if (!form.originalTitle) field("originalTitle", product.title);
  }

  return <form className="admin-editor" onSubmit={save} noValidate>
    <div className="admin-heading"><div><p className="eyebrow">{existing ? "Edit offer" : "New offer"}</p><h1>{form.productTitle || "Untitled offer"}</h1><p>{existing ? selected.readinessSummary : "Analyze the retailer link, review the public card, then save a draft or publish."}</p></div><div className="admin-heading-actions">{existing && <Link className="button button-secondary" href={selected.previewPath} target="_blank">Public preview</Link>}<button className="button button-secondary" type="button" onClick={cancel}>Cancel</button><button className="button button-secondary" name="publicationAction" value="draft" type="submit" disabled={pending}>{pending ? "Saving…" : existing && form.isEnabled ? "Unpublish to draft" : "Save draft"}</button><button className="button button-primary" name="publicationAction" value="publish" type="submit" disabled={pending || publishBlockedByPendingImage} title={publishBlockedByPendingImage ? "Activate the reviewed image before publishing, or save the offer as a draft." : undefined}>{pending ? "Saving…" : existing && form.isEnabled ? "Save published changes" : "Publish offer"}</button></div></div>
    <ol className="admin-offer-progress" aria-label="Offer workflow"><li className={currentStep === 1 ? "is-current" : "is-complete"} aria-current={currentStep === 1 ? "step" : undefined}><span>1</span>Validate and fill</li><li className={currentStep === 2 ? "is-current" : currentStep > 2 ? "is-complete" : undefined} aria-current={currentStep === 2 ? "step" : undefined}><span>2</span>Review card</li><li className={currentStep === 3 ? "is-current" : undefined} aria-current={currentStep === 3 ? "step" : undefined}><span>3</span>Publish</li></ol>
    <div className="admin-editor-grid"><div className="admin-form-stack">
      <AffiliateLinkIntake dashboard={dashboard} selected={selected} form={form} field={field} reportError={reportError} openExisting={openExisting} />
       {selected === "new" && <section className="admin-card admin-product-choice"><h2>Choose the Product</h2><div className="admin-choice-grid" role="radiogroup" aria-label="Product creation mode"><label><input type="radio" name="product-mode" checked={!reusingProduct} onChange={() => useProduct(null)} /><span><strong>Create a new Product</strong><small>Use when this Product does not exist in the catalog.</small></span></label><label><input type="radio" name="product-mode" checked={reusingProduct} onChange={() => useProduct(dashboard.products[0]?.id ?? null)} /><span><strong>Reuse an existing Product identity</strong><small>The new store offer remains independent in discovery and in the Wishlist.</small></span></label></div>{reusingProduct && <label className="admin-product-picker">Existing Product<select value={form.productId ?? ""} onChange={event => useProduct(event.target.value)}>{dashboard.products.map(product => <option key={product.id} value={product.id}>{product.title} · {product.brand}{product.modelNumber ? ` · ${product.modelNumber}` : ""}</option>)}</select></label>}</section>}
      <details open className="admin-product-essentials"><summary>Product essentials</summary><div className="admin-form-grid">
        <label className="span-2">Product title<input required maxLength={240} readOnly={reusingProduct} value={form.productTitle} onChange={e => { field("productTitle", e.target.value); if (!existing && !form.productId) field("slug", slugify(e.target.value)); }} /></label>
        <label>Slug<input required pattern="[a-z0-9-]+" readOnly={existing || reusingProduct} value={form.slug} onChange={e => field("slug", slugify(e.target.value))} /><span className="field-help">{existing || reusingProduct ? "Immutable for existing Products so saved and shared links keep working." : "Generated from the title; lowercase letters, numbers, and hyphens."}</span></label>
        <div className="admin-field-group"><label htmlFor="offer-brand">Brand</label><div className="admin-inline-select"><select id="offer-brand" required disabled={reusingProduct} value={form.brandId ?? ""} onChange={e => { field("brandId", e.target.value || null); field("newBrandName", null); field("newBrandSlug", null); field("confirmBrandCreation", false); setBrandCreatorOpen(false); }}><option value="" disabled>Select a brand</option>{dashboard.brands.filter(item => item.isEnabled || item.id === form.brandId).map(item => <option key={item.id} value={item.id}>{item.label}{item.isEnabled ? "" : " (inactive)"}</option>)}</select>{!reusingProduct && !form.newBrandName && <button className="button button-secondary" type="button" onClick={() => { setBrandCreatorOpen(value => !value); reportError(null); }}>{brandCreatorOpen ? "Close" : "Enter a new brand"}</button>}</div><span className="field-help">An exact catalog match is selected automatically. A new detected brand must be reviewed before it is created with the offer.</span></div>
        <label>Category<select disabled={reusingProduct} value={form.categoryId} onChange={e => field("categoryId", e.target.value)}>{dashboard.categories.filter(item => item.isEnabled || item.id === form.categoryId).map(item => <option key={item.id} value={item.id}>{item.label}{item.isEnabled ? "" : " (inactive)"}</option>)}</select></label>
        <label>Model number<input readOnly={reusingProduct} value={form.modelNumber ?? ""} onChange={e => field("modelNumber", e.target.value)} /></label>
        {(brandCreatorOpen || form.newBrandName) && !reusingProduct && <div className="admin-inline-create span-2" role="group" aria-labelledby="inline-brand-heading"><div><strong id="inline-brand-heading">{form.newBrandName ? "New brand detected" : "Enter a new brand"}</strong><span>{form.newBrandName ? "Review the detected identity. No catalog record has been created yet." : "Enter the public brand name. It will be created only when this offer is saved."}</span></div><div className="admin-inline-create-fields"><label>New brand name<input autoFocus required maxLength={120} value={form.newBrandName ?? ""} onChange={event => { field("newBrandName", event.target.value); field("newBrandSlug", slugify(event.target.value)); field("brandId", null); field("confirmBrandCreation", false); }} /></label><label>Brand slug<input required maxLength={140} pattern="[a-z0-9-]+" value={form.newBrandSlug ?? ""} onChange={event => { field("newBrandSlug", slugify(event.target.value)); field("brandId", null); field("confirmBrandCreation", false); }} /></label></div><label className="admin-toggle"><input type="checkbox" checked={form.confirmBrandCreation} disabled={!form.newBrandName?.trim() || !form.newBrandSlug?.trim()} onChange={event => field("confirmBrandCreation", event.target.checked)} /><span>Create, activate, and use this brand when I save the offer</span></label><div className="admin-inline-create-actions"><button className="button button-text" type="button" onClick={() => { setBrandCreatorOpen(false); field("newBrandName", null); field("newBrandSlug", null); field("confirmBrandCreation", false); }}>Use an existing brand instead</button></div></div>}
      </div></details>
      {form.productId ? <ProductImageEditor productId={form.productId} dashboard={dashboard} refresh={refresh} notify={notify} reportError={reportError} /> : <NewProductImageIntake value={newProductImage} onChange={setNewProductImage} />}
      <details open><summary>Offer essentials</summary><div className="admin-form-grid">
        <label>Store<select disabled={existing} value={form.retailerId} onChange={e => field("retailerId", e.target.value)}>{dashboard.retailers.filter(item => item.isEnabled || item.id === form.retailerId).map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
        <label>Deal price (CAD)<input required type="number" min="0.01" max="1000000" step="0.01" value={form.currentPrice} onChange={e => field("currentPrice", e.target.value)} /><span className="field-help">The price currently available during this promotion.</span></label>
        <label>Regular price (CAD, optional)<input type="number" min="0.01" max="1000000" step="0.01" value={form.regularPrice} onChange={e => { field("regularPrice", e.target.value); if (e.target.value && !form.regularPriceObservedAt) field("regularPriceObservedAt", form.observedAt); }} /><span className="field-help">The same store’s normal price outside this promotion. It must be higher than the deal price.</span></label>
        <label>Regular price verified at<input type="datetime-local" required={Boolean(form.regularPrice)} disabled={!form.regularPrice} value={form.regularPriceObservedAt} onChange={e => field("regularPriceObservedAt", e.target.value)} /></label>
        <label className="span-2">Regular price evidence<input required={Boolean(form.regularPrice)} disabled={!form.regularPrice} maxLength={1000} placeholder="Retailer feed field, approved API reference, or reviewed source record" value={form.regularPriceEvidenceReference ?? ""} onChange={e => field("regularPriceEvidenceReference", e.target.value)} /><span className="field-help">Required before the public card can claim a regular price or savings.</span></label>
        <label className="span-2">Canonical retailer Product page<input required type="url" placeholder="https://www.amazon.ca/dp/ASIN" value={form.productUrl} onChange={e => field("productUrl", e.target.value)} /><span className="field-help">Filled automatically when an Amazon.ca Product ID is found. Review before publishing.</span></label>
        <label>Availability<select value={form.availabilityState} onChange={e => field("availabilityState", e.target.value)}><option>AVAILABLE</option><option>UNAVAILABLE</option><option>UNKNOWN</option></select></label>
        <label>Observed at<input required type="datetime-local" value={form.observedAt} onChange={e => field("observedAt", e.target.value)} /></label>
        <label>Promotion starts (optional)<input type="datetime-local" value={form.offerValidFrom} onChange={e => field("offerValidFrom", e.target.value)} /><span className="field-help">A future promotion remains hidden until this time.</span></label>
        <label>Promotion ends (optional)<input type="datetime-local" value={form.offerValidUntil} onChange={e => field("offerValidUntil", e.target.value)} /><span className="field-help">The offer is automatically hidden after this time.</span></label>
        <label>External Product ID<input required disabled={existing} value={form.externalListingId} onChange={e => field("externalListingId", e.target.value)} /><span className="field-help">Use the ASIN for Amazon, or the stable retailer listing ID for other stores.</span></label>
      </div></details>
      <details><summary>Advanced source and retailer details</summary><div className="admin-form-grid">
        <label>Merchant policy<select disabled={existing} value={form.merchantPolicyId} onChange={e => field("merchantPolicyId", e.target.value)}>{dashboard.policies.map(item => <option key={item.id} value={item.id}>{item.sourceKey} · current price {item.priceStorage}</option>)}</select></label>
        <label>Retailer SKU<input value={form.retailerSku ?? ""} onChange={e => field("retailerSku", e.target.value)} /></label>
        <label className="span-2">Original retailer title<input required value={form.originalTitle} onChange={e => field("originalTitle", e.target.value)} /></label>
        <label className="span-2">Provider-generated link destination<input type="url" placeholder="Optional; used by Impact, CJ, and Rakuten" value={form.approvedAffiliateDestinationReference ?? ""} onChange={e => field("approvedAffiliateDestinationReference", e.target.value)} /><span className="field-help">Amazon owner-provided links use the canonical Product page above. This field remains for providers that generate a protected /go handoff.</span></label>
        <label>Seller<input value={form.seller ?? ""} onChange={e => field("seller", e.target.value)} /></label>
        <label>Marketplace seller<select value={form.isMarketplaceSeller === null ? "unknown" : String(form.isMarketplaceSeller)} onChange={e => field("isMarketplaceSeller", e.target.value === "unknown" ? null : e.target.value === "true")}><option value="false">No</option><option value="true">Yes</option><option value="unknown">Unknown</option></select></label>
        <label>Region<input value={form.regionAvailabilityContext ?? ""} onChange={e => field("regionAvailabilityContext", e.target.value)} /></label>
        <label>Shipping context<input value={form.shippingContext ?? ""} onChange={e => field("shippingContext", e.target.value)} /></label>
        <label>Fetched at<input required type="datetime-local" value={form.fetchedAt} onChange={e => field("fetchedAt", e.target.value)} /></label>
        <label className="span-2">External identifiers (JSON)<textarea rows={5} value={form.externalIdentifiers} onChange={e => field("externalIdentifiers", e.target.value)} /></label>
      </div></details>
    </div><aside className="admin-publication-card">
      <p className="eyebrow">Card preview — not public</p><div className="admin-offer-card-preview">{previewImage ? <img src={previewImage.previewPath} alt="" /> : <div aria-hidden="true">G</div>}<p>{dashboard.retailers.find(item => item.id === form.retailerId)?.label ?? "Retailer"}</p><strong>{form.productTitle || "Product title"}</strong><span className="admin-preview-deal-price">{form.currentPrice ? Number(form.currentPrice).toLocaleString("en-CA", { style: "currency", currency: "CAD" }) : "Deal price"}</span>{form.regularPrice && Number(form.regularPrice) > Number(form.currentPrice) && <small><del>{Number(form.regularPrice).toLocaleString("en-CA", { style: "currency", currency: "CAD" })}</del> · Save {Math.round((1 - Number(form.currentPrice) / Number(form.regularPrice)) * 100)}%</small>}<button type="button" disabled>Check retailer price</button></div>
      <h2>Public readiness</h2><p><strong>{form.isEnabled ? "Currently published" : "Draft / not public"}</strong></p>
      <p>Publication requires an active Product, store and Merchant Policy, reviewed price data, a valid time window, and an eligible retailer handoff.</p>
      {existing && <p className="admin-link-readiness"><strong>Retailer link</strong><span>{selected.affiliateLinkReadiness}</span></p>}
      <label>Internal identity decision<select value={form.matchState} onChange={e => field("matchState", e.target.value)}><option value="CONFIRMED">Offer identity verified</option><option value="POSSIBLEMATCHREVIEW">Identity needs review</option><option value="MANUALREVIEW">Manual review</option><option value="NOMATCH">Identity unavailable</option></select></label>
      <label>Change reason<textarea rows={4} value={form.changeReason ?? ""} onChange={e => field("changeReason", e.target.value)} placeholder="Required when deactivating or changing match state" /></label>
      <div className="admin-readonly"><strong>Derived, not editable</strong><span>Freshness from timestamps</span><span>Evidence from policy</span><span>Amazon handoff uses the exact owner-provided link</span><span>Other providers keep protected /go handoff</span><span>Expired offers leave public discovery automatically</span></div>
    </aside></div>
  </form>;
}

function AffiliateLinkIntake({ dashboard, selected, form, field, reportError, openExisting }: {
  dashboard: AdminDashboard;
  selected: AdminOffer | "new" | null;
  form: OfferForm;
  field: <K extends keyof OfferForm>(key: K, value: OfferForm[K]) => void;
  reportError: (value: string | null) => void;
  openExisting: (value: AdminOffer | "new") => void;
}) {
  const [inspection, setInspection] = useState<AdminAffiliateLinkInspection | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [analysisError, setAnalysisError] = useState<string | null>(null);

  const existingOffer = inspection?.externalProductId
    ? dashboard.offers.find(offer => offer.retailerId === inspection.matchedRetailerId && offer.externalListingId.toUpperCase() === inspection.externalProductId?.toUpperCase())
    : undefined;
  const titleSuggestion = suggestProductTitleFromRetailerUrl(inspection?.resolvedProductUrl ?? form.affiliateTrackingUrl ?? "");

  function applyInspection(result: AdminAffiliateLinkInspection) {
    if (result.matchedRetailerId) field("retailerId", result.matchedRetailerId);
    if (result.canonicalProductUrl) {
      field("productUrl", result.canonicalProductUrl);
      field("approvedAffiliateDestinationReference", result.canonicalProductUrl);
    }
    if (result.externalProductId) {
      field("externalListingId", result.externalProductId);
      if (form.externalIdentifiers.trim() === "{}") field("externalIdentifiers", jsonText({ asin: result.externalProductId }));
    }
    if (result.partnerTag) field("affiliatePartnerTag", result.partnerTag);
    const suggestedTitle = suggestProductTitleFromRetailerUrl(result.resolvedProductUrl ?? form.affiliateTrackingUrl ?? "");
    if (suggestedTitle && !form.productTitle.trim()) {
      field("productTitle", suggestedTitle);
      field("originalTitle", suggestedTitle);
      field("slug", slugify(suggestedTitle));
    }
    if (!form.productId && result.brandCandidate) {
      if (result.brandCandidate.matchStatus === "MATCHED_EXISTING" && result.brandCandidate.matchedBrandId && result.brandCandidate.matchedBrandIsEnabled) {
        field("brandId", result.brandCandidate.matchedBrandId);
        field("newBrandName", null); field("newBrandSlug", null); field("confirmBrandCreation", false);
      } else {
        field("brandId", null);
        field("newBrandName", result.brandCandidate.name);
        field("newBrandSlug", result.brandCandidate.slug);
        field("confirmBrandCreation", false);
      }
    }
  }

  async function analyze() {
    if (!form.affiliateTrackingUrl?.trim()) { reportError("Paste the retailer link you created in the provider account."); return; }
    setAnalyzing(true); setInspection(null); setAnalysisError(null); reportError(null);
    try {
      const result = await inspectAdminAffiliateLink(form.affiliateTrackingUrl);
      setInspection(result);
      applyInspection(result);
    }
    catch (caught) {
      const message = caught instanceof Error ? caught.message : "The retailer link could not be analyzed.";
      setAnalysisError(message); reportError(message);
    }
    finally { setAnalyzing(false); }
  }

  function useDetails() {
    if (!inspection) return;
    applyInspection(inspection);
  }

  return <section className="admin-card admin-affiliate-intake" aria-labelledby="affiliate-link-heading">
    <div className="admin-section-heading"><div><p className="eyebrow">Step 1</p><h2 id="affiliate-link-heading">Add offer from a retailer link</h2></div>{selected !== "new" && selected?.affiliateLinkStatus && <span className={`status-chip ${selected.affiliateLinkStatus === "ACTIVE" ? "status-ready" : ""}`}>{selected.affiliateLinkStatus}</span>}</div>
    <p>Paste a link created through your approved retailer program. We inspect its format and show what still needs review. Nothing is published automatically.</p>
    <div className="admin-affiliate-analyze-row"><label className="admin-affiliate-link-field">Retailer link<input type="url" maxLength={2000} placeholder="https://amzn.to/..." value={form.affiliateTrackingUrl ?? ""} onChange={event => { field("affiliateTrackingUrl", event.target.value); setInspection(null); setAnalysisError(null); }} /></label><button className="button button-secondary" type="button" disabled={analyzing || !form.affiliateTrackingUrl} onClick={() => void analyze()}>{analyzing ? "Validating…" : "Validate and fill"}</button><span className="field-help admin-affiliate-link-help">The original link is preserved. Amazon short links are resolved only far enough to validate an Amazon.ca destination and identify the Product; the Product page is not downloaded.</span></div>
    {analyzing && <p role="status" aria-live="polite">Validating the provider destination and filling available Product identity…</p>}
    {analysisError && <p className="admin-inline-error" role="alert">{analysisError}</p>}
    {inspection && <div className={`admin-affiliate-result ${inspection.status === "READY" ? "is-ready" : "needs-review"}`} tabIndex={-1}>
      <div className="admin-section-heading"><div><strong>{inspection.status === "READY" ? "Link recognized" : "Link recognized — review needed"}</strong><p>{existingOffer ? "This Product and offer already exist in the catalog." : "Store, Product ID, canonical URL, Partner Tag, and URL title suggestion are filled when available."}</p></div><span className="status-chip">{inspection.status.replaceAll("_", " ")}</span></div>
      <dl><div><dt>Provider</dt><dd>{inspection.provider}</dd></div><div><dt>Handoff</dt><dd>{inspection.handoffMode.replaceAll("_", " ")}</dd></div><div><dt>Tracking host</dt><dd>{inspection.trackingHost}</dd></div><div><dt>Store</dt><dd>{inspection.matchedRetailer ?? "Not configured"}</dd></div><div><dt>Product ID</dt><dd>{inspection.externalProductId ?? "Enter ASIN manually"}</dd></div><div><dt>Brand</dt><dd>{inspection.brandCandidate?.matchedBrandName ?? inspection.brandCandidate?.name ?? "Review manually"}</dd></div><div><dt>Destination</dt><dd>{inspection.canonicalProductUrl ?? "Enter canonical Amazon.ca Product page"}</dd></div></dl>
      <div className={`admin-autofill-status ${existingOffer || inspection.brandCandidate?.matchStatus === "MATCHED_EXISTING" ? "is-ready" : ""}`}><strong>{existingOffer ? "Complete catalog data found" : inspection.brandCandidate?.matchStatus === "MATCHED_EXISTING" ? "Existing brand matched" : inspection.brandCandidate ? "New brand candidate filled for review" : "Product identity filled from the URL"}</strong><p>{existingOffer ? "Name, classification, latest permitted price, and reviewed image can be reused from the existing record." : `${titleSuggestion ? "A title suggestion was filled from the URL. " : ""}${inspection.brandCandidate?.confidence === "LOW" ? "The brand candidate came only from URL text and must be confirmed before save. " : ""}Price and image are not present in an Amazon link. Enter reviewed values or configure an approved Creators API connector; GreatDeals.ca will not scrape the Product page.`}</p></div>
      {inspection.warnings.length > 0 && <ul>{inspection.warnings.map(warning => <li key={warning}>{warning}</li>)}</ul>}
      <div className="admin-affiliate-result-actions">{existingOffer ? <button className="button button-primary" type="button" onClick={() => openExisting(existingOffer)}>Open existing offer</button> : <button className="button button-primary" type="button" onClick={useDetails}>Reapply autofill</button>}<button className="button button-text" type="button" onClick={() => setInspection(null)}>Analyze another link</button></div>
    </div>}
    {form.affiliateTrackingUrl && <div className="admin-affiliate-confirmation">
      <label>Amazon Partner Tag<input maxLength={100} placeholder="yourtag-20" value={form.affiliatePartnerTag ?? ""} onChange={event => field("affiliatePartnerTag", event.target.value)} /><span className="field-help">For amzn.to links, confirm the tag from your Amazon Associates Canada account.</span></label>
      <label>Relationship evidence reference<textarea rows={3} maxLength={1000} placeholder="Redacted account approval or SiteStripe evidence reference" value={form.affiliateRelationshipEvidenceReference ?? ""} onChange={event => field("affiliateRelationshipEvidenceReference", event.target.value)} /></label>
      <label className="admin-toggle"><input type="checkbox" checked={form.affiliateRelationshipConfirmed} onChange={event => field("affiliateRelationshipConfirmed", event.target.checked)} /><span>I confirm this link belongs to my approved account and GreatDeals.ca is registered for its use.</span></label>
      <p className="field-help">This confirmation validates the commercial link only. Price, metadata and image rights remain controlled by the selected Merchant Policy.</p>
    </div>}
  </section>;
}

function NewProductImageIntake({ value, onChange }: { value: NewProductImageDraft; onChange: (value: NewProductImageDraft) => void }) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  useEffect(() => {
    if (!value.file) { setPreviewUrl(null); return; }
    const next = URL.createObjectURL(value.file);
    setPreviewUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [value.file]);

  return <details open className="admin-new-product-image"><summary>Product image</summary><div className="admin-new-product-image-layout">
    <div className="admin-new-image-preview">{previewUrl ? <img src={previewUrl} alt="Selected Product preview" /> : <div aria-hidden="true">Image</div>}<strong>{value.file?.name ?? "No image selected"}</strong><span>{value.file ? "This reviewed image will be attached immediately after the Product is created." : "Choose an owner-created or licensed image, or leave the neutral public fallback."}</span></div>
    <div className="admin-new-image-fields"><label>Reviewed Product image<input type="file" accept="image/png,image/jpeg,image/webp" onChange={event => onChange({ ...value, file: event.target.files?.[0] ?? null })} /><span className="field-help">PNG, JPEG, or WebP · up to 1 MB · maximum 2400 × 2400. Amazon images require an approved API/content-rights path and are not copied from the Product page.</span></label><label>Rights evidence reference<textarea rows={3} maxLength={1000} value={value.rightsEvidenceReference} onChange={event => onChange({ ...value, rightsEvidenceReference: event.target.value })} /></label><div className="admin-form-grid admin-card-form-grid"><label>Effective at (optional)<input type="datetime-local" value={value.effectiveAt} onChange={event => onChange({ ...value, effectiveAt: event.target.value })} /></label><label>Expires at (optional)<input type="datetime-local" value={value.expiresAt} onChange={event => onChange({ ...value, expiresAt: event.target.value })} /></label></div><label className="admin-toggle"><input type="checkbox" checked={value.activate} onChange={event => onChange({ ...value, activate: event.target.checked })} /><span>Activate this image after the offer is saved</span></label></div>
  </div></details>;
}

function ProductImageEditor({ productId, dashboard, refresh, notify, reportError }: { productId: string; dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const images = dashboard.productImages.filter(image => image.productId === productId);
  const current = images.find(image => image.isPubliclyVisible) ?? images[0];
  const [file, setFile] = useState<File | null>(null);
  const [evidence, setEvidence] = useState("Owner-created or licensed asset reviewed by the account owner");
  const [effectiveAt, setEffectiveAt] = useState("");
  const [expiresAt, setExpiresAt] = useState("");
  const [activate, setActivate] = useState(true);
  const [reason, setReason] = useState("Editorial product image review");
  const [pending, setPending] = useState(false);

  async function upload() {
    if (!file) { reportError("Choose a PNG, JPEG, or WebP product image."); return; }
    if (!evidence.trim()) { reportError("Add a rights evidence reference before uploading."); return; }
    setPending(true); notify(null); reportError(null);
    try {
      await uploadAdminProductImage(productId, { file, rightsEvidenceReference: evidence.trim(), allowedPlacements: "DEAL_CARD,PRODUCT_PAGE,WISHLIST", effectiveAt: clean(effectiveAt), expiresAt: clean(expiresAt), activate });
      await refresh(); setFile(null); notify(activate ? "Product image uploaded and activated." : "Product image uploaded for review.");
    } catch (caught) { reportError(caught instanceof Error ? caught.message : "Product image could not be uploaded."); }
    finally { setPending(false); }
  }

  async function changeState(action: "activate" | "archive", imageId: string) {
    if (!reason.trim()) { reportError("Add a change reason first."); return; }
    setPending(true); notify(null); reportError(null);
    try { if (action === "activate") await activateAdminProductImage(imageId, reason.trim()); else await archiveAdminProductImage(imageId, reason.trim()); await refresh(); notify(action === "activate" ? "Product image activated." : "Product image archived. The previous asset remains in the audit history."); }
    catch (caught) { reportError(caught instanceof Error ? caught.message : "Product image state could not be changed."); }
    finally { setPending(false); }
  }

  return <details open className="admin-product-image"><summary>Product image</summary><div className="admin-product-image-layout">
    <div className="admin-image-preview">{current ? <><img src={current.previewPath} alt={`Preview for ${current.productTitle}`} /><div><span className={`status-chip ${current.isPubliclyVisible ? "status-ready" : ""}`}>{current.isPubliclyVisible ? "Public" : current.state}</span><strong>{current.fileName}</strong><small>{current.width} × {current.height} · {Math.ceil(current.sizeBytes / 1024)} KB · {current.allowedPlacements}</small></div></> : <p>No reviewed image is associated with this product.</p>}</div>
    <div className="admin-image-controls"><div className="admin-upload"><label>Reviewed product asset <span>PNG, JPEG, or WebP · 1 MB · max 2400 × 2400</span></label><div><input type="file" accept="image/png,image/jpeg,image/webp" onChange={event => setFile(event.target.files?.[0] ?? null)} /><button className="button button-secondary" type="button" disabled={pending || !file} onClick={() => void upload()}>{pending ? "Working…" : "Upload"}</button></div></div>
      <label>Rights evidence reference<textarea rows={3} maxLength={1000} value={evidence} onChange={event => setEvidence(event.target.value)} /></label>
      <div className="admin-form-grid"><label>Effective at (optional)<input type="datetime-local" value={effectiveAt} onChange={event => setEffectiveAt(event.target.value)} /></label><label>Expires at (optional)<input type="datetime-local" value={expiresAt} onChange={event => setExpiresAt(event.target.value)} /></label></div>
      <label className="admin-toggle"><input type="checkbox" checked={activate} onChange={event => setActivate(event.target.checked)} /><span>Activate after upload</span></label>
      {images.length > 0 && <><label>State change reason<input maxLength={300} value={reason} onChange={event => setReason(event.target.value)} /></label><div className="admin-image-history">{images.map(image => <article key={image.id}><div><strong>{image.fileName}</strong><small>{image.state} · reviewed {new Date(image.lastValidatedAt).toLocaleDateString("en-CA")}</small></div><div>{image.state !== "ACTIVE" && <button className="button button-secondary" type="button" disabled={pending} onClick={() => void changeState("activate", image.id)}>Activate</button>}{image.state !== "ARCHIVED" && <button className="button button-text" type="button" disabled={pending} onClick={() => void changeState("archive", image.id)}>Archive</button>}</div></article>)}</div></>}
    </div>
  </div></details>;
}

function Banners({ dashboard, refresh, notify, reportError }: { dashboard: AdminDashboard; refresh: () => Promise<void>; notify: (value: string | null) => void; reportError: (value: string | null) => void }) {
  const [selected, setSelected] = useState<AdminBanner | null>(null);
  const [form, setForm] = useState<AdminBannerInput | null>(null);
  const [pending, setPending] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploadKey, setUploadKey] = useState(0);
  const [filter, setFilter] = useState<"all" | "active" | "inactive" | "attention">("all");
  const [selection, setSelection] = useState<Record<string, boolean>>({});
  const [selectionReason, setSelectionReason] = useState("");
  const [savingSelection, setSavingSelection] = useState(false);

  useEffect(() => {
    setSelection(Object.fromEntries(dashboard.banners.map(banner => [banner.retailerId, banner.isEnabled])));
  }, [dashboard.banners]);

  const artworkOptions = useMemo(() => [
    ...builtInAssets,
    ...dashboard.bannerAssets.map(asset => ({ path: asset.assetPath, label: `${asset.fileName} · uploaded ${new Date(asset.createdAt).toLocaleDateString("en-CA")}` })),
  ], [dashboard.bannerAssets]);
  const changedSelection = dashboard.banners.filter(banner => banner.profileId && (selection[banner.retailerId] ?? false) !== banner.isEnabled);
  const removesBanner = changedSelection.some(banner => banner.isEnabled && !selection[banner.retailerId]);
  const selectedCount = dashboard.banners.filter(banner => banner.profileId && selection[banner.retailerId]).length;
  const visibleBanners = dashboard.banners.filter(banner => filter === "all" ||
    (filter === "active" && banner.isEnabled) ||
    (filter === "inactive" && !banner.isEnabled) ||
    (filter === "attention" && banner.isEnabled && (!banner.isInPublicCarousel || banner.publicArtworkState === "FALLBACK")));

  function open(banner: AdminBanner) {
    setSelected(banner);
    setForm(bannerInput(banner));
    notify(null);
    reportError(null);
    window.scrollTo({ top: 0, left: 0, behavior: "auto" });
  }
  function field<K extends keyof AdminBannerInput>(key: K, value: AdminBannerInput[K]) { setForm(current => current ? { ...current, [key]: value } : current); }

  async function uploadArtwork() {
    if (!uploadFile || !form) return;
    if (uploadFile.size > 2 * 1024 * 1024) { reportError("Choose a PNG, JPEG, or WebP image no larger than 2 MB."); return; }
    setUploading(true); notify(null); reportError(null);
    try {
      const asset = await uploadAdminBannerAsset(uploadFile);
      field("assetPath", asset.assetPath);
      setUploadFile(null); setUploadKey(value => value + 1);
      await refresh();
      notify(`${asset.fileName} was uploaded to the reviewed artwork library and selected for this banner.`);
    } catch (caught) { reportError(caught instanceof Error ? caught.message : "Artwork could not be uploaded."); }
    finally { setUploading(false); }
  }

  async function saveSelection() {
    if (changedSelection.length === 0) return;
    if (removesBanner && !selectionReason.trim()) { reportError("Add a reason before removing a banner from the homepage carousel."); return; }
    setSavingSelection(true); notify(null); reportError(null);
    try {
      const activeRetailerIds = dashboard.banners.filter(banner => banner.profileId && selection[banner.retailerId]).map(banner => banner.retailerId);
      await updateAdminBannerSelection(activeRetailerIds, clean(selectionReason));
      await refresh(); setSelectionReason("");
      notify(`Carousel selection saved. ${activeRetailerIds.length} banner${activeRetailerIds.length === 1 ? " is" : "s are"} active; public eligibility remains fail-closed.`);
    } catch (caught) { reportError(caught instanceof Error ? caught.message : "Carousel selection could not be saved."); }
    finally { setSavingSelection(false); }
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selected || !form) return; setPending(true); notify(null); reportError(null);
    try {
      const input = { ...form, effectiveAt: form.effectiveAt ? new Date(form.effectiveAt).toISOString() : null, expiresAt: form.expiresAt ? new Date(form.expiresAt).toISOString() : null, changeReason: clean(form.changeReason ?? "") };
      await updateAdminBanner(selected.retailerId, input); await refresh(); setSelected(null); setForm(null); notify("Banner updated and audited. Public rights and affiliate state remain fail-closed.");
    } catch (caught) { reportError(caught instanceof Error ? caught.message : "Banner could not be saved."); }
    finally { setPending(false); }
  }
  if (selected && form) return <form className="admin-editor admin-banner-editor" onSubmit={save}><div className="admin-heading"><div><p className="eyebrow">Banner editor</p><h1>{selected.retailer}</h1><p>Artwork and copy are editable here. Affiliate destination remains read-only and provider-managed.</p></div><div className="admin-heading-actions"><button className="button button-secondary" type="button" onClick={() => { setSelected(null); setForm(null); }}>Cancel</button><button className="button button-primary" type="submit" disabled={pending || uploading}>{pending ? "Saving…" : "Save banner"}</button></div></div>
    <div className="admin-editor-grid admin-banner-editor-grid"><div className="admin-form-stack">
      <section className="admin-card"><h2>Banner copy</h2><div className="admin-form-grid admin-card-form-grid">
        <label className="span-2">Title <span className="field-counter">{form.title.length}/120</span><input required maxLength={120} value={form.title} onChange={e => field("title", e.target.value)} /></label>
        <label className="span-2">Subtitle <span className="field-counter">{form.subtitle.length}/180</span><textarea required maxLength={180} rows={3} value={form.subtitle} onChange={e => field("subtitle", e.target.value)} /></label>
      </div></section>
      <section className="admin-card"><h2>Banner artwork</h2><div className="admin-form-grid admin-card-form-grid">
        <label className="span-2">Artwork from reviewed library<select value={form.assetPath ?? ""} onChange={e => field("assetPath", e.target.value)}>{artworkOptions.map(asset => <option value={asset.path} key={asset.path}>{asset.label}</option>)}</select></label>
        <div className="admin-upload span-2"><label htmlFor="banner-artwork-upload">Upload artwork <span>PNG, JPEG, or WebP · maximum 2 MB · 16:9 recommended</span></label><div><input key={uploadKey} id="banner-artwork-upload" type="file" accept="image/png,image/jpeg,image/webp" onChange={event => setUploadFile(event.target.files?.[0] ?? null)} /><button className="button button-secondary" type="button" disabled={!uploadFile || uploading} onClick={uploadArtwork}>{uploading ? "Uploading…" : "Upload and use"}</button></div><p>The upload is stored in the reviewed artwork library. Publication still depends on the provenance and rights fields below.</p></div>
      </div></section>
      <section className="admin-card"><h2>Artwork provenance and rights</h2><div className="admin-form-grid admin-card-form-grid">
        <label className="span-2">Artwork provenance<select value={form.assetSource} onChange={e => field("assetSource", e.target.value)}><option value="CANADADEALSORIGINAL">GreatDeals original</option><option value="MERCHANTAPPROVEDAFFILIATEASSET">Merchant-approved affiliate asset</option></select><span className="field-help">Provenance types are controlled because they determine publication rights.</span></label>
        {form.assetSource === "MERCHANTAPPROVEDAFFILIATEASSET" && <>
          <label>Affiliate provider<select value={form.assetProvider ?? "RAKUTEN"} onChange={e => field("assetProvider", e.target.value)}><option value="RAKUTEN">Rakuten</option><option value="IMPACT">Impact</option><option value="CJ">CJ</option><option value="AMAZONCREATORS">Amazon Creators</option><option value="OTHER">Other</option></select></label>
          <label>Allowed placement<input readOnly value="store_banner" /></label>
          <label className="span-2">Rights evidence reference — do not include credentials<input required maxLength={500} value={form.assetEvidenceReference ?? ""} onChange={e => field("assetEvidenceReference", e.target.value)} /></label>
          <label>Rights effective from<input required type="datetime-local" value={form.effectiveAt ?? ""} onChange={e => field("effectiveAt", e.target.value)} /></label>
          <label>Rights expire on<input type="datetime-local" value={form.expiresAt ?? ""} onChange={e => field("expiresAt", e.target.value)} /></label>
        </>}
      </div></section>
      <section className="admin-card"><h2>Carousel placement</h2><div className="admin-form-grid admin-card-form-grid">
        <label>Carousel position<input type="number" min="0" max="10000" value={form.bannerOrder} onChange={e => field("bannerOrder", Number(e.target.value))} /></label>
        <label className="span-2">Change reason<textarea rows={3} maxLength={300} value={form.changeReason ?? ""} onChange={e => field("changeReason", e.target.value)} placeholder="Optional note for this content or position change" /></label>
      </div></section>
    </div><aside className="admin-publication-card admin-banner-preview-panel"><div className="admin-preview-heading"><div><p className="eyebrow">Homepage placement</p><h2>Public preview</h2></div><span className={`status-chip ${selected.isEnabled ? "status-ready" : ""}`}>{selected.isEnabled ? "Selected" : "Not selected"}</span></div><p className="field-help">Carousel membership is controlled only from the Store banners selection screen. This editor changes content, artwork, rights, and sequence.</p><div className="admin-banner-preview admin-banner-preview-public" style={{ backgroundImage: `linear-gradient(90deg,rgba(4,31,22,.94),rgba(4,31,22,.38)),url(${form.assetPath})` }}><small>{selected.isInPublicCarousel ? "Browse by store" : "Preview only"}</small><strong>{form.title}</strong><span>{form.subtitle}</span><b>See store deals →</b></div><div className="admin-preview-meta"><span>Carousel position <strong>{form.bannerOrder}</strong></span><span>Artwork <strong>{selected.publicArtworkState.toLowerCase()}</strong></span></div><div className="admin-readonly"><strong>Current public state</strong><span>Visibility: {selected.visibilityState}</span><span>Rights: {selected.rightsState}</span><span>Brand policy: {selected.brandAssetPolicy}</span><span>{selected.publicEligibilityReason}</span></div></aside></div></form>;

  const publicCount = dashboard.banners.filter(banner => banner.isInPublicCarousel).length;
  const needsAttention = dashboard.banners.filter(banner => banner.isEnabled && (!banner.isInPublicCarousel || banner.publicArtworkState === "FALLBACK")).length;
  return <><div className="admin-heading"><div><p className="eyebrow">Homepage merchandising</p><h1>Store banners</h1><p><strong>{publicCount} public</strong> · {selectedCount} selected · maximum 4 visible per carousel page.</p><p>Select the configured banners that should participate. Public eligibility and artwork rights remain enforced by the backend.</p></div></div>
    <section className={`admin-carousel-selection ${removesBanner ? "has-removals" : ""}`} aria-labelledby="carousel-selection-heading"><div className="admin-carousel-intro"><h2 id="carousel-selection-heading">Carousel selection</h2><p>Choose active banners, then save the selection once. Unconfigured stores must be edited before activation.</p></div><dl className="admin-selection-summary" aria-label="Carousel selection summary" aria-live="polite"><div><dt>Selected</dt><dd>{selectedCount}</dd></div><div><dt>Currently public</dt><dd>{publicCount}</dd></div><div><dt>Need attention</dt><dd>{needsAttention}</dd></div></dl>{removesBanner && <label className="admin-carousel-removal-reason">Change reason for removals<input maxLength={300} value={selectionReason} onChange={event => setSelectionReason(event.target.value)} placeholder="Why are these banners being removed?" /></label>}<button className="button button-primary admin-carousel-save" type="button" onClick={saveSelection} disabled={changedSelection.length === 0 || savingSelection}>{savingSelection ? "Saving selection…" : `Save active banners (${changedSelection.length} change${changedSelection.length === 1 ? "" : "s"})`}</button></section>
    <div className="admin-banner-filters" role="group" aria-label="Filter store banners">{(["all", "active", "inactive", "attention"] as const).map(value => <button type="button" key={value} aria-pressed={filter === value} onClick={() => setFilter(value)}>{value === "attention" ? "Needs attention" : value[0].toUpperCase() + value.slice(1)}</button>)}</div>
    <div className="admin-banner-grid">{visibleBanners.map(banner => <article className="admin-banner-card" key={banner.retailerId}><div className="admin-banner-preview" style={{ backgroundImage: `linear-gradient(90deg,rgba(4,31,22,.94),rgba(4,31,22,.38)),url(${banner.assetPath ?? builtInAssets[0].path})` }}><small>{banner.retailer}</small><strong>{banner.title}</strong><span>{banner.subtitle}</span></div><div className="admin-banner-card-body"><div className="admin-banner-card-status"><span className={`status-chip ${banner.isInPublicCarousel ? "status-ready" : banner.isEnabled ? "status-warning" : ""}`}>{banner.isInPublicCarousel ? `Public · position ${banner.publicPosition}` : banner.visibilityState}</span><span>{banner.publicArtworkState === "FALLBACK" ? "Fallback artwork" : banner.assetSource === "CANADADEALSORIGINAL" ? "GreatDeals original" : "Merchant-approved"}</span></div><label className="admin-toggle"><input type="checkbox" checked={selection[banner.retailerId] ?? false} disabled={!banner.profileId} onChange={event => setSelection(current => ({ ...current, [banner.retailerId]: event.target.checked }))} aria-describedby={`banner-reason-${banner.retailerId}`} /><span>Active in homepage carousel</span></label><p id={`banner-reason-${banner.retailerId}`}>{banner.publicEligibilityReason}</p><div className="admin-banner-card-actions"><span>Carousel position {banner.bannerOrder === 2147483647 ? "—" : banner.bannerOrder}</span><button className="button button-secondary" type="button" onClick={() => open(banner)}>Edit banner</button></div></div></article>)}</div>{visibleBanners.length === 0 && <p className="admin-empty">No banners match this filter.</p>}</>;
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
