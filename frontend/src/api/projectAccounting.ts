// Project Accounting module API client (Phase 10).
import { useAuthStore } from '@stores/authStore'
import { get, post, put, del } from './client'
import { companyId as defaultCompanyId } from './inventory'

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
