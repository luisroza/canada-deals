import { describe, expect, it } from "vitest";
import { buildContentSecurityPolicy, parseAllowedDevOrigins } from "./next.config";

describe("Content Security Policy", () => {
  it("keeps HTTP LAN assets loadable during local development", () => {
    const policy = buildContentSecurityPolicy("development");

    expect(policy).not.toContain("upgrade-insecure-requests");
    expect(policy).toContain("style-src 'self' 'unsafe-inline'");
  });

  it("retains insecure-request upgrading in production", () => {
    expect(buildContentSecurityPolicy("production")).toContain("upgrade-insecure-requests");
  });

  it("parses an explicit private-network development allowlist", () => {
    expect(parseAllowedDevOrigins("10.0.0.108, greatdeals.test, 10.0.0.108")).toEqual([
      "10.0.0.108",
      "greatdeals.test",
    ]);
  });
});
