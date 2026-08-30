// UOM master (global Unit of Measure definitions) API client.
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'


export function companyId(): string {
  const current = useAuthStore.getState().currentCompany
  // Empty id is the "All Companies" sentinel for super admins: omit the
  // companyId query param so the backend returns data across every company.
  return current?.id ?? ''
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
