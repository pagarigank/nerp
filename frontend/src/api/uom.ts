// UOM master (global Unit of Measure definitions) API client.
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'

export const DEMO_COMPANY_ID = '11111111-1111-1111-1111-111111111111'

export function companyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? DEMO_COMPANY_ID
}

export interface UnitOfMeasureSummary {
  id: string
  code: string
  description: string
  baseUOM: string
  factorToBase: number
  isActive: boolean
}

export function getUoms(companyIdParam?: string): Promise<UnitOfMeasureSummary[]> {
  return get('/inventory/uoms', { companyId: companyIdParam ?? companyId() })
}

export function createUom(body: {
  companyId: string
  code: string
  description: string
  baseUOM: string
  factorToBase: number
}): Promise<UnitOfMeasureSummary> {
  return post('/inventory/uoms', body)
}

export function updateUom(
  id: string,
  body: { description: string; baseUOM: string; factorToBase: number; isActive: boolean },
): Promise<string> {
  return put(`/inventory/uoms/${id}`, body)
}

export function deleteUom(id: string): Promise<string> {
  return del(`/inventory/uoms/${id}`)
}
