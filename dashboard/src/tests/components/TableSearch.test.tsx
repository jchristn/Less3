import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import TableSearch from "#/components/table-search/TableSearch";

describe("TableSearch", () => {
  describe("Rendering", () => {
    it("should render with default placeholder", () => {
      const setSelectedKeys = jest.fn();
      const confirm = jest.fn();
      render(
        <TableSearch
          setSelectedKeys={setSelectedKeys}
          selectedKeys={[]}
          confirm={confirm}
        />
      );
      expect(screen.getByPlaceholderText("Search")).toBeInTheDocument();
    });

    it("should render with custom placeholder", () => {
      const setSelectedKeys = jest.fn();
      const confirm = jest.fn();
      render(
        <TableSearch
          setSelectedKeys={setSelectedKeys}
          selectedKeys={[]}
          confirm={confirm}
          placeholder="Custom Search"
        />
      );
      expect(screen.getByPlaceholderText("Custom Search")).toBeInTheDocument();
    });

    it("should display selected keys value", () => {
      const setSelectedKeys = jest.fn();
      const confirm = jest.fn();
      render(
        <TableSearch
          setSelectedKeys={setSelectedKeys}
          selectedKeys={["test value"]}
          confirm={confirm}
        />
      );
      expect(screen.getByDisplayValue("test value")).toBeInTheDocument();
    });
  });

  describe("User Interactions", () => {
    it("should call setSelectedKeys on input change", async () => {
      const setSelectedKeys = jest.fn();
      const confirm = jest.fn();
      render(
        <TableSearch
          setSelectedKeys={setSelectedKeys}
          selectedKeys={[]}
          confirm={confirm}
        />
      );

      const input = screen.getByPlaceholderText("Search");
      await userEvent.type(input, "test");
      expect(setSelectedKeys).toHaveBeenCalled();
    });

    it("should call confirm when search is triggered", async () => {
      const setSelectedKeys = jest.fn();
      const confirm = jest.fn();
      render(
        <TableSearch
          setSelectedKeys={setSelectedKeys}
          selectedKeys={[]}
          confirm={confirm}
        />
      );

      const input = screen.getByPlaceholderText("Search");
      await userEvent.type(input, "test{enter}");
      expect(confirm).toHaveBeenCalled();
    });

    it("should call confirm when input is cleared", async () => {
      const setSelectedKeys = jest.fn();
      const confirm = jest.fn();
      render(
        <TableSearch
          setSelectedKeys={setSelectedKeys}
          selectedKeys={["test"]}
          confirm={confirm}
        />
      );

      const input = screen.getByDisplayValue("test");
      await userEvent.clear(input);
      expect(confirm).toHaveBeenCalled();
    });
  });
});

