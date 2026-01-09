import React, { useEffect, useState } from 'react';
import { Table, TableProps, TablePaginationConfig } from 'antd';
import { Resizable } from 'react-resizable';

const ResizableTitle = (props: any) => {
  const { onResize, width, ...restProps } = props;

  if (!width) {
    return <th {...restProps} />;
  }

  return (
    <Resizable
      width={width}
      height={0}
      handle={
        <span
          className="react-resizable-handle"
          onClick={(e: any) => {
            e.stopPropagation();
          }}
        />
      }
      onResize={onResize}
      draggableOpts={{ enableUserSelectHack: false }}
    >
      <th {...restProps} />
    </Resizable>
  );
};

const defaultPagination: TablePaginationConfig = {
  showSizeChanger: true,
  showQuickJumper: true,
  pageSizeOptions: ['10', '20', '50', '100'],
  defaultPageSize: 10,
  showTotal: (total: number, range: [number, number]) => `${range[0]}-${range[1]} of ${total} items`,
};

interface Less3TableProps extends TableProps<any> {
  enablePagination?: boolean;
}

const Less3Table = (props: Less3TableProps) => {
  const { columns, pagination, enablePagination = true, ...rest } = props;
  const [columnsState, setColumnsState] = useState(columns);

  const handleResize =
    (index: number) =>
    (e: any, { size }: any) => {
      setColumnsState((prev: any) => {
        const nextColumns = [...prev];
        nextColumns[index] = {
          ...nextColumns[index],
          width: size.width,
        };
        return nextColumns;
      });
    };

  const columnsWithResizable = columnsState?.map((col: any, index: number) => ({
    ...col,
    onHeaderCell: (column: any) => ({
      width: column.width,
      onResize: handleResize(index),
    }),
  }));

  useEffect(() => {
    setColumnsState(columns);
  }, [columns]);

  // Determine pagination config
  const paginationConfig = pagination === false || !enablePagination
    ? false
    : { ...defaultPagination, ...pagination };

  return (
    <Table
      {...rest}
      columns={columnsWithResizable}
      pagination={paginationConfig}
      components={{
        header: {
          cell: ResizableTitle,
        },
      }}
    />
  );
};

export default Less3Table;
