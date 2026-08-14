import { describe, expect, it } from "vitest";
import { availabilityLabel, conditionLabel, schemaAvailability, sellerLabel } from "./offerPresentation";

describe("offer presentation", () => {
  it("never publishes stale availability as a structured-data fact", () => {
    expect(schemaAvailability({ freshnessState: "STALE", availabilityState: "AVAILABLE" })).toBeUndefined();
  });

  it("maps current explicit availability without inventing unknown state", () => {
    expect(schemaAvailability({ freshnessState: "RECENT", availabilityState: "AVAILABLE" })).toBe("https://schema.org/InStock");
    expect(schemaAvailability({ freshnessState: "RECENT", availabilityState: "UNAVAILABLE" })).toBe("https://schema.org/OutOfStock");
    expect(schemaAvailability({ freshnessState: "RECENT", availabilityState: "UNKNOWN" })).toBeUndefined();
    expect(availabilityLabel("UNKNOWN")).toBe("Availability unknown");
  });

  it("makes unknown seller and condition explicit", () => {
    expect(sellerLabel(null, "Retailer")).toBe("Seller not provided by Retailer");
    expect(conditionLabel("UNKNOWN")).toBe("Condition unknown");
  });
});
