import { render, screen } from "@testing-library/react";
import Page from "#/app/dashboard/page";

jest.mock("#/page/dashboard/DashboardPage", () => {
  return function MockDashboardPage() {
    return <div>Dashboard Page</div>;
  };
});

describe("Dashboard Page", () => {
  describe("Rendering", () => {
    it("should render DashboardPage", () => {
      render(<Page />);
      expect(screen.getByText("Dashboard Page")).toBeInTheDocument();
    });
  });
});

