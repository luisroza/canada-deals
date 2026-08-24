import { expect, test } from "@playwright/test";

const password = "SecurePass42";

function uniqueEmail(prefix: string) {
  return `${prefix}-${Date.now()}-${Math.random().toString(16).slice(2)}@example.test`;
}

async function register(page: import("@playwright/test").Page, email: string, returnTo: string) {
  await page.goto(`/account/register?returnTo=${encodeURIComponent(returnTo)}`);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Create account" }).click();
  await finishEmailConfirmation(page, email, returnTo);
}

async function finishEmailConfirmation(page: import("@playwright/test").Page, email: string, returnTo: string) {
  await expect.poll(async () => {
    const response = await page.request.get(`/api/internal/email-captures/latest?to=${encodeURIComponent(email)}`);
    if (!response.ok()) return "";
    const capture = await response.json() as { textBody: string };
    return capture.textBody.split("\n").find((line) => line.startsWith("http://localhost:3000/account/confirm-email?")) ?? "";
  }).not.toBe("");
  const captureResponse = await page.request.get(`/api/internal/email-captures/latest?to=${encodeURIComponent(email)}`);
  const confirmationLink = (await captureResponse.json() as { textBody: string }).textBody.split("\n").find((line) => line.startsWith("http://localhost:3000/account/confirm-email?"))!;
  const confirmationUrl = new URL(confirmationLink);
  await page.goto(`${confirmationUrl.pathname}${confirmationUrl.search}`);
  await expect(page.getByText("Email confirmed.")).toBeVisible();
  await signIn(page, email, returnTo);
}

async function signIn(page: import("@playwright/test").Page, email: string, returnTo: string) {
  await page.goto(`/account/sign-in?returnTo=${encodeURIComponent(returnTo)}`);
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(page).toHaveURL(returnTo);
}

test("visitor can inspect evidence, freshness, and safe comparison", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Find the right deal. Fast." })).toBeVisible();
  const northstarCard = page.locator(".deal-card", { has: page.getByRole("link", { name: "Northstar 55-inch QLED TV" }) });
  await expect(northstarCard.getByText(/Checked recently.*Strong evidence/)).toBeVisible();
  await expect(northstarCard.getByRole("link", { name: "Check retailer price at Demo North Electronics" })).toBeVisible();
  await expect(northstarCard.getByText("View details")).not.toBeVisible();
  await page.getByRole("link", { name: /Northstar 55-inch QLED TV/ }).first().click();
  await expect(page.getByRole("heading", { name: "What we know about this offer" })).toBeVisible();
  await expect(page.getByText(/Price tracking and target-price alerts are not part/)).not.toBeVisible();
  await expect(page.getByRole("heading", { name: "Compare retailer offers" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Offer conditions" })).toBeVisible();
  await expect(page.getByText("Sold by Demo North Electronics").first()).toBeVisible();
  await expect(page.getByText("Shipping calculated at checkout")).toBeVisible();
  await expect(page.getByText(/No verified requirement was supplied/)).toBeVisible();
  await expect(page.getByText(/No verified expiry was supplied/)).toBeVisible();
  await expect(page.locator(".primary-offer").getByRole("link", { name: "Continue to Demo North Electronics" })).toHaveAttribute("href", /\/go\//);
});

test("affiliate CTA stays centralized on go and resolves a persisted safe tracking link", async ({ page }) => {
  await page.goto("/products/northstar-55-qled-tv");
  const cta = page.locator(".primary-offer").getByRole("link", { name: "Continue to Demo North Electronics" });
  const href = await cta.getAttribute("href");
  expect(href).toMatch(/^\/go\/[0-9a-f-]+$/i);

  const response = await page.request.get(href!, { maxRedirects: 0 });
  expect(response.status()).toBe(302);
  expect(response.headers().location).toMatch(/^https:\/\/demo\.local\//);
});

test("active store banner uses a protected new-tab handoff without exposing the affiliate URL", async ({ page }) => {
  await page.goto("/");
  const banner = page.getByRole("link", { name: /Retailer website Shop Demo North.*Visit retailer/i });
  await expect(banner).toHaveAttribute("href", "/go/store/demo-north-electronics");
  await expect(banner).toHaveAttribute("target", "_blank");
  await expect(banner).toHaveAttribute("rel", "noopener noreferrer sponsored");
  await expect(banner).not.toHaveAttribute("href", /https?:\/\//);

  const handoff = await page.request.get("/go/store/demo-north-electronics?destination=https%3A%2F%2Fattacker.example", { maxRedirects: 0 });
  expect(handoff.status()).toBe(302);
  expect(handoff.headers().location).toBe("https://demo.local/go/demo-north-electronics");
});

test("discovery-only store banner stays inside the store-filtered catalog", async ({ page }) => {
  await page.goto("/");
  const banner = page.getByRole("link", { name: /Browse by store Shop Demo Home & Tool.*See store deals/i });
  await expect(banner).not.toHaveAttribute("target");
  await banner.click();
  await expect(page).toHaveURL(/retailer=demo-home-tool/);
  await expect(page.getByRole("link", { name: "MapleForge 20V Cordless Drill Kit", exact: true })).toBeVisible();
});

test("controlled Rakuten fixture crosses search, product evidence, and persisted handoff", async ({ page }) => {
  await page.goto("/?search=RKT-FIXTURE-100");
  await page.getByRole("link", { name: "Rakuten Controlled Fixture Headphones", exact: true }).click();
  await expect(page.getByRole("heading", { level: 1, name: "Rakuten Controlled Fixture Headphones" })).toBeVisible();
  await expect(page.locator(".primary-offer").getByText("Rakuten Controlled Fixture Retailer", { exact: true })).toBeVisible();

  const cta = page.locator(".primary-offer").getByRole("link", { name: "Continue to Rakuten Controlled Fixture Retailer" });
  const href = await cta.getAttribute("href");
  expect(href).toMatch(/^\/go\/[0-9a-f-]+$/i);

  const response = await page.request.get(href!, { maxRedirects: 0 });
  expect(response.status()).toBe(302);
  expect(response.headers().location).toBe("https://click.linksynergy.test/deep?id=fixture-only");
});

test("visitor sees a possible variant outside safe comparison", async ({ page }) => {
  await page.goto("/products/mapleforge-20v-drill-kit");

  const related = page.locator('section[aria-labelledby="related-heading"]');
  await expect(related).toBeVisible();
  await expect(related.getByText("MapleForge 20V Cordless Drill Tool-Only")).toBeVisible();
  await expect(related.getByRole("link", { name: /continue to/i })).not.toBeVisible();
  await expect(page.getByText("No other confirmed retailer offer.")).toBeVisible();
});

test("missing product presents a useful recovery state instead of a generic 404 page", async ({ page }) => {
  const response = await page.goto("/products/product-not-in-catalog");
  expect(response?.status()).toBe(404);
  await expect(page.getByRole("heading", { level: 1, name: "We couldn’t find this product" })).toBeVisible();
  await expect(page.getByRole("searchbox", { name: "Search for another product" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Browse current deals" })).toHaveAttribute("href", "/#deals");
  await expect(page.getByText("404", { exact: true })).not.toBeVisible();
});

test("price tracker and target-price controls are absent and legacy routes fail closed", async ({ page }) => {
  await page.goto("/products/northstar-55-qled-tv?history=90d");
  await expect(page.getByRole("heading", { name: "Price history" })).not.toBeVisible();
  await expect(page.getByRole("heading", { name: "Target-price alert" })).not.toBeVisible();
  await expect(page.getByRole("button", { name: "Save product" })).toBeVisible();
  expect((await page.request.get("/api/v1/products/northstar-55-qled-tv/history?window=90d")).status()).toBe(404);
});

test("product page remains readable and contained on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/products/northstar-55-qled-tv");
  const productHeading = page.getByRole("heading", { name: "Northstar 55-inch QLED TV", level: 1 });
  const comparisonHeading = page.getByRole("heading", { name: "Compare retailer offers" });

  await expect(productHeading).toBeVisible();
  await expect(comparisonHeading).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
  await expect(page.locator(".primary-offer").getByRole("link", { name: "Continue to Demo North Electronics" })).toBeVisible();
  await expect(page.locator(".mobile-retailer-bar")).not.toBeVisible();
  await page.evaluate(() => window.scrollTo(0, 1400));
  await expect(page.locator(".mobile-retailer-bar")).toBeVisible();
});

test("homepage remains usable at a representative mobile viewport", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "Find the right deal. Fast." })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Mobile primary navigation" })).toBeVisible();
  await expect(page.getByRole("navigation", { name: "Mobile primary navigation" }).getByRole("link")).toHaveCount(5);
  await expect(page.getByRole("main")).toBeVisible();
  await page.keyboard.press("Tab");
  await expect(page.locator(":focus")).toBeVisible();
  const hasHorizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(hasHorizontalOverflow).toBe(false);
  const searchTop = await page.getByRole("combobox", { name: "Search products, models, or categories" }).evaluate((node) => node.getBoundingClientRect().top);
  const storesTop = await page.locator(".store-banners").evaluate((node) => node.getBoundingClientRect().top);
  const firstDealTop = await page.locator("article.deal-card").first().evaluate((node) => node.getBoundingClientRect().top);
  expect(searchTop).toBeLessThan(storesTop);
  expect(firstDealTop).toBeLessThan(1500);
  await expect(page.getByText(/Alert product [0-9a-f]{32}/i)).not.toBeVisible();
});

test("global search offers model and category suggestions on every page", async ({ page }) => {
  await page.goto("/products/northstar-quiet-headphones");
  const search = page.getByRole("combobox", { name: "Search products, models, or categories" });
  await search.fill("NS55QLED-2026");
  await expect(page.getByRole("listbox", { name: "Search suggestions" })).toBeVisible();
  await expect(page.getByRole("option", { name: /Northstar 55-inch QLED TV/ })).toBeVisible();
  await search.press("ArrowDown");
  await search.press("Enter");
  await expect(page).toHaveURL(/\/products\/northstar-55-qled-tv/);
});

test("exact model search uses relevance and opens the canonical product", async ({ page }) => {
  await page.goto("/");
  const search = page.getByRole("combobox", { name: "Search products, models, or categories" });
  await search.fill("NS55QLED-2026");
  await search.press("Enter");

  await expect(page).toHaveURL(/search=NS55QLED-2026/);
  await expect(page.getByRole("heading", { name: "Most relevant deals" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Northstar 55-inch QLED TV" }).first()).toBeVisible();
});

test("category and store filters remain visible in the URL", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Category").selectOption("home-improvement-tools");
  await page.getByLabel("Store", { exact: true }).selectOption("demo-home-tool");
  await page.getByRole("button", { name: "Show deals" }).click();

  await expect(page).toHaveURL(/category=home-improvement-tools/);
  await expect(page).toHaveURL(/retailer=demo-home-tool/);
  await expect(page.getByRole("link", { name: "MapleForge 20V Cordless Drill Kit", exact: true })).toBeVisible();
  await expect(page.getByRole("link", { name: "Remove Category filter" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Remove Store filter" })).toBeVisible();
  await expect(page.getByLabel("Minimum price")).not.toBeVisible();
});

test("browser back restores search and filter state", async ({ page }) => {
  await page.goto("/?search=northstar&category=electronics");
  await page.getByRole("link", { name: "Northstar 55-inch QLED TV" }).first().click();
  await expect(page.getByRole("heading", { name: "What we know about this offer" })).toBeVisible();
  await page.goBack();

  await expect(page).toHaveURL(/search=northstar/);
  await expect(page.getByLabel("Category", { exact: true })).toHaveValue("electronics");
});

test("mobile category and store controls stay compact and usable", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await expect(page.getByRole("region", { name: "Filter deals" })).toBeVisible();
  await page.getByLabel("Category").selectOption("electronics");
  await page.getByRole("button", { name: "Show deals" }).click();
  await expect(page).toHaveURL(/category=electronics/);
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);
});

test("public SEO endpoints and stale Product structured data remain truthful", async ({ page }) => {
  const robots = await page.request.get("/robots.txt");
  const sitemap = await page.request.get("/sitemap.xml");
  expect(robots.ok()).toBe(true);
  expect(await robots.text()).toContain("Sitemap: http://localhost:3000/sitemap.xml");
  expect(sitemap.ok()).toBe(true);
  expect(await sitemap.text()).toContain("/products/northstar-55-qled-tv");

  await page.goto("/products/northstar-65-oled-tv");
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", "http://localhost:3000/products/northstar-65-oled-tv");
  await expect(page.locator('meta[property="og:title"]')).toHaveAttribute("content", /Northstar 65-inch OLED TV/);
  const jsonLd = JSON.parse(await page.locator('script[type="application/ld+json"]').textContent() ?? "{}");
  expect(jsonLd.offers.availability).toBeUndefined();
  await expect(page.getByText(/price may have changed/i).first()).toBeVisible();
});

test("no-result search offers a clear recovery action", async ({ page }) => {
  await page.goto("/?search=definitely-no-such-product-xyz");
  await expect(page.getByRole("heading", { name: "No products match this selection." })).toBeVisible();
  await page.getByRole("link", { name: "Clear selection" }).click();
  await expect(page).toHaveURL("/");
  await expect(page.getByRole("heading", { name: "Recently checked deals" })).toBeVisible();
});

test("visitor can submit a price change report that becomes reviewable", async ({ page }) => {
  const note = `Playwright price changed ${Date.now()}`;
  await page.goto("/");
  await page.getByRole("link", { name: /Northstar 55-inch QLED TV/ }).first().click();
  await page.getByRole("button", { name: "Report stale or wrong" }).click();
  await page.getByRole("radio", { name: "Price changed" }).check();
  await page.getByLabel("Optional note").fill(note);
  await page.getByRole("button", { name: "Send report" }).click();

  await expect(page.getByRole("status")).toContainText("attached to this listing for review");

  const review = await page.request.get("http://localhost:3000/api/internal/listing-issue-reports?status=OPEN");
  expect(review.ok()).toBe(true);
  const reports = await review.json() as Array<{ note: string | null; status: string }>;
  expect(reports).toContainEqual(expect.objectContaining({ note, status: "OPEN" }));
});

test("visitor can report a wrong variant as a review signal", async ({ page }) => {
  const note = `Playwright wrong variant ${Date.now()}`;
  await page.goto("/products/mapleforge-20v-drill-kit");
  await page.getByRole("button", { name: "Report stale or wrong" }).click();
  await page.getByRole("radio", { name: "Wrong variant" }).check();
  await page.getByLabel("Optional note").fill(note);
  await page.getByRole("button", { name: "Send report" }).click();

  await expect(page.getByRole("status")).toContainText("attached to this listing for review");
  const review = await page.request.get("http://localhost:3000/api/internal/listing-issue-reports?status=OPEN");
  const reports = await review.json() as Array<{ note: string | null; reason: string }>;
  expect(reports).toContainEqual(expect.objectContaining({ note, reason: "WRONG_VARIANT" }));
});

test("signed-out shopper crosses the minimal account boundary and saves a product", async ({ page }) => {
  const email = uniqueEmail("save-flow");
  await page.goto("/products/northstar-55-qled-tv");
  await page.getByRole("button", { name: "Save product" }).click();
  await expect(page.getByRole("heading", { name: "Sign in to save this product" }).locator("..")).toContainText("account is needed only");
  await page.getByRole("heading", { name: "Sign in to save this product" }).locator("..").getByRole("link", { name: "Create account" }).click();
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Create account" }).click();
  await finishEmailConfirmation(page, email, "/products/northstar-55-qled-tv");

  await page.getByRole("button", { name: "Save product" }).click();
  await expect(page.getByRole("button", { name: "Saved — remove" })).toBeVisible();
  await page.goto("/saved");
  await expect(page.getByRole("heading", { name: "Northstar 55-inch QLED TV" })).toBeVisible();
  await expect(page.getByText("Evidence: strong")).toBeVisible();
});

test("saved product persists across logout and a new login session", async ({ page }) => {
  const email = uniqueEmail("session-persistence");
  const productPath = "/products/northstar-quiet-headphones";
  await register(page, email, productPath);
  await page.getByRole("button", { name: "Save product" }).click();
  await expect(page.getByRole("button", { name: "Saved — remove" })).toBeVisible();

  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(page).toHaveURL("/");
  await signIn(page, email, "/saved");
  await expect(page.getByRole("heading", { name: "Northstar Quiet Wireless Headphones" })).toBeVisible();
});

test("saved products are isolated between accounts", async ({ page }) => {
  const productPath = "/products/mapleforge-20v-drill-kit";
  await register(page, uniqueEmail("isolation-a"), productPath);
  await page.getByRole("button", { name: "Save product" }).click();
  await expect(page.getByRole("button", { name: "Saved — remove" })).toBeVisible();
  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(page).toHaveURL("/");

  await register(page, uniqueEmail("isolation-b"), "/saved");
  await expect(page.getByRole("heading", { name: "Your wishlist is empty." })).toBeVisible();
  await expect(page.getByText("MapleForge 20V Cordless Drill Kit")).not.toBeVisible();
});

test("shopper can unsave a product from the saved list", async ({ page }) => {
  const productPath = "/products/northstar-65-oled-tv";
  await register(page, uniqueEmail("unsave"), productPath);
  await page.getByRole("button", { name: "Save product" }).click();
  await page.goto("/saved");
  await expect(page.getByRole("heading", { name: "Northstar 65-inch OLED TV" })).toBeVisible();
  await page.getByRole("button", { name: "Remove from wishlist" }).click();
  await expect(page.getByRole("heading", { name: "Your wishlist is empty." })).toBeVisible();
  await expect(page.getByText("Northstar 65-inch OLED TV")).not.toBeVisible();
});

test("account return path rejects an external redirect", async ({ page }) => {
  await page.goto("/account/sign-in?returnTo=https%3A%2F%2Fattacker.example%2Fsteal");
  await expect(page.getByRole("link", { name: "Continue without signing in" })).toHaveAttribute("href", "/");
});

test("unconfirmed shopper can request a fresh confirmation email", async ({ page }) => {
  const email = uniqueEmail("confirmation-resend");
  await page.goto("/account/register");
  await page.getByLabel("Email").fill(email);
  await page.getByLabel("Password").fill(password);
  await page.getByRole("button", { name: "Create account" }).click();
  await expect(page.getByRole("status")).toContainText("Confirm your email");
  const initial = await page.request.get(`/api/internal/email-captures/latest?to=${encodeURIComponent(email)}`);
  const initialCapture = await initial.json() as { capturedAt: string };
  await page.getByRole("button", { name: "Resend confirmation email" }).click();
  await expect(page.getByRole("status")).toContainText("If an unconfirmed account exists");
  await expect.poll(async () => {
    const response = await page.request.get(`/api/internal/email-captures/latest?to=${encodeURIComponent(email)}`);
    return (await response.json() as { capturedAt: string }).capturedAt;
  }).not.toBe(initialCapture.capturedAt);
  await finishEmailConfirmation(page, email, "/");
});

test("price-alert routes fail closed in the current product", async ({ page }) => {
  const email = uniqueEmail("wishlist-only");
  await register(page, email, "/products/northstar-quiet-headphones");
  await expect(page.getByLabel("Target price (CAD)")).not.toBeVisible();
  expect((await page.request.get("/api/v1/price-alerts")).status()).toBe(404);
  expect((await page.request.get("/api/internal/price-alert-evaluation/deliveries")).status()).toBe(404);
});
