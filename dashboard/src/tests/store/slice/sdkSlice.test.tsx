import { act, renderHook, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import { useValidateConnectivityMutation } from "#/store/slice/sdkSlice";
import resettableRootReducer, { apiMiddleWares } from "#/store/rootReducer";
import { adminFetch } from "#/services/backendApi.service";

jest.mock("#/services/backendApi.service", () => ({
  adminFetch: jest.fn(),
  readResponseText: jest.fn((response: Response) => response.text().then((text) => text.trim())),
}));

const createTestStore = () => {
  return configureStore({
    reducer: resettableRootReducer,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat(apiMiddleWares),
  });
};

describe("sdkSlice", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("useValidateConnectivityMutation", () => {
    it("should return mutation hook", () => {
      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useValidateConnectivityMutation(), { wrapper });
      expect(result.current).toBeDefined();
      expect(Array.isArray(result.current)).toBe(true);
      expect(typeof result.current[0]).toBe("function");
    });

    it("should handle successful connectivity validation", async () => {
      (adminFetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: {
          get: (name: string) => (name === "content-type" ? "application/json" : null),
        },
        text: async () => "[]",
      });

      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useValidateConnectivityMutation(), { wrapper });
      const [validateConnectivity] = result.current;

      let promise: ReturnType<typeof validateConnectivity>;
      await act(async () => {
        promise = validateConnectivity();
      });
      await waitFor(() => {
        expect(result.current[1].isLoading).toBe(false);
      });

      expect(adminFetch).toHaveBeenCalledWith("admin/users", {
        endpoint: undefined,
        apiKey: undefined,
        headers: {
          "Content-Type": "application/json",
        },
        cache: "no-store",
      });
    });

    it("should reject HTML responses", async () => {
      (adminFetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: {
          get: (name: string) => (name === "content-type" ? "text/html" : null),
        },
        text: async () => "<!DOCTYPE html><html></html>",
      });

      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useValidateConnectivityMutation(), { wrapper });
      const [validateConnectivity] = result.current;

      let promise: ReturnType<typeof validateConnectivity>;
      await act(async () => {
        promise = validateConnectivity();
      });

      await expect(promise!.unwrap()).rejects.toBeDefined();
    });

    it("should surface the invalid API key message on 401", async () => {
      (adminFetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 401,
        statusText: "Unauthorized",
      });

      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useValidateConnectivityMutation(), { wrapper });
      const [validateConnectivity] = result.current;

      let promise: ReturnType<typeof validateConnectivity>;
      await act(async () => {
        promise = validateConnectivity();
      });

      await expect(promise!.unwrap()).rejects.toEqual(
        expect.objectContaining({
          status: 401,
          data: "We are unable to authenticate using the supplied API key.",
        })
      );
    });
  });
});
