import type { ComponentProps } from 'react'
import type { Badge } from '@components/ui/Badge'

type BadgeVariant = NonNullable<ComponentProps<typeof Badge>['variant']>

export interface StatusMapEntry {
  variant: BadgeVariant
  label: string
}

export const journalBatchStatusMap: Record<string, StatusMapEntry> = {
  Draft: { variant: 'neutral', label: 'Draft' },
  0: { variant: 'neutral', label: 'Draft' },
  Balanced: { variant: 'info', label: 'Balanced' },
  1: { variant: 'info', label: 'Balanced' },
  Posted: { variant: 'success', label: 'Posted' },
  2: { variant: 'success', label: 'Posted' },
  Reversed: { variant: 'error', label: 'Reversed' },
  3: { variant: 'error', label: 'Reversed' },
}

export const recurringFrequencyMap: Record<string, string> = {
  Monthly: 'Monthly',
  0: 'Monthly',
  Quarterly: 'Quarterly',
  1: 'Quarterly',
  SemiAnnually: 'Semi-Annually',
  2: 'Semi-Annually',
  Annually: 'Annually',
  3: 'Annually',
  Custom: 'Custom',
  4: 'Custom',
}

export const allocationMethodMap: Record<string, string> = {
  Percentage: 'Percentage',
  0: 'Percentage',
  FixedAmount: 'Fixed Amount',
  1: 'Fixed Amount',
  Equally: 'Equally',
  2: 'Equally',
}

export const budgetTypeMap: Record<string, string> = {
  Original: 'Original',
  0: 'Original',
  Revised: 'Revised',
  1: 'Revised',
  Encumbrance: 'Encumbrance',
  2: 'Encumbrance',
}
