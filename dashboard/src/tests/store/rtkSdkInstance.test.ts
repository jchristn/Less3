import { dynamicBaseQuery } from "#/store/rtk/rtkSdkInstance";
import { getApiEndpoint } from "#/services/sdk.service";

jest.mock("#/services/sdk.service", () => ({
  getApiEndpoint: jest.fn(),
}));

describe("dynamicBaseQuery", () => {
  const baseUrl = "http://api.test";
  const fetchMock = jest.fn();
  const originalFetch = global.fetch;

  const createResponse = () => {
    const res: any = {
      ok: true,
      json: async () => ({}),
      text: async () => "",
      headers: new Headers({ "content-type": "application/json" }),
      status: 200,
      statusText: "OK",
    };
    res.clone = () => res;
    return res;
  };

  beforeEach(() => {
    jest.resetAllMocks();
    (getApiEndpoint as jest.Mock).mockReturnValue(baseUrl);
    fetchMock.mockResolvedValue(createResponse());
    global.fetch = fetchMock as any;
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it("sets JSON headers and uses dynamic base URL", async () => {
    await dynamicBaseQuery({ url: "/test", method: "GET" }, {} as any, {} as any);

    expect(getApiEndpoint).toHaveBeenCalled();
    expect(fetchMock).toHaveBeenCalled();
    const request = fetchMock.mock.calls[0][0] as Request;
    const headerSnapshot = Object.fromEntries(
      Array.from(request.headers.entries()).sort(([a], [b]) => a.localeCompare(b))
    );

    expect({
      url: request.url,
      method: request.method,
      headers: headerSnapshot,
    }).toMatchInlineSnapshot(`
{
  "headers": {
    "accept": "application/json",
    "content-type": "application/json",
    "x-api-key": "less3admin",
  },
  "method": "GET",
  "url": "http://api.test/test",
}
`);
  });

  it("omits Content-Type when body is FormData", async () => {
    const formData = new FormData();
    formData.append("file", new Blob(["test"]), "file.txt");

    await dynamicBaseQuery({ url: "/upload", method: "POST", body: formData }, {} as any, {} as any);

    const request = fetchMock.mock.calls[0][0] as Request;
    const headers = Object.fromEntries(
      Array.from(request.headers.entries()).map(([key, value]) => [
        key,
        key === "content-type" ? "<form-data>" : value,
      ])
    );

    expect({
      url: request.url,
      method: request.method,
      hasBody: !!request.body,
      headers,
    }).toMatchInlineSnapshot(`
{
  "hasBody": true,
  "headers": {
    "accept": "application/json",
    "content-type": "<form-data>",
    "x-api-key": "less3admin",
  },
  "method": "POST",
  "url": "http://api.test/upload",
}
`);
  });

  describe("Snapshots", () => {
    it("captures request snapshot for JSON call", async () => {
      await dynamicBaseQuery({ url: "/snapshot-test", method: "GET" }, {} as any, {} as any);
      const request = fetchMock.mock.calls[0][0] as Request;
      const headers = Object.fromEntries(request.headers.entries());
      expect({
        url: request.url,
        method: request.method,
        headers,
      }).toMatchSnapshot();
    });

    it("captures request snapshot for FormData call", async () => {
      const formData = new FormData();
      formData.append("file", new Blob(["snapshot"]), "file.txt");

      await dynamicBaseQuery({ url: "/snapshot-upload", method: "POST", body: formData }, {} as any, {} as any);
      const request = fetchMock.mock.calls[0][0] as Request;
      const headers = Object.fromEntries(
        Array.from(request.headers.entries()).map(([key, value]) => [
          key,
          key === "content-type" ? "<form-data>" : value,
        ])
      );
      expect({
        url: request.url,
        method: request.method,
        hasBody: !!request.body,
        headers,
      }).toMatchSnapshot();
    });
  });
});

