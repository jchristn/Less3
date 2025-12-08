import { render, screen } from "@testing-library/react";
import Paragraph from "#/components/base/typograpghy/Paragraph";

describe("Paragraph", () => {
  describe("Rendering", () => {
    it("should render paragraph content", () => {
      render(<Paragraph>Test Paragraph</Paragraph>);
      expect(screen.getByText("Test Paragraph")).toBeInTheDocument();
    });

    it("should render with weight prop", () => {
      render(<Paragraph weight={600}>Bold Paragraph</Paragraph>);
      const paragraph = screen.getByText("Bold Paragraph");
      expect(paragraph).toHaveStyle({ fontWeight: 600 });
    });

    it("should render with fontSize prop", () => {
      render(<Paragraph fontSize={16}>Large Paragraph</Paragraph>);
      const paragraph = screen.getByText("Large Paragraph");
      expect(paragraph).toHaveStyle({ fontSize: 16 });
    });

    it("should render with color prop", () => {
      render(<Paragraph color="gray">Gray Paragraph</Paragraph>);
      const paragraph = screen.getByText("Gray Paragraph");
      expect(paragraph).toHaveStyle({ color: "gray" });
    });
  });
});

