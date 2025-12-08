import { getDashboardPathKey, transformToOptions } from "#/utils/appUtils";

describe("appUtils", () => {
  describe("getDashboardPathKey", () => {
    it("returns dashboard for GUID paths", () => {
      const guid = "123e4567-e89b-12d3-a456-426614174000";
      const result = getDashboardPathKey(`/dashboard/${guid}`);

      expect(result).toEqual({ pathKey: "dashboard", patentPathKey: "" });
    });

    it("returns last and parent segments for regular paths", () => {
      const result = getDashboardPathKey("/dashboard/settings/profile");

      expect(result).toEqual({ pathKey: "profile", patentPathKey: "settings" });
    });
  });

  describe("transformToOptions", () => {
    it("maps GUID and Name to option values and labels", () => {
      const options = transformToOptions([
        { GUID: "1", name: "First" },
        { Name: "SecondName" },
      ]);

      expect(options).toEqual([
        { value: "1", label: "First" },
        { value: "SecondName", label: "SecondName" },
      ]);
    });

    it("falls back to provided label field when GUID is missing", () => {
      const options = transformToOptions(
        [
          { name: "Alpha" },
          { name: "Beta" },
        ],
        "name"
      );

      expect(options).toEqual([
        { value: "Alpha", label: "Alpha" },
        { value: "Beta", label: "Beta" },
      ]);
    });

    it("handles null/undefined data by returning empty array", () => {
      expect(transformToOptions(null as any)).toEqual([]);
      expect(transformToOptions(undefined as any)).toEqual([]);
    });
  });
});
