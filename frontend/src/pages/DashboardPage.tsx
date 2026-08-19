import { useState } from 'react'
import { Building2, DollarSign, Users, Package, CreditCard, TrendingUp, AlertTriangle, CheckCircle } from 'lucide-react'
import { formatNumber } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge, StatusBadge, AmountBadge } from '@components/ui/Badge'
import { Button } from '@components/ui/Button'
import { LoadingOverlay } from '@components/ui/LoadingSpinner'
import { useAuth } from '@stores/authStore'

const stats = [
  {
    key: 'companies',
    label: 'Active Companies',
    value: 3,
    icon: Building2,
    color: 'text-primary-600 bg-primary-100 dark:bg-primary-900/30',
    trend: { value: '+1', label: 'this month', positive: true },
  },
  {
    key: 'revenue',
    label: 'Total Revenue (YTD)',
    value: 2847500,
    icon: DollarSign,
    color: 'text-emerald-600 bg-emerald-100 dark:bg-emerald-900/30',
    isCurrency: true,
    trend: { value: '+12.5%', label: 'vs last year', positive: true },
  },
  {
    key: 'users',
    label: 'Active Users',
    value: 127,
    icon: Users,
    color: 'text-blue-600 bg-blue-100 dark:bg-blue-900/30',
    trend: { value: '+5', label: 'new this week', positive: true },
  },
  {
    key: 'inventory',
    label: 'Inventory Value',
    value: 892300,
    icon: Package,
    color: 'text-amber-600 bg-amber-100 dark:bg-amber-900/30',
    isCurrency: true,
    trend: { value: '-2.3%', label: 'vs last month', positive: false },
  },
  {
    key: 'ap',
    label: 'Outstanding Payables',
    value: 156800,
    icon: CreditCard,
    color: 'text-red-600 bg-red-100 dark:bg-red-900/30',
    isCurrency: true,
    trend: { value: '+8', label: 'overdue', positive: false },
  },
  {
    key: 'ar',
    label: 'Outstanding Receivables',
    value: 342100,
    icon: TrendingUp,
    color: 'text-emerald-600 bg-emerald-100 dark:bg-emerald-900/30',
    isCurrency: true,
    trend: { value: '-15', label: 'collected today', positive: true },
  },
]

const recentActivity = [
  { id: 1, type: 'posted', module: 'GL', description: 'Journal Batch GL-2026-0045 posted', amount: 12500, time: '2 min ago', status: 'posted' },
  { id: 2, type: 'approved', module: 'AP', description: 'Voucher batch VCH-2026-0123 approved for payment', amount: 45600, time: '15 min ago', status: 'approved' },
  { id: 3, type: 'received', module: 'AR', description: 'Cash receipt CR-2026-0456 applied to invoices', amount: 28900, time: '1 hour ago', status: 'posted' },
  { id: 4, type: 'shipped', module: 'OM', description: 'Sales order SO-2026-0789 shipped complete', amount: 15750, time: '3 hours ago', status: 'completed' },
  { id: 5, type: 'received', module: 'INV', description: 'PO receipt RCV-2026-0234 received into warehouse', amount: 8900, time: '5 hours ago', status: 'posted' },
  { id: 6, type: 'created', module: 'PRJ', description: 'Project PRJ-2026-001 created with budget $500K', amount: 500000, time: '1 day ago', status: 'draft' },
]

const alerts = [
  { id: 1, type: 'warning', message: '3 vouchers over 30 days past due', action: 'View AP Aging', href: '/ap/aging' },
  { id: 2, type: 'info', message: 'Period close checklist 80% complete', action: 'View Checklist', href: '/platform/period-close' },
  { id: 3, type: 'warning', message: '5 inventory items below reorder point', action: 'View Reorder Report', href: '/inventory/reorder' },
  { id: 4, type: 'success', message: 'Monthly financial statements generated', action: 'View Reports', href: '/reporting/financial' },
]

const quickActions = [
  { label: 'Create Journal Entry', href: '/gl/batch/new', icon: 'plus', primary: true },
  { label: 'Enter Vouchers', href: '/ap/voucher/new', icon: 'file-text' },
  { label: 'Create Invoice', href: '/ar/invoice/new', icon: 'file-text' },
  { label: 'New Purchase Order', href: '/purchasing/po/new', icon: 'package' },
  { label: 'Enter Timesheet', href: '/payroll/timesheet/new', icon: 'clock' },
  { label: 'Create Project', href: '/projects/new', icon: 'folder-plus' },
]

export function DashboardPage() {
  const { user, currentCompany, currentPeriod, isLoading } = useAuth()
  const [isLoaded] = useState(true)

  return (
    <LoadingOverlay isLoading={!isLoaded || isLoading} message="Loading dashboard...">
      <div className="space-y-6">
        {/* Welcome Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
              Welcome back, {user?.firstName || 'User'}!
            </h1>
            <p className="text-gray-500 dark:text-gray-400 mt-1">
              {currentCompany?.name} • Period: {currentPeriod?.periodName || 'Not selected'}
            </p>
          </div>
          <div className="flex gap-3">
            <Button variant="secondary" size="sm">
              <TrendingUp className="h-4 w-4 mr-2" />
              Run Reports
            </Button>
            <Button variant="primary" size="sm">
              <Building2 className="h-4 w-4 mr-2" />
              New Company
            </Button>
          </div>
        </div>

        {/* Alerts */}
        {alerts.length > 0 && (
          <div className="space-y-2" role="region" aria-label="System alerts">
            {alerts.map(alert => (
              <div
                key={alert.id}
                className={(
                  'flex items-center justify-between p-4 rounded-lg border ' +
                  (alert.type === 'warning' ? 'bg-amber-50 border-amber-200 dark:bg-amber-900/20 dark:border-amber-800' :
                    alert.type === 'info' ? 'bg-blue-50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' :
                    'bg-emerald-50 border-emerald-200 dark:bg-emerald-900/20 dark:border-emerald-800')
                )}
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
                    <div className="mt-2 flex items-baseline gap-2">
                      {stat.isCurrency ? (
                        <AmountBadge amount={stat.value} currency="USD" size="lg" />
                      ) : (
                        <span className="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                          {formatNumber(stat.value)}
                        </span>
                      )}
                      {stat.trend && (
                        <Badge
                          variant={stat.trend.positive ? 'success' : 'error'}
                          size="sm"
                          className="mt-1"
                        >
                          {stat.trend.value} {stat.trend.label}
                        </Badge>
                      )}
                    </div>
                  </div>
                  <div className={('p-3 rounded-xl flex-shrink-0 ' + stat.color)}>
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
              className="h-24 flex-col gap-2 justify-center py-4"
              asChild
            >
              <a href={action.href}>
                <div className="w-10 h-10 rounded-lg bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center">
                  <Building2 className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                </div>
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
              description="Latest transactions and system events"
              action={
                <Button variant="ghost" size="sm" asChild>
                  <a href="/reporting/activity">View All</a>
                </Button>
              }
            />
            <CardContent className="p-0">
              <div className="divide-y divide-gray-200 dark:divide-gray-700">
                {recentActivity.map(activity => (
                  <div
                    key={activity.id}
                    className="flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      <div className={(
                        'w-10 h-10 rounded-lg flex items-center justify-center ' +
                        (activity.status === 'posted' ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600' :
                          activity.status === 'approved' ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-600' :
                          activity.status === 'completed' ? 'bg-purple-100 dark:bg-purple-900/30 text-purple-600' :
                          'bg-gray-100 dark:bg-gray-800 text-gray-600')
                      )}>
                        {activity.module === 'GL' && <DollarSign className="h-5 w-5" />}
                        {activity.module === 'AP' && <CreditCard className="h-5 w-5" />}
                        {activity.module === 'AR' && <TrendingUp className="h-5 w-5" />}
                        {activity.module === 'OM' && <Package className="h-5 w-5" />}
                        {activity.module === 'INV' && <Package className="h-5 w-5" />}
                        {activity.module === 'PRJ' && <Building2 className="h-5 w-5" />}
                      </div>
                      <div>
                        <p className="text-sm font-medium text-gray-900 dark:text-white">{activity.description}</p>
                        <p className="text-xs text-gray-500 dark:text-gray-400 flex items-center gap-2">
                          <Badge variant="neutral" size="sm">{activity.module}</Badge>
                          <span>{activity.time}</span>
                        </p>
                      </div>
                    </div>
                    <AmountBadge amount={activity.amount} currency="USD" size="sm" isNegative={activity.amount < 0} />
                  </div>
                ))}
              </div>
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
                    <p className="text-sm font-medium text-gray-900 dark:text-white">{currentPeriod?.periodName || 'No period selected'}</p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {currentPeriod?.startDate && currentPeriod?.endDate
                        ? `${formatDate(currentPeriod.startDate)} - ${formatDate(currentPeriod.endDate)}`
                        : 'Period dates not available'}
                    </p>
                  </div>
                  <StatusBadge status={currentPeriod?.isClosed ? 'posted' : 'active'} />
                </div>
                <div className="pt-4 border-t border-gray-200 dark:border-gray-700 space-y-3">
                  {[
                    { label: 'GL Batches', posted: 12, pending: 3 },
                    { label: 'AP Vouchers', posted: 45, pending: 7 },
                    { label: 'AR Invoices', posted: 38, pending: 2 },
                    { label: 'Inventory Txns', posted: 156, pending: 0 },
                  ].map(item => (
                    <div key={item.label} className="flex items-center justify-between text-sm">
                      <span className="text-gray-600 dark:text-gray-400">{item.label}</span>
                      <div className="flex items-center gap-2">
                        <Badge variant="success" size="sm">{item.posted} posted</Badge>
                        {item.pending > 0 && <Badge variant="warning" size="sm">{item.pending} pending</Badge>}
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            {/* Cash Position */}
            <Card>
              <CardHeader title="Cash Position" />
              <CardContent className="space-y-3">
                {[
                  { label: 'Operating Account', balance: 485600, currency: 'USD' },
                  { label: 'Payroll Account', balance: 125000, currency: 'USD' },
                  { label: 'Tax Account', balance: 89200, currency: 'USD' },
                  { label: 'Foreign Currency (CAD)', balance: 56700, currency: 'CAD' },
                ].map(item => (
                  <div key={item.label} className="flex items-center justify-between">
                    <span className="text-sm text-gray-600 dark:text-gray-400">{item.label}</span>
                    <AmountBadge amount={item.balance} currency={item.currency} size="sm" />
                  </div>
                ))}
                <div className="pt-3 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between text-sm font-medium">
                  <span className="text-gray-900 dark:text-white">Total Cash</span>
                  <AmountBadge amount={756500} currency="USD" size="sm" />
                </div>
              </CardContent>
            </Card>

            {/* Upcoming Deadlines */}
            <Card>
              <CardHeader title="Upcoming Deadlines" />
              <CardContent className="space-y-3">
                {[
                  { date: 'Jul 31', label: 'Period Close - July 2026', type: 'critical' },
                  { date: 'Aug 05', label: 'Payroll Run - Biweekly', type: 'normal' },
                  { date: 'Aug 10', label: 'Sales Tax Return Due', type: 'warning' },
                  { date: 'Aug 15', label: '1099 Filing Preparation', type: 'normal' },
                ].map((item, index) => (
                  <div key={index} className="flex items-center gap-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800/50">
                    <div className="w-14 h-14 rounded-lg flex flex-col items-center justify-center bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700">
                      <span className="text-lg font-bold text-gray-900 dark:text-white">{item.date.split(' ')[1]}</span>
                      <span className="text-xs text-gray-500 dark:text-gray-400">{item.date.split(' ')[0]}</span>
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{item.label}</p>
                      <Badge variant={item.type === 'critical' ? 'error' : item.type === 'warning' ? 'warning' : 'info'} size="sm">
                        {item.type}
                      </Badge>
                    </div>
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </LoadingOverlay>
  )
}

function formatDate(dateString: string): string {
  return new Date(dateString).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}