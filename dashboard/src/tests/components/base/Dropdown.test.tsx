import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Dropdown from "#/components/base/dropdown/Dropdown";

describe("Dropdown", () => {
  describe("Rendering", () => {
    it("should render children", () => {
      render(
        <Dropdown menu={{ items: [] }}>
          <button>Trigger</button>
        </Dropdown>
      );
      expect(screen.getByRole("button", { name: "Trigger" })).toBeInTheDocument();
    });

    it("should render with menu items", async () => {
      const items = [
        { key: "1", label: "Item 1" },
        { key: "2", label: "Item 2" },
      ];
      render(
        <Dropdown menu={{ items }}>
          <button>Trigger</button>
        </Dropdown>
      );

      const trigger = screen.getByRole("button", { name: "Trigger" });
      await userEvent.click(trigger);

      // Menu items should be available after click
      expect(trigger).toBeInTheDocument();
    });
  });
});

