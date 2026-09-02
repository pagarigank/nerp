import { get, post, put, del } from './client'

// Trading Partners
export const getTradingPartners = (companyId?: string) =>
  get<any[]>('/integration/trading-partners', companyId ? { companyId } : undefined)

export const getTradingPartner = (id: string) =>
  get<any>(`/integration/trading-partners/${id}`)

export const createTradingPartner = (data: any) =>
  post<any>('/integration/trading-partners', data)

export const updateTradingPartner = (id: string, data: any) =>
  put<any>(`/integration/trading-partners/${id}`, data)

export const activateTradingPartner = (id: string) =>
  post<void>(`/integration/trading-partners/${id}/activate`)

export const deactivateTradingPartner = (id: string) =>
  post<void>(`/integration/trading-partners/${id}/deactivate`)

export const deleteTradingPartner = (id: string) =>
  del<void>(`/integration/trading-partners/${id}`)

// EDI Document Mappings
export const getEdiMappings = (companyId?: string, documentType?: string, direction?: string) =>
  get<any[]>('/integration/edi-mappings', { companyId, documentType, direction })

export const getEdiMapping = (id: string) =>
  get<any>(`/integration/edi-mappings/${id}`)

export const createEdiMapping = (data: any) =>
  post<any>('/integration/edi-mappings', data)

export const updateEdiMapping = (id: string, data: any) =>
  put<any>(`/integration/edi-mappings/${id}`, data)

export const deleteEdiMapping = (id: string) =>
  del<void>(`/integration/edi-mappings/${id}`)

export const getEdiDocumentTypes = () =>
  get<string[]>('/integration/edi-mappings/document-types')

// Webhook Subscriptions
export const getWebhookSubscriptions = (companyId?: string, eventType?: string) =>
  get<any[]>('/integration/webhooks', { companyId, eventType })

export const getWebhookSubscription = (id: string) =>
  get<any>(`/integration/webhooks/${id}`)

export const createWebhookSubscription = (data: any) =>
  post<any>('/integration/webhooks', data)

export const updateWebhookSubscription = (id: string, data: any) =>
  put<any>(`/integration/webhooks/${id}`, data)

export const testWebhook = (id: string) =>
  post<any>(`/integration/webhooks/${id}/test`)

export const deleteWebhookSubscription = (id: string) =>
  del<void>(`/integration/webhooks/${id}`)

export const getWebhookEventTypes = () =>
  get<string[]>('/integration/webhooks/event-types')

export const getWebhookDeliveryLogs = (query?: {
  webhookSubscriptionId?: string
  status?: string
  page?: number
  pageSize?: number
}) =>
  get<any[]>('/integration/webhooks/delivery-logs', query as any)

// Import Jobs
export const getImportJobs = (companyId?: string, status?: string) =>
  get<any[]>('/integration/import-jobs', { companyId, status })

export const getImportJob = (id: string) =>
  get<any>(`/integration/import-jobs/${id}`)

export const createImportJob = (data: any) =>
  post<any>('/integration/import-jobs', data)

export const addImportLines = (id: string, rows: string[]) =>
  post<any>(`/integration/import-jobs/${id}/lines`, { rows })

export const validateImportJob = (id: string) =>
  post<any>(`/integration/import-jobs/${id}/validate`)

export const commitImportJob = (id: string) =>
  post<any>(`/integration/import-jobs/${id}/commit`)

export const resetImportJob = (id: string) =>
  post<void>(`/integration/import-jobs/${id}/reset`)

export const deleteImportJob = (id: string) =>
  del<void>(`/integration/import-jobs/${id}`)

// Reports
export const getEdiTransactionLog = (query?: {
  companyId?: string
  documentType?: string
  direction?: string
  status?: string
  from?: string
  to?: string
  page?: number
  pageSize?: number
}) =>
  get<any[]>('/integration/reports/edi-transaction-log', query as any)

export const getIntegrationHealth = (companyId?: string) =>
  get<any>('/integration/reports/integration-health', companyId ? { companyId } : undefined)

export const getTradingPartnerPerformance = (companyId?: string, days?: number) =>
  get<any[]>('/integration/reports/trading-partner-performance', { companyId, days })
