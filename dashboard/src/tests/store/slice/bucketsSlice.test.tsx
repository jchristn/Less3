import { renderHook } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import {
  useGetBucketsQuery,
  useGetBucketByIdQuery,
  useCreateBucketMutation,
  useDeleteBucketMutation,
  useListBucketObjectsQuery,
} from "#/store/slice/bucketsSlice";
import resettableRootReducer, { apiMiddleWares } from "#/store/rootReducer";

const createTestStore = () => {
  return configureStore({
    reducer: resettableRootReducer,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat(apiMiddleWares),
  });
};

// Mock fetch globally
global.fetch = jest.fn();

describe("bucketsSlice", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("useGetBucketsQuery", () => {
    it("should return query hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useGetBucketsQuery(), { wrapper });
      expect(result.current).toBeDefined();
      expect(result.current.isLoading).toBeDefined();
    });
  });

  describe("useGetBucketByIdQuery", () => {
    it("should return query hook with bucketName parameter", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useGetBucketByIdQuery("test-bucket", { skip: true }), { wrapper });
      expect(result.current).toBeDefined();
    });
  });

  describe("useCreateBucketMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useCreateBucketMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });
  });

  describe("useDeleteBucketMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useDeleteBucketMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });
  });

  describe("useListBucketObjectsQuery", () => {
    it("should return query hook with bucketGUID parameter", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(
        () => useListBucketObjectsQuery({ bucketGUID: "test-guid" }, { skip: true }),
        { wrapper }
      );
      expect(result.current).toBeDefined();
    });
  });
});

