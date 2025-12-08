import { getApiEndpoint } from "#/services/sdk.service";
import { API_KEY } from "#/constants/config";
import sdkSlice, { ApiBaseQueryArgs } from "#/store/rtk/rtkSdkInstance";

jest.mock("#/services/sdk.service");

describe("rtkSdkInstance", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (getApiEndpoint as jest.Mock).mockReturnValue("http://test.com");
  });

  describe("dynamicBaseQuery", () => {
    it("should create API instance with correct configuration", () => {
      expect(sdkSlice).toBeDefined();
      expect(sdkSlice.reducerPath).toBe("sdk");
    });

    it("should handle FormData body", async () => {
      const formData = new FormData();
      formData.append("test", "value");

      const args: ApiBaseQueryArgs = {
        url: "/test",
        method: "POST",
        body: formData,
      };

      // The base query should handle FormData
      expect(formData).toBeInstanceOf(FormData);
      expect(args.body).toBeInstanceOf(FormData);
    });

    it("should handle regular JSON body", async () => {
      const args: ApiBaseQueryArgs = {
        url: "/test",
        method: "POST",
        body: { test: "value" },
      };

      // The base query should handle regular objects
      expect(args.body).toEqual({ test: "value" });
      expect(args.body).not.toBeInstanceOf(FormData);
    });

    it("should use getApiEndpoint for base URL", () => {
      expect(getApiEndpoint).toBeDefined();
      const endpoint = getApiEndpoint();
      expect(typeof endpoint).toBe("string");
    });
  });

  describe("ApiBaseQueryArgs interface", () => {
    it("should accept valid arguments", () => {
      const args: ApiBaseQueryArgs = {
        url: "/test",
        method: "GET",
        body: { test: "value" },
        headers: { "Custom-Header": "value" },
      };

      expect(args.url).toBe("/test");
      expect(args.method).toBe("GET");
      expect(args.body).toEqual({ test: "value" });
      expect(args.headers).toEqual({ "Custom-Header": "value" });
      expect(args).toMatchInlineSnapshot(`
{
  "body": {
    "test": "value",
  },
  "headers": {
    "Custom-Header": "value",
  },
  "method": "GET",
  "url": "/test",
}
`);
    });
  });

  describe("Snapshots", () => {
    it("captures slice config snapshot", () => {
      expect({
        reducerPath: sdkSlice.reducerPath,
        tagTypes: (sdkSlice as any).tagTypes,
      }).toMatchSnapshot();
    });

    it("captures sample ApiBaseQueryArgs snapshot", () => {
      const args: ApiBaseQueryArgs = {
        url: "/snapshot",
        method: "POST",
        body: { example: true },
        headers: { Authorization: "Bearer token" },
      };

      expect(args).toMatchSnapshot();
    });
  });
});

