import type { ComponentProps } from 'react'
import type { Badge } from '@components/ui/Badge'

type BadgeVariant = NonNullable<ComponentProps<typeof Badge>['variant']>

export interface StatusMapEntry {
  variant: BadgeVariant
  label: string
}

export const voucherBatchStatusMap: Record<string, StatusMapEntry> = {
  Draft: { variant: 'neutral', label: 'Draft' },
  0: { variant: 'neutral', label: 'Draft' },
  Batched: { variant: 'info', label: 'Batched' },
  1: { variant: 'info', label: 'Batched' },
  Posted: { variant: 'success', label: 'Posted' },
  2: { variant: 'success', label: 'Posted' },
  Reversed: { variant: 'error', label: 'Reversed' },
  3: { variant: 'error', label: 'Reversed' },
}

export const voucherTypeMap: Record<string, string> = {
  Invoice: 'Invoice',
  0: 'Invoice',
  CreditMemo: 'Credit Memo',
  1: 'Credit Memo',
  DebitMemo: 'Debit Memo',
  2: 'Debit Memo',
  Prepayment: 'Prepayment',
  3: 'Prepayment',
}

export const paymentStatusMap: Record<string, StatusMapEntry> = {
  Selected: { variant: 'info', label: 'Selected' },
  0: { variant: 'info', label: 'Selected' },
  Issued: { variant: 'success', label: 'Issued' },
  1: { variant: 'success', label: 'Issued' },
  Cleared: { variant: 'neutral', label: 'Cleared' },
  2: { variant: 'neutral', label: 'Cleared' },
  Voided: { variant: 'error', label: 'Voided' },
  3: { variant: 'error', label: 'Voided' },
}

export const paymentMethodMap: Record<string, string> = {
  Check: 'Check',
  0: 'Check',
  ACH: 'ACH',
  1: 'ACH',
  WireTransfer: 'Wire Transfer',
  2: 'Wire Transfer',
  CreditCard: 'Credit Card',
  3: 'Credit Card',
  Cash: 'Cash',
  4: 'Cash',
}

export const vendor1099CategoryMap: Record<string, string> = {
  None: 'None',
  0: 'None',
  IndependentContractor: 'Independent Contractor',
  1: 'Independent Contractor',
  Rent: 'Rent',
  2: 'Rent',
  Royalties: 'Royalties',
  3: 'Royalties',
  NonEmployeeCompensation: 'Non-Employee Compensation',
  4: 'Non-Employee Compensation',
  MedicalAndHealth: 'Medical & Health',
  5: 'Medical & Health',
  Attorney: 'Attorney',
  6: 'Attorney',
  Other: 'Other',
  99: 'Other',
}
