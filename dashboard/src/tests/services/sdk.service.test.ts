import {
  buildAdminApiHeaders,
  buildApiUrl,
  clearDashboardSession,
  getAdminApiKey,
  getApiEndpoint,
  getInitialAdminApiKey,
  getInitialApiEndpoint,
  persistDashboardSession,
  updateAdminApiKey,
  updateSdkEndPoint,
} from "#/services/sdk.service";
import { apiEndpointURL } from "#/constants/config";
import { localStorageKeys } from "#/constants/constant";

describe("sdk.service", () => {
  const originalLocation = window.location;

  beforeEach(() => {
    localStorage.clear();
    clearDashboardSession();
  });

  afterEach(() => {
    Object.defineProperty(window, "location", {
      configurable: true,
      value: originalLocation,
    });
  });

  describe("getApiEndpoint", () => {
    it("should return current API endpoint", () => {
      const endpoint = getApiEndpoint();
      expect(endpoint).toBeDefined();
      expect(typeof endpoint).toBe("string");
    });

    it("should prefer saved API endpoint from localStorage in the browser", () => {
      localStorage.setItem("less3APIUrl", "http://saved-endpoint.com");
      expect(getApiEndpoint()).toBe("http://saved-endpoint.com/");
    });

    it("should ignore the legacy dashboard URL saved in localStorage", () => {
      localStorage.setItem("less3APIUrl", "http://localhost:3000");
      const normalizedDefaultEndpoint = apiEndpointURL.endsWith("/") ? apiEndpointURL : `${apiEndpointURL}/`;

      expect(getInitialApiEndpoint()).toBe(apiEndpointURL);
      expect(localStorage.getItem("less3APIUrl")).toBeNull();
      expect(getApiEndpoint()).toBe(normalizedDefaultEndpoint);
    });

    it("should derive the default API host from the current browser hostname", () => {
      Object.defineProperty(window, "location", {
        configurable: true,
        value: {
          ...originalLocation,
          hostname: "public.less3.example",
        },
      });

      expect(getInitialApiEndpoint()).toBe("http://public.less3.example:8000");
      expect(getApiEndpoint()).toBe("http://public.less3.example:8000/");
    });
  });

  describe("admin api key helpers", () => {
    it("should return saved admin API key", () => {
      localStorage.setItem(localStorageKeys.adminApiKey, "secret-key");
      expect(getInitialAdminApiKey()).toBe("secret-key");
      expect(getAdminApiKey()).toBe("secret-key");
    });

    it("should store session values together", () => {
      persistDashboardSession("http://saved-endpoint.com", "secret-key");

      expect(localStorage.getItem(localStorageKeys.less3APIUrl)).toBe("http://saved-endpoint.com");
      expect(localStorage.getItem(localStorageKeys.adminApiKey)).toBe("secret-key");
      expect(getApiEndpoint()).toBe("http://saved-endpoint.com/");
      expect(getAdminApiKey()).toBe("secret-key");
    });

    it("should clear session values", () => {
      persistDashboardSession("http://saved-endpoint.com", "secret-key");
      clearDashboardSession();

      expect(localStorage.getItem(localStorageKeys.less3APIUrl)).toBeNull();
      expect(localStorage.getItem(localStorageKeys.adminApiKey)).toBeNull();
      expect(getApiEndpoint()).toBe(apiEndpointURL.endsWith("/") ? apiEndpointURL : `${apiEndpointURL}/`);
      expect(getAdminApiKey()).toBe("");
    });

    it("should build admin headers from the saved key", () => {
      updateAdminApiKey("secret-key");
      expect(buildAdminApiHeaders({ Accept: "application/json" })).toEqual({
        Accept: "application/json",
        "x-api-key": "secret-key",
      });
    });

    it("should allow overriding the saved admin key", () => {
      updateAdminApiKey("secret-key");
      expect(buildAdminApiHeaders({}, "override-key")).toEqual({
        "x-api-key": "override-key",
      });
    });
  });

  describe("updateSdkEndPoint", () => {
    it("should update API endpoint", () => {
      const newEndpoint = "http://example.com";
      updateSdkEndPoint(newEndpoint);
      expect(getApiEndpoint()).toBe("http://example.com/");
    });

    it("should add trailing slash if missing", () => {
      updateSdkEndPoint("http://example.com");
      expect(getApiEndpoint()).toBe("http://example.com/");
    });

    it("should not add duplicate trailing slash", () => {
      updateSdkEndPoint("http://example.com/");
      expect(getApiEndpoint()).toBe("http://example.com/");
    });
  });

  describe("buildApiUrl", () => {
    beforeEach(() => {
      updateSdkEndPoint("http://example.com/");
    });

    it("should build URL with path", () => {
      const url = buildApiUrl("test/path");
      expect(url).toBe("http://example.com/test/path");
    });

    it("should remove leading slash from path", () => {
      const url = buildApiUrl("/test/path");
      expect(url).toBe("http://example.com/test/path");
    });

    it("should handle empty path", () => {
      const url = buildApiUrl("");
      expect(url).toBe("http://example.com/");
    });

    it("should handle nested paths", () => {
      const url = buildApiUrl("admin/users/123");
      expect(url).toBe("http://example.com/admin/users/123");
    });
  });
});
