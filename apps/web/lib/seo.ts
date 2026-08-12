const localOrigin = "http://localhost:3000";

export function siteOrigin() {
  const configured = process.env.SITE_URL ?? process.env.APP_URL ?? localOrigin;
  try {
    const url = new URL(configured);
    if (url.protocol !== "http:" && url.protocol !== "https:") return localOrigin;
    return url.origin;
  } catch {
    return localOrigin;
  }
}

export function absoluteUrl(path = "/") {
  return new URL(path, `${siteOrigin()}/`).toString();
}
