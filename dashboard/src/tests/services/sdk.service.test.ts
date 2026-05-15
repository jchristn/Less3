import { getApiEndpoint, getInitialApiEndpoint, updateSdkEndPoint, buildApiUrl } from "#/services/sdk.service";
import { apiEndpointURL } from "#/constants/config";

describe("sdk.service", () => {
  beforeEach(() => {
    localStorage.clear();
    // Reset to default endpoint before each test
    updateSdkEndPoint(apiEndpointURL);
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
