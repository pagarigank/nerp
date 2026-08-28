// Project Accounting module API client (Phase 10).
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'
import { companyId as defaultCompanyId } from './inventory'
import type {
  Asc606AllocateResult,
  Asc606RecognitionStatus,
  Asc606RecognizeResult,
  CertifiedPayrollRow,
  ContractAssetLiabilityRow,
  ContractValueAnalysisRow,
  EacPortfolioPoint,
  EacTrendPoint,
  EarnedValueRow,
  EmployeeProfitabilityRow,
  EmployeeUtilizationRow,
  FiveStepSummary,
  LienWaiverRegisterRow,
  PendingCoImpactRow,
  PerformanceObligation,
  PmPerformanceRow,
  PortfolioDashboardRow,
  ProjectAgingRow,
  ProjectDocumentItem,
  SubcontractCommitmentRow,
  SubcontractStatusRow,
} from '@types/projectAccounting'

// --- Projects ---
export function getProjects(companyIdParam?: string, status?: string) {
  const params: any = { companyId: companyIdParam ?? defaultCompanyId() }
  if (status) params.status = status
  return get('/projects', params)
}
export function getProject(id: string) {
  return get(`/projects/${id}`)
}
export function createProject(data: any) {
  return post('/projects', data)
}
export function updateProject(id: string, data: any) {
  return put(`/projects/${id}`, data)
}
export function updateProjectStatus(id: string, status: string) {
  return put(`/projects/${id}/status`, { status })
}

// --- Tasks ---
export function getProjectTasks(projectId: string) {
  return get(`/projects/${projectId}/tasks`)
}
export function addProjectTask(projectId: string, data: any) {
  return post(`/projects/${projectId}/tasks`, data)
}
export function updateProjectTask(projectId: string, taskId: string, data: any) {
  return put(`/projects/${projectId}/tasks/${taskId}`, data)
}
export function deleteProjectTask(projectId: string, taskId: string) {
  return del(`/projects/${projectId}/tasks/${taskId}`)
}

// --- Budget ---
export function getBudgetLines(projectId: string) {
  return get(`/projects/${projectId}/budget`)
}
export function addBudgetLine(projectId: string, data: any) {
  return post(`/projects/${projectId}/budget`, data)
}
export function updateBudgetLine(projectId: string, lineId: string, data: any) {
  return put(`/projects/${projectId}/budget/${lineId}`, data)
}
export function deleteBudgetLine(projectId: string, lineId: string) {
  return del(`/projects/${projectId}/budget/${lineId}`)
}
export function reviseBudgetLine(projectId: string, lineId: string, data: any) {
  return post(`/projects/${projectId}/budget/${lineId}/revise`, data)
}
export function getWipSchedule(projectId: string) {
  return get(`/projects/${projectId}/budget/wip`)
}

// --- Costs ---
export function getCostTransactions(projectId: string, category?: string) {
  const params: any = {}
  if (category) params.category = category
  return get(`/projects/${projectId}/costs`, params)
}
export function postCost(projectId: string, data: any) {
  return post(`/projects/${projectId}/costs`, data)
}
export function getCostSummary(projectId: string) {
  return get(`/projects/${projectId}/costs/summary`)
}

// --- Change Orders ---
export function getChangeOrders(projectId: string) {
  return get(`/projects/${projectId}/change-orders`)
}
export function createChangeOrder(projectId: string, data: any) {
  return post(`/projects/${projectId}/change-orders`, data)
}
export function submitChangeOrder(projectId: string, coId: string) {
  return post(`/projects/${projectId}/change-orders/${coId}/submit`)
}
export function approveChangeOrder(projectId: string, coId: string, data?: any) {
  return post(`/projects/${projectId}/change-orders/${coId}/approve`, data ?? {})
}
export function rejectChangeOrder(projectId: string, coId: string, data?: any) {
  return post(`/projects/${projectId}/change-orders/${coId}/reject`, data ?? {})
}
export function executeChangeOrder(projectId: string, coId: string) {
  return post(`/projects/${projectId}/change-orders/${coId}/execute`)
}

// --- Billing ---
export function getContractLines(projectId: string) {
  return get(`/projects/${projectId}/billing/contracts`)
}
export function addContractLine(projectId: string, data: any) {
  return post(`/projects/${projectId}/billing/contracts`, data)
}
export function getBillingSchedule(projectId: string) {
  return get(`/projects/${projectId}/billing/schedule`)
}
export function addBillingSchedule(projectId: string, data: any) {
  return post(`/projects/${projectId}/billing/schedule`, data)
}
export function generateInvoice(projectId: string, data?: any) {
  return post(`/projects/${projectId}/billing/generate-invoice`, data ?? {})
}

// --- Analysis (WIP / Forecast / Profitability / Reports) ---
export function getWipScheduleAnalysis(projectId: string) {
  return get(`/projects/${projectId}/analysis/wip`)
}
export function getForecast(projectId: string) {
  return get(`/projects/${projectId}/analysis/forecast`)
}
export function getProfitability(projectId: string) {
  return get(`/projects/${projectId}/analysis/profitability`)
}
export function getBudgetVsActual(projectId: string) {
  return get(`/projects/${projectId}/analysis/budget-vs-actual`)
}
export function getUnbilled(projectId: string) {
  return get(`/projects/${projectId}/analysis/unbilled`)
}
export function getChangeOrderSummary(projectId: string) {
  return get(`/projects/${projectId}/analysis/change-orders`)
}
export function getCostDetail(projectId: string) {
  return get(`/projects/${projectId}/analysis/cost-detail`)
}

// --- Portfolio analysis reports (Phase 10 long-tail reports) ---
export function getEmployeeUtilizationReport(params?: { from?: string; to?: string; capacityHours?: number }) {
  const query: Record<string, string | number> = { companyId: defaultCompanyId() }
  if (params?.from) query.from = params.from
  if (params?.to) query.to = params.to
  if (params?.capacityHours != null) query.capacityHours = params.capacityHours
  return get<EmployeeUtilizationRow[]>('/projects/analysis/employee-utilization', query)
}

export function getEmployeeProfitabilityReport(params?: { from?: string; to?: string }) {
  const query: Record<string, string | number> = { companyId: defaultCompanyId() }
  if (params?.from) query.from = params.from
  if (params?.to) query.to = params.to
  return get<EmployeeProfitabilityRow[]>('/projects/analysis/employee-profitability', query)
}

export function getSubcontractStatusReport(projectId?: string) {
  const query: Record<string, string | number> = { companyId: defaultCompanyId() }
  if (projectId) query.projectId = projectId
  return get<SubcontractStatusRow[]>('/projects/analysis/subcontract-status', query)
}

export function getSubcontractCommitmentReport() {
  return get<SubcontractCommitmentRow[]>('/projects/analysis/subcontract-commitment', {
    companyId: defaultCompanyId(),
  })
}

export function getCertifiedPayrollReport(params?: { from?: string; to?: string }) {
  const query: Record<string, string | number> = { companyId: defaultCompanyId() }
  if (params?.from) query.from = params.from
  if (params?.to) query.to = params.to
  return get<CertifiedPayrollRow[]>('/projects/analysis/certified-payroll', query)
}

export function getPortfolioDashboard() {
  return get<PortfolioDashboardRow[]>('/projects/analysis/portfolio-dashboard', {
    companyId: defaultCompanyId(),
  })
}

export function getProjectAgingReport() {
  return get<ProjectAgingRow[]>('/projects/analysis/project-aging', { companyId: defaultCompanyId() })
}

export function getContractValueAnalysisReport() {
  return get<ContractValueAnalysisRow[]>('/projects/analysis/contract-value-analysis', {
    companyId: defaultCompanyId(),
  })
}

export function getPmPerformanceReport() {
  return get<PmPerformanceRow[]>('/projects/analysis/pm-performance', { companyId: defaultCompanyId() })
}

export function getEarnedValueReport() {
  return get<EarnedValueRow[]>('/projects/analysis/earned-value', { companyId: defaultCompanyId() })
}

export function getPendingCoImpactReport() {
  return get<PendingCoImpactRow[]>('/projects/analysis/pending-co-impact', { companyId: defaultCompanyId() })
}

export function getLienWaiverRegisterReport() {
  return get<LienWaiverRegisterRow[]>('/projects/analysis/lien-waiver-register', {
    companyId: defaultCompanyId(),
  })
}

export function getContractAssetLiabilityReport() {
  return get<ContractAssetLiabilityRow[]>('/projects/analysis/contract-asset-liability', {
    companyId: defaultCompanyId(),
  })
}

// --- ASC 606 revenue recognition (Phase 10) ---
export function getAsc606Obligations(projectId: string) {
  return get<PerformanceObligation[]>(`/projects/${projectId}/asc606`)
}

export function createAsc606Obligation(
  projectId: string,
  data: { description: string; transactionPriceAllocated: number; standaloneSellingPriceBasis?: string },
) {
  return post<string>(`/projects/${projectId}/asc606`, data)
}

export function updateAsc606Obligation(
  projectId: string,
  obligationId: string,
  data: { description?: string; transactionPriceAllocated?: number; standaloneSellingPriceBasis?: string },
) {
  return put(`/projects/${projectId}/asc606/${obligationId}`, data)
}

export function deleteAsc606Obligation(projectId: string, obligationId: string) {
  return del(`/projects/${projectId}/asc606/${obligationId}`)
}

export function allocateAsc606ContractPrice(projectId: string, totalContractPrice: number) {
  return post<Asc606AllocateResult>(`/projects/${projectId}/asc606/allocate`, { totalContractPrice })
}

export function getAsc606RecognitionStatus(projectId: string) {
  return get<Asc606RecognitionStatus>(`/projects/${projectId}/asc606/recognition-status`)
}

export function recognizeAsc606Revenue(
  projectId: string,
  obligationId: string,
  data: { amount: number; asOf?: string },
) {
  return post<Asc606RecognizeResult>(`/projects/${projectId}/asc606/${obligationId}/recognize`, data)
}

export function getAsc606FiveStepSummary(projectId: string) {
  return get<FiveStepSummary>(`/projects/${projectId}/asc606/five-step-summary`)
}

// --- Project documents (attachments) ---
export function getProjectDocuments(projectId: string) {
  return get<ProjectDocumentItem[]>(`/projects/${projectId}/documents`)
}

export function addProjectDocument(
  projectId: string,
  data: { name: string; documentType: string; fileReference: string; contentType?: string; sizeBytes?: number },
) {
  return post<string>(`/projects/${projectId}/documents`, data)
}
export function uploadProjectDocument(projectId: string, file: File, documentType = 'General') {
  const fd = new FormData()
  fd.append('file', file)
  return fetch(`/api/v1/projects/${projectId}/documents/upload?documentType=${encodeURIComponent(documentType)}`, { method: 'POST', body: fd, headers: { Authorization: `Bearer ${useAuthStore.getState().accessToken}` } }).then(async r => { if (!r.ok) throw new Error(await r.text()); return r.json() })
}

export function deleteProjectDocument(projectId: string, documentId: string) {
  return del(`/projects/${projectId}/documents/${documentId}`)
}

// --- Profit fade / EAC trend snapshots ---
export function getEacTrend(projectId: string) {
  return get<EacTrendPoint[]>('/projects/analysis/eac-trend', { projectId })
}

export function getEacTrendPortfolio() {
  return get<EacPortfolioPoint[]>('/projects/analysis/eac-trend/portfolio', {
    companyId: defaultCompanyId(),
  })
}
