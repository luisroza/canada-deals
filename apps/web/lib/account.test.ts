import { describe, expect, it } from "vitest";
import { safeReturnPath } from "./account";

describe("safeReturnPath", () => {
  it("preserves an internal product context", () => {
    expect(safeReturnPath("/products/northstar-55-qled-tv?from=saved#evidence")).toBe("/products/northstar-55-qled-tv?from=saved#evidence");
  });

  it.each([
    "https://attacker.example/steal",
    "//attacker.example/steal",
    "/\\attacker.example/steal",
    "javascript:alert(1)",
  ])("rejects an unsafe return destination: %s", (destination) => {
    expect(safeReturnPath(destination, "/saved")).toBe("/saved");
  });
});
