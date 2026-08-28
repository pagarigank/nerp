// <copyright file="ExecutiveDashboardPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  DollarSign, TrendingUp, TrendingDown, Package, Users, AlertTriangle,
  CheckCircle, BarChart3, PieChart as PieChartIcon, RefreshCcw,
} from 'lucide-react'
import { formatCurrency, formatNumber } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge, StatusBadge } from '@components/ui/Badge'
import { Button } from '@components/ui/Button'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getExecutiveDashboard, type ExecutiveDashboardData } from '@api/reporting'
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend, LineChart, Line,
} from 'recharts'

const COLORS = ['#10b981', '#f59e0b', '#ef4444', '#6366f1', '#8b5cf6', '#06b6d4']

function StatCard({
  label, value, icon: Icon, color, trend, isCurrency,
}: {
  label: string; value: number; icon: any; color: string;
  trend?: { value: string; positive: boolean }; isCurrency?: boolean;
}) {
  return (
    <Card className="bg-white dark:bg-gray-800">
      <CardContent className="p-5">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-500 dark:text-gray-400 truncate">{label}</p>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-bold text-gray-900 dark:text-white tabular-nums">
                {isCurrency ? formatCurrency(value) : formatNumber(value)}
              </span>
              {trend && (
                <Badge variant={trend.positive ? 'success' : 'error'} size="sm">
                  {trend.value}
                </Badge>
              )}
            </div>
          </div>
          <div className={`p-3 rounded-xl flex-shrink-0 ${color}`}>
            <Icon className="h-5 w-5" aria-hidden="true" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function AgingBar({ label, current, d30, d60, d90, over90 }: {
  label: string; current: number; d30: number; d60: number; d90: number; over90: number;
}) {
  const total = current + d30 + d60 + d90 + over90
  if (total === 0) return (
    <div className="text-sm text-gray-500 dark:text-gray-400 py-4 text-center">No outstanding {label}.</div>
  )

  const data = [
    { name: 'Current', value: current, fill: '#10b981' },
    { name: '1-30 Days', value: d30, fill: '#f59e0b' },
    { name: '31-60 Days', value: d60, fill: '#f97316' },
    { name: '61-90 Days', value: d90, fill: '#ef4444' },
    { name: '90+ Days', value: over90, fill: '#dc2626' },
  ].filter(d => d.value > 0)

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{label}</span>
        <span className="text-sm font-semibold text-gray-900 dark:text-white tabular-nums">
          {formatCurrency(total)}
        </span>
      </div>
      <div className="h-32">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={data}
              cx="50%"
              cy="50%"
              innerRadius={30}
              outerRadius={55}
              paddingAngle={2}
              dataKey="value"
            >
              {data.map((entry, i) => (
                <Cell key={`cell-${i}`} fill={entry.fill} />
              ))}
            </Pie>
            <Tooltip formatter={(v: number) => formatCurrency(v)} />
            <Legend iconType="circle" iconSize={8} />
          </PieChart>
        </ResponsiveContainer>
      </div>
    </div>
  )
}

export function ExecutiveDashboardPage() {
  const [runId, setRunId] = useState(0)

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['reporting', 'executive-dashboard', runId],
    queryFn: getExecutiveDashboard,
    enabled: true,
    staleTime: 60_000,
  })

  const dash = data as ExecutiveDashboardData | undefined

  // Build chart data for project portfolio
  const projectChartData = dash?.projectPortfolio
    ? [
        { name: 'Budget', value: dash.projectPortfolio.totalBudget },
        { name: 'Costs', value: dash.projectPortfolio.totalCosts },
      ]
    : []

  // Build chart data for inventory value
  const inventoryChartData = dash?.cashPosition?.accounts?.map((a) => ({
    name: a.name.length > 15 ? a.name.slice(0, 15) + '…' : a.name,
    value: a.balance,
  })) ?? []

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Executive Dashboard</h1>
          <p className="text-gray-500 dark:text-gray-400 mt-1">
            Cross-module KPIs and financial overview
          </p>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => { refetch(); setRunId(id => id + 1) }}
          leftIcon={<RefreshCcw className="h-4 w-4" />}
        >
          Refresh
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            {[1, 2, 3, 4].map(i => <Skeleton key={i} className="h-28" />)}
          </div>
          <Skeleton className="h-64" />
          <Skeleton className="h-64" />
        </div>
      ) : error ? (
        <Card>
          <CardContent className="py-10 text-center">
            <p className="text-sm text-red-600 dark:text-red-400">
              {getErrorMessage(error)}
            </p>
            <Button variant="secondary" size="sm" className="mt-4" onClick={() => refetch()}>
              Retry
            </Button>
          </CardContent>
        </Card>
      ) : dash ? (
        <>
          {/* Key Metrics */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard
              label="Cash Position"
              value={dash.cashPosition.totalCash}
              icon={DollarSign}
              color="bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600"
              isCurrency
            />
            <StatCard
              label="Accounts Receivable"
              value={dash.arAging.totalOutstanding}
              icon={TrendingUp}
              color="bg-blue-100 dark:bg-blue-900/30 text-blue-600"
              isCurrency
            />
            <StatCard
              label="Accounts Payable"
              value={dash.apAging.totalOutstanding}
              icon={TrendingDown}
              color="bg-red-100 dark:bg-red-900/30 text-red-600"
              isCurrency
            />
            <StatCard
              label="Inventory Value"
              value={dash.inventoryValue.totalValue}
              icon={Package}
              color="bg-amber-100 dark:bg-amber-900/30 text-amber-600"
              isCurrency
            />
          </div>

          {/* Second Row Metrics */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard
              label="Active Projects"
              value={dash.projectPortfolio.activeProjects}
              icon={BarChart3}
              color="bg-purple-100 dark:bg-purple-900/30 text-purple-600"
            />
            <StatCard
              label="Total Project Budget"
              value={dash.projectPortfolio.totalBudget}
              icon={DollarSign}
              color="bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600"
              isCurrency
            />
            <StatCard
              label="Avg Project Margin"
              value={Math.round(dash.projectPortfolio.avgMarginPercent)}
              icon={PieChartIcon}
              color="bg-teal-100 dark:bg-teal-900/30 text-teal-600"
            />
            <StatCard
              label="Over-Budget Projects"
              value={dash.projectPortfolio.overBudgetCount}
              icon={AlertTriangle}
              color={dash.projectPortfolio.overBudgetCount > 0
                ? 'bg-red-100 dark:bg-red-900/30 text-red-600'
                : 'bg-green-100 dark:bg-green-900/30 text-green-600'}
            />
          </div>

          {/* Aging Summaries */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader title="Accounts Receivable Aging" />
              <CardContent>
                <AgingBar
                  label="Receivables"
                  current={dash.arAging.current}
                  d30={dash.arAging.days30}
                  d60={dash.arAging.days60}
                  d90={dash.arAging.days90}
                  over90={dash.arAging.over90}
                />
              </CardContent>
            </Card>
            <Card>
              <CardHeader title="Accounts Payable Aging" />
              <CardContent>
                <AgingBar
                  label="Payables"
                  current={dash.apAging.current}
                  d30={dash.apAging.days30}
                  d60={dash.apAging.days60}
                  d90={dash.apAging.days90}
                  over90={dash.apAging.over90}
                />
              </CardContent>
            </Card>
          </div>

          {/* Charts Row */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Cash Position by Account */}
            <Card>
              <CardHeader title="Cash Position by Account" />
              <CardContent>
                {inventoryChartData.length > 0 ? (
                  <div className="h-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={inventoryChartData}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                        <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                        <YAxis tick={{ fontSize: 12 }} tickFormatter={(v) => `$${(v / 1000).toFixed(0)}k`} />
                        <Tooltip formatter={(v: number) => formatCurrency(v)} />
                        <Bar dataKey="value" fill="#10b981" radius={[4, 4, 0, 0]} />
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                ) : (
                  <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
                    No bank accounts configured.
                  </p>
                )}
              </CardContent>
            </Card>

            {/* Project Budget vs Costs */}
            <Card>
              <CardHeader title="Project Portfolio: Budget vs Costs" />
              <CardContent>
                {projectChartData.length > 0 && dash.projectPortfolio.totalBudget > 0 ? (
                  <div className="h-64">
                    <ResponsiveContainer width="100%" height="100%">
                      <BarChart data={projectChartData}>
                        <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                        <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                        <YAxis tick={{ fontSize: 12 }} tickFormatter={(v) => `$${(v / 1000).toFixed(0)}k`} />
                        <Tooltip formatter={(v: number) => formatCurrency(v)} />
                        <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                          {projectChartData.map((_, i) => (
                            <Cell key={`cell-${i}`} fill={i === 0 ? '#6366f1' : '#f59e0b'} />
                          ))}
                        </Bar>
                      </BarChart>
                    </ResponsiveContainer>
                  </div>
                ) : (
                  <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
                    No project data available.
                  </p>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Summary Table */}
          <Card>
            <CardHeader title="Module Summary" description="Key metrics across all modules" />
            <CardContent className="p-0">
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                      <th className="px-4 py-3 font-medium text-gray-500 dark:text-gray-400">Module</th>
                      <th className="px-4 py-3 font-medium text-gray-500 dark:text-gray-400 text-right">Outstanding</th>
                      <th className="px-4 py-3 font-medium text-gray-500 dark:text-gray-400 text-right">Current</th>
                      <th className="px-4 py-3 font-medium text-gray-500 dark:text-gray-400 text-right">30+ Days</th>
                      <th className="px-4 py-3 font-medium text-gray-500 dark:text-gray-400 text-right">90+ Days</th>
                      <th className="px-4 py-3 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">Accounts Receivable</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.arAging.totalOutstanding)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.arAging.current)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.arAging.days30 + dash.arAging.days60 + dash.arAging.days90)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.arAging.over90)}</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={dash.arAging.over90 > 0 ? 'warning' : 'active'} />
                      </td>
                    </tr>
                    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">Accounts Payable</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.apAging.totalOutstanding)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.apAging.current)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.apAging.days30 + dash.apAging.days60 + dash.apAging.days90)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.apAging.over90)}</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={dash.apAging.over90 > 0 ? 'warning' : 'active'} />
                      </td>
                    </tr>
                    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">Cash Management</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums" colSpan={3}>{formatCurrency(dash.cashPosition.totalCash)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">—</td>
                      <td className="px-4 py-3">
                        <StatusBadge status="active" />
                      </td>
                    </tr>
                    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">Inventory</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums" colSpan={3}>{formatCurrency(dash.inventoryValue.totalValue)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{dash.inventoryValue.totalItems} items</td>
                      <td className="px-4 py-3">
                        <StatusBadge status="active" />
                      </td>
                    </tr>
                    <tr className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-4 py-3 font-medium text-gray-900 dark:text-white">Project Accounting</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{formatCurrency(dash.projectPortfolio.totalCosts)}</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{dash.projectPortfolio.activeProjects} active</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{dash.projectPortfolio.avgMarginPercent.toFixed(1)}% margin</td>
                      <td className="px-4 py-3 text-right font-tabular tabular-nums">{dash.projectPortfolio.overBudgetCount} over budget</td>
                      <td className="px-4 py-3">
                        <StatusBadge status={dash.projectPortfolio.overBudgetCount > 0 ? 'warning' : 'active'} />
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </CardContent>
          </Card>

          {/* Generated timestamp */}
          <p className="text-xs text-gray-400 dark:text-gray-500 text-right">
            Generated {new Date(dash.generatedOn).toLocaleString()}
          </p>
        </>
      ) : null}
    </div>
  )
}
