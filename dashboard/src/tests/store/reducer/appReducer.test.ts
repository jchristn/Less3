import appReducer, { setEndpoint, resetEndpoint } from "#/store/reducer/appReducer";

describe("appReducer", () => {
  const initialState = {
    endpoint: null,
  };

  describe("setEndpoint", () => {
    it("should set endpoint in state", () => {
      const action = setEndpoint("http://example.com");
      const newState = appReducer(initialState, action);
      expect(newState.endpoint).toBe("http://example.com");
    });

    it("should set endpoint to null", () => {
      const action = setEndpoint(null);
      const newState = appReducer(initialState, action);
      expect(newState.endpoint).toBe(null);
    });

    it("should update existing endpoint", () => {
      const stateWithEndpoint = { endpoint: "http://old.com" };
      const action = setEndpoint("http://new.com");
      const newState = appReducer(stateWithEndpoint, action);
      expect(newState.endpoint).toBe("http://new.com");
    });
  });

  describe("resetEndpoint", () => {
    it("should reset endpoint to null", () => {
      const stateWithEndpoint = { endpoint: "http://example.com" };
      const action = resetEndpoint();
      const newState = appReducer(stateWithEndpoint, action);
      expect(newState.endpoint).toBe(null);
    });

    it("should handle reset when endpoint is already null", () => {
      const action = resetEndpoint();
      const newState = appReducer(initialState, action);
      expect(newState.endpoint).toBe(null);
    });
  });

  describe("initial state", () => {
    it("should return initial state for unknown action", () => {
      const unknownAction = { type: "UNKNOWN_ACTION" };
      const newState = appReducer(undefined, unknownAction as any);
      expect(newState).toEqual(initialState);
    });
  });
});

