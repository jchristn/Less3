import { IdRegex } from "#/constants/regex";

describe("IdRegex", () => {
  it("should match valid Id format", () => {
    const validId = "123e4567-e89b-12d3-a456-426614174000";
    expect(IdRegex.test(validId)).toBe(true);
  });

  it("should match Id with uppercase letters", () => {
    const validId = "123E4567-E89B-12D3-A456-426614174000";
    expect(IdRegex.test(validId)).toBe(true);
  });

  it("should not match invalid Id format", () => {
    const invalidId = "not-an-id";
    expect(IdRegex.test(invalidId)).toBe(false);
  });

  it("should not match Id with wrong length", () => {
    const invalidId = "123e4567-e89b-12d3-a456";
    expect(IdRegex.test(invalidId)).toBe(false);
  });

  it("should not match empty string", () => {
    expect(IdRegex.test("")).toBe(false);
  });

  it("should match Id with lowercase letters", () => {
    const validId = "550e8400-e29b-41d4-a716-446655440000";
    expect(IdRegex.test(validId)).toBe(true);
  });
});

