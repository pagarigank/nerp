import { useMemo } from 'react'
import { cn } from '@utils/helpers'
import { ChevronLeft, ChevronRight, ChevronFirst, ChevronLast } from 'lucide-react'

export interface PaginationProps {
  currentPage: number
  totalPages: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
  onPageSizeChange?: (pageSize: number) => void
  pageSizeOptions?: number[]
  showPageSizeSelector?: boolean
  showTotalCount?: boolean
  className?: string
  disabled?: boolean
}

export const Pagination = ({
  currentPage,
  totalPages,
  pageSize,
  totalCount,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
  showPageSizeSelector = true,
  showTotalCount = true,
  className,
  disabled = false,
}: PaginationProps) => {
  const startItem = totalCount === 0 ? 0 : (currentPage - 1) * pageSize + 1
  const endItem = Math.min(currentPage * pageSize, totalCount)

  const pages = useMemo(() => {
    if (totalPages <= 7) {
      return Array.from({ length: totalPages }, (_, i) => i + 1)
    }

    const pages: (number | 'ellipsis')[] = [1]

    if (currentPage > 4) {
      pages.push('ellipsis')
    }

    const start = Math.max(2, currentPage - 1)
    const end = Math.min(totalPages - 1, currentPage + 1)

    for (let i = start; i <= end; i++) {
      pages.push(i)
    }

    if (currentPage < totalPages - 3) {
      pages.push('ellipsis')
    }

    if (totalPages > 1) {
      pages.push(totalPages)
    }

    return pages
  }, [currentPage, totalPages])

  const handlePageSizeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const newPageSize = Number(e.target.value)
    onPageSizeChange?.(newPageSize)
    onPageChange(1)
  }

  return (
    <nav
      className={cn('flex flex-col sm:flex-row items-center justify-between gap-4 p-4 border-t border-gray-200 dark:border-gray-700', className)}
      aria-label="Pagination"
    >
      {showTotalCount && (
        <div className="text-sm text-gray-500 dark:text-gray-400 order-3 sm:order-1">
          {totalCount === 0
            ? 'No items'
            : `Showing ${startItem.toLocaleString()} to ${endItem.toLocaleString()} of ${totalCount.toLocaleString()} results`}
        </div>
      )}

      <div className="flex items-center gap-2 order-1 sm:order-2">
        <button
          onClick={() => onPageChange(1)}
          disabled={disabled || currentPage === 1}
          className={cn(
            'p-2 rounded-lg border border-gray-300 dark:border-gray-600',
            'bg-white dark:bg-gray-800',
            'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'transition-colors duration-fast'
          )}
          aria-label="Go to first page"
          aria-disabled={disabled || currentPage === 1}
        >
          <ChevronFirst className="h-5 w-5" aria-hidden="true" />
        </button>

        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={disabled || currentPage === 1}
          className={cn(
            'p-2 rounded-lg border border-gray-300 dark:border-gray-600',
            'bg-white dark:bg-gray-800',
            'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'transition-colors duration-fast'
          )}
          aria-label="Go to previous page"
          aria-disabled={disabled || currentPage === 1}
        >
          <ChevronLeft className="h-5 w-5" aria-hidden="true" />
        </button>

        <div className="flex items-center gap-1" role="navigation" aria-label="Page numbers">
          {pages.map((page, index) =>
            page === 'ellipsis' ? (
              <span key={`ellipsis-${index}`} className="px-2 text-gray-400 dark:text-gray-500" aria-hidden="true">
                ...
              </span>
            ) : (
              <button
                key={page}
                onClick={() => onPageChange(page)}
                disabled={disabled || currentPage === page}
                className={cn(
                  'min-w-[40px] h-9 rounded-lg font-medium text-sm transition-colors duration-fast',
                  currentPage === page
                    ? 'bg-primary-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 border border-gray-300 dark:border-gray-600',
                  disabled && 'opacity-50 cursor-not-allowed'
                )}
                aria-label={`Page ${page}`}
                aria-current={currentPage === page ? 'page' : undefined}
              >
                {page}
              </button>
            )
          )}
        </div>

        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={disabled || currentPage === totalPages}
          className={cn(
            'p-2 rounded-lg border border-gray-300 dark:border-gray-600',
            'bg-white dark:bg-gray-800',
            'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'transition-colors duration-fast'
          )}
          aria-label="Go to next page"
          aria-disabled={disabled || currentPage === totalPages}
        >
          <ChevronRight className="h-5 w-5" aria-hidden="true" />
        </button>

        <button
          onClick={() => onPageChange(totalPages)}
          disabled={disabled || currentPage === totalPages}
          className={cn(
            'p-2 rounded-lg border border-gray-300 dark:border-gray-600',
            'bg-white dark:bg-gray-800',
            'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300',
            'disabled:opacity-50 disabled:cursor-not-allowed',
            'transition-colors duration-fast'
          )}
          aria-label="Go to last page"
          aria-disabled={disabled || currentPage === totalPages}
        >
          <ChevronLast className="h-5 w-5" aria-hidden="true" />
        </button>
      </div>

      {showPageSizeSelector && onPageSizeChange && (
        <div className="order-2 sm:order-3">
          <label htmlFor="page-size" className="sr-only">
            Items per page
          </label>
          <select
            id="page-size"
            value={pageSize}
            onChange={handlePageSizeChange}
            disabled={disabled}
            className="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent disabled:opacity-50 cursor-pointer"
            aria-label="Items per page"
          >
            {pageSizeOptions.map(size => (
              <option key={size} value={size}>
                {size} per page
              </option>
            ))}
          </select>
        </div>
      )}
    </nav>
  )
}