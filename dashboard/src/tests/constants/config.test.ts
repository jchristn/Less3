import { apiEndpointURL, MIN_PASSWORD_LENGTH, keepUnusedDataFor, API_KEY } from "#/constants/config";

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

  it("should have API_KEY", () => {
    expect(API_KEY).toBe("less3admin");
  });
});

