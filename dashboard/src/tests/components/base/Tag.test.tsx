import { render, screen } from "@testing-library/react";
import Tag from "#/components/base/tag/Tag";

describe("Tag", () => {
  describe("Rendering", () => {
    it("should render with text", () => {
      render(<Tag>Tag Text</Tag>);
      expect(screen.getByText("Tag Text")).toBeInTheDocument();
    });

    it("should render with different colors", () => {
      const { rerender } = render(<Tag color="blue">Blue Tag</Tag>);
      expect(screen.getByText("Blue Tag")).toBeInTheDocument();

      rerender(<Tag color="red">Red Tag</Tag>);
      expect(screen.getByText("Red Tag")).toBeInTheDocument();
    });

    it("should render closable tag", () => {
      render(<Tag closable>Closable Tag</Tag>);
      expect(screen.getByText("Closable Tag")).toBeInTheDocument();
    });
  });
});

