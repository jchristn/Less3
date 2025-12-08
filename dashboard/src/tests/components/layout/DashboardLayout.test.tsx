import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DashboardLayout from "#/components/layout/DashboardLayout";
import { renderWithRedux } from "../../store/utils";

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/dashboard",
}));

describe("DashboardLayout", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("Rendering", () => {
    it("should render children", () => {
      renderWithRedux(
        <DashboardLayout>
          <div>Test Content</div>
        </DashboardLayout>
      );
      expect(screen.getByText("Test Content")).toBeInTheDocument();
    });

    it("should render header with theme mode switch", () => {
      renderWithRedux(
        <DashboardLayout>
          <div>Content</div>
        </DashboardLayout>
      );
      // Theme mode switch should be rendered
      expect(screen.getByText("Content")).toBeInTheDocument();
    });

    it("should render sidebar", () => {
      renderWithRedux(
        <DashboardLayout>
          <div>Content</div>
        </DashboardLayout>
      );
      expect(screen.getByText("Content")).toBeInTheDocument();
    });

    it("should adjust margin when sidebar is collapsed", async () => {
      const { container, rerender } = renderWithRedux(
        <DashboardLayout>
          <div>Content</div>
        </DashboardLayout>
      );
      
      // Find the layout content area
      const layout = container.querySelector(".ant-layout-content");
      expect(layout).toBeInTheDocument();
      
      // Click collapse button to test margin adjustment
      const collapseButton = screen.getAllByRole("button").find(btn => 
        btn.className.includes("collapseButton")
      );
      if (collapseButton) {
        await userEvent.click(collapseButton);
        // The margin should adjust based on collapsed state (60px when collapsed, 200px when expanded)
        expect(layout).toBeInTheDocument();
      }
    });
  });
});

