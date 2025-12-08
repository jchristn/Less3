import { render, screen } from "@testing-library/react";
import Table from "#/components/base/table/Table";

describe("Table", () => {
  describe("Rendering", () => {
    it("should render table with columns and data", () => {
      const columns = [
        { title: "Name", dataIndex: "name", key: "name" },
        { title: "Age", dataIndex: "age", key: "age" },
      ];
      const dataSource = [
        { key: "1", name: "John", age: 30 },
        { key: "2", name: "Jane", age: 25 },
      ];
      render(<Table columns={columns} dataSource={dataSource} />);
      expect(screen.getByText("Name")).toBeInTheDocument();
      expect(screen.getByText("Age")).toBeInTheDocument();
      expect(screen.getByText("John")).toBeInTheDocument();
      expect(screen.getByText("Jane")).toBeInTheDocument();
    });

    it("should render empty table when no data", () => {
      const columns = [{ title: "Name", dataIndex: "name", key: "name" }];
      render(<Table columns={columns} dataSource={[]} />);
      expect(screen.getByText("Name")).toBeInTheDocument();
    });
  });
});

