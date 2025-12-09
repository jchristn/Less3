import { localStorageKeys, paths, dynamicSlugs } from "#/constants/constant";

describe("constants", () => {
  describe("localStorageKeys", () => {
    it("should have less3APIUrl key", () => {
      expect(localStorageKeys.less3APIUrl).toBe("less3APIUrl");
    });

    it("should have theme key", () => {
      expect(localStorageKeys.theme).toBe("theme");
    });
  });

  describe("paths", () => {
    it("should have login path", () => {
      expect(paths.login).toBe("/");
    });

    it("should have dashboard path", () => {
      expect(paths.dashboard).toBe("/dashboard");
    });

    it("should have buckets path", () => {
      expect(paths.buckets).toBe("/admin/buckets");
    });

    it("should have objects path", () => {
      expect(paths.objects).toBe("/admin/objects");
    });

    it("should have users path", () => {
      expect(paths.users).toBe("/admin/users");
    });

    it("should have credentials path", () => {
      expect(paths.credentials).toBe("/admin/credentials");
    });
  });

  describe("dynamicSlugs", () => {
    it("should have accountId slug", () => {
      expect(dynamicSlugs.accountId).toBe(":accountId");
    });
  });
});

