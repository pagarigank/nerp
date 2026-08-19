import { describe, it, expect } from 'vitest'
import { batchStatusMap, invoiceStatusMap, receiptStatusMap, statementStatusMap, memoStatusMap } from './statusMaps'

describe('status maps', () => {
  it('maps string enum names for batches', () => {
    expect(batchStatusMap.Draft).toEqual({ variant: 'neutral', label: 'Draft' })
    expect(batchStatusMap.Posted).toEqual({ variant: 'success', label: 'Posted' })
  })

  it('maps numeric enum values for batches', () => {
    expect(batchStatusMap['0']).toEqual(batchStatusMap.Draft)
    expect(batchStatusMap['2']).toEqual(batchStatusMap.Posted)
  })

  it('maps invoice statuses by name and number', () => {
    expect(invoiceStatusMap.Open).toEqual({ variant: 'info', label: 'Open' })
    expect(invoiceStatusMap['3']).toEqual({ variant: 'error', label: 'Voided' })
  })

  it('maps receipt statuses by name and number', () => {
    expect(receiptStatusMap.Unapplied).toEqual({ variant: 'neutral', label: 'Unapplied' })
    expect(receiptStatusMap['2']).toEqual({ variant: 'success', label: 'Fully Applied' })
  })

  it('maps statement statuses by name and number', () => {
    expect(statementStatusMap.Generated).toEqual({ variant: 'info', label: 'Generated' })
    expect(statementStatusMap['1']).toEqual({ variant: 'success', label: 'Delivered' })
  })

  it('maps memo statuses by name and number', () => {
    expect(memoStatusMap.Applied).toEqual({ variant: 'success', label: 'Applied' })
    expect(memoStatusMap['0']).toEqual({ variant: 'info', label: 'Open' })
  })
})
