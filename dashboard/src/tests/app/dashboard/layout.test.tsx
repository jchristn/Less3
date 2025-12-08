import { render, screen } from "@testing-library/react";
import Layout from "#/app/dashboard/layout";

jest.mock("#/components/layout/DashboardLayout", () => {
  return function MockDashboardLayout({ children }: { children: React.ReactNode }) {
    return <div data-testid="dashboard-layout">{children}</div>;
  };
});

jest.mock("#/hoc/hoc", () => ({
  __esModule: true,
  default: (Component: React.ComponentType<any>) => Component,
}));

describe("Dashboard Layout", () => {
  describe("Rendering", () => {
    it("should render DashboardLayout with children", () => {
      render(
        <Layout>
          <div>Test Content</div>
        </Layout>
      );
      expect(screen.getByTestId("dashboard-layout")).toBeInTheDocument();
      expect(screen.getByText("Test Content")).toBeInTheDocument();
    });
  });
});

