import { render, screen } from "@testing-library/react";
import Card from "#/components/base/card/Card";

describe("Card", () => {
  describe("Rendering", () => {
    it("should render with required props", () => {
      render(<Card>Card Content</Card>);
      expect(screen.getByText("Card Content")).toBeInTheDocument();
    });

    it("should render with title", () => {
      render(<Card title="Card Title">Card Content</Card>);
      expect(screen.getByText("Card Title")).toBeInTheDocument();
      expect(screen.getByText("Card Content")).toBeInTheDocument();
    });

    it("should render with extra content", () => {
      render(
        <Card title="Card Title" extra={<button>Action</button>}>
          Card Content
        </Card>
      );
      expect(screen.getByRole("button", { name: "Action" })).toBeInTheDocument();
    });
  });

  describe("Props/Features", () => {
    it("should render with different sizes", () => {
      const { rerender } = render(<Card size="default">Default</Card>);
      expect(screen.getByText("Default")).toBeInTheDocument();

      rerender(<Card size="small">Small</Card>);
      expect(screen.getByText("Small")).toBeInTheDocument();
    });

    it("should render with hoverable prop", () => {
      render(<Card hoverable>Hoverable Card</Card>);
      expect(screen.getByText("Hoverable Card")).toBeInTheDocument();
    });

    it("should render with bordered prop", () => {
      render(<Card bordered={false}>No Border</Card>);
      expect(screen.getByText("No Border")).toBeInTheDocument();
    });

    it("should render with loading state", () => {
      const { container } = render(<Card loading>Loading Card</Card>);
      // When loading, Card shows skeleton instead of children
      expect(container.querySelector(".ant-skeleton")).toBeInTheDocument();
    });
  });
});

