import { render, screen } from "@testing-library/react";
import PageLoading from "#/components/base/loading/PageLoading";

describe("PageLoading", () => {
  describe("Rendering", () => {
    it("should render with default message", () => {
      render(<PageLoading />);
      expect(screen.getByText("Loading...")).toBeInTheDocument();
    });

    it("should render with custom message", () => {
      render(<PageLoading message="Please wait..." />);
      expect(screen.getByText("Please wait...")).toBeInTheDocument();
    });

    it("should render with JSX message", () => {
      render(<PageLoading message={<span data-testid="custom-message">Custom</span>} />);
      expect(screen.getByTestId("custom-message")).toBeInTheDocument();
    });

    it("should render with dataTestId", () => {
      render(<PageLoading dataTestId="loading-component" />);
      expect(screen.getByTestId("loading-component")).toBeInTheDocument();
    });

    it("should render loading icon", () => {
      const { container } = render(<PageLoading />);
      const icon = container.querySelector(".anticon-loading");
      expect(icon).toBeInTheDocument();
    });
  });
});

