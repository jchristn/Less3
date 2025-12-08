import moment from "moment";
import {
  formatDate,
  formatDateTime,
  formatSecondsForTimer,
} from "#/utils/dateUtils";

describe("dateUtils", () => {
  describe("formatDateTime", () => {
    it("returns formatted string when a valid date is provided", () => {
      const formatted = formatDateTime("2024-01-02T03:04:05Z", "YYYY/MM/DD");
      expect(formatted).toBe("2024/01/02");
    });

    it("returns 'Invalid Date' for falsy input", () => {
      expect(formatDateTime("")).toBe("Invalid Date");
    });

    it("falls back to 'Invalid Date' when moment throws", () => {
      const spy = jest.spyOn(moment.prototype, "format").mockImplementation(() => {
        throw new Error("boom");
      });

      expect(formatDateTime("2024-01-02T03:04:05Z")).toBe("Invalid Date");

      spy.mockRestore();
    });
  });

  describe("formatDate", () => {
    it("returns '-' when no date string is provided", () => {
      expect(formatDate("")).toBe("-");
    });

    it("returns formatted date string using toLocaleString", () => {
      const localeSpy = jest
        .spyOn(Date.prototype, "toLocaleString")
        .mockReturnValue("01/02/2024, 12:34");

      expect(formatDate("2024-01-02T12:34:00Z")).toBe("01/02/2024, 12:34");

      localeSpy.mockRestore();
    });

    it("gracefully handles errors thrown by Date formatting", () => {
      const OriginalDate = Date;
      // Force constructor to throw to exercise catch branch
      // @ts-expect-error - deliberately overriding Date for the test
      global.Date = class extends OriginalDate {
        constructor() {
          throw new Error("bad date");
        }
      };

      expect(formatDate("2024-01-02T12:34:00Z")).toBe("-");

      global.Date = OriginalDate;
    });
  });

  describe("formatSecondsForTimer", () => {
    it("formats seconds into mm:ss", () => {
      expect(formatSecondsForTimer(65)).toBe("01:05");
      expect(formatSecondsForTimer(9)).toBe("00:09");
    });
  });
});
