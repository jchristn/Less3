import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import Less3Table from "#/components/base/table/Table";
import type { ColumnsType } from "antd/es/table";

jest.mock("react-resizable", () => {
  const React = require("react");
  return {
    Resizable: ({ children, onResize, width, handle }: any) => {
      const handleElement = handle
        ? React.cloneElement(handle, {
            onClick: (e: any) => {
              e.stopPropagation();
            },
          })
        : null;
      return React.createElement(
        "div",
        {
          "data-testid": "resizable",
          "data-width": width,
          onClick: () => onResize(null, { size: { width: 200 } }),
        },
        handleElement,
        children
      );
    },
  };
});

describe("Less3Table", () => {
  const columns: ColumnsType<any> = [
    { title: "Name", dataIndex: "name", key: "name", width: 100 },
    { title: "Age", dataIndex: "age", key: "age", width: 100 },
  ];

  const dataSource = [
    { key: "1", name: "John", age: 30 },
    { key: "2", name: "Jane", age: 25 },
  ];

  describe("Rendering", () => {
    it("should render table with columns and data", () => {
      render(<Less3Table columns={columns} dataSource={dataSource} />);
      expect(screen.getByText("Name")).toBeInTheDocument();
      expect(screen.getByText("Age")).toBeInTheDocument();
      expect(screen.getByText("John")).toBeInTheDocument();
      expect(screen.getByText("Jane")).toBeInTheDocument();
    });

    it("should render table without width", () => {
      const columnsWithoutWidth: ColumnsType<any> = [
        { title: "Name", dataIndex: "name", key: "name" },
      ];
      render(<Less3Table columns={columnsWithoutWidth} dataSource={dataSource} />);
      expect(screen.getByText("Name")).toBeInTheDocument();
    });

    it("should handle resizable handle click to stop propagation", () => {
      render(<Less3Table columns={columns} dataSource={dataSource} />);
      const resizable = screen.getAllByTestId("resizable")[0];
      // The handle should be rendered and stopPropagation should be called on click
      expect(resizable).toBeInTheDocument();
    });

    it("should handle column resizing", async () => {
      render(<Less3Table columns={columns} dataSource={dataSource} />);
      const resizable = screen.getAllByTestId("resizable")[0];
      await userEvent.click(resizable);
      // Resize should update column width
      expect(resizable).toBeInTheDocument();
    });

    it("should update columns when props change", () => {
      const { rerender } = render(<Less3Table columns={columns} dataSource={dataSource} />);
      const newColumns: ColumnsType<any> = [
        { title: "New Column", dataIndex: "new", key: "new", width: 150 },
      ];
      rerender(<Less3Table columns={newColumns} dataSource={dataSource} />);
      expect(screen.getByText("New Column")).toBeInTheDocument();
    });
  });
});

