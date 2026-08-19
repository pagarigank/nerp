import { forwardRef, type LabelHTMLAttributes } from 'react'
import { cn } from '@utils/helpers'

export interface LabelProps extends LabelHTMLAttributes<HTMLLabelElement> {
  required?: boolean
  size?: 'sm' | 'md' | 'lg'
  variant?: 'default' | 'muted' | 'required'
}

const sizeClasses = {
  sm: 'text-xs',
  md: 'text-sm',
  lg: 'text-base',
}

const variantClasses = {
  default: 'text-gray-700 dark:text-gray-300',
  muted: 'text-gray-500 dark:text-gray-400',
  required: 'text-gray-700 dark:text-gray-300',
}

export const Label = forwardRef<HTMLLabelElement, LabelProps>(
  ({ className, required = false, size = 'md', variant = 'default', children, ...props }, ref) => (
    <label
      ref={ref}
      className={cn(
        'block font-medium mb-1.5',
        sizeClasses[size],
        variantClasses[variant],
        className
      )}
      {...props}
    >
      {children}
      {required && <span className="text-red-500 ml-1" aria-hidden="true">*</span>}
    </label>
  )
)

Label.displayName = 'Label'

export interface BadgeProps {
  children: React.ReactNode
  variant?: 'default' | 'success' | 'warning' | 'error' | 'info' | 'neutral'
  size?: 'sm' | 'md' | 'lg'
  className?: string | undefined
  dot?: boolean
  dotColor?: string
}

const variantStyles = {
  default: 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300',
  success: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300',
  warning: 'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300',
  error: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300',
  info: 'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300',
  neutral: 'bg-slate-100 text-slate-800 dark:bg-slate-900/30 dark:text-slate-300',
}

const sizeStyles = {
  sm: 'px-2 py-0.5 text-xs gap-1',
  md: 'px-2.5 py-0.5 text-sm gap-1.5',
  lg: 'px-3 py-1 text-base gap-2',
}

export const Badge = ({
  children,
  variant = 'default',
  size = 'md',
  className,
  dot = false,
  dotColor,
}: BadgeProps) => (
  <span
    className={cn(
      'inline-flex items-center font-medium rounded-full',
      variantStyles[variant],
      sizeStyles[size],
      className
    )}
  >
    {dot && (
      <span
        className={cn('h-1.5 w-1.5 rounded-full flex-shrink-0', dotColor)}
        style={{ backgroundColor: dotColor }}
        aria-hidden="true"
      />
    )}
    {children}
  </span>
)

export interface StatusBadgeProps {
  status: 'draft' | 'pending' | 'approved' | 'posted' | 'voided' | 'cancelled' | 'active' | 'inactive' | 'onhold'
  size?: 'sm' | 'md' | 'lg'
  showDot?: boolean
  className?: string
}

const statusConfig = {
  draft: { variant: 'neutral' as const, label: 'Draft' },
  pending: { variant: 'warning' as const, label: 'Pending' },
  approved: { variant: 'info' as const, label: 'Approved' },
  posted: { variant: 'success' as const, label: 'Posted' },
  voided: { variant: 'error' as const, label: 'Voided' },
  cancelled: { variant: 'error' as const, label: 'Cancelled' },
  active: { variant: 'success' as const, label: 'Active' },
  inactive: { variant: 'neutral' as const, label: 'Inactive' },
  onhold: { variant: 'warning' as const, label: 'On Hold' },
}

export const StatusBadge = ({
  status,
  size = 'md',
  showDot = true,
  className,
}: StatusBadgeProps) => {
  const config = statusConfig[status]
  return (
    <Badge variant={config.variant} size={size} dot={showDot} className={className}>
      {config.label}
    </Badge>
  )
}

export interface AmountBadgeProps {
  amount: number
  currency?: string
  isNegative?: boolean
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

export const AmountBadge = ({ amount, currency = 'USD', isNegative, size = 'md', className }: AmountBadgeProps) => {
  const formatted = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Math.abs(amount))

  return (
    <Badge
      variant={isNegative || amount < 0 ? 'error' : 'success'}
      size={size}
      className={cn('font-tabular', className)}
    >
      {amount < 0 ? '-' : ''}{formatted}
    </Badge>
  )
}