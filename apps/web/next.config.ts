import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  poweredByHeader: false,
  reactStrictMode: true,
  async rewrites() {
    const apiOrigin = process.env.API_ORIGIN ?? process.env.NEXT_PUBLIC_API_ORIGIN ?? "http://localhost:5099";
    return [
      { source: "/api/:path*", destination: `${apiOrigin}/api/:path*` },
      { source: "/go/:path*", destination: `${apiOrigin}/go/:path*` },
    ];
  },
};

export default nextConfig;
