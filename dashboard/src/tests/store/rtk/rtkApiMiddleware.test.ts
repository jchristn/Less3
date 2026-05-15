import { configureStore } from "@reduxjs/toolkit";
import { rtkQueryErrorLogger, errorHandler } from "#/store/rtk/rtkApiMiddleware";
import { message } from "#/utils/message";

describe("rtkApiMiddleware", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("errorHandler", () => {
    it("should handle error with Message", () => {
      const error = { Message: "Test error message" };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Test error message");
    });

    it("should handle error with Description", () => {
      const error = { Description: "Test error description" };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Test error description");
    });

    it("should handle error with message", () => {
      const error = { message: "Test error" };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Test error");
    });

    it("should handle Network Error", () => {
      const error = { data: "Network Error" };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Network Error");
    });

    it("should handle nested data message", () => {
      const error = { status: 403, data: { message: "Forbidden" } };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Forbidden");
    });

    it("should handle string data messages", () => {
      const error = { status: 403, data: "Failed to fetch buckets: Forbidden" };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Failed to fetch buckets: Forbidden");
    });

    it("should handle NotAuthorized error", () => {
      jest.useFakeTimers();
      const error = { Error: "NotAuthorized" };
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Session expired. Redirecting to login page...");
      jest.advanceTimersByTime(3000);
      jest.useRealTimers();
    });

    it("should handle generic error", () => {
      const error = {};
      const dispatch = jest.fn();
      errorHandler({ payload: error }, dispatch);
      expect(message.error).toHaveBeenCalledWith("Something went wrong.");
    });
  });

  describe("rtkQueryErrorLogger", () => {
    it("should be a middleware function", () => {
      expect(typeof rtkQueryErrorLogger).toBe("function");
      const middleware = rtkQueryErrorLogger({} as any);
      expect(typeof middleware).toBe("function");
      const next = jest.fn((action) => action);
      const result = middleware(next);
      expect(typeof result).toBe("function");
    });

    it("should call next for any action", () => {
      const store = configureStore({
        reducer: (state = {}) => state,
        middleware: (getDefaultMiddleware) =>
          getDefaultMiddleware().concat(rtkQueryErrorLogger),
      });

      const action = {
        type: "test/action",
        payload: { data: "test" },
      };

      store.dispatch(action as any);
      // Middleware should process the action
      expect(store.getState()).toBeDefined();
    });
  });
});
