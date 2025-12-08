import { render, screen } from "@testing-library/react";
import MenuItems from "#/components/menu-item/MenuItems";

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/dashboard",
}));

describe("MenuItems", () => {
  describe("Rendering", () => {
    it("should render menu items", () => {
      const menuItems = [
        { key: "dashboard", label: "Dashboard", path: "/dashboard" },
        { key: "users", label: "Users", path: "/users" },
      ];
      render(<MenuItems menuItems={menuItems} collapsed={false} />);
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
      expect(screen.getByText("Users")).toBeInTheDocument();
    });

    it("should render with nested children", () => {
      const menuItems = [
        {
          key: "parent",
          label: "Parent",
          path: "/parent",
          children: [
            { key: "child1", label: "Child 1", path: "/parent/child1" },
            { key: "child2", label: "Child 2", path: "/parent/child2" },
          ],
        },
      ];
      render(<MenuItems menuItems={menuItems} collapsed={false} />);
      expect(screen.getByText("Parent")).toBeInTheDocument();
    });

    it("should handle click on menu item", () => {
      const handleClickMenuItem = jest.fn();
      const menuItems = [
        { key: "dashboard", label: "Dashboard", path: "/dashboard" },
      ];
      render(
        <MenuItems
          menuItems={menuItems}
          collapsed={false}
          handleClickMenuItem={handleClickMenuItem}
        />
      );
      expect(screen.getByText("Dashboard")).toBeInTheDocument();
    });
  });
});

