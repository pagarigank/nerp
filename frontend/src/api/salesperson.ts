/**
 * Salesperson Module API Client
 * Routes, Sync Package Download, Offline Calls Upload, and History
 */
import { get, post, put, del } from './client'
import { useAuthStore } from '@stores/authStore'

export function currentCompanyId(): string {
  const current = useAuthStore.getState().currentCompany
  return current?.id ?? ''
}

export interface SalespersonRouteSummary {
  id: string
  companyId: string
  salesRepId: string
  salesRepName: string
  customerId: string
  customerCode: string
  customerName: string
  dayOfWeek: number
  dayOfWeekNumber: number
  sequenceOrder: number
  estimatedTime?: string
  notes?: string
  customerAddress?: string
  customerPhone?: string
  creditLimit: number
  currentBalance: number
  isActive: boolean
}

export interface CreateSalespersonRouteRequest {
  companyId: string
  salesRepId: string
  customerId: string
  dayOfWeek: number
  sequenceOrder: number
  estimatedTime?: string
  notes?: string
}

export interface UpdateSalespersonRouteRequest {
  dayOfWeek: number
  sequenceOrder: number
  estimatedTime?: string
  notes?: string
  isActive: boolean
}

export interface BulkAssignRoutesRequest {
  companyId: string
  salesRepId: string
  dayOfWeek: number
  customerIds: string[]
}

export interface OfflineSyncPackage {
  companyId: string
  rep: {
    id: string
    code: string
    name: string
    commissionRate: number
    email?: string
  }
  serverTime: string
  routes: Array<{
    id: string
    salesRepId: string
    customerId: string
    customerCode: string
    customerName: string
    contactName: string
    phone: string
    address: string
    dayOfWeek: number
    sequenceOrder: number
    estimatedTime?: string
    notes?: string
    creditLimit: number
    currentBalance: number
  }>
  customers: Array<{
    id: string
    customerCode: string
    customerName: string
    contactName: string
    phone: string
    billingAddress: string
    shippingAddress: string
    creditLimit: number
    currentBalance: number
  }>
  products: Array<{
    id: string
    itemCode: string
    description: string
    basePrice: number
    unitOfMeasure: string
    itemCategoryId?: string
    categoryName: string
    onHandQty: number
  }>
  pricingRules: Array<{
    id: string
    customerId?: string
    itemId?: string
    itemCategoryId?: string
    discountPercent: number
    unitPriceOverride?: number
    minimumQuantity?: number
    prioritySequence: number
  }>
}

export interface SyncUploadRequest {
  companyId: string
  salesRepId: string
  calls: Array<{
    clientCallId?: string
    customerId: string
    routeId?: string
    callDate: string
    startTime: string
    endTime?: string
    notes?: string
    lines: Array<{
      itemId: string
      itemCode: string
      description: string
      quantity: number
      sellingPrice: number
      unitOfMeasure: string
      discountPercent: number
      lineTotal: number
    }>
    createSalesOrder?: boolean
  }>
}

export interface SyncCallResultItem {
  clientCallId: string
  callId: string
  callNumber: string
  salesOrderId?: string
  salesOrderNumber?: string
  status: string
  syncedAt: string
}

export interface SyncUploadResult {
  items: SyncCallResultItem[]
  totalProcessed: number
}

export interface SalesCallSummary {
  id: string
  companyId: string
  callNumber: string
  salesRepId: string
  salesRepName: string
  customerId: string
  customerCode: string
  customerName: string
  callDate: string
  startTime: string
  endTime?: string
  status: number
  notes?: string
  salesOrderId?: string
  salesOrderNumber?: string
  totalAmount: number
  isOfflineCreated: boolean
  clientCallId?: string
  syncedAt?: string
}

export interface SalesCallDetail extends SalesCallSummary {
  lines: Array<{
    id: string
    lineNumber: number
    itemId: string
    itemCode: string
    description: string
    quantity: number
    sellingPrice: number
    unitOfMeasure: string
    discountPercent: number
    lineTotal: number
  }>
}

// Route API methods
export function getSalespersonRoutes(
  companyIdParam?: string,
  salesRepId?: string,
  dayOfWeek?: number,
): Promise<SalespersonRouteSummary[]> {
  const params: Record<string, string | number> = {
    companyId: companyIdParam || currentCompanyId(),
  }
  if (salesRepId) params.salesRepId = salesRepId
  if (dayOfWeek !== undefined) params.dayOfWeek = dayOfWeek
  return get('/om/salesperson-routes', params)
}

export function getSalespersonRoute(id: string): Promise<SalespersonRouteSummary> {
  return get(`/om/salesperson-routes/${id}`)
}

export function createSalespersonRoute(data: CreateSalespersonRouteRequest): Promise<string> {
  return post('/om/salesperson-routes', data)
}

export function updateSalespersonRoute(id: string, data: UpdateSalespersonRouteRequest): Promise<string> {
  return put(`/om/salesperson-routes/${id}`, data)
}

export function deleteSalespersonRoute(id: string): Promise<string> {
  return del(`/om/salesperson-routes/${id}`)
}

export function bulkAssignRoutes(data: BulkAssignRoutesRequest): Promise<number> {
  return post('/om/salesperson-routes/bulk-assign', data)
}

export function reorderRoutes(routeIds: string[]): Promise<string> {
  return post('/om/salesperson-routes/reorder', { routeIds })
}

// Sync API methods
export function getOfflineSyncPackage(salesRepId: string, companyIdParam?: string): Promise<OfflineSyncPackage> {
  return get('/om/salesperson/sync-package', {
    companyId: companyIdParam || currentCompanyId(),
    salesRepId,
  })
}

export function uploadOfflineSalesCalls(data: SyncUploadRequest): Promise<SyncUploadResult> {
  return post('/om/salesperson/sync-upload', data)
}

// Sales Calls History API
export function getSalesCalls(params?: {
  companyId?: string
  salesRepId?: string
  customerId?: string
  fromDate?: string
  toDate?: string
}): Promise<SalesCallSummary[]> {
  return get('/om/sales-calls', {
    companyId: params?.companyId || currentCompanyId(),
    ...params,
  })
}

export function getSalesCallDetail(id: string): Promise<SalesCallDetail> {
  return get(`/om/sales-calls/${id}`)
}
