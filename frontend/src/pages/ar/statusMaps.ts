import type { ComponentProps } from 'react'
import type { Badge } from '@components/ui/Badge'

type BadgeVariant = NonNullable<ComponentProps<typeof Badge>['variant']>

export interface StatusMapEntry {
  variant: BadgeVariant
  label: string
}

export const batchStatusMap: Record<string, StatusMapEntry> = {
  Draft: { variant: 'neutral', label: 'Draft' },
  0: { variant: 'neutral', label: 'Draft' },
  Batched: { variant: 'info', label: 'Batched' },
  1: { variant: 'info', label: 'Batched' },
  Posted: { variant: 'success', label: 'Posted' },
  2: { variant: 'success', label: 'Posted' },
  Reversed: { variant: 'error', label: 'Reversed' },
  3: { variant: 'error', label: 'Reversed' },
}

export const invoiceStatusMap: Record<string, StatusMapEntry> = {
  Open: { variant: 'info', label: 'Open' },
  0: { variant: 'info', label: 'Open' },
  PartiallyPaid: { variant: 'warning', label: 'Partially Paid' },
  1: { variant: 'warning', label: 'Partially Paid' },
  Paid: { variant: 'success', label: 'Paid' },
  2: { variant: 'success', label: 'Paid' },
  Voided: { variant: 'error', label: 'Voided' },
  3: { variant: 'error', label: 'Voided' },
  WriteOff: { variant: 'error', label: 'Written Off' },
  4: { variant: 'error', label: 'Written Off' },
}

export const receiptStatusMap: Record<string, StatusMapEntry> = {
  Unapplied: { variant: 'neutral', label: 'Unapplied' },
  0: { variant: 'neutral', label: 'Unapplied' },
  PartiallyApplied: { variant: 'warning', label: 'Partially Applied' },
  1: { variant: 'warning', label: 'Partially Applied' },
  FullyApplied: { variant: 'success', label: 'Fully Applied' },
  2: { variant: 'success', label: 'Fully Applied' },
  Refunded: { variant: 'error', label: 'Refunded' },
  3: { variant: 'error', label: 'Refunded' },
}

export const statementStatusMap: Record<string, StatusMapEntry> = {
  Generated: { variant: 'info', label: 'Generated' },
  0: { variant: 'info', label: 'Generated' },
  Delivered: { variant: 'success', label: 'Delivered' },
  1: { variant: 'success', label: 'Delivered' },
}

export const memoStatusMap: Record<string, StatusMapEntry> = {
  Open: { variant: 'info', label: 'Open' },
  0: { variant: 'info', label: 'Open' },
  Applied: { variant: 'success', label: 'Applied' },
  1: { variant: 'success', label: 'Applied' },
  Voided: { variant: 'error', label: 'Voided' },
  2: { variant: 'error', label: 'Voided' },
}
