import { adminRequest, toRtkQueryError } from "#/services/backendApi.service";
import { dynamicBaseQuery } from "#/store/rtk/rtkSdkInstance";

jest.mock("#/services/backendApi.service", () => ({
  adminRequest: jest.fn(),
  toRtkQueryError: jest.fn((error: unknown, fallbackMessage: string) => ({
    status: "FETCH_ERROR",
    data: error instanceof Error ? error.message : fallbackMessage,
  })),
}));

describe("dynamicBaseQuery", () => {
  beforeEach(() => {
    jest.resetAllMocks();
    (adminRequest as jest.Mock).mockResolvedValue({});
  });

  it("delegates backend requests to the shared admin request helper", async () => {
    await dynamicBaseQuery(
      {
        url: "/test",
        method: "POST",
        body: { Name: "Example" },
        headers: { Accept: "application/json" },
        cache: "no-store",
      },
      {} as any,
      {} as any
    );

    expect(adminRequest).toHaveBeenCalledWith("/test", {
      method: "POST",
      headers: { Accept: "application/json" },
      body: { Name: "Example" },
      cache: "no-store",
    });
  });

  it("returns data from the shared admin request helper", async () => {
    (adminRequest as jest.Mock).mockResolvedValueOnce({ ID: "bkt_example" });

    const result = await dynamicBaseQuery({ url: "/admin/buckets", method: "GET" }, {} as any, {} as any);

    expect(result).toEqual({
      data: { ID: "bkt_example" },
    });
  });

  it("normalizes failures through the shared RTK error mapper", async () => {
    const error = new Error("Boom");
    (adminRequest as jest.Mock).mockRejectedValueOnce(error);
    (toRtkQueryError as jest.Mock).mockReturnValueOnce({
      status: 500,
      data: "Boom",
    });

    const result = await dynamicBaseQuery({ url: "/admin/buckets", method: "GET" }, {} as any, {} as any);

    expect(toRtkQueryError).toHaveBeenCalledWith(error, "Backend request failed");
    expect(result).toEqual({
      error: {
        status: 500,
        data: "Boom",
      },
    });
  });

  describe("Snapshots", () => {
    it("captures the shared-helper call contract", async () => {
      await dynamicBaseQuery(
        {
          url: "/snapshot-test",
          method: "PUT",
          body: { enabled: true },
          headers: { "X-Test": "value" },
        },
        {} as any,
        {} as any
      );

      expect((adminRequest as jest.Mock).mock.calls[0]).toMatchSnapshot();
    });
  });
});
