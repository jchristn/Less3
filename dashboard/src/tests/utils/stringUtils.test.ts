import { toTitleCase, getFirstLetterOfTheWord, uuid, decodePayload } from "#/utils/stringUtils";
import { message } from "antd";

jest.mock("antd", () => ({
  message: {
    error: jest.fn(),
  },
}));

describe("stringUtils", () => {
  describe("toTitleCase", () => {
    it("should convert lowercase string to title case", () => {
      expect(toTitleCase("hello world")).toBe("Hello World");
    });

    it("should handle string with hyphens", () => {
      expect(toTitleCase("hello-world")).toBe("Hello World");
    });

    it("should handle mixed case string", () => {
      expect(toTitleCase("hELLo WoRLd")).toBe("Hello World");
    });

    it("should handle single word", () => {
      expect(toTitleCase("hello")).toBe("Hello");
    });

    it("should handle empty string", () => {
      expect(toTitleCase("")).toBe("");
    });

    it("should handle string with multiple hyphens", () => {
      expect(toTitleCase("hello-world-test")).toBe("Hello World Test");
    });

    it("should handle already title case string", () => {
      expect(toTitleCase("Hello World")).toBe("Hello World");
    });
  });

  describe("getFirstLetterOfTheWord", () => {
    it("should get first letter in uppercase", () => {
      expect(getFirstLetterOfTheWord("hello")).toBe("H");
    });

    it("should handle uppercase string", () => {
      expect(getFirstLetterOfTheWord("HELLO")).toBe("H");
    });

    it("should handle single character", () => {
      expect(getFirstLetterOfTheWord("a")).toBe("A");
    });

    it("should handle empty string", () => {
      expect(getFirstLetterOfTheWord("")).toBe("");
    });

    it("should handle undefined", () => {
      expect(getFirstLetterOfTheWord(undefined as any)).toBe("");
    });

    it("should handle null", () => {
      expect(getFirstLetterOfTheWord(null as any)).toBe("");
    });
  });

  describe("uuid", () => {
    it("should generate a UUID", () => {
      const id = uuid();
      expect(id).toBeDefined();
      expect(typeof id).toBe("string");
      expect(id.length).toBeGreaterThan(0);
    });

    it("should generate unique UUIDs", () => {
      const id1 = uuid();
      const id2 = uuid();
      expect(id1).not.toBe(id2);
    });

    it("should generate valid UUID format", () => {
      const id = uuid();
      const uuidRegex =
        /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
      expect(id).toMatch(uuidRegex);
    });
  });

  describe("decodePayload", () => {
    beforeEach(() => {
      jest.clearAllMocks();
    });

    it("should decode valid base64 JSON payload", () => {
      const payload = { name: "test", value: 123 };
      const encoded = btoa(JSON.stringify(payload));
      const result = decodePayload(encoded);
      expect(result).toEqual(payload);
    });

    it("should return original payload on decode error", () => {
      const invalidPayload = "invalid-base64!@#";
      const result = decodePayload(invalidPayload);
      expect(result).toBe(invalidPayload);
      expect(message.error).toHaveBeenCalledWith("Failed to decode payload.");
    });

    it("should return original payload on JSON parse error", () => {
      const invalidJson = btoa("not valid json");
      const result = decodePayload(invalidJson);
      expect(result).toBe(invalidJson);
      expect(message.error).toHaveBeenCalledWith("Failed to decode payload.");
    });

    it("should handle empty string", () => {
      const result = decodePayload("");
      expect(result).toBe("");
    });

    it("should decode complex nested object", () => {
      const payload = {
        user: { id: 1, name: "John" },
        items: [1, 2, 3],
      };
      const encoded = btoa(JSON.stringify(payload));
      const result = decodePayload(encoded);
      expect(result).toEqual(payload);
    });
  });
});

