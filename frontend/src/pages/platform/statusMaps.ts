import type { ComponentProps } from 'react'
import type { Badge } from '@components/ui/Badge'

type BadgeVariant = NonNullable<ComponentProps<typeof Badge>['variant']>

export interface StatusMapEntry {
  variant: BadgeVariant
  label: string
}

export const periodStatusMap: Record<string, StatusMapEntry> = {
  Open: { variant: 'success', label: 'Open' },
  0: { variant: 'success', label: 'Open' },
  Closed: { variant: 'neutral', label: 'Closed' },
  1: { variant: 'neutral', label: 'Closed' },
  Locked: { variant: 'error', label: 'Locked' },
  2: { variant: 'error', label: 'Locked' },
}

export const accountTypeMap: Record<string, StatusMapEntry> = {
  Asset: { variant: 'info', label: 'Asset' },
  0: { variant: 'info', label: 'Asset' },
  Liability: { variant: 'warning', label: 'Liability' },
  1: { variant: 'warning', label: 'Liability' },
  Equity: { variant: 'success', label: 'Equity' },
  2: { variant: 'success', label: 'Equity' },
  Revenue: { variant: 'neutral', label: 'Revenue' },
  3: { variant: 'neutral', label: 'Revenue' },
  Expense: { variant: 'error', label: 'Expense' },
  4: { variant: 'error', label: 'Expense' },
}
