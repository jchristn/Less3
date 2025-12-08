import { render, screen } from "@testing-library/react";
import Alert from "#/components/base/alert/Alert";

describe("Alert", () => {
  describe("Rendering", () => {
    it("should render with required props", () => {
      render(<Alert message="Alert message" />);
      expect(screen.getByText("Alert message")).toBeInTheDocument();
    });

    it("should render with description", () => {
      render(<Alert message="Alert" description="Alert description" />);
      expect(screen.getByText("Alert")).toBeInTheDocument();
      expect(screen.getByText("Alert description")).toBeInTheDocument();
    });

    it("should render with different types", () => {
      const { rerender } = render(<Alert message="Success" type="success" />);
      expect(screen.getByText("Success")).toBeInTheDocument();

      rerender(<Alert message="Error" type="error" />);
      expect(screen.getByText("Error")).toBeInTheDocument();

      rerender(<Alert message="Warning" type="warning" />);
      expect(screen.getByText("Warning")).toBeInTheDocument();

      rerender(<Alert message="Info" type="info" />);
      expect(screen.getByText("Info")).toBeInTheDocument();
    });
  });

  describe("Props/Features", () => {
    it("should render with closable prop", () => {
      render(<Alert message="Closable Alert" closable />);
      expect(screen.getByText("Closable Alert")).toBeInTheDocument();
    });

    it("should render with showIcon prop", () => {
      render(<Alert message="With Icon" showIcon />);
      expect(screen.getByText("With Icon")).toBeInTheDocument();
    });

    it("should render with banner prop", () => {
      render(<Alert message="Banner Alert" banner />);
      expect(screen.getByText("Banner Alert")).toBeInTheDocument();
    });

    it("should render with action", () => {
      render(
        <Alert message="Alert" action={<button>Action</button>} />
      );
      expect(screen.getByRole("button", { name: "Action" })).toBeInTheDocument();
    });
  });
});

