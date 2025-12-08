import { render, screen } from "@testing-library/react";
import Less3Table from "#/components/base/table/Table";

describe("Less3Table", () => {
  beforeAll(() => {
    (global as any).ResizeObserver = class {
      observe() {}
      unobserve() {}
      disconnect() {}
    };
  });

  const dataSource = [
    { key: "1", name: "Alpha" },
    { key: "2", name: "Beta" },
  ];

  it("renders without resizable handle when width is missing", () => {
    render(
      <Less3Table
        columns={[{ title: "Name", dataIndex: "name", key: "name" }]}
        dataSource={dataSource}
        pagination={false}
        rowKey="key"
      />
    );

    expect(screen.getByRole("columnheader", { name: "Name" })).toBeInTheDocument();
    expect(document.querySelector(".react-resizable-handle")).toBeNull();
  });

  it("renders resizable handle when width is provided", () => {
    render(
      <Less3Table
        columns={[{ title: "Name", dataIndex: "name", key: "name", width: 150 }]}
        dataSource={dataSource}
        pagination={false}
        rowKey="key"
      />
    );

    expect(document.querySelector(".react-resizable-handle")).not.toBeNull();
  });
});

