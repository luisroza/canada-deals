import type { SavedProduct } from "./api";

export type AccountSession = { isAuthenticated: boolean; email: string | null; emailConfirmed: boolean };

export type PriceAlert = {
  productId: string;
  productSlug: string;
  productTitle: string;
  targetPrice: number;
  currency: "CAD";
  status: "ACTIVE" | "DISABLED";
  targetVersion: number;
  consentGrantedAt: string;
  consentVersion: string;
  lastEvaluatedAt: string | null;
  lastTriggeredAt: string | null;
};

type AccountMutation = { message: string; isAuthenticated: boolean };
export type EmailConfirmationResult = { status: "CONFIRMED" | "ALREADY_CONFIRMED" | "INVALID_OR_EXPIRED"; message: string };

export class AccountApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}

export function safeReturnPath(value: string | null | undefined, fallback = "/") {
  if (!value || !value.startsWith("/") || value.startsWith("//") || value.includes("\\")) return fallback;
  try {
    const parsed = new URL(value, "https://canadadeals.local");
    return parsed.origin === "https://canadadeals.local" ? `${parsed.pathname}${parsed.search}${parsed.hash}` : fallback;
  } catch {
    return fallback;
  }
}

async function requestToken() {
  const response = await fetch("/api/v1/account/antiforgery", { cache: "no-store", credentials: "same-origin" });
  if (!response.ok) throw new AccountApiError("Security validation could not be started.", response.status);
  const body = await response.json() as { requestToken: string };
  return body.requestToken;
}

async function mutation<T>(path: string, method: "POST" | "PUT" | "DELETE", body?: unknown): Promise<T | null> {
  const token = await requestToken();
  const response = await fetch(path, {
    method,
    credentials: "same-origin",
    headers: {
      "X-CSRF-TOKEN": token,
      ...(body === undefined ? {} : { "Content-Type": "application/json" }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { detail?: string; errors?: Record<string, string[]> } | null;
    const validation = problem?.errors ? Object.values(problem.errors).flat()[0] : null;
    throw new AccountApiError(validation ?? problem?.detail ?? "The request could not be completed.", response.status);
  }
  return response.status === 204 ? null : response.json() as Promise<T>;
}

export async function getSession(): Promise<AccountSession> {
  const response = await fetch("/api/v1/account/me", { cache: "no-store", credentials: "same-origin" });
  if (!response.ok) throw new AccountApiError("Account state could not be loaded.", response.status);
  return response.json() as Promise<AccountSession>;
}

export function register(email: string, password: string) {
  return mutation<AccountMutation>("/api/v1/account/register", "POST", { email, password });
}

export function signIn(email: string, password: string) {
  return mutation<AccountMutation>("/api/v1/account/login", "POST", { email, password });
}

export function confirmEmail(userId: string, code: string) {
  return mutation<EmailConfirmationResult>("/api/v1/account/confirm-email", "POST", { userId, code });
}

export function resendConfirmation(email: string) {
  return mutation<AccountMutation>("/api/v1/account/resend-confirmation", "POST", { email });
}

export function signOut() {
  return mutation<never>("/api/v1/account/logout", "POST");
}

export async function getSavedProducts(): Promise<SavedProduct[]> {
  const response = await fetch("/api/v1/saved-products", { cache: "no-store", credentials: "same-origin" });
  if (!response.ok) throw new AccountApiError("Saved products could not be loaded.", response.status);
  return response.json() as Promise<SavedProduct[]>;
}

export function saveProduct(productId: string) {
  return mutation<{ productId: string; isSaved: boolean }>(`/api/v1/saved-products/${encodeURIComponent(productId)}`, "PUT");
}

export function unsaveProduct(productId: string) {
  return mutation<never>(`/api/v1/saved-products/${encodeURIComponent(productId)}`, "DELETE");
}

export async function getPriceAlerts(): Promise<PriceAlert[]> {
  const response = await fetch("/api/v1/price-alerts", { cache: "no-store", credentials: "same-origin" });
  if (!response.ok) throw new AccountApiError("Price alerts could not be loaded.", response.status);
  return response.json() as Promise<PriceAlert[]>;
}

export function upsertPriceAlert(productId: string, targetPrice: number) {
  return mutation<{ productId: string; targetPrice: number; currency: string; status: string; targetVersion: number; message: string }>(
    `/api/v1/price-alerts/${encodeURIComponent(productId)}`,
    "PUT",
    { targetPrice, consentToEmail: true },
  );
}

export function removePriceAlert(productId: string) {
  return mutation<never>(`/api/v1/price-alerts/${encodeURIComponent(productId)}`, "DELETE");
}
