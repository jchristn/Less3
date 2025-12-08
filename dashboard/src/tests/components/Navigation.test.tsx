import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Navigation from "#/components/navigation";

jest.mock("next/image", () => ({
  __esModule: true,
  default: (props: any) => {
    // eslint-disable-next-line @next/next/no-img-element, jsx-a11y/alt-text
    return <img {...props} />;
  },
}));

describe("Navigation", () => {
  const mockMenuItems = [
    { key: "dashboard", label: "Dashboard", path: "/dashboard" },
    { key: "users", label: "Users", path: "/users" },
  ];

  describe("Rendering", () => {
    it("should render with menu items", () => {
      const setCollapsed = jest.fn();
      render(<Navigation collapsed={false} menuItems={mockMenuItems} setCollapsed={setCollapsed} />);
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
      expect(screen.getByText("Users")).toBeInTheDocument();
    });

    it("should render collapse button", () => {
      const setCollapsed = jest.fn();
      render(<Navigation collapsed={false} menuItems={mockMenuItems} setCollapsed={setCollapsed} />);
      const collapseButton = screen.getByRole("button");
      expect(collapseButton).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should call setCollapsed when collapse button is clicked", async () => {
      const setCollapsed = jest.fn();
      render(<Navigation collapsed={false} menuItems={mockMenuItems} setCollapsed={setCollapsed} />);

      const collapseButton = screen.getByRole("button");
      await userEvent.click(collapseButton);

      expect(setCollapsed).toHaveBeenCalledWith(true);
    });

    it("should toggle collapse state", async () => {
      const setCollapsed = jest.fn();
      const { rerender } = render(
        <Navigation collapsed={false} menuItems={mockMenuItems} setCollapsed={setCollapsed} />
      );

      const collapseButton = screen.getByRole("button");
      await userEvent.click(collapseButton);
      expect(setCollapsed).toHaveBeenCalledWith(true);

      rerender(<Navigation collapsed={true} menuItems={mockMenuItems} setCollapsed={setCollapsed} />);
      await userEvent.click(collapseButton);
      expect(setCollapsed).toHaveBeenCalledWith(false);
    });
  });
});

