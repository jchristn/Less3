import { render, screen } from "@testing-library/react";
import Avatar from "#/components/base/avatar/Avatar";

describe("Avatar", () => {
  describe("Rendering", () => {
    it("should render with text", () => {
      render(<Avatar>AB</Avatar>);
      expect(screen.getByText("AB")).toBeInTheDocument();
    });

    it("should render with image src", () => {
      render(<Avatar src="https://example.com/avatar.jpg" alt="Avatar" />);
      const img = screen.getByAltText("Avatar");
      expect(img).toBeInTheDocument();
      expect(img).toHaveAttribute("src", "https://example.com/avatar.jpg");
    });

    it("should render with icon", () => {
      render(<Avatar icon={<span data-testid="icon">Icon</span>} />);
      expect(screen.getByTestId("icon")).toBeInTheDocument();
    });
  });

  describe("Props/Features", () => {
    it("should render with different sizes", () => {
      const { rerender } = render(<Avatar size="small">S</Avatar>);
      expect(screen.getByText("S")).toBeInTheDocument();

      rerender(<Avatar size="large">L</Avatar>);
      expect(screen.getByText("L")).toBeInTheDocument();

      rerender(<Avatar size={64}>64</Avatar>);
      expect(screen.getByText("64")).toBeInTheDocument();
    });

    it("should render with shape prop", () => {
      const { rerender } = render(<Avatar shape="circle">C</Avatar>);
      expect(screen.getByText("C")).toBeInTheDocument();

      rerender(<Avatar shape="square">S</Avatar>);
      expect(screen.getByText("S")).toBeInTheDocument();
    });
  });
});

