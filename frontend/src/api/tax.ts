import { get, post, put, del } from './client'

// ---- Tax Codes ----

export interface TaxCode {
  id: string
  code: string
  description: string
  lhdntTaxType: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
}

export function getTaxCodes(): Promise<TaxCode[]> {
  return get('/TaxCode')
}

export function getTaxCode(id: string): Promise<TaxCode> {
  return get(`/TaxCode/${id}`)
}

export function createTaxCode(data: { code: string; description: string; lhdntTaxType: string }): Promise<TaxCode> {
  return post('/TaxCode', data)
}

export function updateTaxCode(id: string, data: Partial<TaxCode>): Promise<TaxCode> {
  return put(`/TaxCode/${id}`, { ...data, id })
}

export function deleteTaxCode(id: string): Promise<void> {
  return del(`/TaxCode/${id}`)
}

// ---- Tax Rates ----

export interface TaxRate {
  id: string
  taxCodeId: string
  ratePercent: number
  effectiveFrom: string
  effectiveTo?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
  taxCode?: TaxCode
}

export function getTaxRates(): Promise<TaxRate[]> {
  return get('/TaxRate')
}

export function getTaxRate(id: string): Promise<TaxRate> {
  return get(`/TaxRate/${id}`)
}

export function getTaxRatesByCode(taxCodeId: string): Promise<TaxRate[]> {
  return get(`/TaxRate/by-tax-code/${taxCodeId}`)
}

export function createTaxRate(data: { taxCodeId: string; ratePercent: number; effectiveFrom: string; effectiveTo?: string }): Promise<TaxRate> {
  return post('/TaxRate', data)
}

export function updateTaxRate(id: string, data: Partial<TaxRate>): Promise<TaxRate> {
  return put(`/TaxRate/${id}`, { ...data, id })
}

export function deleteTaxRate(id: string): Promise<void> {
  return del(`/TaxRate/${id}`)
}

export function getCurrentTaxRate(taxCodeId: string, effectiveDate?: string): Promise<TaxRate> {
  return get(`/TaxRate/current/${taxCodeId}`, effectiveDate ? { effectiveDate } : undefined)
}

// ---- Tax Determination Rules ----

export interface TaxDeterminationRule {
  id: string
  companyId: string
  itemCategory: string
  customerType: string
  placeOfSupply: string
  taxCodeId: string
  priority: number
  effectiveFrom: string
  effectiveTo?: string
  isActive: boolean
  createdAt: string
  updatedAt?: string
  taxCode?: TaxCode
}

export function getTaxDeterminationRules(companyId?: string): Promise<TaxDeterminationRule[]> {
  return get('/TaxDeterminationRule', companyId ? { companyId } : undefined)
}

export function createTaxDeterminationRule(data: Partial<TaxDeterminationRule>): Promise<TaxDeterminationRule> {
  return post('/TaxDeterminationRule', data)
}

export function updateTaxDeterminationRule(id: string, data: Partial<TaxDeterminationRule>): Promise<TaxDeterminationRule> {
  return put(`/TaxDeterminationRule/${id}`, { ...data, id })
}

export function deleteTaxDeterminationRule(id: string): Promise<void> {
  return del(`/TaxDeterminationRule/${id}`)
}

// ---- Tax Calculator ----

export interface TaxPreviewResult {
  inputAmount: number
  pricingMode: string
  ratePercent: number
  netAmount: number
  taxAmount: number
  grossAmount: number
}

export function previewTax(data: { amount: number; ratePercent: number; pricingMode: string }): Promise<TaxPreviewResult> {
  return post('/tax/calculator/preview', data)
}
