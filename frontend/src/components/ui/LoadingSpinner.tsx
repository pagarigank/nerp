import { cn } from '@utils/helpers'

export interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg' | 'xl'
  className?: string
  'aria-label'?: string
}

const sizeClasses = {
  sm: 'h-4 w-4 border-2',
  md: 'h-6 w-6 border-2',
  lg: 'h-8 w-8 border-3',
  xl: 'h-12 w-12 border-4',
}

export const LoadingSpinner = ({
  size = 'md',
  className,
  'aria-label': ariaLabel = 'Loading',
}: LoadingSpinnerProps) => (
  <span
    className={cn('inline-block animate-spin text-primary-600', sizeClasses[size], className)}
    role="status"
    aria-label={ariaLabel}
  >
    <svg
      className="h-full w-full"
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
      aria-hidden="true"
    >
      <circle
        className="opacity-25"
        cx="12"
        cy="12"
        r="10"
        stroke="currentColor"
        strokeWidth="4"
      />
      <path
        className="opacity-75"
        fill="currentColor"
        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
      />
    </svg>
    <span className="sr-only">{ariaLabel}</span>
  </span>
)

export interface LoadingOverlayProps {
  isLoading: boolean
  children: React.ReactNode
  message?: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
}

export const LoadingOverlay = ({ isLoading, children, message = 'Loading...', size = 'md' }: LoadingOverlayProps) => (
  <div className="relative">
    {children}
    {isLoading && (
      <div
        className="absolute inset-0 bg-white/80 dark:bg-gray-900/80 flex items-center justify-center z-10"
        role="status"
        aria-live="polite"
        aria-label={message}
      >
        <div className="flex flex-col items-center gap-3 p-4 bg-white dark:bg-gray-800 rounded-xl shadow-lg border border-gray-200 dark:border-gray-700">
          <LoadingSpinner size={size} />
          <p className="text-sm text-gray-600 dark:text-gray-400">{message}</p>
        </div>
      </div>
    )}
  </div>
)

export interface SkeletonProps {
  className?: string
  variant?: 'text' | 'circular' | 'rectangular'
  width?: string | number
  height?: string | number
}

export const Skeleton = ({ className, variant = 'text', width, height }: SkeletonProps) => {
  const baseClasses = 'animate-pulse bg-gray-200 dark:bg-gray-700 rounded'

  const variantClasses = {
    text: 'h-4',
    circular: 'rounded-full',
    rectangular: 'rounded-lg',
  }

  return (
    <div
      className={cn(baseClasses, variantClasses[variant], className)}
      style={{ width, height }}
      aria-hidden="true"
    />
  )
}

export interface SkeletonCardProps {
  className?: string
  hasImage?: boolean
  lines?: number
}

export const SkeletonCard = ({ className, hasImage = true, lines = 3 }: SkeletonCardProps) => (
  <div className={cn('bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden', className)}>
    {hasImage && (
      <Skeleton variant="rectangular" width="100%" height="160px" />
    )}
    <div className="p-4 space-y-3">
      <Skeleton variant="text" width="60%" height="24px" />
      {Array.from({ length: lines }).map((_, i) => (
        <Skeleton key={i} variant="text" width={i === lines - 1 ? '40%' : '100%'} />
      ))}
    </div>
  </div>
)

export interface SkeletonTableProps {
  columns: number
  rows?: number
  className?: string
}

export const SkeletonTable = ({ columns, rows = 5, className }: SkeletonTableProps) => (
  <div className={cn('overflow-x-auto', className)}>
    <table className="w-full">
      <thead>
        <tr>
          {Array.from({ length: columns }).map((_, i) => (
            <th key={i} className="p-3 text-left">
              <Skeleton variant="text" width="80%" height="16px" />
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <tr key={rowIndex}>
            {Array.from({ length: columns }).map((_, colIndex) => (
              <td key={colIndex} className="p-3">
                <Skeleton variant="text" width="90%" height="16px" />
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  </div>
)