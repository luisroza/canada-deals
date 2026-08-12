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

async function authenticatedMutation(page: import("@playwright/test").Page, path: string, body?: unknown) {
  const tokenResponse = await page.request.get("/api/v1/account/antiforgery");
  expect(tokenResponse.ok()).toBe(true);
  const { requestToken } = await tokenResponse.json() as { requestToken: string };
  return page.request.post(path, { headers: { "X-CSRF-TOKEN": requestToken }, data: body });
}

test("visitor can inspect evidence, freshness, and safe comparison", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Deals with strong evidence." })).toBeVisible();
  await page.getByRole("link", { name: /Northstar 55-inch QLED TV/ }).first().click();
  await expect(page.getByRole("heading", { name: "Price evidence" })).toBeVisible();
  await expect(page.getByText(/history coverage|history unavailable/i).first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Safe retailer comparison" })).toBeVisible();
  await expect(page.locator(".primary-offer").getByRole("link", { name: "Continue to Demo North Electronics" })).toHaveAttribute("href", /\/go\//);
});

test("visitor sees a possible variant outside safe comparison", async ({ page }) => {
  await page.goto("/products/mapleforge-20v-drill-kit");

  const related = page.locator('section[aria-labelledby="related-heading"]');
  await expect(related).toBeVisible();
  await expect(related.getByText("MapleForge 20V Cordless Drill Tool-Only")).toBeVisible();
  await expect(related.getByRole("link", { name: /continue to/i })).not.toBeVisible();
  await expect(page.getByText("No safe comparison available.")).toBeVisible();
});

test("visitor sees unavailable history without a fabricated claim", async ({ page }) => {
  await page.goto("/products/northstar-quiet-headphones");

  await expect(page.getByRole("heading", { name: "Price evidence" })).toBeVisible();
  await expect(page.getByText("Price history unavailable.")).toBeVisible();
  await expect(page.getByText(/all-time-low/i)).not.toBeVisible();
});

test("visitor switches reliable Product history from 30 to 90 days", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("link", { name: /Northstar 55-inch QLED TV/ }).first().click();
  const history = page.locator("section", { has: page.getByRole("heading", { name: "Price history" }) });

  await expect(history.getByText("Reliable history", { exact: true })).toBeVisible();
  await expect(history.locator(".history-summary")).toContainText("Lowest observed in the last 30 days: $1,049.99");
  await expect(history.getByRole("img", { name: /Observed product prices over 30 days/ })).toBeVisible();
  await history.getByRole("link", { name: "90 days" }).click();
  await expect(page).toHaveURL(/history=90d/);
  await expect(history.locator(".history-summary")).toContainText("Lowest observed in the last 90 days: $1,049.99");
  await expect(history.getByText(/Tracking since/)).toBeVisible();
  await expect(history.getByText(/lowest price ever/i)).toBeVisible();
  await expect(history.getByText(/all-time low/i)).not.toBeVisible();
});

test("partial Product history shows real points and an explicit gap boundary", async ({ page }) => {
  await page.goto("/products/mapleforge-20v-drill-kit?history=90d");
  const history = page.locator("section", { has: page.getByRole("heading", { name: "Price history" }) });

  await expect(history.getByText("Partial history", { exact: true })).toBeVisible();
  await expect(history.getByText(/2 qualifying observations across 2 observed days/)).toBeVisible();
  await expect(history.locator(".history-summary")).toContainText(/gaps limit stronger conclusions/i);
  await expect(history.getByText(/lowest price ever/i)).toBeVisible();
});

test("unavailable Product history has no fake chart while current price remains visible", async ({ page }) => {
  await page.goto("/products/search-fixture-unavailable-kettle?history=90d");
  const history = page.locator("section", { has: page.getByRole("heading", { name: "Price history" }) });

  await expect(history.getByRole("heading", { name: "Price history unavailable" })).toBeVisible();
  await expect(history.getByRole("img")).not.toBeVisible();
  await expect(history.locator(".history-current")).toContainText("$");
});

test("unsafe cheaper variant never enters canonical Product history", async ({ page }) => {
  await page.goto("/products/mapleforge-20v-drill-kit?history=90d");
  const history = page.locator("section", { has: page.getByRole("heading", { name: "Price history" }) });

  await expect(history.locator(".history-summary")).toContainText("Lowest observed in the last 90 days: $179.99");
  await expect(history.getByText("$49.99")).not.toBeVisible();
  await expect(history.getByText("$59.99")).not.toBeVisible();
  await expect(history.getByText("$89.99")).not.toBeVisible();
});

test("Product history remains readable and contained on mobile", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/products/northstar-55-qled-tv?history=90d");
  const productHeading = page.getByRole("heading", { name: "Northstar 55-inch QLED TV", level: 1 });
  const historyHeading = page.getByRole("heading", { name: "Price history" });
  const comparisonHeading = page.getByRole("heading", { name: "Safe retailer comparison" });

  await expect(productHeading).toBeVisible();
  await expect(historyHeading).toBeVisible();
  expect(await productHeading.evaluate((node) => node.getBoundingClientRect().top)).toBeLessThan(await historyHeading.evaluate((node) => node.getBoundingClientRect().top));
  expect(await comparisonHeading.evaluate((node) => node.getBoundingClientRect().top + window.scrollY)).toBeLessThan(await historyHeading.evaluate((node) => node.getBoundingClientRect().top + window.scrollY));
  await expect(page.getByRole("link", { name: "30 days" })).toBeVisible();
  await expect(page.getByRole("link", { name: "90 days" })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)).toBe(false);

  const currentOffer = page.getByRole("heading", { name: "Current observed price" });
  const targetAlert = page.getByRole("heading", { name: "Target-price alert" });
  expect(await currentOffer.evaluate((node) => node.getBoundingClientRect().top + window.scrollY)).toBeLessThan(await targetAlert.evaluate((node) => node.getBoundingClientRect().top + window.scrollY));
  await expect(page.locator(".primary-offer").getByRole("link", { name: "Continue to Demo North Electronics" })).toBeVisible();
  await expect(page.locator(".mobile-retailer-bar")).not.toBeVisible();
  await page.evaluate(() => window.scrollTo(0, 1400));
  await expect(page.locator(".mobile-retailer-bar")).toBeVisible();
});

test("homepage remains usable at a representative mobile viewport", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");

  await expect(page.getByRole("heading", { name: "Deals with strong evidence." })).toBeVisible();
  await expect(page.getByRole("main")).toBeVisible();
  await page.keyboard.press("Tab");
  await expect(page.locator(":focus")).toBeVisible();
  const hasHorizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
  expect(hasHorizontalOverflow).toBe(false);
  const searchTop = await page.getByLabel("Search a product or model number").evaluate((node) => node.getBoundingClientRect().top);
  const trustTop = await page.locator(".trust-strip").evaluate((node) => node.getBoundingClientRect().top);
  const firstDealTop = await page.locator("article.deal-card").first().evaluate((node) => node.getBoundingClientRect().top);
  expect(searchTop).toBeLessThan(trustTop);
  expect(firstDealTop).toBeLessThan(1000);
  await expect(page.getByText(/Alert product [0-9a-f]{32}/i)).not.toBeVisible();
});

test("exact model search uses relevance and opens the canonical product", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Search a product or model number").fill("NS55QLED-2026");
  await page.getByRole("button", { name: "Search deals" }).click();

  await expect(page).toHaveURL(/search=NS55QLED-2026/);
  await expect(page.getByRole("heading", { name: "Most relevant" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Northstar 55-inch QLED TV" }).first()).toBeVisible();
});

test("combined discovery filters remain visible in the URL and exclude unsafe variants", async ({ page }) => {
  await page.goto("/");
  await page.getByLabel("Category").selectOption("home-improvement-tools");
  await page.getByLabel("Minimum price").fill("100");
  await page.getByLabel("Maximum price").fill("200");
  await page.getByLabel("Comparison confidence").selectOption("safe");
  await page.getByLabel("Availability").selectOption("online");
  await page.getByRole("button", { name: "Apply filters" }).click();

  await expect(page).toHaveURL(/category=home-improvement-tools/);
  await expect(page).toHaveURL(/minPrice=100/);
  await expect(page.getByRole("link", { name: "MapleForge 20V Cordless Drill Kit" })).toBeVisible();
  await expect(page.getByText("Ridgeway 20V Cordless Drill Tool-Only")).not.toBeVisible();
  await expect(page.getByRole("link", { name: "Remove Category filter" })).toBeVisible();
});

test("browser back restores search and filter state", async ({ page }) => {
  await page.goto("/?search=northstar&category=electronics&match=safe");
  await page.getByRole("link", { name: "Northstar 55-inch QLED TV" }).first().click();
  await expect(page.getByRole("heading", { name: "Price evidence" })).toBeVisible();
  await page.goBack();

  await expect(page).toHaveURL(/search=northstar/);
  await expect(page.getByLabel("Search a product or model number")).toHaveValue("northstar");
  await expect(page.getByLabel("Category", { exact: true })).toHaveValue("electronics");
  await expect(page.getByLabel("Comparison confidence", { exact: true })).toHaveValue("safe");
});

test("mobile filter sheet is labelled, focusable, and applies controls", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");
  await page.getByRole("button", { name: "Filters" }).click();

  const dialog = page.getByRole("dialog", { name: "Filter deals" });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByRole("heading", { name: "Filter deals" })).toBeFocused();
  await page.keyboard.press("Escape");
  await expect(dialog).not.toBeVisible();
  await expect(page.getByRole("button", { name: "Filters" })).toBeFocused();
  await page.getByRole("button", { name: "Filters" }).click();
  await dialog.getByLabel("Availability").selectOption("unavailable");
  await dialog.getByRole("button", { name: "Apply filters" }).click();
  await expect(page).toHaveURL(/availability=unavailable/);
  await expect(page.getByRole("link", { name: "Northstar Search Fixture Kettle" })).toBeVisible();
});

test("public SEO endpoints and stale Product structured data remain truthful", async ({ page }) => {
  const robots = await page.request.get("/robots.txt");
  const sitemap = await page.request.get("/sitemap.xml");
  expect(robots.ok()).toBe(true);
  expect(await robots.text()).toContain("Sitemap: http://localhost:3000/sitemap.xml");
  expect(sitemap.ok()).toBe(true);
  expect(await sitemap.text()).toContain("/products/northstar-55-qled-tv");

  await page.goto("/products/northstar-65-oled-tv?history=90d");
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute("href", "http://localhost:3000/products/northstar-65-oled-tv");
  await expect(page.locator('meta[property="og:title"]')).toHaveAttribute("content", /Northstar 65-inch OLED TV/);
  const jsonLd = JSON.parse(await page.locator('script[type="application/ld+json"]').textContent() ?? "{}");
  expect(jsonLd.offers.availability).toBeUndefined();
  await expect(page.getByText(/price may have changed/i).first()).toBeVisible();
});

test("no-result search offers a clear recovery action", async ({ page }) => {
  await page.goto("/?search=definitely-no-such-product-xyz");
  await expect(page.getByRole("heading", { name: "No products match these controls." })).toBeVisible();
  await page.getByRole("link", { name: "Clear search and filters" }).click();
  await expect(page).toHaveURL("/");
  await expect(page.getByRole("heading", { name: "Most recently checked" })).toBeVisible();
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
  await expect(page.getByRole("heading", { name: "No saved products yet." })).toBeVisible();
  await expect(page.getByText("MapleForge 20V Cordless Drill Kit")).not.toBeVisible();
});

test("shopper can unsave a product from the saved list", async ({ page }) => {
  const productPath = "/products/northstar-65-oled-tv";
  await register(page, uniqueEmail("unsave"), productPath);
  await page.getByRole("button", { name: "Save product" }).click();
  await page.goto("/saved");
  await expect(page.getByRole("heading", { name: "Northstar 65-inch OLED TV" })).toBeVisible();
  await page.getByRole("button", { name: "Remove saved product" }).click();
  await expect(page.getByRole("heading", { name: "No saved products yet." })).toBeVisible();
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

test("confirmed shopper creates a target alert and the real worker captures one deduplicated delivery", async ({ page }) => {
  const productPath = "/products/northstar-quiet-headphones";
  const email = uniqueEmail("target-alert");
  await register(page, email, productPath);

  await page.getByLabel("Target price (CAD)").fill("200.00");
  await page.getByRole("checkbox", { name: /I agree to receive an email/ }).check();
  await page.getByRole("button", { name: "Set alert" }).click();
  await expect(page.getByText(/Active target:/)).toContainText("$200.00");
  await expect(page.getByText(/not marketing or a weekly digest/i)).toBeVisible();

  const productResponse = await page.request.get("/api/v1/products/northstar-quiet-headphones");
  const productId = (await productResponse.json() as { productId: string }).productId;
  const scenario = await authenticatedMutation(page, `/api/internal/price-alert-evaluation/scenarios/${productId}`, { price: 199, listingScope: "safe" });
  expect(scenario.ok()).toBe(true);
  const run = await authenticatedMutation(page, "/api/internal/price-alert-evaluation/run");
  expect(run.status()).toBe(202);

  await expect.poll(async () => {
    const response = await page.request.get(`/api/internal/price-alert-evaluation/deliveries?productId=${productId}`);
    const deliveries = await response.json() as Array<{ status: string }>;
    return deliveries[0]?.status;
  }, { timeout: 30_000 }).toBe("DEVELOPMENTCAPTURED");
  const capturedResponse = await page.request.get(`/api/internal/price-alert-evaluation/deliveries?productId=${productId}`);
  const captured = await capturedResponse.json() as Array<{ status: string; statusReason: string }>;
  expect(captured[0]).toEqual(expect.objectContaining({ status: "DEVELOPMENTCAPTURED", statusReason: "CONTROLLED_DEVELOPMENT_TEST_CAPTURE" }));
  const emailResponse = await page.request.get(`/api/internal/email-captures/latest?to=${encodeURIComponent(email)}`);
  expect(emailResponse.ok()).toBe(true);
  const alertEmail = await emailResponse.json() as { subject: string; textBody: string; htmlBody: string };
  expect(alertEmail.subject).toContain("Northstar Quiet Wireless Headphones");
  expect(alertEmail.textBody).toContain("Qualifying observed price: CAD 199.00");
  expect(alertEmail.textBody).toContain("Your target: CAD 200.00");
  expect(alertEmail.htmlBody).not.toContain("tracking");

  const secondRun = await authenticatedMutation(page, "/api/internal/price-alert-evaluation/run");
  expect(secondRun.status()).toBe(202);
  await expect.poll(async () => {
    const response = await page.request.get(`/api/internal/price-alert-evaluation/deliveries?productId=${productId}`);
    return (await response.json() as unknown[]).length;
  }, { timeout: 30_000 }).toBe(1);

  await page.goto("/saved");
  await expect(page.getByText(/Target alert:/)).toContainText("$200.00");
  await page.getByRole("button", { name: "Remove alert" }).click();
  await expect(page.getByText("No active target-price alert")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Northstar Quiet Wireless Headphones" })).toBeVisible();
});

test("worker ignores a cheaper possible-match variant for alert eligibility", async ({ page }) => {
  const productPath = "/products/mapleforge-20v-drill-kit";
  await register(page, uniqueEmail("unsafe-target"), productPath);
  await page.getByLabel("Target price (CAD)").fill("100.00");
  await page.getByRole("checkbox", { name: /I agree to receive an email/ }).check();
  await page.getByRole("button", { name: "Set alert" }).click();

  const productResponse = await page.request.get("/api/v1/products/mapleforge-20v-drill-kit");
  const productId = (await productResponse.json() as { productId: string }).productId;
  const scenario = await authenticatedMutation(page, `/api/internal/price-alert-evaluation/scenarios/${productId}`, { price: 89, listingScope: "review" });
  expect(scenario.ok()).toBe(true);
  const run = await authenticatedMutation(page, "/api/internal/price-alert-evaluation/run");
  expect(run.status()).toBe(202);
  const { jobId } = await run.json() as { jobId: string };

  await expect.poll(async () => {
    const response = await page.request.get(`/api/internal/price-alert-evaluation/jobs/${jobId}`);
    return response.ok() ? ((await response.json() as { state: string }).state) : "MISSING";
  }, { timeout: 30_000 }).toBe("SUCCEEDED");
  const deliveriesResponse = await page.request.get(`/api/internal/price-alert-evaluation/deliveries?productId=${productId}`);
  expect(await deliveriesResponse.json()).toEqual([]);
});
