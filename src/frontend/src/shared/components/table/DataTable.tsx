import type { ReactNode } from 'react'
import { useMemo, useState } from 'react'

type SortDirection = 'asc' | 'desc'
type SortValue = string | number | boolean | null | undefined

export interface DataTableColumn<T> {
  key: string
  header: string
  render: (row: T) => ReactNode
  sortValue?: (row: T) => SortValue
  sortable?: boolean
  className?: string
  headerClassName?: string
  align?: 'left' | 'center' | 'right'
}

interface DataTableProps<T> {
  rows: T[]
  columns: DataTableColumn<T>[]
  getRowKey: (row: T) => string | number
  emptyMessage?: string
  defaultSortKey?: string
  defaultSortDirection?: SortDirection
  rowClassName?: (row: T) => string
}

export default function DataTable<T>({
  rows,
  columns,
  getRowKey,
  emptyMessage = 'ไม่พบข้อมูล',
  defaultSortKey,
  defaultSortDirection = 'asc',
  rowClassName,
}: DataTableProps<T>) {
  const [sortKey, setSortKey] = useState(defaultSortKey ?? '')
  const [sortDirection, setSortDirection] = useState<SortDirection>(defaultSortDirection)

  const sortedRows = useMemo(() => {
    const sortColumn = columns.find((column) => column.key === sortKey && column.sortable)
    if (!sortColumn?.sortValue) return rows

    return [...rows].sort((a, b) => compareValues(sortColumn.sortValue?.(a), sortColumn.sortValue?.(b), sortDirection))
  }, [columns, rows, sortDirection, sortKey])

  function handleSort(column: DataTableColumn<T>) {
    if (!column.sortable) return

    if (sortKey === column.key) {
      setSortDirection((current) => (current === 'asc' ? 'desc' : 'asc'))
      return
    }

    setSortKey(column.key)
    setSortDirection('asc')
  }

  return (
    <div className="overflow-hidden rounded-lg bg-white shadow">
      <table className="w-full text-sm">
        <thead className="border-b bg-slate-50">
          <tr>
            {columns.map((column) => (
              <th
                key={column.key}
                className={`px-4 py-3 font-medium text-gray-600 ${alignmentClass(column.align)} ${column.headerClassName ?? ''}`}
              >
                {column.sortable ? (
                  <button
                    type="button"
                    onClick={() => handleSort(column)}
                    className={`inline-flex items-center gap-1.5 font-medium text-gray-600 transition hover:text-sky-700 ${
                      column.align === 'right' ? 'justify-end' : column.align === 'center' ? 'justify-center' : 'justify-start'
                    }`}
                  >
                    <span>{column.header}</span>
                    <SortIndicator active={sortKey === column.key} direction={sortDirection} />
                  </button>
                ) : (
                  column.header
                )}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {rows.length === 0 && (
            <tr>
              <td colSpan={columns.length} className="px-4 py-8 text-center text-gray-400">
                {emptyMessage}
              </td>
            </tr>
          )}
          {sortedRows.map((row) => (
            <tr key={getRowKey(row)} className={`hover:bg-gray-50 ${rowClassName?.(row) ?? ''}`}>
              {columns.map((column) => (
                <td
                  key={column.key}
                  className={`px-4 py-3 ${alignmentClass(column.align)} ${column.className ?? ''}`}
                >
                  {column.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function SortIndicator({ active, direction }: { active: boolean; direction: SortDirection }) {
  return (
    <span className={`text-[10px] ${active ? 'text-sky-600' : 'text-gray-300'}`}>
      {active ? (direction === 'asc' ? '▲' : '▼') : '↕'}
    </span>
  )
}

function compareValues(a: SortValue, b: SortValue, direction: SortDirection) {
  const modifier = direction === 'asc' ? 1 : -1

  if (typeof a === 'boolean' || typeof b === 'boolean') {
    return (Number(a) - Number(b)) * modifier
  }

  if (typeof a === 'number' || typeof b === 'number') {
    return (Number(a ?? 0) - Number(b ?? 0)) * modifier
  }

  return String(a ?? '').localeCompare(String(b ?? ''), 'th') * modifier
}

function alignmentClass(align: DataTableColumn<unknown>['align']) {
  if (align === 'right') return 'text-right'
  if (align === 'center') return 'text-center'
  return 'text-left'
}
