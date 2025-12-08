/**
 * Buckets slice coverage: exercise several endpoints with mocked fetch.
 */
import { configureStore } from "@reduxjs/toolkit";
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

  afterEach(() => {
    jest.clearAllMocks();
    global.fetch = originalFetch as any;
  });

  it("getBuckets success populates data", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      statusText: "OK",
      text: async () =>
        `<ListAllMyBucketsResult><Buckets><Bucket><Name>one</Name><CreationDate>now</CreationDate></Bucket></Buckets></ListAllMyBucketsResult>`,
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(bucketsSliceApi.endpoints.getBuckets.initiate());
    const result = await promise.unwrap();
    expect(result).toEqual([{ Name: "one", CreationDate: "now" }]);
    promise.unsubscribe?.();
  });

  it("createBucket error response is surfaced", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 500,
      statusText: "Bad",
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.createBucket.initiate({ Name: "err" } as any)
    );
    await expect(promise.unwrap()).rejects.toBeDefined();
    promise.unsubscribe?.();
  });

  it("listBucketObjects returns contents", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      statusText: "OK",
      text: async () =>
        `<ListBucketResult><Contents><Key>file.txt</Key><Size>1</Size><LastModified>now</LastModified><ContentType>text/plain</ContentType></Contents></ListBucketResult>`,
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.listBucketObjects.initiate({
        bucketGUID: "g",
        prefix: "",
        continuationToken: "",
      } as any)
    );
    const res = await promise.unwrap();
    expect(res).toHaveProperty("Contents");
    promise.unsubscribe?.();
  });

  it("downloadBucketObject error is returned", async () => {
    global.fetch = jest.fn().mockResolvedValue({
      ok: false,
      status: 404,
      statusText: "Not Found",
    }) as any;

    const store = makeStore();
    const promise = store.dispatch(
      bucketsSliceApi.endpoints.downloadBucketObject.initiate({
        bucketGUID: "g",
        objectKey: "k",
      } as any)
    );
    await expect(promise.unwrap()).rejects.toBeDefined();
    promise.unsubscribe?.();
  });
});

