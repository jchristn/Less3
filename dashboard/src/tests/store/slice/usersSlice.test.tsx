import { renderHook } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import {
  useGetUsersQuery,
  useGetUserByIdQuery,
  useCreateUserMutation,
  useDeleteUserMutation,
} from "#/store/slice/usersSlice";
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

describe("usersSlice", () => {
  describe("useGetUsersQuery", () => {
    it("should return query hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useGetUsersQuery(), { wrapper });
      expect(result.current).toBeDefined();
      // Query hooks return an object with status, data, isLoading, etc.
      // Data might be undefined initially, but isLoading should always be present
      expect(result.current).toHaveProperty("isLoading");
      expect(typeof result.current.isLoading).toBe("boolean");
    });
  });

  describe("useGetUserByIdQuery", () => {
    it("should return query hook with userId parameter", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useGetUserByIdQuery("test-id", { skip: true }), { wrapper });
      expect(result.current).toBeDefined();
    });
  });

  describe("useCreateUserMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useCreateUserMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });
  });

  describe("useDeleteUserMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useDeleteUserMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });
  });
});

