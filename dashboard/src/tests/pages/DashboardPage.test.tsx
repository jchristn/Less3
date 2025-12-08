import { screen } from "@testing-library/react";
import DashboardPage from "#/page/dashboard/DashboardPage";
import { renderWithRedux } from "../store/utils";

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/dashboard",
}));

describe("DashboardPage", () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe("Rendering", () => {
    it("should render welcome section", () => {
      const { container } = renderWithRedux(<DashboardPage />);
      expect(screen.getByText("Welcome to Less3")).toBeInTheDocument();
      expect(
        container.textContent?.replace(/\s+/g, " ").trim()
      ).toMatchInlineSnapshot(
        `"HomeBucketsObjectsUsersCredentialsChange Server URLHomeWelcome to Less3Manage your storage buckets and configure your storage infrastructure from this centralized home. Use the navigation menu to access different sections and manage your resources."`
      );
    });

    it("should render welcome message", () => {
      renderWithRedux(<DashboardPage />);
      expect(
        screen.getByText(/Manage your storage buckets and configure your storage infrastructure/i)
      ).toBeInTheDocument();
    });

    it("should render info icon", () => {
      const { container } = renderWithRedux(<DashboardPage />);
      const icon = container.querySelector(".anticon-info-circle");
      expect(icon).toBeInTheDocument();
    });
  });

  describe("Snapshots", () => {
    it("should match default render", () => {
      const { container } = renderWithRedux(<DashboardPage />);
      expect(container.firstChild).toMatchSnapshot();
    });
  });
});

