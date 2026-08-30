import { currentCompanyId } from '@/api/company'
import { useState, useEffect, useCallback } from 'react'
import { Building2, DollarSign, Users, Package, CreditCard, TrendingUp, AlertTriangle, CheckCircle, Clock, FileText } from 'lucide-react'
import { formatNumber } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge, StatusBadge, AmountBadge } from '@components/ui/Badge'
import { Button } from '@components/ui/Button'
import { LoadingOverlay } from '@components/ui/LoadingSpinner'
import { useAuth } from '@stores/authStore'
import { useAuthStore } from '@stores/authStore'

async function apiFetch<T>(url: string): Promise<T | null> {
  try {
    const token = useAuthStore.getState().accessToken
    const headers: Record<string, string> = {}
    if (token) headers['Authorization'] = `Bearer ${token}`
    const res = await fetch(url, { headers })
    if (!res.ok) return null
    const json = await res.json()
    return json.data ?? json
  } catch {
    return null
  }
}

interface Company { id: string; name: string; isActive: boolean }
interface FiscalPeriod { id: string; periodName: string; startDate: string; endDate: string; isClosed: boolean; periodStatus: string }
interface JournalBatch { id: string; batchNumber: string; status: string; totalDebit: number; totalCredit: number; description: string; createdOn: string }
interface BankAccount { id: string; name: string; accountNumber: string; currentBalance: number; currencyCode: string; isActive: boolean }
interface Vendor { id: string; name: string; vendorNumber: string; isActive: boolean }
interface Customer { id: string; name: string; customerNumber: string; isActive: boolean }
interface Item { id: string; itemNumber: string; description: string; isActive: boolean }
interface Project { id: string; projectNumber: string; name: string; status: string; originalBudget: number; actualCost: number }
interface Employee { id: string; employeeNumber: string; firstName: string; lastName: string; isActive: boolean }
interface WorkOrder { id: string; workOrderNumber: string; description: string; status: string; priority: string; scheduledDate: string }
interface AuditLog { id: string; action: string; entityType: string; entityId: string; performedBy: string; performedOn: string; details: string }

export function DashboardPage() {
  const { user, currentCompany, currentPeriod, isLoading: authLoading } = useAuth()

  const [companies, setCompanies] = useState<Company[]>([])
  const [periods, setPeriods] = useState<FiscalPeriod[]>([])
  const [batches, setBatches] = useState<JournalBatch[]>([])
  const [bankAccounts, setBankAccounts] = useState<BankAccount[]>([])
  const [vendors, setVendors] = useState<Vendor[]>([])
  const [customers, setCustomers] = useState<Customer[]>([])
  const [items, setItems] = useState<Item[]>([])
  const [projects, setProjects] = useState<Project[]>([])
  const [employees, setEmployees] = useState<Employee[]>([])
  const [workOrders, setWorkOrders] = useState<WorkOrder[]>([])
  const [auditLogs, setAuditLogs] = useState<AuditLog[]>([])
  const [loading, setLoading] = useState(true)

  const fetchAll = useCallback(async () => {
    setLoading(true)
    const companyId = useAuthStore.getState().currentCompany?.id ?? currentCompanyId()
    const [c, p, b, ba, v, cu, it, pr, em, wo, al] = await Promise.all([
      apiFetch<{ data: Company[] } | Company[]>('/api/v1/platform/companies'),
      apiFetch<{ data: FiscalPeriod[] } | FiscalPeriod[]>('/api/v1/platform/fiscal-periods'),
      apiFetch<{ data: JournalBatch[] } | JournalBatch[]>(`/api/v1/gl/journal-batches?companyId=${companyId}`),
      apiFetch<{ data: BankAccount[] } | BankAccount[]>('/api/v1/cash/bank-accounts'),
      apiFetch<{ data: Vendor[] } | Vendor[]>(`/api/v1/ap/vendors?companyId=${companyId}`),
      apiFetch<{ data: Customer[] } | Customer[]>(`/api/v1/ar/customers?companyId=${companyId}`),
      apiFetch<{ data: Item[] } | Item[]>(`/api/v1/inventory/items?companyId=${companyId}`),
      apiFetch<{ data: Project[] } | Project[]>('/api/v1/projects'),
      apiFetch<{ data: Employee[] } | Employee[]>('/api/v1/payroll/employees'),
      apiFetch<{ data: WorkOrder[] } | WorkOrder[]>('/api/v1/field-service/work-orders'),
      apiFetch<{ data: AuditLog[] } | AuditLog[]>('/api/v1/platform/audit-logs'),
    ])

    const extract = <T,>(raw: { data: T[] } | T[] | null): T[] => {
      if (!raw) return []
      return Array.isArray(raw) ? raw : (raw as { data: T[] }).data ?? []
    }

    setCompanies(extract(c))
    setPeriods(extract(p))
    setBatches(extract(b))
    setBankAccounts(extract(ba))
    setVendors(extract(v))
    setCustomers(extract(cu))
    setItems(extract(it))
    setProjects(extract(pr))
    setEmployees(extract(em))
    setWorkOrders(extract(wo))
    setAuditLogs(extract(al))
    setLoading(false)
  }, [])

  useEffect(() => { fetchAll() }, [fetchAll])

  // Computed metrics
  const activeCompanies = companies.filter(c => c.isActive).length
  const activeVendors = vendors.filter(v => v.isActive).length
  const activeCustomers = customers.filter(c => c.isActive).length
  const activeItems = items.filter(i => i.isActive).length
  const activeEmployees = employees.filter(e => e.isActive).length
  const totalCashBalance = bankAccounts.reduce((sum, ba) => sum + (ba.currentBalance || 0), 0)
  const pendingBatches = batches.filter(b => b.status === 'Draft' || b.status === 'PendingApproval').length
  const postedBatches = batches.filter(b => b.status === 'Posted').length
  const openProjects = projects.filter(p => p.status === 'Active' || p.status === 'InProgress').length
  const totalBudget = projects.reduce((sum, p) => sum + (p.originalBudget || 0), 0)
  const totalActualCost = projects.reduce((sum, p) => sum + (p.actualCost || 0), 0)
  const activeWorkOrders = workOrders.filter(wo => wo.status === 'Open' || wo.status === 'InProgress').length
  const overdueWorkOrders = workOrders.filter(wo => wo.status === 'Overdue').length
  const currentFiscalPeriod = periods.find(p => !p.isClosed) || periods[0]

  // Stats cards from real data
  const stats = [
    {
      key: 'companies', label: 'Active Companies', value: activeCompanies,
      icon: Building2, color: 'text-primary-600 bg-primary-100 dark:bg-primary-900/30',
    },
    {
      key: 'cash', label: 'Total Cash Balance', value: totalCashBalance, isCurrency: true,
      icon: DollarSign, color: 'text-emerald-600 bg-emerald-100 dark:bg-emerald-900/30',
    },
    {
      key: 'employees', label: 'Active Employees', value: activeEmployees,
      icon: Users, color: 'text-blue-600 bg-blue-100 dark:bg-blue-900/30',
    },
    {
      key: 'inventory', label: 'Inventory Items', value: activeItems,
      icon: Package, color: 'text-amber-600 bg-amber-100 dark:bg-amber-900/30',
    },
    {
      key: 'vendors', label: 'Active Vendors', value: activeVendors,
      icon: CreditCard, color: 'text-rose-600 bg-rose-100 dark:bg-rose-900/30',
    },
    {
      key: 'customers', label: 'Active Customers', value: activeCustomers,
      icon: TrendingUp, color: 'text-cyan-600 bg-cyan-100 dark:bg-cyan-900/30',
    },
  ]

  // Recent activity from audit logs
  const recentActivity = auditLogs.slice(0, 8).map((log, i) => ({
    id: i,
    action: log.action,
    module: log.entityType?.split('.')[0] || 'System',
    description: `${log.action} ${log.entityType || ''} ${log.entityId ? `(${log.entityId.substring(0, 8)}...)` : ''}`,
    performedBy: log.performedBy || 'System',
    time: log.performedOn ? new Date(log.performedOn).toLocaleString() : '',
    details: log.details || '',
  }))

  // Alerts from real data
  const alerts: Array<{ id: number; type: string; message: string; action: string; href: string }> = []
  let alertId = 1
  if (pendingBatches > 0) alerts.push({ id: alertId++, type: 'warning', message: `${pendingBatches} GL journal batches pending review`, action: 'View Batches', href: '/gl/journal-batches' })
  if (overdueWorkOrders > 0) alerts.push({ id: alertId++, type: 'warning', message: `${overdueWorkOrders} field service work orders overdue`, action: 'View Work Orders', href: '/field-service/work-orders' })
  if (totalBudget > 0 && totalActualCost > totalBudget) alerts.push({ id: alertId++, type: 'warning', message: `Project costs ($${(totalActualCost / 1000).toFixed(0)}K) exceed total budget ($${(totalBudget / 1000).toFixed(0)}K)`, action: 'View Projects', href: '/projects' })
  if (activeWorkOrders > 0) alerts.push({ id: alertId++, type: 'info', message: `${activeWorkOrders} active field service work orders`, action: 'View Dispatch', href: '/field-service/dispatch' })
  if (alerts.length === 0) alerts.push({ id: alertId++, type: 'success', message: 'All systems operational', action: 'View Reports', href: '/reporting/catalog' })

  // Quick actions
  const quickActions = [
    { label: 'Journal Batches', href: '/gl/journal-batches', primary: true },
    { label: 'AP Vouchers', href: '/ap/voucher-batches' },
    { label: 'AR Invoices', href: '/ar/invoice-batches' },
    { label: 'Purchase Orders', href: '/purchasing/purchase-orders' },
    { label: 'Timesheets', href: '/payroll/timesheets' },
    { label: 'Projects', href: '/projects' },
  ]

  return (
    <LoadingOverlay isLoading={loading || authLoading} message="Loading dashboard...">
      <div className="space-y-6">
        {/* Welcome Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
              Welcome back, {user?.firstName || 'User'}!
            </h1>
            <p className="text-gray-500 dark:text-gray-400 mt-1">
              {currentCompany?.name || (companies.length > 0 ? companies[0].name : 'No company')} • Period: {currentFiscalPeriod?.periodName || currentPeriod?.periodName || 'Not selected'}
            </p>
          </div>
          <div className="flex gap-3">
            <Button variant="secondary" size="sm">
              <TrendingUp className="h-4 w-4 mr-2" />
              Reports
            </Button>
          </div>
        </div>

        {/* Alerts */}
        {alerts.length > 0 && (
          <div className="space-y-2" role="region" aria-label="System alerts">
            {alerts.map(alert => (
              <div
                key={alert.id}
                className={
                  'flex items-center justify-between p-4 rounded-lg border ' +
                  (alert.type === 'warning' ? 'bg-amber-50 border-amber-200 dark:bg-amber-900/20 dark:border-amber-800' :
                    alert.type === 'info' ? 'bg-blue-50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' :
                    'bg-emerald-50 border-emerald-200 dark:bg-emerald-900/20 dark:border-emerald-800')
                }
                role="alert"
              >
                <div className="flex items-center gap-3">
                  {alert.type === 'warning' && <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400" />}
                  {alert.type === 'info' && <Building2 className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
                  {alert.type === 'success' && <CheckCircle className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />}
                  <span className="text-sm text-gray-700 dark:text-gray-300">{alert.message}</span>
                </div>
                <Button variant="ghost" size="sm" asChild>
                  <a href={alert.href}>{alert.action}</a>
                </Button>
              </div>
            ))}
          </div>
        )}

        {/* Key Metrics */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
          {stats.map(stat => (
            <Card key={stat.key} className="bg-white dark:bg-gray-800">
              <CardContent className="p-6">
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{stat.label}</p>
                    <div className="mt-2">
                      {stat.isCurrency ? (
                        <AmountBadge amount={stat.value} currency="USD" size="lg" />
                      ) : (
                        <span className="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                          {formatNumber(stat.value)}
                        </span>
                      )}
                    </div>
                  </div>
                  <div className={'p-3 rounded-xl flex-shrink-0 ' + stat.color}>
                    <stat.icon className="h-6 w-6" aria-hidden="true" />
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Quick Actions */}
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          {quickActions.map(action => (
            <Button
              key={action.label}
              variant={action.primary ? 'primary' : 'outline'}
              size="sm"
              className="h-20 flex-col gap-1 justify-center"
              asChild
            >
              <a href={action.href}>
                <FileText className="h-5 w-5 text-gray-500" />
                <span className="text-sm font-medium text-center">{action.label}</span>
              </a>
            </Button>
          ))}
        </div>

        {/* Main Content Grid */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Recent Activity */}
          <Card className="lg:col-span-2">
            <CardHeader
              title="Recent Activity"
              description="Latest audit log entries"
              action={
                <Button variant="ghost" size="sm" asChild>
                  <a href="/platform/audit-logs">View All</a>
                </Button>
              }
            />
            <CardContent className="p-0">
              {recentActivity.length === 0 ? (
                <div className="p-8 text-center text-gray-500">
                  <Clock className="h-8 w-8 mx-auto mb-2 text-gray-300" />
                  <p className="text-sm">No recent activity yet</p>
                </div>
              ) : (
                <div className="divide-y divide-gray-200 dark:divide-gray-700">
                  {recentActivity.map(activity => (
                    <div
                      key={activity.id}
                      className="flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-10 h-10 rounded-lg flex items-center justify-center bg-gray-100 dark:bg-gray-800 text-gray-600">
                          <FileText className="h-5 w-5" />
                        </div>
                        <div>
                          <p className="text-sm font-medium text-gray-900 dark:text-white">{activity.description}</p>
                          <p className="text-xs text-gray-500 dark:text-gray-400 flex items-center gap-2">
                            <Badge variant="neutral" size="sm">{activity.action}</Badge>
                            <span>{activity.time}</span>
                            {activity.performedBy && <span>by {activity.performedBy}</span>}
                          </p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Side Panel */}
          <div className="space-y-6">
            {/* Period Status */}
            <Card>
              <CardHeader title="Period Status" />
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {currentFiscalPeriod?.periodName || 'No open period'}
                    </p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {currentFiscalPeriod?.startDate && currentFiscalPeriod?.endDate
                        ? `${new Date(currentFiscalPeriod.startDate).toLocaleDateString()} - ${new Date(currentFiscalPeriod.endDate).toLocaleDateString()}`
                        : 'Period dates not available'}
                    </p>
                  </div>
                  <StatusBadge status={currentFiscalPeriod?.isClosed ? 'posted' : 'active'} />
                </div>
                <div className="pt-4 border-t border-gray-200 dark:border-gray-700 space-y-3">
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">GL Batches</span>
                    <div className="flex items-center gap-2">
                      <Badge variant="success" size="sm">{postedBatches} posted</Badge>
                      {pendingBatches > 0 && <Badge variant="warning" size="sm">{pendingBatches} pending</Badge>}
                    </div>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">Open Projects</span>
                    <Badge variant="info" size="sm">{openProjects}</Badge>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-600 dark:text-gray-400">Work Orders</span>
                    <div className="flex items-center gap-2">
                      <Badge variant="success" size="sm">{activeWorkOrders} active</Badge>
                      {overdueWorkOrders > 0 && <Badge variant="error" size="sm">{overdueWorkOrders} overdue</Badge>}
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Cash Position */}
            <Card>
              <CardHeader title="Cash Position" />
              <CardContent className="space-y-3">
                {bankAccounts.length === 0 ? (
                  <p className="text-sm text-gray-500 text-center py-4">No bank accounts configured</p>
                ) : (
                  <>
                    {bankAccounts.slice(0, 4).map(ba => (
                      <div key={ba.id} className="flex items-center justify-between">
                        <span className="text-sm text-gray-600 dark:text-gray-400 truncate">{ba.name}</span>
                        <AmountBadge amount={ba.currentBalance} currency={ba.currencyCode || 'USD'} size="sm" />
                      </div>
                    ))}
                    <div className="pt-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between text-sm font-medium">
                      <span className="text-gray-900 dark:text-white">Total Cash</span>
                      <AmountBadge amount={totalCashBalance} currency="USD" size="sm" />
                    </div>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Upcoming Work Orders */}
            <Card>
              <CardHeader
                title="Active Work Orders"
                action={
                  <Button variant="ghost" size="sm" asChild>
                    <a href="/field-service/work-orders">View All</a>
                  </Button>
                }
              />
              <CardContent className="space-y-3">
                {workOrders.filter(wo => wo.status !== 'Completed' && wo.status !== 'Cancelled').slice(0, 4).length === 0 ? (
                  <p className="text-sm text-gray-500 text-center py-4">No active work orders</p>
                ) : (
                  workOrders.filter(wo => wo.status !== 'Completed' && wo.status !== 'Cancelled').slice(0, 4).map(wo => (
                    <div key={wo.id} className="flex items-center gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/50">
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{wo.workOrderNumber} - {wo.description}</p>
                        <div className="flex items-center gap-2 mt-1">
                          <Badge
                            variant={wo.priority === 'High' || wo.priority === 'Critical' ? 'error' : wo.priority === 'Medium' ? 'warning' : 'info'}
                            size="sm"
                          >
                            {wo.priority}
                          </Badge>
                          <span className="text-xs text-gray-500">{wo.status}</span>
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </LoadingOverlay>
  )
}
