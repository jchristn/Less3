import { render, screen } from "@testing-library/react";
import Title from "#/components/base/typograpghy/Title";

describe("Title", () => {
  describe("Rendering", () => {
    it("should render title content", () => {
      render(<Title>Test Title</Title>);
      expect(screen.getByText("Test Title")).toBeInTheDocument();
    });

    it("should render with weight prop", () => {
      render(<Title weight={700}>Bold Title</Title>);
      const title = screen.getByText("Bold Title");
      expect(title).toHaveStyle({ fontWeight: 700 });
    });

    it("should render with fontSize prop", () => {
      render(<Title fontSize={24}>Large Title</Title>);
      const title = screen.getByText("Large Title");
      expect(title).toHaveStyle({ fontSize: 24 });
    });

    it("should render with center prop", () => {
      render(<Title center>Centered Title</Title>);
      const title = screen.getByText("Centered Title");
      expect(title).toHaveStyle({ textAlign: "center" });
    });

    it("should render with different heading levels", () => {
      const { rerender } = render(<Title level={1}>H1 Title</Title>);
      expect(screen.getByText("H1 Title")).toBeInTheDocument();

      rerender(<Title level={2}>H2 Title</Title>);
      expect(screen.getByText("H2 Title")).toBeInTheDocument();
    });
  });
});

