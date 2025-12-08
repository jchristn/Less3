import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import FallBack, { FallBackEnums } from "#/components/base/fallback/FallBack";

describe("FallBack", () => {
  describe("Rendering", () => {
    it("should render with default message", () => {
      render(<FallBack />);
      expect(screen.getByText("Something went wrong.")).toBeInTheDocument();
    });

    it("should render with custom children", () => {
      render(<FallBack>Custom error message</FallBack>);
      expect(screen.getByText("Custom error message")).toBeInTheDocument();
    });

    it("should render with ERROR type by default", () => {
      const { container } = render(<FallBack />);
      const icon = container.querySelector(".anticon-close-circle");
      expect(icon).toBeInTheDocument();
    });

    it("should render with WARNING type", () => {
      render(<FallBack type={FallBackEnums.WARNING} />);
      const { container } = render(<FallBack type={FallBackEnums.WARNING} />);
      const icon = container.querySelector(".anticon-warning");
      expect(icon).toBeInTheDocument();
    });

    it("should render with INFO type", () => {
      const { container } = render(<FallBack type={FallBackEnums.INFO} />);
      const icon = container.querySelector(".anticon-info-circle");
      expect(icon).toBeInTheDocument();
    });

    it("should render with custom icon", () => {
      render(
        <FallBack icon={<span data-testid="custom-icon">Custom Icon</span>} />
      );
      expect(screen.getByTestId("custom-icon")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should call retry function when retry is clicked", async () => {
      const handleRetry = jest.fn();
      render(<FallBack retry={handleRetry} />);

      const retryButton = screen.getByText("Retry");
      await userEvent.click(retryButton);
      expect(handleRetry).toHaveBeenCalledTimes(1);
    });

    it("should not render retry button when retry prop is not provided", () => {
      render(<FallBack />);
      expect(screen.queryByText("Retry")).not.toBeInTheDocument();
    });
  });

  describe("Props/Features", () => {
    it("should render with custom className", () => {
      const { container } = render(<FallBack className="custom-class" />);
      expect(container.firstChild).toHaveClass("custom-class");
    });

    it("should render with custom style", () => {
      const { container } = render(<FallBack style={{ color: "red" }} />);
      expect(container.firstChild).toHaveStyle({ color: "red" });
    });
  });
});

