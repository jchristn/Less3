import { render } from "@testing-library/react";
import { renderWithRedux } from "./utils";

describe("renderWithRedux", () => {
  describe("Rendering", () => {
    it("should render component with Redux store", () => {
      const TestComponent = () => <div>Test</div>;
      const { container } = renderWithRedux(<TestComponent />);
      expect(container).toBeInTheDocument();
    });

    it("should render with login layout when specified", () => {
      const TestComponent = () => <div>Test</div>;
      const { container } = renderWithRedux(<TestComponent />, true);
      expect(container).toBeInTheDocument();
    });

    it("should render with custom Redux state", () => {
      const TestComponent = () => <div>Test</div>;
      const customState = {} as any;
      const { container } = renderWithRedux(<TestComponent />, false, customState);
      expect(container).toBeInTheDocument();
    });
  });
});

