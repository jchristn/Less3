import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Sidebar from "#/components/base/sidebar/Sidebar";

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/dashboard",
}));

describe("Sidebar", () => {
  describe("Rendering", () => {
    it("should render sidebar", () => {
      render(<Sidebar collapsed={false} />);
      expect(document.querySelector(".ant-layout-sider")).toBeInTheDocument();
    });

    it("should render collapsed sidebar", () => {
      render(<Sidebar collapsed={true} />);
      expect(document.querySelector(".ant-layout-sider")).toBeInTheDocument();
    });

    it("should call onCollapse when collapse button is clicked", async () => {
      const onCollapse = jest.fn();
      render(<Sidebar collapsed={false} onCollapse={onCollapse} />);
      
      const collapseButton = screen.getByRole("button");
      await userEvent.click(collapseButton);
      
      expect(onCollapse).toHaveBeenCalledWith(true);
    });

    it("should call onCollapse with false when expanding", async () => {
      const onCollapse = jest.fn();
      render(<Sidebar collapsed={true} onCollapse={onCollapse} />);
      
      const collapseButton = screen.getByRole("button");
      await userEvent.click(collapseButton);
      
      expect(onCollapse).toHaveBeenCalledWith(false);
    });
  });
});

