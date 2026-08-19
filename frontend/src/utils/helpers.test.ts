import { describe, it, expect } from 'vitest'
import {
  cn,
  formatCurrency,
  formatNumber,
  formatDate,
  formatPercent,
  getInitials,
  truncate,
  generateGuid,
  classNames,
} from './helpers'

describe('formatCurrency', () => {
  it('formats USD with 2 decimals', () => {
    expect(formatCurrency(1234.5)).toBe('$1,234.50')
  })

  it('formats EUR using its symbol', () => {
    expect(formatCurrency(99.99, 'EUR')).toBe('€99.99')
  })

  it('handles zero and negative amounts', () => {
    expect(formatCurrency(0)).toBe('$0.00')
    expect(formatCurrency(-5)).toBe('-$5.00')
  })
})

describe('formatNumber', () => {
  it('uses the requested decimal places', () => {
    expect(formatNumber(1234.567, 2)).toBe('1,234.57')
    expect(formatNumber(42, 0)).toBe('42')
  })
})

describe('formatPercent', () => {
  it('treats input as a percentage value', () => {
    expect(formatPercent(18)).toBe('18.00%')
    expect(formatPercent(12.5, 1)).toBe('12.5%')
  })
})

describe('formatDate', () => {
  it('formats a date string', () => {
    expect(formatDate('2026-07-31')).toBe('Jul 31, 2026')
  })
})

describe('getInitials', () => {
  it('returns up to two uppercased initials', () => {
    expect(getInitials('John Smith')).toBe('JS')
    expect(getInitials('Jane')).toBe('J')
  })
})

describe('truncate', () => {
  it('truncates long strings and leaves short ones alone', () => {
    expect(truncate('short', 10)).toBe('short')
    expect(truncate('a very long string', 8)).toBe('a very l...')
  })
})

describe('generateGuid', () => {
  it('returns a v4-shaped GUID', () => {
    const id = generateGuid()
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/
    )
  })

  it('returns unique values across calls', () => {
    expect(generateGuid()).not.toBe(generateGuid())
  })
})

describe('classNames', () => {
  it('filters falsy values and joins the rest', () => {
    expect(classNames('a', undefined, 'b', false, null, 'c')).toBe('a b c')
  })
})

describe('cn', () => {
  it('merges tailwind classes', () => {
    expect(cn('px-2 py-2', 'px-4')).toBe('py-2 px-4')
  })
})
