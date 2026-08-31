// <copyright file="DataTable.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { cn } from '@utils/helpers'
import { Search, X, ChevronUp, ChevronDown, ChevronsUpDown } from 'lucide-react'
import { LoadingSpinner } from './LoadingSpinner'
import { Pagination } from './Pagination'

export interface DataTableColumn<T = any> {
  key: string
  header: string
  sortable?: boolean
  align?: 'left' | 'center' | 'right'
  render?: (row: T, value?: any) => React.ReactNode
  /** Value used for client-side search (defaults to row[key] stringified). */
  searchValue?: (row: T) => string
  /** Value used for client-side sorting (defaults to row[key]). */
  sortValue?: (row: T) => string | number
}

export interface DataTableProps<T = any> {
  columns: DataTableColumn<T>[]
  data: T[]
  loading?: boolean
  emptyMessage?: string
  className?: string
  /** Render a search box that filters across all columns. */
  searchable?: boolean
  searchPlaceholder?: string
  /** Enable client-side column sorting (click a sortable header). */
  clientSort?: boolean
  /** Enable pagination with the given page size (set 0/false to disable). */
  pageSize?: number
  /** Stable row key resolver (avoids React key warnings on re-sorts). */
  getRowKey?: (row: T, index: number) => string | number
  /** Row click handler for master-detail flow. */
  onRowClick?: (row: T) => void
  /** Highlight the active row (e.g. the one matching rowKey). */
  rowKey?: string | number
  rowKeyField?: string
}

type SortState = { key: string; dir: 'asc' | 'desc' } | null

function cellText<T>(column: DataTableColumn<T>, row: T): string {
  if (column.searchValue) return column.searchValue(row) ?? ''
  const v = row[column.key]
  if (v == null) return ''
  if (typeof v === 'object') return ''
  return String(v)
}

export function DataTable<T = any>({
  columns,
  data,
  loading = false,
  emptyMessage = 'No data available',
  className,
  searchable = false,
  searchPlaceholder = 'Search...',
  clientSort = false,
  pageSize = 0,
  getRowKey,
  onRowClick,
  rowKey,
  rowKeyField = 'id',
}: DataTableProps<T>) {
  const [query, setQuery] = useState('')
  const [sort, setSort] = useState<SortState>(null)
  const [page, setPage] = useState(1)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return data
    return data.filter(row =>
      columns.some(col => cellText(col, row).toLowerCase().includes(q))
    )
  }, [data, query, columns])

  const sorted = useMemo(() => {
    if (!sort || !clientSort) return filtered
    const col = columns.find(c => c.key === sort.key)
    if (!col) return filtered
    const getVal = col.sortValue
      ? (r: T) => col.sortValue!(r)
      : (r: T) => {
          const v = r[col.key]
          return typeof v === 'number' ? v : String(v ?? '').toLowerCase()
        }
    const dir = sort.dir === 'asc' ? 1 : -1
    return [...filtered].sort((a, b) => {
      const av = getVal(a)
      const bv = getVal(b)
      if (av < bv) return -1 * dir
      if (av > bv) return 1 * dir
      return 0
    })
  }, [filtered, sort, clientSort, columns])

  const totalCount = sorted.length
  const totalPages = pageSize > 0 ? Math.max(1, Math.ceil(totalCount / pageSize)) : 1
  const currentPage = Math.min(page, totalPages)
  const paged = useMemo(() => {
    if (pageSize <= 0) return sorted
    const start = (currentPage - 1) * pageSize
    return sorted.slice(start, start + pageSize)
  }, [sorted, pageSize, currentPage])

  const toggleSort = (key: string) => {
    setSort(prev => {
      if (!prev || prev.key !== key) return { key, dir: 'asc' }
      if (prev.dir === 'asc') return { key, dir: 'desc' }
      return null
    })
    setPage(1)
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <LoadingSpinner size="lg" />
      </div>
    )
  }

  const visibleRows = paged
  const isEmpty = totalCount === 0

  return (
    <div className={className}>
      {searchable && (
        <div className="mb-4 max-w-md">
          <div className="relative">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400 dark:text-gray-500">
              <Search className="h-4 w-4" aria-hidden="true" />
            </div>
            <input
              type="search"
              value={query}
              onChange={e => {
                setQuery(e.target.value)
                setPage(1)
              }}
              placeholder={searchPlaceholder}
              aria-label={searchPlaceholder}
              className="block w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 pl-10 pr-10 py-2.5 text-sm text-gray-900 dark:text-gray-100 placeholder:text-gray-400 dark:placeholder:text-gray-500 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-colors duration-fast"
            />
            {query && (
              <button
                type="button"
                onClick={() => setQuery('')}
                aria-label="Clear search"
                className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            )}
          </div>
          {data.length > 0 && (
            <p className="mt-1.5 text-xs text-gray-500 dark:text-gray-400">
              {totalCount === data.length
                ? `${data.length} record(s)`
                : `${totalCount} of ${data.length} match`}
            </p>
          )}
        </div>
      )}

      {isEmpty ? (
        <div className="text-center py-12">
          <p className="text-gray-500 dark:text-gray-400">
            {query ? `No results match "${query}".` : emptyMessage}
          </p>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                {columns.map(column => {
                  const canSort = clientSort && column.sortable
                  const active = sort?.key === column.key
                  return (
                    <th
                      key={column.key}
                      scope="col"
                      aria-sort={active ? (sort!.dir === 'asc' ? 'ascending' : 'descending') : canSort ? 'none' : undefined}
                      className={cn(
                        'px-6 py-3 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider',
                        column.align === 'right' && 'text-right',
                        column.align === 'center' && 'text-center',
                        !column.align && 'text-left'
                      )}
                    >
                      {canSort ? (
                        <button
                          type="button"
                          onClick={() => toggleSort(column.key)}
                          className={cn(
                            'inline-flex items-center gap-1 hover:text-gray-700 dark:hover:text-gray-200 transition-colors',
                            column.align === 'right' && 'flex-row-reverse',
                            active && 'text-primary-600 dark:text-primary-400'
                          )}
                        >
                          {column.header}
                          {active ? (
                            sort!.dir === 'asc' ? <ChevronUp className="h-3.5 w-3.5" aria-hidden="true" /> : <ChevronDown className="h-3.5 w-3.5" aria-hidden="true" />
                          ) : (
                            <ChevronsUpDown className="h-3.5 w-3.5 opacity-50" aria-hidden="true" />
                          )}
                        </button>
                      ) : (
                        column.header
                      )}
                    </th>
                  )
                })}
              </tr>
            </thead>
            <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              {visibleRows.map((row: any, rowIndex) => {
                const key = getRowKey ? getRowKey(row, rowIndex) : row[rowKeyField] ?? rowIndex
                const isActive = rowKey != null && row[rowKeyField] === rowKey
                return (
                  <tr
                    key={key}
                    onClick={onRowClick ? () => onRowClick(row) : undefined}
                    className={cn(
                      'hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors',
                      onRowClick && 'cursor-pointer',
                      isActive && 'bg-primary-50 dark:bg-primary-900/20'
                    )}
                  >
                    {columns.map(column => (
                      <td
                        key={column.key}
                        className={cn(
                          'px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100',
                          column.align === 'right' && 'text-right',
                          column.align === 'center' && 'text-center'
                        )}
                      >
                        {column.render
                          ? column.render(row, row[column.key])
                          : row[column.key] ?? '-'}
                      </td>
                    ))}
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {pageSize > 0 && !isEmpty && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          pageSize={pageSize}
          totalCount={totalCount}
          onPageChange={setPage}
        />
      )}
    </div>
  )
}
