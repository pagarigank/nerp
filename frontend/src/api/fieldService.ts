// Field Service Management module API client (Phase 12).
import { get, post } from './client'
import { companyId as defaultCompanyId } from './inventory'

const MOD = '/field-service'

// --- Work Orders ---
export function getWorkOrders(companyIdParam?: string) {
  return get<any[]>(`${MOD}/work-orders`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getWorkOrder(id: string, companyIdParam?: string) {
  return get<any>(`${MOD}/work-orders/${id}`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createWorkOrder(data: any) {
  return post(`${MOD}/work-orders`, data)
}
export function dispatchWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/dispatch`, data)
}
export function scheduleWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/schedule`, data)
}
export function clockInWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/clock-in`, data)
}
export function clockOutWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/clock-out`, data)
}
export function addWorkOrderLine(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/lines`, data)
}
export function completeWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/complete`, data)
}
export function closeWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/close`, data)
}
export function cancelWorkOrder(id: string, data: any) {
  return post(`${MOD}/work-orders/${id}/cancel`, data)
}

// --- Service Calls (intake) ---
export function getServiceCalls(companyIdParam?: string) {
  return get<any[]>(`${MOD}/service-calls`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createServiceCall(data: any) {
  return post(`${MOD}/service-calls`, data)
}

// --- Estimates ---
export function getEstimates(companyIdParam?: string) {
  return get<any[]>(`${MOD}/estimates`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createEstimate(data: any) {
  return post(`${MOD}/estimates`, data)
}
export function approveEstimate(id: string, data: any) {
  return post(`${MOD}/estimates/${id}/approve`, data)
}
export function convertEstimate(id: string, data: any) {
  return post(`${MOD}/estimates/${id}/convert`, data)
}

// --- Service Contracts ---
export function getServiceContracts(companyIdParam?: string) {
  return get<any[]>(`${MOD}/service-contracts`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createServiceContract(data: any) {
  return post(`${MOD}/service-contracts`, data)
}
export function activateServiceContract(id: string, data: any) {
  return post(`${MOD}/service-contracts/${id}/activate`, data)
}

// --- Equipment ---
export function getEquipment(companyIdParam?: string) {
  return get<any[]>(`${MOD}/equipment`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createEquipment(data: any) {
  return post(`${MOD}/equipment`, data)
}
export function setEquipmentWarranty(id: string, data: any) {
  return post(`${MOD}/equipment/${id}/warranty`, data)
}

// --- Technicians ---
export function getTechnicians(companyIdParam?: string) {
  return get<any[]>(`${MOD}/technicians`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createTechnician(data: any) {
  return post(`${MOD}/technicians`, data)
}

// --- Skills ---
export function getSkills(companyIdParam?: string) {
  return get<any[]>(`${MOD}/skills`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createSkill(data: any) {
  return post(`${MOD}/skills`, data)
}
export function assignSkill(technicianId: string, data: any) {
  return post(`${MOD}/technicians/${technicianId}/skills`, data)
}

// --- SLAs ---
export function getSlas(companyIdParam?: string) {
  return get<any[]>(`${MOD}/slas`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createSla(data: any) {
  return post(`${MOD}/slas`, data)
}

// --- Territories ---
export function getTerritories(companyIdParam?: string) {
  return get<any[]>(`${MOD}/territories`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createTerritory(data: any) {
  return post(`${MOD}/territories`, data)
}

// --- Rate Cards ---
export function getRateCards(companyIdParam?: string) {
  return get<any[]>(`${MOD}/rate-cards`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createRateCard(data: any) {
  return post(`${MOD}/rate-cards`, data)
}

// --- Preventive Maintenance ---
export function getPreventiveMaintenance(companyIdParam?: string) {
  return get<any[]>(`${MOD}/preventive-maintenance`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createPreventiveMaintenance(data: any) {
  return post(`${MOD}/preventive-maintenance`, data)
}

// --- Van Stock ---
export function getVanStock(companyIdParam?: string) {
  return get<any[]>(`${MOD}/van-stock`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createVanStock(data: any) {
  return post(`${MOD}/van-stock`, data)
}
export function issueVanStock(id: string, data: any) {
  return post(`${MOD}/van-stock/${id}/issue`, data)
}
export function receiveVanStock(id: string, data: any) {
  return post(`${MOD}/van-stock/${id}/receive`, data)
}

// --- Warranty Claims ---
export function getWarrantyClaims(companyIdParam?: string) {
  return get<any[]>(`${MOD}/warranty-claims`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function createWarrantyClaim(data: any) {
  return post(`${MOD}/warranty-claims`, data)
}

export function getDispatchSuggestions(workOrderId: string, companyIdParam?: string) {
  return get<any[]>(`${MOD}/dispatch/suggestions`, { companyId: companyIdParam ?? defaultCompanyId(), workOrderId })
}
export function getTechnicianAvailability(technicianId: string, start: string, end: string, companyIdParam?: string) {
  return get<any>(`${MOD}/technicians/${technicianId}/availability`, { companyId: companyIdParam ?? defaultCompanyId(), start, end })
}
export function createFollowUp(workOrderId: string, data: any) {
  return post(`${MOD}/work-orders/${workOrderId}/follow-up`, data)
}
export function addWorkOrderExpense(workOrderId: string, data: any) {
  return post(`${MOD}/work-orders/${workOrderId}/expenses`, data)
}

// --- Reports ---
export function getSlaCompliance(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/sla-compliance`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getTechnicianUtilization(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/technician-utilization`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getOpenAging(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/open-aging`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getContractStatus(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/contract-status`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getPmDue(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/pm-due`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getFirstTimeFix(companyIdParam?: string) {
  return get<any>(`${MOD}/reports/first-time-fix`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getRevenueProfitability(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/revenue-profitability`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getWarrantyExpiration(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/warranty-expiration`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getPartsUsage(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/parts-usage`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getTravelExpense(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/travel-expense`, { companyId: companyIdParam ?? defaultCompanyId() })
}
export function getWorkOrderStatusReport(companyIdParam?: string) {
  return get<any[]>(`${MOD}/reports/work-order-status`, { companyId: companyIdParam ?? defaultCompanyId() })
}
