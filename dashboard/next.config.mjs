/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: false,
  // The installers (src/Less3.Publisher) bundle the dashboard with its own Node.js runtime: LESS3_DASHBOARD_STANDALONE=1
  // builds the minimal self-contained server in .next/standalone. Docker and `npm run start` use the normal build.
  ...(process.env.LESS3_DASHBOARD_STANDALONE === '1' ? { output: 'standalone', outputFileTracingRoot: import.meta.dirname, images: { unoptimized: true } } : {}),
  env: {
    NEXT_PUBLIC_LESS3_SERVER_URL:
      process.env.NEXT_PUBLIC_LESS3_SERVER_URL || process.env.LESS3_SERVER_URL || 'http://127.0.0.1:8000',
    NEXT_PUBLIC_LESS3_GRAFANA_URL:
      process.env.NEXT_PUBLIC_LESS3_GRAFANA_URL || process.env.LESS3_GRAFANA_URL || 'http://localhost:3001',
    NEXT_PUBLIC_LESS3_PROMETHEUS_URL:
      process.env.NEXT_PUBLIC_LESS3_PROMETHEUS_URL || process.env.LESS3_PROMETHEUS_URL || 'http://localhost:9090',
    NEXT_PUBLIC_LESS3_CLUTCH_UI_URL:
      process.env.NEXT_PUBLIC_LESS3_CLUTCH_UI_URL || process.env.LESS3_CLUTCH_UI_URL || 'http://localhost:3002',
    NEXT_PUBLIC_LESS3_CLUTCH_API_URL:
      process.env.NEXT_PUBLIC_LESS3_CLUTCH_API_URL || process.env.LESS3_CLUTCH_API_URL || 'http://localhost:8080',
  },
  // eslint: {
  //   ignoreDuringBuilds: true,
  // },
  // typescript: {
  //   ignoreBuildErrors: true,
  // },
};

export default nextConfig;
