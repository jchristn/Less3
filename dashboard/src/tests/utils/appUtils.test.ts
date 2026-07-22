import { getDashboardPathKey, transformToOptions } from "#/utils/appUtils";

describe("appUtils", () => {
  describe("getDashboardPathKey", () => {
    it("returns dashboard for Id paths", () => {
      const id = "123e4567-e89b-12d3-a456-426614174000";
      const result = getDashboardPathKey(`/dashboard/${id}`);

      expect(result).toEqual({ pathKey: "dashboard", patentPathKey: "" });
    });

    it("returns last and parent segments for regular paths", () => {
      const result = getDashboardPathKey("/dashboard/settings/profile");

      expect(result).toEqual({ pathKey: "profile", patentPathKey: "settings" });
    });
  });

  describe("transformToOptions", () => {
    it("maps Id and Name to option values and labels", () => {
      const options = transformToOptions([
        { Id: "1", name: "First" },
        { Name: "SecondName" },
      ]);

      expect(options).toEqual([
        { value: "1", label: "First" },
        { value: "SecondName", label: "SecondName" },
      ]);
    });

    it("falls back to provided label field when Id is missing", () => {
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
