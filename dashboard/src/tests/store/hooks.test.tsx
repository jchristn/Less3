import { useAppDispatch, useAppSelector, useAppStore } from "#/store/hooks";
import { renderHook } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
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

describe("store hooks", () => {
  describe("useAppDispatch", () => {
    it("should return dispatch function", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useAppDispatch(), { wrapper });
      expect(typeof result.current).toBe("function");
    });
  });

  describe("useAppSelector", () => {
    it("should return selected state", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useAppSelector((state) => state), { wrapper });
      expect(result.current).toBeDefined();
    });
  });

  describe("useAppStore", () => {
    it("should return store instance", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useAppStore(), { wrapper });
      expect(result.current).toBeDefined();
      expect(result.current.getState).toBeDefined();
    });
  });
});

