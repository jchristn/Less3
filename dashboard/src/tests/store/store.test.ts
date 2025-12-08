import { makeStore } from "#/store/store";
import type { AppStore } from "#/store/store";

describe("store", () => {
  describe("makeStore", () => {
    it("should create a store instance", () => {
      const store = makeStore();
      expect(store).toBeDefined();
      expect(store.getState).toBeDefined();
      expect(store.dispatch).toBeDefined();
    });

    it("should have initial state", () => {
      const store = makeStore();
      const state = store.getState();
      expect(state).toBeDefined();
    });

    it("should be of type AppStore", () => {
      const store = makeStore();
      expect(store).toBeDefined();
      // Type check - if this compiles, the type is correct
      const appStore: AppStore = store;
      expect(appStore).toBe(store);
    });
  });
});

