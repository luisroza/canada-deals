import { expect, test } from "@playwright/test";

test("visitor can inspect evidence, freshness, and safe comparison", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Deals with strong evidence." })).toBeVisible();
  await page.getByRole("link", { name: /Northstar 55-inch QLED TV/ }).first().click();
  await expect(page.getByRole("heading", { name: "Price evidence" })).toBeVisible();
  await expect(page.getByText(/history coverage|history unavailable/i).first()).toBeVisible();
  await expect(page.getByRole("heading", { name: "Safe retailer comparison" })).toBeVisible();
});
