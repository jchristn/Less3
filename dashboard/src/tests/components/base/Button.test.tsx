import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Button from "#/components/base/button/Button";

describe("Button", () => {
  describe("Rendering", () => {
    it("should render with required props", () => {
      render(<Button>Click me</Button>);
      expect(screen.getByRole("button", { name: "Click me" })).toBeInTheDocument();
    });

    it("should render with different button types", () => {
      const { rerender } = render(<Button type="primary">Primary</Button>);
      expect(screen.getByRole("button")).toBeInTheDocument();

      rerender(<Button type="default">Default</Button>);
      expect(screen.getByRole("button")).toBeInTheDocument();
    });

    it("should render with weight prop", () => {
      render(<Button weight={700}>Bold Button</Button>);
      const button = screen.getByRole("button");
      expect(button).toHaveStyle({ fontWeight: 700 });
    });

    it("should render with icon", () => {
      render(<Button icon={<span data-testid="icon">Icon</span>}>With Icon</Button>);
      expect(screen.getByTestId("icon")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should handle click event", async () => {
      const handleClick = jest.fn();
      render(<Button onClick={handleClick}>Click me</Button>);

      await userEvent.click(screen.getByRole("button"));
      expect(handleClick).toHaveBeenCalledTimes(1);
    });

    it("should be disabled when disabled prop is true", () => {
      render(<Button disabled>Disabled Button</Button>);
      expect(screen.getByRole("button")).toBeDisabled();
    });

    it("should not call onClick when disabled", async () => {
      const handleClick = jest.fn();
      render(
        <Button disabled onClick={handleClick}>
          Disabled
        </Button>
      );

      await userEvent.click(screen.getByRole("button"));
      expect(handleClick).not.toHaveBeenCalled();
    });
  });

  describe("Props/Features", () => {
    it("should pass through all antd Button props", () => {
      render(
        <Button size="large" shape="round" danger>
          Custom Button
        </Button>
      );
      expect(screen.getByRole("button")).toBeInTheDocument();
    });

    it("should render loading state", () => {
      render(<Button loading>Loading</Button>);
      expect(screen.getByRole("button")).toBeInTheDocument();
    });
  });
});

