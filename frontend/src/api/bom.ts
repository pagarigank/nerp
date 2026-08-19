// Bill of Materials module API client (Phase 9).
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'
import { companyId as defaultCompanyId } from './inventory'

// --- BOM Headers ---
export function getBomHeaders(companyIdParam?: string) {
  return get('/bom/bom-headers', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getBomHeader(id: string) {
  return get(`/bom/bom-headers/${id}`)
}
export function createBomHeader(data: any) {
  return post('/bom/bom-headers', data)
}
export function updateBomHeader(id: string, data: any) {
  return put(`/bom/bom-headers/${id}`, data)
}
export function deleteBomHeader(id: string) {
  return del(`/bom/bom-headers/${id}`)
}

// --- Component Lines ---
export function getBomComponents(bomHeaderId: string) {
  return get(`/bom/bom-headers/${bomHeaderId}/components`)
}
export function addBomComponent(bomHeaderId: string, data: any) {
  return post(`/bom/bom-headers/${bomHeaderId}/components`, data)
}
export function updateBomComponent(bomHeaderId: string, lineId: string, data: any) {
  return put(`/bom/bom-headers/${bomHeaderId}/components/${lineId}`, data)
}
export function deleteBomComponent(bomHeaderId: string, lineId: string) {
  return del(`/bom/bom-headers/${bomHeaderId}/components/${lineId}`)
}

// --- BOM Operations ---
export function explodeBom(bomHeaderId: string, quantity = 1) {
  return get(`/bom/bom-headers/${bomHeaderId}/explode`, { quantity })
}
export function getWhereUsed(componentItemId: string) {
  return get('/bom/bom-headers/where-used', { componentItemId })
}
export function getCostRollup(bomHeaderId: string) {
  return get(`/bom/bom-headers/${bomHeaderId}/cost-rollup`)
}

// --- Work Centers ---
export function getWorkCenters(companyIdParam?: string) {
  return get('/bom/work-centers', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createWorkCenter(data: any) {
  return post('/bom/work-centers', data)
}
export function updateWorkCenter(id: string, data: any) {
  return put(`/bom/work-centers/${id}`, data)
}
export function deleteWorkCenter(id: string) {
  return del(`/bom/work-centers/${id}`)
}

// --- Build Orders ---
export function getBuildOrders(companyIdParam?: string, status?: string) {
  const params: any = { companyId: companyIdParam ?? defaultCompanyId() }
  if (status) params.status = status
  return get('/bom/build-orders', params)
}
export function getBuildOrder(id: string) {
  return get(`/bom/build-orders/${id}`)
}
export function createBuildOrder(data: any) {
  return post('/bom/build-orders', data)
}
export function releaseBuildOrder(id: string) {
  return post(`/bom/build-orders/${id}/release`)
}
export function completeBuildOrder(id: string, data?: any) {
  return post(`/bom/build-orders/${id}/complete`, data ?? {})
}
export function disassembleBuildOrder(id: string, data?: any) {
  return post(`/bom/build-orders/${id}/disassemble`, data ?? {})
}

// --- Reports ---
export function getBomListing(companyIdParam?: string) {
  return get('/bom/reports/listing', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getBuildHistory(companyIdParam?: string, parentItemId?: string) {
  const params: any = { companyId: companyIdParam ?? defaultCompanyId() }
  if (parentItemId) params.parentItemId = parentItemId
  return get('/bom/reports/build-history', params)
}
export function getBomAccuracy(companyIdParam?: string) {
  return get('/bom/reports/accuracy', { companyId: companyIdParam ?? defaultCompanyId() })
}
