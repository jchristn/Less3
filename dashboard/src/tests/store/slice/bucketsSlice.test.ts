/**
 * Buckets slice coverage: exercise several endpoints with mocked fetch.
 */
import { configureStore } from "@reduxjs/toolkit";
import { localStorageKeys } from "#/constants/constant";
import { bucketsSliceApi } from "#/store/slice/bucketsSlice";

/**
 * Integration-style tests using a real RTK Query store so queryFns execute.
 */
describe("bucketsSlice endpoints", () => {
  const originalFetch = global.fetch;

  const makeStore = () =>
    configureStore({
      reducer: {
        [bucketsSliceApi.reducerPath]: bucketsSliceApi.reducer,
      },
      middleware: (gDM) => gDM().concat(bucketsSliceApi.middleware),
    });

  beforeEach(() => {
    localStorage.setItem(localStorageKeys.adminApiKey, "less3admin");
  });

  afterEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    global.fetch = originalFetch as any;
  });

  it("getBuckets success populates data", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      statusText: "OK",
      json: async () => [{ Id: "bkt_test", Name: "one", CreatedUtc: "now" }],
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(bucketsSliceApi.endpoints.getBuckets.initiate());
    const result = await promise.unwrap();
    expect(result).toEqual([{ Id: "bkt_test", Name: "one", CreatedUtc: "now", CreationDate: "now" }]);
    promise.unsubscribe?.();
  });

  it("deleteBucket uses admin bucket id", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 204,
      statusText: "No Content",
      text: async () => "",
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.deleteBucket.initiate({ id: "bkt_test", bucketName: "one" })
    );
    const result = await promise.unwrap();
    expect(result).toEqual({ success: true });
    expect(global.fetch).toHaveBeenCalledWith(
      "http://localhost:8000/admin/buckets/bkt_test?destroy=true",
      expect.objectContaining({
        method: "DELETE",
        headers: expect.objectContaining({
          "x-api-key": "less3admin",
        }),
      })
    );
    promise.unsubscribe?.();
  });

  it("createBucket error response is surfaced", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 500,
      statusText: "Bad",
      text: async () => "",
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.createBucket.initiate({ Name: "err" } as any)
    );
    await expect(promise.unwrap()).rejects.toBeDefined();
    promise.unsubscribe?.();
  });

  it("listBucketObjects returns contents", async () => {
    global.fetch = jest
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: "OK",
        json: async () => [{ Id: "crd_test", AccessKey: "default", SecretKey: "default" }],
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: "OK",
        text: async () =>
          `<ListBucketResult><Contents><Key>file.txt</Key><Size>1</Size><LastModified>now</LastModified><ContentType>text/plain</ContentType></Contents></ListBucketResult>`,
      }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.listBucketObjects.initiate({
        bucketId: "g",
        prefix: "",
        continuationToken: "",
      } as any)
    );
    const res = await promise.unwrap();
    expect(res).toHaveProperty("Contents");
    expect(global.fetch).toHaveBeenNthCalledWith(
      1,
      "http://localhost:8000/admin/credentials",
      expect.objectContaining({
        method: "GET",
        headers: expect.objectContaining({
          "x-api-key": "less3admin",
        }),
      })
    );
    expect(global.fetch).toHaveBeenNthCalledWith(
      2,
      "http://localhost:8000/g/",
      expect.objectContaining({
        method: "GET",
        headers: expect.objectContaining({
          Authorization: expect.stringContaining("Credential=default/"),
          "x-amz-content-sha256": expect.any(String),
          "x-amz-date": expect.any(String),
        }),
      })
    );
    promise.unsubscribe?.();
  });

  it("downloadBucketObject error is returned", async () => {
    global.fetch = jest
      .fn()
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        statusText: "OK",
        json: async () => [{ Id: "crd_test", AccessKey: "default", SecretKey: "default" }],
      })
      .mockResolvedValueOnce({
        ok: false,
        status: 404,
        statusText: "Not Found",
        text: async () => "<Error><Code>NoSuchKey</Code><Message>Missing</Message></Error>",
      }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.downloadBucketObject.initiate({
        bucketId: "g",
        objectKey: "k",
      } as any)
    );
    await expect(promise.unwrap()).rejects.toBeDefined();
    promise.unsubscribe?.();
  });
});
