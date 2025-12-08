import { GUIDRegex } from "#/constants/regex";

describe("GUIDRegex", () => {
  it("should match valid GUID format", () => {
    const validGuid = "123e4567-e89b-12d3-a456-426614174000";
    expect(GUIDRegex.test(validGuid)).toBe(true);
  });

  it("should match GUID with uppercase letters", () => {
    const validGuid = "123E4567-E89B-12D3-A456-426614174000";
    expect(GUIDRegex.test(validGuid)).toBe(true);
  });

  it("should not match invalid GUID format", () => {
    const invalidGuid = "invalid-guid";
    expect(GUIDRegex.test(invalidGuid)).toBe(false);
  });

  it("should not match GUID with wrong length", () => {
    const invalidGuid = "123e4567-e89b-12d3-a456";
    expect(GUIDRegex.test(invalidGuid)).toBe(false);
  });

  it("should not match empty string", () => {
    expect(GUIDRegex.test("")).toBe(false);
  });

  it("should match GUID with lowercase letters", () => {
    const validGuid = "550e8400-e29b-41d4-a716-446655440000";
    expect(GUIDRegex.test(validGuid)).toBe(true);
  });
});

