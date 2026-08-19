import type { ComponentProps } from 'react'
import type { Badge } from '@components/ui/Badge'

type BadgeVariant = NonNullable<ComponentProps<typeof Badge>['variant']>

export interface StatusMapEntry {
  variant: BadgeVariant
  label: string
}

export const bankAccountStatusMap: Record<string, StatusMapEntry> = {
  Active: { variant: 'success', label: 'Active' },
  0: { variant: 'success', label: 'Active' },
  Inactive: { variant: 'neutral', label: 'Inactive' },
  1: { variant: 'neutral', label: 'Inactive' },
  Closed: { variant: 'error', label: 'Closed' },
  2: { variant: 'error', label: 'Closed' },
}

export const bankAccountTypeMap: Record<string, string> = {
  Checking: 'Checking',
  Savings: 'Savings',
  MoneyMarket: 'Money Market',
  PettyCash: 'Petty Cash',
  Investment: 'Investment',
}

export const bankAccountTypeValue: Record<string, number> = {
  Checking: 0,
  Savings: 1,
  MoneyMarket: 2,
  PettyCash: 3,
  Investment: 4,
}

export const depositStatusMap: Record<string, StatusMapEntry> = {
  Draft: { variant: 'neutral', label: 'Draft' },
  0: { variant: 'neutral', label: 'Draft' },
  Confirmed: { variant: 'info', label: 'Confirmed' },
  1: { variant: 'info', label: 'Confirmed' },
  Cleared: { variant: 'success', label: 'Cleared' },
  2: { variant: 'success', label: 'Cleared' },
  Voided: { variant: 'error', label: 'Voided' },
  3: { variant: 'error', label: 'Voided' },
}

export const statementStatusMap: Record<string, StatusMapEntry> = {
  Imported: { variant: 'neutral', label: 'Imported' },
  0: { variant: 'neutral', label: 'Imported' },
  Validated: { variant: 'info', label: 'Validated' },
  1: { variant: 'info', label: 'Validated' },
  Reconciled: { variant: 'success', label: 'Reconciled' },
  2: { variant: 'success', label: 'Reconciled' },
  Locked: { variant: 'error', label: 'Locked' },
  3: { variant: 'error', label: 'Locked' },
}

export const lineStatusMap: Record<string, StatusMapEntry> = {
  Unreconciled: { variant: 'neutral', label: 'Unreconciled' },
  0: { variant: 'neutral', label: 'Unreconciled' },
  Matched: { variant: 'info', label: 'Matched' },
  1: { variant: 'info', label: 'Matched' },
  Cleared: { variant: 'success', label: 'Cleared' },
  2: { variant: 'success', label: 'Cleared' },
  Locked: { variant: 'error', label: 'Locked' },
  3: { variant: 'error', label: 'Locked' },
}

export const reconciliationStatusMap: Record<string, StatusMapEntry> = {
  InProgress: { variant: 'info', label: 'In Progress' },
  0: { variant: 'info', label: 'In Progress' },
  Locked: { variant: 'success', label: 'Locked' },
  1: { variant: 'success', label: 'Locked' },
}

export const transferStatusMap: Record<string, StatusMapEntry> = {
  Draft: { variant: 'neutral', label: 'Draft' },
  0: { variant: 'neutral', label: 'Draft' },
  InTransit: { variant: 'info', label: 'In Transit' },
  1: { variant: 'info', label: 'In Transit' },
  Completed: { variant: 'success', label: 'Completed' },
  2: { variant: 'success', label: 'Completed' },
  Voided: { variant: 'error', label: 'Voided' },
  3: { variant: 'error', label: 'Voided' },
}

export const bankFeeStatusMap: Record<string, StatusMapEntry> = {
  Draft: { variant: 'neutral', label: 'Draft' },
  0: { variant: 'neutral', label: 'Draft' },
  Posted: { variant: 'success', label: 'Posted' },
  1: { variant: 'success', label: 'Posted' },
  Voided: { variant: 'error', label: 'Voided' },
  2: { variant: 'error', label: 'Voided' },
}

export const bankFeeTypeMap: Record<string, string> = {
  ServiceCharge: 'Service Charge',
  WireFee: 'Wire Fee',
  ACHFee: 'ACH Fee',
  OverdraftFee: 'Overdraft Fee',
  NsfFee: 'NSF Fee',
  CreditCardProcessing: 'Card Processing',
  Other: 'Other',
}

export const nsfStatusMap: Record<string, StatusMapEntry> = {
  Processed: { variant: 'error', label: 'Processed' },
  0: { variant: 'error', label: 'Processed' },
  Voided: { variant: 'neutral', label: 'Voided' },
  1: { variant: 'neutral', label: 'Voided' },
}

export const matchSourceMap: Record<string, string> = {
  ApPayment: 'AP Payment',
  ArCashReceipt: 'AR Receipt',
  Deposit: 'Deposit',
  BankTransfer: 'Bank Transfer',
  BankAdjustment: 'Bank Adjustment',
}

export const matchSourceValue: Record<string, number> = {
  ApPayment: 0,
  ArCashReceipt: 1,
  Deposit: 2,
  BankTransfer: 3,
  BankAdjustment: 4,
}
