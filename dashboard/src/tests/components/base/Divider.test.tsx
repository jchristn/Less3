import { render, screen } from "@testing-library/react";
import Divider from "#/components/base/divider/Divider";

describe("Divider", () => {
  describe("Rendering", () => {
    it("should render divider", () => {
      const { container } = render(<Divider />);
      expect(container.querySelector(".ant-divider")).toBeInTheDocument();
    });

    it("should render with text", () => {
      render(<Divider>Divider Text</Divider>);
      expect(screen.getByText("Divider Text")).toBeInTheDocument();
    });

    it("should render with orientation", () => {
      const { container } = render(<Divider orientation="left">Left</Divider>);
      expect(container.querySelector(".ant-divider")).toBeInTheDocument();
    });

    it("should render with type", () => {
      const { container } = render(<Divider type="vertical" />);
      expect(container.querySelector(".ant-divider-vertical")).toBeInTheDocument();
    });
  });
});

