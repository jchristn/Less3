const nextJest = require("next/jest");

const createJestConfig = nextJest({
  // Provide the path to your Next.js app to load next.config.js and .env files in your test environment
  dir: "./",
});

// Add any custom config to be passed to Jest
const config = {
  coverageProvider: "v8",
  testEnvironment: "./jest.environment.js",
  // Add more setup options before each test is run
  // setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
  moduleNameMapper: {
    "^#/(.*)$": "<rootDir>/src/$1",
    "^antd/es/(.*)$": "<rootDir>/node_modules/antd/lib/$1",
    uuid: require.resolve("uuid"),
  },
  setupFilesAfterEnv: ["<rootDir>/jest.setup.js"],
  testEnvironmentOptions: {
    customExportConditions: [""],
  },
  collectCoverageFrom: [
    "src/**/*.{ts,tsx,js,jsx}",
    "!src/**/*.d.ts",
    "!**/mocks/**",
    "!**/lib/**",
    "!**/data/**",
    "!**/tests/**",
  ],
};

// createJestConfig is exported this way to ensure that next/jest can load the Next.js config which is async
module.exports = createJestConfig(config);
