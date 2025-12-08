import { render, screen } from "@testing-library/react";
import RootLayout from "#/app/layout";
import { localStorageKeys } from "#/constants/constant";
import { ThemeEnum } from "#/types/types";

jest.mock("next/font/google", () => ({
  Inter: () => ({ className: "inter-font" }),
}));

jest.mock("next/navigation", () => ({
  useServerInsertedHTML: jest.fn(() => null),
}));

describe("RootLayout", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe("Rendering", () => {
    it("should render children", () => {
      render(
        <RootLayout>
          <div>Test Content</div>
        </RootLayout>
      );
      expect(screen.getByText("Test Content")).toBeInTheDocument();
    });

    it("should use light theme by default", () => {
      const { container } = render(
        <RootLayout>
          <div>Content</div>
        </RootLayout>
      );
      expect(container.querySelector("body")).not.toHaveClass("theme-dark-mode");
    });

    it("should use dark theme from localStorage", () => {
      localStorage.setItem(localStorageKeys.theme, ThemeEnum.DARK);
      const { container } = render(
        <RootLayout>
          <div>Content</div>
        </RootLayout>
      );
      expect(container.querySelector("body")).toHaveClass("theme-dark-mode");
    });
  });
});

