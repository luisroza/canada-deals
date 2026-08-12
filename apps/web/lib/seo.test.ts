import { afterEach, describe, expect, it } from "vitest";
import { absoluteUrl, siteOrigin } from "./seo";

const originalSiteUrl = process.env.SITE_URL;

afterEach(() => {
  if (originalSiteUrl === undefined) delete process.env.SITE_URL;
  else process.env.SITE_URL = originalSiteUrl;
});

describe("SEO URL policy", () => {
  it("builds canonical URLs from the configured public origin", () => {
    process.env.SITE_URL = "https://deals.example.ca/path-that-must-not-leak";
    expect(siteOrigin()).toBe("https://deals.example.ca");
    expect(absoluteUrl("/products/example")).toBe("https://deals.example.ca/products/example");
  });

  it("fails safely to the local origin for an invalid public URL", () => {
    process.env.SITE_URL = "javascript:alert(1)";
    expect(siteOrigin()).toBe("http://localhost:3000");
  });
});
