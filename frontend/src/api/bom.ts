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

// --- Routing Operations ---
export function getRoutingOperations(companyIdParam?: string) {
  return get('/bom/routing-operations', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createRoutingOperation(data: any) {
  return post('/bom/routing-operations', data)
}
export function updateRoutingOperation(id: string, data: any) {
  return put(`/bom/routing-operations/${id}`, data)
}
export function activateRoutingOperation(id: string) {
  return post(`/bom/routing-operations/${id}/activate`, {})
}
export function deactivateRoutingOperation(id: string) {
  return post(`/bom/routing-operations/${id}/deactivate`, {})
}
export function deleteRoutingOperation(id: string) {
  return del(`/bom/routing-operations/${id}`)
}

// --- Background Jobs (manual triggers) ---
export function runBomValidation() {
  return post('/bom/validation/run', {})
}
export function runCostRollup() {
  return post('/bom/cost-rollup/run', {})
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

// --- GAP: Substitutions / Allocations / ECN / Comparison / Mass-Update ---
export function getBomSubstitutions(bomHeaderId: string) {
  return get(`/bom/bom-headers/${bomHeaderId}/substitutions`)
}
export function addBomSubstitution(bomHeaderId: string, data: any) {
  return post(`/bom/bom-headers/${bomHeaderId}/substitutions`, data)
}
export function approveBomSubstitution(bomHeaderId: string, subId: string) {
  return post(`/bom/bom-headers/${bomHeaderId}/substitutions/${subId}/approve`, {})
}
export function getBomAllocations(bomHeaderId: string) {
  return get(`/bom/bom-headers/${bomHeaderId}/allocations`)
}
export function allocateBomComponent(bomHeaderId: string, data: any) {
  return post(`/bom/bom-headers/${bomHeaderId}/allocations`, data)
}
export function releaseBomAllocation(bomHeaderId: string, allocId: string) {
  return put(`/bom/bom-headers/${bomHeaderId}/allocations/${allocId}/release`, {})
}
export function getBomEcns(bomHeaderId: string) {
  return get(`/bom/bom-headers/${bomHeaderId}/ecns`)
}
export function createBomEcn(bomHeaderId: string, data: any) {
  return post(`/bom/bom-headers/${bomHeaderId}/ecns`, data)
}
export function transitionBomEcn(bomHeaderId: string, ecnId: string, action: string, extra?: any) {
  return put(`/bom/bom-headers/${bomHeaderId}/ecns/${ecnId}/transition`, { action, ...(extra ?? {}) })
}
export function compareBoms(bomA: string, bomB: string) {
  return get('/bom/bom-headers/compare', { bomA, bomB })
}
export function massUpdateBom(data: any) {
  return post('/bom/bom-headers/mass-update', data)
}
export function applyBomCostToItem(bomHeaderId: string) {
  return post(`/bom/bom-headers/${bomHeaderId}/apply-cost-to-item`, {})
}

// --- GAP: Additional reports ---
export function getComponentShortage(companyIdParam?: string) {
  return get('/bom/reports/component-shortage', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getRevisionHistory(companyIdParam?: string) {
  return get('/bom/reports/revision-history', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getBuildVariance(companyIdParam?: string) {
  return get('/bom/reports/build-variance', { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getWorkCenterUtilization(companyIdParam?: string) {
  return get('/bom/reports/work-center-utilization', { companyId: companyIdParam ?? defaultCompanyId() })
}
