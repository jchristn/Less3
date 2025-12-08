import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Input from "#/components/base/input/Input";

describe("Input", () => {
  describe("Rendering", () => {
    it("should render with required props", () => {
      render(<Input placeholder="Enter text" />);
      expect(screen.getByPlaceholderText("Enter text")).toBeInTheDocument();
    });

    it("should render with value", () => {
      render(<Input value="test value" readOnly />);
      expect(screen.getByDisplayValue("test value")).toBeInTheDocument();
    });

    it("should render with different input types", () => {
      const { rerender } = render(<Input type="text" placeholder="Text" />);
      expect(screen.getByPlaceholderText("Text")).toBeInTheDocument();

      rerender(<Input type="password" placeholder="Password" />);
      expect(screen.getByPlaceholderText("Password")).toHaveAttribute("type", "password");
    });
  });

  describe("User Interactions", () => {
    it("should handle input change", async () => {
      const handleChange = jest.fn();
      render(<Input onChange={handleChange} placeholder="Enter text" />);

      const input = screen.getByPlaceholderText("Enter text");
      await userEvent.type(input, "test");
      expect(handleChange).toHaveBeenCalled();
    });

    it("should handle focus and blur events", async () => {
      const handleFocus = jest.fn();
      const handleBlur = jest.fn();
      render(
        <Input onFocus={handleFocus} onBlur={handleBlur} placeholder="Enter text" />
      );

      const input = screen.getByPlaceholderText("Enter text");
      await userEvent.click(input);
      expect(handleFocus).toHaveBeenCalled();

      await userEvent.tab();
      expect(handleBlur).toHaveBeenCalled();
    });

    it("should be disabled when disabled prop is true", () => {
      render(<Input disabled placeholder="Disabled" />);
      expect(screen.getByPlaceholderText("Disabled")).toBeDisabled();
    });
  });

  describe("Props/Features", () => {
    it("should forward ref", () => {
      const ref = { current: null };
      render(<Input ref={ref} placeholder="Test" />);
      expect(ref.current).toBeTruthy();
    });

    it("should render with prefix and suffix", () => {
      render(
        <Input
          prefix={<span data-testid="prefix">$</span>}
          suffix={<span data-testid="suffix">USD</span>}
          placeholder="Amount"
        />
      );
      expect(screen.getByTestId("prefix")).toBeInTheDocument();
      expect(screen.getByTestId("suffix")).toBeInTheDocument();
    });

    it("should render with size prop", () => {
      const { rerender } = render(<Input size="small" placeholder="Small" />);
      expect(screen.getByPlaceholderText("Small")).toBeInTheDocument();

      rerender(<Input size="large" placeholder="Large" />);
      expect(screen.getByPlaceholderText("Large")).toBeInTheDocument();
    });
  });
});

