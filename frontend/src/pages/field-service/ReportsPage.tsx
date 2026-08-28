import { useQuery } from '@tanstack/react-query'
import { getSlaCompliance, getTechnicianUtilization, getOpenAging, getContractStatus, getPmDue, getFirstTimeFix, getRevenueProfitability, getWarrantyExpiration, getPartsUsage, getTravelExpense, getWorkOrderStatusReport } from '@api/fieldService'
import { DataTable } from '@components/ui/DataTable'

export function ReportsPage() {
  const sla = useQuery({ queryKey: ['fs-sla'], queryFn: () => getSlaCompliance() })
  const util = useQuery({ queryKey: ['fs-util'], queryFn: () => getTechnicianUtilization() })
  const aging = useQuery({ queryKey: ['fs-aging'], queryFn: () => getOpenAging() })
  const contracts = useQuery({ queryKey: ['fs-contracts-rpt'], queryFn: () => getContractStatus() })
  const pm = useQuery({ queryKey: ['fs-pm-due'], queryFn: () => getPmDue() })
  const ftf = useQuery({ queryKey: ['fs-ftf'], queryFn: () => getFirstTimeFix() })
  const rev = useQuery({ queryKey: ['fs-rev'], queryFn: () => getRevenueProfitability() })
  const warranty = useQuery({ queryKey: ['fs-warranty'], queryFn: () => getWarrantyExpiration() })
  const parts = useQuery({ queryKey: ['fs-parts'], queryFn: () => getPartsUsage() })
  const travel = useQuery({ queryKey: ['fs-travel'], queryFn: () => getTravelExpense() })
  const status = useQuery({ queryKey: ['fs-status'], queryFn: () => getWorkOrderStatusReport() })

  const s = (d: any) => (d?.data ?? d ?? [])
  const ftfRow = (ftf.data as any)?.data ?? ftf.data

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-bold">Field Service Reports</h2>
      {ftfRow && <div className="rounded border p-4 bg-white">First-Time Fix: {ftfRow.ratePercent ?? ftfRow.RatePercent ?? 0}% ({ftfRow.firstTimeFixed ?? ftfRow.FirstTimeFixed ?? 0}/{ftfRow.totalCompleted ?? ftfRow.TotalCompleted ?? 0})</div>}
      <ReportSection title="SLA Compliance" data={s(sla.data)} cols={[{ key: 'priority', header: 'Priority' }, { key: 'total', header: 'Total' }, { key: 'breached', header: 'Breached' }]} />
      <ReportSection title="Technician Utilization" data={s(util.data)} cols={[{ key: 'technicianId', header: 'Technician' }, { key: 'workOrders', header: 'WOs' }, { key: 'laborHours', header: 'Hours' }, { key: 'billableTotal', header: 'Billable' }]} />
      <ReportSection title="Work Order Status" data={s(status.data)} cols={[{ key: 'status', header: 'Status' }, { key: 'count', header: 'Count' }, { key: 'totalBillable', header: 'Billable' }]} />
      <ReportSection title="Revenue / Profitability" data={s(rev.data)} cols={[{ key: 'technicianId', header: 'Tech' }, { key: 'revenue', header: 'Revenue' }, { key: 'cost', header: 'Cost' }, { key: 'profit', header: 'Profit' }]} />
      <ReportSection title="Open Aging" data={s(aging.data)} cols={[{ key: 'workOrderNumber', header: 'WO' }, { key: 'status', header: 'Status' }, { key: 'ageDays', header: 'Age Days' }]} />
      <ReportSection title="Warranty Expiration (90d)" data={s(warranty.data)} cols={[{ key: 'assetTag', header: 'Asset' }, { key: 'warrantyEnd', header: 'Expires' }, { key: 'daysRemaining', header: 'Days Left' }]} />
      <ReportSection title="Parts Usage" data={s(parts.data)} cols={[{ key: 'itemId', header: 'Item' }, { key: 'totalQuantity', header: 'Qty' }, { key: 'totalCost', header: 'Cost' }]} />
      <ReportSection title="Travel / Expense" data={s(travel.data)} cols={[{ key: 'workOrderNumber', header: 'WO' }, { key: 'travelCost', header: 'Travel' }, { key: 'expenseTotal', header: 'Expenses' }]} />
      <ReportSection title="Contract Status" data={s(contracts.data)} cols={[{ key: 'contractNumber', header: 'Contract' }, { key: 'status', header: 'Status' }]} />
      <ReportSection title="PM Due" data={s(pm.data)} cols={[{ key: 'code', header: 'Code' }, { key: 'nextDue', header: 'Due' }]} />
    </div>
  )
}

function ReportSection({ title, data, cols }: any) {
  if (!data?.length) return <div className="rounded border p-4"><h3 className="font-semibold mb-2">{title}</h3><p className="text-sm text-gray-500">No data.</p></div>
  return <div className="rounded border p-4 bg-white"><h3 className="font-semibold mb-2">{title}</h3><DataTable data={data} columns={cols as any} /></div>
}
