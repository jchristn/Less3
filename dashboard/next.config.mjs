/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: false,
  env: {
    NEXT_PUBLIC_LESS3_SERVER_URL:
      process.env.NEXT_PUBLIC_LESS3_SERVER_URL || process.env.LESS3_SERVER_URL || 'http://127.0.0.1:8000',
  },
  // eslint: {
  //   ignoreDuringBuilds: true,
  // },
  // typescript: {
  //   ignoreBuildErrors: true,
  // },
};

export default nextConfig;
