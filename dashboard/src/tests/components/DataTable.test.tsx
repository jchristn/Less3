import React from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import DataTable from "#/components/DataTable";

describe("DataTable", () => {
  const columns = [
    {
      key: "Name",
      label: "Name",
    },
    {
      key: "actions",
      label: "Actions",
      isAction: true,
      sortable: false,
      filterable: false,
      render: () => <span>Open Actions</span>,
    },
  ];

  const data = [
    {
      Id: "row-1",
      Name: "First Row",
    },
  ];

  const SelectAllHarness = () => {
    const [selectedKeys, setSelectedKeys] = React.useState<React.Key[]>([]);
    const selectAllRef = React.useRef<HTMLInputElement | null>(null);
    const selectableKeys = data.map((item) => item.Id);
    const areAllRowsSelected = selectedKeys.length === selectableKeys.length;
    const isSelectAllIndeterminate = selectedKeys.length > 0 && !areAllRowsSelected;

    React.useEffect(() => {
      if (selectAllRef.current) {
        selectAllRef.current.indeterminate = isSelectAllIndeterminate;
      }
    }, [isSelectAllIndeterminate]);

    const selectionColumns = [
      {
        key: "select",
        label: (
          <input
            ref={selectAllRef}
            type="checkbox"
            aria-label="Select all rows"
            checked={areAllRowsSelected}
            onChange={(event) => {
              setSelectedKeys(event.target.checked ? selectableKeys : []);
            }}
            onClick={(event) => event.stopPropagation()}
          />
        ),
        isAction: true,
        sortable: false,
        filterable: false,
        render: (item: (typeof data)[number]) => (
          <input
            type="checkbox"
            aria-label={`Select ${item.Name}`}
            checked={selectedKeys.includes(item.Id)}
            onChange={(event) => {
              if (event.target.checked) {
                setSelectedKeys((prev) => [...prev, item.Id]);
              } else {
                setSelectedKeys((prev) => prev.filter((key) => key !== item.Id));
              }
            }}
            onClick={(event) => event.stopPropagation()}
          />
        ),
      },
      ...columns,
    ];

    return (
      <>
        <DataTable columns={selectionColumns} data={data} />
        <button type="button" disabled={selectedKeys.length === 0}>
          Delete Selected
        </button>
      </>
    );
  };

  it("should call the row handler when a non-action cell is clicked", async () => {
    const handleRowClick = jest.fn();

    render(<DataTable columns={columns} data={data} onRowClick={handleRowClick} />);

    await userEvent.click(screen.getByText("First Row"));

    expect(handleRowClick).toHaveBeenCalledTimes(1);
    expect(handleRowClick).toHaveBeenCalledWith(data[0]);
  });

  it("should not call the row handler when an action cell is clicked", async () => {
    const handleRowClick = jest.fn();

    render(<DataTable columns={columns} data={data} onRowClick={handleRowClick} />);

    await userEvent.click(screen.getByText("Open Actions"));

    expect(handleRowClick).not.toHaveBeenCalled();
  });

  it("should support a select-all header control for action columns", async () => {
    render(<SelectAllHarness />);

    await userEvent.click(screen.getByRole("checkbox", { name: "Select all rows" }));

    expect(screen.getByRole("checkbox", { name: "Select all rows" })).toBeChecked();
    expect(screen.getByRole("checkbox", { name: "Select First Row" })).toBeChecked();
    expect(screen.getByRole("button", { name: "Delete Selected" })).toBeEnabled();
  });
});
