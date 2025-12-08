import { render, screen } from "@testing-library/react";
import Tooltip from "#/components/base/tooltip/Tooltip";

describe("Tooltip", () => {
  describe("Rendering", () => {
    it("should render children", () => {
      render(
        <Tooltip title="Tooltip text">
          <button>Hover me</button>
        </Tooltip>
      );
      expect(screen.getByRole("button", { name: "Hover me" })).toBeInTheDocument();
    });

    it("should render with title", () => {
      render(
        <Tooltip title="Tooltip text">
          <button>Button</button>
        </Tooltip>
      );
      expect(screen.getByRole("button")).toBeInTheDocument();
    });

    it("should render with placement prop", () => {
      render(
        <Tooltip title="Tooltip" placement="top">
          <button>Button</button>
        </Tooltip>
      );
      expect(screen.getByRole("button")).toBeInTheDocument();
    });
  });
});

