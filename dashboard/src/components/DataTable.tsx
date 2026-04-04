'use client';
import React, { useState, useMemo } from 'react';

export interface DataTableColumn<T = any> {
  key: string;
  label: string;
  width?: string;
  tooltip?: string;
  sortable?: boolean;
  filterable?: boolean;
  isAction?: boolean;
  render?: (item: T) => React.ReactNode;
  sortValue?: (item: T) => any;
  filterValue?: (item: T) => string;
}

interface DataTableProps<T = any> {
  data: T[];
  columns: DataTableColumn<T>[];
  loading?: boolean;
  pageSize?: number;
  onRowClick?: (item: T) => void;
  hidePagination?: boolean;
  rowKey?: string;
}

type SortDirection = 'asc' | 'desc' | null;

const DataTable = <T extends Record<string, any>>({
  data,
  columns,
  loading = false,
  pageSize: defaultPageSize = 25,
  onRowClick,
  hidePagination = false,
  rowKey = 'Id',
}: DataTableProps<T>) => {
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<SortDirection>(null);
  const [filters, setFilters] = useState<Record<string, string>>({});
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(defaultPageSize);

  const handleSort = (columnKey: string) => {
    if (sortColumn === columnKey) {
      if (sortDirection === 'asc') {
        setSortDirection('desc');
      } else if (sortDirection === 'desc') {
        setSortColumn(null);
        setSortDirection(null);
      }
    } else {
      setSortColumn(columnKey);
      setSortDirection('asc');
    }
    setCurrentPage(1);
  };

  const handleFilterChange = (columnKey: string, value: string) => {
    setFilters((prev) => ({ ...prev, [columnKey]: value }));
    setCurrentPage(1);
  };

  const filteredData = useMemo(() => {
    let result = [...data];

    // Apply filters
    Object.entries(filters).forEach(([columnKey, filterValue]) => {
      if (!filterValue) return;
      const column = columns.find((c) => c.key === columnKey);
      const lowerFilter = filterValue.toLowerCase();

      result = result.filter((item) => {
        if (column?.filterValue) {
          return column.filterValue(item).toLowerCase().includes(lowerFilter);
        }
        const val = item[columnKey];
        if (val === null || val === undefined) return false;
        return String(val).toLowerCase().includes(lowerFilter);
      });
    });

    // Apply sorting
    if (sortColumn && sortDirection) {
      const column = columns.find((c) => c.key === sortColumn);
      result.sort((a, b) => {
        let aVal: any;
        let bVal: any;

        if (column?.sortValue) {
          aVal = column.sortValue(a);
          bVal = column.sortValue(b);
        } else {
          aVal = a[sortColumn];
          bVal = b[sortColumn];
        }

        if (aVal === null || aVal === undefined) aVal = '';
        if (bVal === null || bVal === undefined) bVal = '';

        if (typeof aVal === 'string') aVal = aVal.toLowerCase();
        if (typeof bVal === 'string') bVal = bVal.toLowerCase();

        if (aVal < bVal) return sortDirection === 'asc' ? -1 : 1;
        if (aVal > bVal) return sortDirection === 'asc' ? 1 : -1;
        return 0;
      });
    }

    return result;
  }, [data, filters, sortColumn, sortDirection, columns]);

  const totalPages = Math.ceil(filteredData.length / pageSize);
  const paginatedData = hidePagination
    ? filteredData
    : filteredData.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const startItem = filteredData.length === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const endItem = Math.min(currentPage * pageSize, filteredData.length);

  const getSortIndicator = (columnKey: string) => {
    if (sortColumn !== columnKey) return <span className="datatable-sort-icon">&#8597;</span>;
    if (sortDirection === 'asc') return <span className="datatable-sort-icon active">&#8593;</span>;
    return <span className="datatable-sort-icon active">&#8595;</span>;
  };

  const hasAnyFilter = columns.some((col) => col.filterable !== false && !col.isAction);

  if (loading) {
    return (
      <div className="datatable-loading">
        <div className="datatable-spinner" />
        <span>Loading...</span>
      </div>
    );
  }

  return (
    <div className="datatable-wrapper">
      {!hidePagination && filteredData.length > 0 && (
        <div className="datatable-pagination">
          <div className="datatable-pagination-info">
            {startItem}-{endItem} of {filteredData.length} items
          </div>
          <div className="datatable-pagination-controls">
            <label className="datatable-pagesize-label">
              Rows:
              <select
                value={pageSize}
                onChange={(e) => {
                  setPageSize(Number(e.target.value));
                  setCurrentPage(1);
                }}
                className="datatable-pagesize-select"
              >
                {[10, 25, 50, 100].map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
            </label>
            <button
              className="datatable-page-btn"
              onClick={() => setCurrentPage(1)}
              disabled={currentPage === 1}
            >
              &laquo;
            </button>
            <button
              className="datatable-page-btn"
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
            >
              &lsaquo;
            </button>
            <span className="datatable-page-indicator">
              <input
                type="number"
                min={1}
                max={totalPages}
                value={currentPage}
                onChange={(e) => {
                  const page = Number(e.target.value);
                  if (page >= 1 && page <= totalPages) {
                    setCurrentPage(page);
                  }
                }}
                className="datatable-page-input"
              />
              / {totalPages}
            </span>
            <button
              className="datatable-page-btn"
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
            >
              &rsaquo;
            </button>
            <button
              className="datatable-page-btn"
              onClick={() => setCurrentPage(totalPages)}
              disabled={currentPage === totalPages}
            >
              &raquo;
            </button>
          </div>
        </div>
      )}
      <div className="datatable-scroll">
        <table className="datatable">
          <thead>
            <tr>
              {columns.map((col) => {
                const isSortable = col.sortable !== false && !col.isAction;
                return (
                  <th
                    key={col.key}
                    style={col.width ? { width: col.width, minWidth: col.width } : undefined}
                    className={isSortable ? 'sortable' : ''}
                    onClick={isSortable ? () => handleSort(col.key) : undefined}
                    title={col.tooltip}
                  >
                    <div className="datatable-th-content">
                      <span>{col.label}</span>
                      {isSortable && getSortIndicator(col.key)}
                    </div>
                  </th>
                );
              })}
            </tr>
            {hasAnyFilter && (
              <tr className="datatable-filter-row">
                {columns.map((col) => {
                  const isFilterable = col.filterable !== false && !col.isAction;
                  return (
                    <th key={`filter-${col.key}`}>
                      {isFilterable && (
                        <input
                          type="text"
                          className="datatable-filter-input"
                          placeholder={`Filter...`}
                          value={filters[col.key] || ''}
                          onChange={(e) => handleFilterChange(col.key, e.target.value)}
                          onClick={(e) => e.stopPropagation()}
                        />
                      )}
                    </th>
                  );
                })}
              </tr>
            )}
          </thead>
          <tbody>
            {paginatedData.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="datatable-empty">
                  No data available
                </td>
              </tr>
            ) : (
              paginatedData.map((item, rowIndex) => (
                <tr
                  key={item[rowKey] ?? rowIndex}
                  onClick={onRowClick ? () => onRowClick(item) : undefined}
                  className={onRowClick ? 'clickable' : ''}
                >
                  {columns.map((col) => (
                    <td key={col.key}>
                      {col.render ? col.render(item) : (item[col.key] ?? '')}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

    </div>
  );
};

export default DataTable;
