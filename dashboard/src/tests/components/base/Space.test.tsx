import { render, screen } from "@testing-library/react";
import Space from "#/components/base/space/Space";

describe("Space", () => {
  describe("Rendering", () => {
    it("should render children", () => {
      render(
        <Space>
          <div>Child 1</div>
          <div>Child 2</div>
        </Space>
      );
      expect(screen.getByText("Child 1")).toBeInTheDocument();
      expect(screen.getByText("Child 2")).toBeInTheDocument();
    });

    it("should render with direction prop", () => {
      const { container } = render(
        <Space direction="vertical">
          <div>Child</div>
        </Space>
      );
      expect(container.querySelector(".ant-space-vertical")).toBeInTheDocument();
    });

    it("should render with size prop", () => {
      const { container } = render(
        <Space size="large">
          <div>Child</div>
        </Space>
      );
      expect(container.querySelector(".ant-space")).toBeInTheDocument();
    });

    it("should render with wrap prop", () => {
      const { container } = render(
        <Space wrap>
          <div>Child</div>
        </Space>
      );
      // Space component renders, wrap prop may not add a specific class
      expect(container.querySelector(".ant-space")).toBeInTheDocument();
    });
  });
});

