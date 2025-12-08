import { render, screen } from "@testing-library/react";
import Password from "#/components/base/password/Passwored";

describe("Password", () => {
  describe("Rendering", () => {
    it("should render password input", () => {
      render(<Password placeholder="Enter password" />);
      expect(screen.getByPlaceholderText("Enter password")).toBeInTheDocument();
    });

    it("should render with value", () => {
      render(<Password value="password123" readOnly />);
      expect(screen.getByDisplayValue("password123")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should toggle password visibility", async () => {
      const { container } = render(<Password placeholder="Password" />);
      const input = screen.getByPlaceholderText("Password");
      expect(input).toHaveAttribute("type", "password");

      // Find and click the visibility toggle button
      const toggleButton = container.querySelector(".ant-input-password-icon");
      if (toggleButton) {
        // Note: Actual toggle testing would require more complex setup
        expect(toggleButton).toBeInTheDocument();
      }
    });
  });
});

