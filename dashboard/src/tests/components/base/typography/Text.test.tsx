import { render, screen } from "@testing-library/react";
import Text from "#/components/base/typograpghy/Text";

describe("Text", () => {
  describe("Rendering", () => {
    it("should render text content", () => {
      render(<Text>Test Text</Text>);
      expect(screen.getByText("Test Text")).toBeInTheDocument();
    });

    it("should render with weight prop", () => {
      render(<Text weight={700}>Bold Text</Text>);
      const text = screen.getByText("Bold Text");
      expect(text).toHaveStyle({ fontWeight: 700 });
    });

    it("should render with fontSize prop", () => {
      render(<Text fontSize={18}>Large Text</Text>);
      const text = screen.getByText("Large Text");
      expect(text).toHaveStyle({ fontSize: 18 });
    });

    it("should render with color prop", () => {
      render(<Text color="red">Red Text</Text>);
      const text = screen.getByText("Red Text");
      expect(text).toHaveStyle({ color: "red" });
    });

    it("should combine multiple style props", () => {
      render(
        <Text weight={600} fontSize={20} color="blue">
          Styled Text
        </Text>
      );
      const text = screen.getByText("Styled Text");
      expect(text).toHaveStyle({ fontWeight: 600, fontSize: 20, color: "blue" });
    });
  });
});

