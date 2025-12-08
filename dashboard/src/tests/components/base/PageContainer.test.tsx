import { render, screen } from "@testing-library/react";
import PageContainer from "#/components/base/pageContainer/PageContainer";

jest.mock("next/navigation", () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  usePathname: () => "/",
}));

describe("PageContainer", () => {
  describe("Rendering", () => {
    it("should render children", () => {
      render(
        <PageContainer>
          <div>Page Content</div>
        </PageContainer>
      );
      expect(screen.getByText("Page Content")).toBeInTheDocument();
    });

    it("should render with pageTitle", () => {
      render(
        <PageContainer pageTitle="Test Page">
          <div>Content</div>
        </PageContainer>
      );
      expect(screen.getByText("Test Page")).toBeInTheDocument();
    });

    it("should render with backPath", () => {
      render(
        <PageContainer pageTitle="Test Page" backPath="/previous">
          <div>Content</div>
        </PageContainer>
      );
      expect(screen.getByText("Test Page")).toBeInTheDocument();
    });

    it("should render with pageTitleRightContent", () => {
      render(
        <PageContainer pageTitle="Test Page" pageTitleRightContent={<button>Action</button>}>
          <div>Content</div>
        </PageContainer>
      );
      expect(screen.getByRole("button", { name: "Action" })).toBeInTheDocument();
    });

    it("should render with dataTestId", () => {
      render(
        <PageContainer dataTestId="page-container">
          <div>Content</div>
        </PageContainer>
      );
      expect(screen.getByTestId("page-container")).toBeInTheDocument();
    });

    it("should render with is100vh style", () => {
      const { container } = render(
        <PageContainer is100vh={true}>
          <div>Content</div>
        </PageContainer>
      );
      const content = container.querySelector(".ant-layout-content");
      expect(content).toHaveStyle({ height: "100vh" });
    });

    it("should render with custom style", () => {
      const { container } = render(
        <PageContainer style={{ backgroundColor: "red" }}>
          <div>Content</div>
        </PageContainer>
      );
      const content = container.querySelector(".ant-layout-content");
      expect(content).toHaveStyle({ backgroundColor: "red" });
    });
  });
});

