import { render, screen } from "@testing-library/react";
import MenuItems from "#/components/menu-item/MenuItems";
import { usePathname } from "next/navigation";

jest.mock("next/navigation", () => ({
  usePathname: jest.fn(),
}));

jest.mock("next/link", () => ({
  __esModule: true,
  default: ({ href, children }: any) => <a href={href}>{children}</a>,
}));

describe("MenuItems", () => {
  const usePathnameMock = usePathname as jest.Mock;

  beforeAll(() => {
    (global as any).ResizeObserver = class {
      observe() {}
      unobserve() {}
      disconnect() {}
    };
  });

  it("renders items and marks current path as selected", () => {
    usePathnameMock.mockReturnValue("/dashboard/reports");

    render(
      <MenuItems
        collapsed={false}
        menuItems={[
          { key: "reports", label: "Reports", path: "/dashboard/reports" },
          { key: "settings", label: "Settings", path: "/dashboard/settings" },
        ]}
      />
    );

    expect(screen.getByRole("menuitem", { name: "Reports" })).toHaveClass(
      "ant-menu-item-selected"
    );
    expect(screen.getByRole("menuitem", { name: "Settings" })).not.toHaveClass(
      "ant-menu-item-selected"
    );
  });

  it("serializes children with links", () => {
    usePathnameMock.mockReturnValue("/dashboard/parent/child");

    render(
      <MenuItems
        collapsed={false}
        defaultOpenKeys={["parent"]}
        menuItems={[
          {
            key: "parent",
            label: "Parent",
            path: "/dashboard/parent",
            children: [
              {
                key: "child",
                label: "Child",
                path: "/dashboard/parent/child",
              },
            ],
          },
        ]}
      />
    );

    const childLink = screen.getByText("Child").closest("a");
    expect(childLink).toHaveAttribute("href", "/dashboard/parent/child");
  });
});

