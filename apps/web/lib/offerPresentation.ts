import type { RetailerOffer } from "./api";

export function availabilityLabel(state: RetailerOffer["availabilityState"]) {
  if (state === "AVAILABLE") return "Available online";
  if (state === "UNAVAILABLE") return "Currently unavailable";
  return "Availability unknown";
}

export function schemaAvailability(offer: Pick<RetailerOffer, "availabilityState" | "freshnessState">) {
  if (offer.freshnessState === "STALE") return undefined;
  if (offer.availabilityState === "AVAILABLE") return "https://schema.org/InStock";
  if (offer.availabilityState === "UNAVAILABLE") return "https://schema.org/OutOfStock";
  return undefined;
}
