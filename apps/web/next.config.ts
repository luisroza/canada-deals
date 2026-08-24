import type { NextConfig } from "next";

const apiOrigin = process.env.API_ORIGIN ?? process.env.API_BASE_URL ?? "http://localhost:5099";

export function parseAllowedDevOrigins(value = process.env.DEV_ALLOWED_ORIGINS) {
  return [...new Set((value ?? "")
    .split(",")
    .map((origin) => origin.trim())
    .filter(Boolean))];
}

export function buildContentSecurityPolicy(environment = process.env.NODE_ENV) {
  const scriptPolicy = environment === "development"
    ? "script-src 'self' 'unsafe-inline' 'unsafe-eval'"
    : "script-src 'self' 'unsafe-inline'";
  const httpsUpgrade = environment === "production" ? "; upgrade-insecure-requests" : "";

  return `default-src 'self'; ${scriptPolicy}; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'; font-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'${httpsUpgrade}`;
}

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  reactStrictMode: true,
  allowedDevOrigins: parseAllowedDevOrigins(),
  async rewrites() {
    return [
      { source: "/api/:path*", destination: `${apiOrigin}/api/:path*` },
      { source: "/go/:path*", destination: `${apiOrigin}/go/:path*` },
    ];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "Content-Security-Policy", value: buildContentSecurityPolicy() },
          { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "X-Frame-Options", value: "DENY" },
        ],
      },
    ];
  },
};

export default nextConfig;
