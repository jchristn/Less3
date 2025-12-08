import { render, screen } from "@testing-library/react";
import StoreProvider from "#/store/StoreProvider";

describe("StoreProvider", () => {
  describe("Rendering", () => {
    it("should render children", () => {
      render(
        <StoreProvider>
          <div>Test Content</div>
        </StoreProvider>
      );
      expect(screen.getByText("Test Content")).toBeInTheDocument();
    });

    it("should provide Redux store context", () => {
      const TestComponent = () => <div>Test</div>;
      render(
        <StoreProvider>
          <TestComponent />
        </StoreProvider>
      );
      expect(screen.getByText("Test")).toBeInTheDocument();
    });
  });
});

