import type { RetailerOffer } from "./api";

export function availabilityLabel(state: RetailerOffer["availabilityState"]) {
  if (state === "AVAILABLE") return "Available online";
  if (state === "UNAVAILABLE") return "Currently unavailable";
  return "Availability unknown";
}

export function conditionLabel(state: RetailerOffer["conditionState"]) {
  if (state === "NEW") return "New condition";
  if (state === "USED") return "Used condition";
  if (state === "REFURBISHED") return "Refurbished condition";
  return "Condition unknown";
}

export function sellerLabel(seller: string | null, retailer: string) {
  return seller ? `Sold by ${seller}` : `Seller not provided by ${retailer}`;
}

export function schemaAvailability(offer: Pick<RetailerOffer, "availabilityState" | "freshnessState">) {
  if (offer.freshnessState === "STALE") return undefined;
  if (offer.availabilityState === "AVAILABLE") return "https://schema.org/InStock";
  if (offer.availabilityState === "UNAVAILABLE") return "https://schema.org/OutOfStock";
  return undefined;
}
