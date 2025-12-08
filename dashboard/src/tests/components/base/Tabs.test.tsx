import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Tabs from "#/components/base/tabs/Tabs";

describe("Tabs", () => {
  describe("Rendering", () => {
    it("should render tabs with items", () => {
      const items = [
        { key: "1", label: "Tab 1", children: "Content 1" },
        { key: "2", label: "Tab 2", children: "Content 2" },
      ];
      render(<Tabs items={items} />);
      expect(screen.getByText("Tab 1")).toBeInTheDocument();
      expect(screen.getByText("Tab 2")).toBeInTheDocument();
      expect(screen.getByText("Content 1")).toBeInTheDocument();
    });

    it("should render with custom className", () => {
      const items = [{ key: "1", label: "Tab 1", children: "Content" }];
      const { container } = render(<Tabs items={items} custom className="custom-class" />);
      expect(container.querySelector(".custom-tabs")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should switch tabs when clicked", async () => {
      const items = [
        { key: "1", label: "Tab 1", children: "Content 1" },
        { key: "2", label: "Tab 2", children: "Content 2" },
      ];
      render(<Tabs items={items} />);

      const tab2 = screen.getByText("Tab 2");
      await userEvent.click(tab2);
      expect(screen.getByText("Content 2")).toBeInTheDocument();
    });
  });
});

