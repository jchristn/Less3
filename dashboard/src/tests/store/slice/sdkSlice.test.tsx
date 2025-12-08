import { renderHook, waitFor } from "@testing-library/react";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import { useValidateConnectivityMutation } from "#/store/slice/sdkSlice";
import resettableRootReducer, { apiMiddleWares } from "#/store/rootReducer";
import { buildApiUrl } from "#/services/sdk.service";

jest.mock("#/services/sdk.service");

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

describe("sdkSlice", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (buildApiUrl as jest.Mock).mockReturnValue("http://test.com/");
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
      (global.fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        text: async () => "",
      });

      const store = createTestStore();
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <Provider store={store}>{children}</Provider>
      );

      const { result } = renderHook(() => useValidateConnectivityMutation(), { wrapper });
      const [validateConnectivity] = result.current;

      const promise = validateConnectivity();
      await waitFor(() => {
        expect(result.current[1].isLoading).toBe(false);
      });

      expect(global.fetch).toHaveBeenCalled();
    });
  });
});

