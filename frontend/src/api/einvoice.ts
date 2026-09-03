import { get, post, put, del } from './client'

// ---- Taxpayer Profile ----

export interface TaxpayerProfile {
  id: string
  companyId: string
  tin: string
  brn: string
  salesTaxNumber?: string
  serviceTaxNumber?: string
  msicCode?: string
  address?: string
  city?: string
  state?: string
  postCode?: string
  createdAt: string
  updatedAt: string
}

export interface TaxpayerProfileUpdateRequest {
  companyId: string
  tin: string
  brn: string
  salesTaxNumber?: string
  serviceTaxNumber?: string
  msicCode?: string
  address?: string
  city?: string
  state?: string
  postCode?: string
}

export interface TinValidationResult {
  tin: string
  isValid: boolean
  entityName: string
  businessDescription: string
  isAgent: boolean
  success: boolean
  errorMessage?: string
  validationDate?: string
}

export function getTaxpayerProfile(companyId: string): Promise<TaxpayerProfile> {
  return get(`/einvoice/taxpayer-profiles/by-company/${companyId}`)
}

export function saveTaxpayerProfile(data: TaxpayerProfileUpdateRequest): Promise<TaxpayerProfile> {
  return post('/einvoice/taxpayer-profiles', data)
}

export function validateTin(tin: string): Promise<TinValidationResult> {
  return get(`/einvoice/taxpayer-profiles/tin-validate/${encodeURIComponent(tin)}`)
}

// ---- Digital Certificates ----

export interface DigitalCertificate {
  id: string
  companyId: string
  certificateName: string
  issuer: string
  subject: string
  serialNumber: string
  thumbprint: string
  notBefore: string
  notAfter: string
  isActive: boolean
  isRevoked: boolean
  createdAt: string
}

export function getCertificates(companyId: string): Promise<DigitalCertificate[]> {
  return get('/einvoice/certificates', { companyId })
}

export function uploadCertificate(
  companyId: string,
  name: string,
  file: File,
  isActive: boolean = false
): Promise<DigitalCertificate> {
  const formData = new FormData()
  formData.append('file', file)
  return post(
    `/einvoice/certificates/upload?companyId=${companyId}&name=${encodeURIComponent(name)}&isActive=${isActive}`,
    formData
  ) as Promise<DigitalCertificate>
}

export function activateCertificate(id: string): Promise<void> {
  return post(`/einvoice/certificates/${id}/activate`)
}

export function revokeCertificate(id: string, reason: string): Promise<void> {
  return post(`/einvoice/certificates/${id}/revoke`, { reason })
}

export function deleteCertificate(id: string): Promise<void> {
  return del(`/einvoice/certificates/${id}`)
}

// ---- API Credentials ----

export interface ApiCredential {
  id: string
  companyId: string
  clientId: string
  clientSecretHint: string
  environment: string
  isActive: boolean
  createdAt: string
}

export function getCredentials(companyId: string): Promise<ApiCredential[]> {
  return get('/einvoice/credentials', { companyId })
}

export function createCredential(
  companyId: string,
  clientId: string,
  clientSecret: string,
  environment: string
): Promise<ApiCredential & { clientSecret?: string }> {
  return post('/einvoice/credentials', { companyId, clientId, clientSecret, environment })
}

export function rotateSecret(id: string): Promise<{ clientSecret: string }> {
  return post(`/einvoice/credentials/${id}/rotate-secret`)
}

export function deactivateCredential(id: string): Promise<void> {
  return post(`/einvoice/credentials/${id}/deactivate`)
}

// ---- E-Invoice Documents ----

export interface EInvoiceDocument {
  id: string
  companyId: string
  sourceDocumentId: string
  sourceDocumentType: string
  documentType: string
  totalAmount: number
  taxAmount: number
  currencyCode: string
  myInvoisUuid?: string
  validationLink?: string
  status: string
  lhdnsErrorMessage?: string
  submittedAt?: string
  validatedAt?: string
  createdAt: string
}

export function getDocuments(
  companyId: string,
  status?: string
): Promise<EInvoiceDocument[]> {
  return get('/einvoice/documents', { companyId, status })
}

export function getDocumentStatus(id: string): Promise<EInvoiceDocument> {
  return get(`/einvoice/documents/${id}/status`)
}

export function resubmitDocument(
  documentId: string,
  correctedPayload: string
): Promise<{ success: boolean; documentId?: string; uuid?: string; error?: string }> {
  return post(`/einvoice/documents/${documentId}/resubmit`, { correctedPayload })
}

// ---- Reconciliation ----

export interface ReconciliationSummary {
  totalDocuments: number
  validDocuments: number
  invalidDocuments: number
  pendingDocuments: number
  cancelledDocuments: number
  totalAmount: number
  validAmount: number
  invalidAmount: number
  successRate: number
}

export interface ReconciliationItem {
  documentId: string
  sourceDocumentId: string
  sourceDocumentType: string
  documentType: string
  totalAmount: number
  taxAmount: number
  currencyCode: string
  status: string
  myInvoisUuid?: string
  validationLink?: string
  errorMessage?: string
  errorCode?: string
  submittedAt?: string
  validatedAt?: string
  createdAt: string
}

export interface ReconciliationDiscrepancy {
  type: string
  severity: string
  documentId: string
  description: string
  detail: string
  errorCode?: string
  amount: number
}

export interface ReconciliationReport {
  companyId: string
  fromDate: string
  toDate: string
  generatedAt: string
  summary: ReconciliationSummary
  items: ReconciliationItem[]
  discrepancies: ReconciliationDiscrepancy[]
}

export function getReconciliationReport(
  companyId: string,
  fromDate: string,
  toDate: string
): Promise<ReconciliationReport> {
  return get('/einvoice/reconciliation/report', { companyId, fromDate, toDate })
}

export function getUnsubmittedDocuments(companyId: string): Promise<ReconciliationItem[]> {
  return get('/einvoice/reconciliation/unsubmitted', { companyId })
}

export function getPendingValidationDocuments(companyId: string): Promise<ReconciliationItem[]> {
  return get('/einvoice/reconciliation/pending-validation', { companyId })
}
