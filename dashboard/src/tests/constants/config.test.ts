import { apiEndpointURL, MIN_PASSWORD_LENGTH, keepUnusedDataFor } from "#/constants/config";

describe("config", () => {
  it("should have apiEndpointURL", () => {
    expect(apiEndpointURL).toBeDefined();
    expect(typeof apiEndpointURL).toBe("string");
  });

  it("should have MIN_PASSWORD_LENGTH", () => {
    expect(MIN_PASSWORD_LENGTH).toBe(8);
  });

  it("should have keepUnusedDataFor", () => {
    expect(keepUnusedDataFor).toBe(300);
  });
});
