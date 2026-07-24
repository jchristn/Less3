import {
  adminFetch,
  adminRequest,
  BackendApiError,
  buildAdminRequestHeaders,
  buildBackendUrl,
  buildS3Url,
  s3Fetch,
} from "#/services/backendApi.service";
import { buildSignedS3Headers } from "#/utils/s3Auth";

jest.mock("#/services/sdk.service", () => {
  const buildUrl = (baseUrl: string, path: string): string => {
    const cleanBaseUrl = baseUrl.replace(/\/$/, "");
    const cleanPath = path.startsWith("/") ? path.slice(1) : path;
    return cleanPath ? `${cleanBaseUrl}/${cleanPath}` : `${cleanBaseUrl}/`;
  };

  return {
    buildAdminApiHeaders: jest.fn((headers: Record<string, string> = {}, apiKey?: string) => {
      const resolvedApiKey = apiKey ?? "less3admin";
      return resolvedApiKey
        ? {
            ...headers,
            "x-api-key": resolvedApiKey,
          }
        : headers;
    }),
    buildApiUrl: jest.fn((path: string) => buildUrl("http://api.test", path)),
    buildApiUrlFromEndpoint: jest.fn((endpoint: string, path: string) => buildUrl(endpoint, path)),
    getBaseUrl: jest.fn(() => "http://api.test"),
  };
});

jest.mock("#/utils/s3Auth", () => ({
  buildSignedS3Headers: jest.fn(async ({ headers }: { headers: Record<string, string> }) => ({
    ...headers,
    Authorization: "signed-request",
  })),
  selectS3Credential: jest.fn((credentials: unknown[]) => credentials[0] || null),
}));

const createResponse = (
  body: string,
  options: {
    ok?: boolean;
    status?: number;
    statusText?: string;
    contentType?: string;
    url?: string;
  } = {}
): Response =>
  ({
    ok: options.ok ?? true,
    status: options.status ?? 200,
    statusText: options.statusText ?? "OK",
    headers: new Headers(options.contentType ? { "content-type": options.contentType } : {}),
    text: jest.fn(async () => body),
    url: options.url || "http://api.test/request",
  }) as unknown as Response;

describe("backendApi.service", () => {
  const fetchMock = jest.fn();
  const originalFetch = global.fetch;

  beforeEach(() => {
    jest.clearAllMocks();
    fetchMock.mockReset();
    global.fetch = fetchMock as any;
  });

  afterAll(() => {
    global.fetch = originalFetch;
  });

  it("builds backend and S3 URLs through the shared helpers", () => {
    expect(buildBackendUrl("admin/users")).toBe("http://api.test/admin/users");
    expect(buildBackendUrl("/api/v1/tenants", "http://custom.test/")).toBe("http://custom.test/api/v1/tenants");
    expect(buildBackendUrl("http://already.test/path")).toBe("http://already.test/path");
    expect(buildS3Url("/bucket/key")).toBe("http://api.test/bucket/key");
  });

  it("builds admin headers through a single exported helper", () => {
    expect(buildAdminRequestHeaders({ Accept: "application/json", Empty: undefined }, "override-key")).toEqual({
      Accept: "application/json",
      "x-api-key": "override-key",
    });
  });

  it("serializes JSON admin request bodies and applies admin headers", async () => {
    const response = createResponse("{}", { contentType: "application/json" });
    fetchMock.mockResolvedValueOnce(response);

    const result = await adminFetch("admin/users", {
      method: "POST",
      headers: { Accept: "application/json" },
      body: { Name: "Operator" },
      apiKey: "override-key",
      cache: "no-store",
    });

    expect(result).toBe(response);
    expect(fetchMock).toHaveBeenCalledWith("http://api.test/admin/users", {
      method: "POST",
      headers: {
        Accept: "application/json",
        "x-api-key": "override-key",
        "Content-Type": "application/json",
      },
      body: "{\"Name\":\"Operator\"}",
      cache: "no-store",
      signal: undefined,
    });
  });

  it("parses successful admin JSON responses", async () => {
    fetchMock.mockResolvedValueOnce(createResponse("{\"ID\":\"usr_test\"}", { contentType: "application/json" }));

    await expect(adminRequest<{ ID: string }>("admin/users/usr_test")).resolves.toEqual({
      ID: "usr_test",
    });
  });

  it("throws a normalized API error for failed backend responses", async () => {
    fetchMock.mockResolvedValueOnce(
      createResponse("<Error><Code>InvalidRequest</Code><Message>Request is invalid.</Message></Error>", {
        ok: false,
        status: 400,
        statusText: "Bad Request",
      })
    );

    await expect(adminRequest("admin/stats")).rejects.toEqual(
      expect.objectContaining<Partial<BackendApiError>>({
        name: "BackendApiError",
        status: 400,
        data: "Backend request failed: InvalidRequest - Request is invalid.",
      })
    );
  });

  it("resolves credentials and signs S3 requests through the shared S3 transport", async () => {
    fetchMock
      .mockResolvedValueOnce(
        createResponse(
          JSON.stringify([
            {
              Id: "crd_default",
              AccessKey: "default",
              SecretKey: "secret",
            },
          ]),
          { contentType: "application/json" }
        )
      )
      .mockResolvedValueOnce(createResponse("", { statusText: "No Content" }));

    await s3Fetch("/bucket/key", {
      method: "GET",
      cache: "no-store",
    });

    expect(fetchMock.mock.calls[0][0]).toBe("http://api.test/admin/credentials");
    expect(fetchMock.mock.calls[1]).toEqual([
      "http://api.test/bucket/key",
      {
        method: "GET",
        headers: {
          "x-api-key": "less3admin",
          Authorization: "signed-request",
        },
        body: undefined,
        cache: "no-store",
        signal: undefined,
      },
    ]);
    expect(buildSignedS3Headers).toHaveBeenCalledWith(
      expect.objectContaining({
        method: "GET",
        url: "http://api.test/bucket/key",
        accessKey: "default",
        secretKey: "secret",
      })
    );
  });
});
