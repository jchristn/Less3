import { renderHook } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import {
  useGetCredentialsQuery,
  useGetCredentialByIdQuery,
  useCreateCredentialMutation,
  useDeleteCredentialMutation,
} from "#/store/slice/credentialsSlice";
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

describe("credentialsSlice", () => {
  describe("useGetCredentialsQuery", () => {
    it("should return query hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useGetCredentialsQuery(), { wrapper });
      expect(result.current).toBeDefined();
      expect(result.current.isLoading).toBeDefined();
    });
  });

  describe("useGetCredentialByIdQuery", () => {
    it("should return query hook with guid parameter", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useGetCredentialByIdQuery("test-id", { skip: true }), { wrapper });
      expect(result.current).toBeDefined();
    });
  });

  describe("useCreateCredentialMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useCreateCredentialMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });
  });

  describe("useDeleteCredentialMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useDeleteCredentialMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });
  });
});

