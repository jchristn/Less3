/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: false,
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
