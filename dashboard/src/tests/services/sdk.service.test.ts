import { getApiEndpoint, updateSdkEndPoint, buildApiUrl } from "#/services/sdk.service";
import { apiEndpointURL } from "#/constants/config";

describe("sdk.service", () => {
  beforeEach(() => {
    // Reset to default endpoint before each test
    updateSdkEndPoint(apiEndpointURL);
  });

  describe("getApiEndpoint", () => {
    it("should return current API endpoint", () => {
      const endpoint = getApiEndpoint();
      expect(endpoint).toBeDefined();
      expect(typeof endpoint).toBe("string");
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

