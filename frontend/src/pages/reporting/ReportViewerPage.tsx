// <copyright file="ReportViewerPage.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Play, Download, RefreshCcw, Filter, ChevronDown, ChevronRight,
  FileText, ExternalLink,
} from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Skeleton } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { useAuthStore } from '@stores/authStore'
import { getFiscalPeriods, getCompanies } from '@api/platform'

// ---------------------------------------------------------------------------
// Unified report registry with parameter definitions
// ---------------------------------------------------------------------------

interface ReportParamDef {
  name: string; label: string; type: 'date' | 'select' | 'text' | 'number'
  required?: boolean; options?: { value: string; label: string }[]
  fetchOptions?: () => Promise<{ value: string; label: string }[]>
}

interface ReportConfig {
  id: string; name: string; module: string; category: string
  description: string; endpoint: string; params: ReportParamDef[]
  columns: { key: string; header: string; align?: 'left' | 'right'; format?: (v: any) => string }[]
  adapter: (raw: any) => any[]
  drillBack?: (row: any) => string
}

function companyId(): string {
  const current = useAuthStore.getState().currentCompany
  return current?.id ?? ''
}

async function fetchPeriodOptions(): Promise<{ value: string; label: string }[]> {
  try {
    const periods = await getFiscalPeriods()
    return (periods as any[]).map(p => ({
      value: p.id, label: `P${p.periodNumber} - ${p.description}`
    }))
  } catch { return [] }
}

const REPORTS: ReportConfig[] = [
  // GL
  {
    id: 'gl-trial-balance', name: 'Trial Balance', module: 'General Ledger', category: 'Financial',
    description: 'Account balances by period.',
    endpoint: '/gl/reports/trial-balance', params: [
      { name: 'fiscalPeriodId', label: 'Fiscal Period', type: 'select', fetchOptions: fetchPeriodOptions },
    ],
    columns: [
      { key: 'accountNumber', header: 'Account' }, { key: 'accountDescription', header: 'Description' },
      { key: 'debit', header: 'Debit', align: 'right', format: formatCurrency },
      { key: 'credit', header: 'Credit', align: 'right', format: formatCurrency },
      { key: 'endingBalance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
    drillBack: (row) => `/platform/accounts?highlight=${row.accountId}`,
  },
  {
    id: 'gl-balance-sheet', name: 'Balance Sheet', module: 'General Ledger', category: 'Financial',
    description: 'Assets, liabilities, equity.',
    endpoint: '/gl/reports/balance-sheet', params: [
      { name: 'fiscalPeriodId', label: 'Fiscal Period', type: 'select', fetchOptions: fetchPeriodOptions },
    ],
    columns: [
      { key: 'accountNumber', header: 'Account' }, { key: 'accountDescription', header: 'Description' },
      { key: 'balance', header: 'Balance', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },
  {
    id: 'gl-income-statement', name: 'Income Statement', module: 'General Ledger', category: 'Financial',
    description: 'Revenue and expenses.',
    endpoint: '/gl/reports/income-statement', params: [
      { name: 'fiscalPeriodId', label: 'Fiscal Period', type: 'select', fetchOptions: fetchPeriodOptions },
    ],
    columns: [
      { key: 'accountNumber', header: 'Account' }, { key: 'accountDescription', header: 'Description' },
      { key: 'balance', header: 'Amount', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },
  {
    id: 'gl-cash-flow', name: 'Cash Flow Statement', module: 'General Ledger', category: 'Financial',
    description: 'Operating, investing, financing.',
    endpoint: '/gl/reports/cash-flow', params: [
      { name: 'fiscalPeriodId', label: 'Fiscal Period', type: 'select', fetchOptions: fetchPeriodOptions },
    ],
    columns: [
      { key: 'category', header: 'Category' }, { key: 'accountNumber', header: 'Account' },
      { key: 'accountDescription', header: 'Description' },
      { key: 'amount', header: 'Amount', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? [],
  },

  // AP
  {
    id: 'ap-aging', name: 'AP Aging', module: 'Accounts Payable', category: 'Aging',
    description: 'Payables by aging bucket.',
    endpoint: '/ap/reports/aging', params: [],
    columns: [
      { key: 'vendorName', header: 'Vendor' },
      { key: 'current', header: 'Current', align: 'right', format: formatCurrency },
      { key: 'days30', header: '1-30', align: 'right', format: formatCurrency },
      { key: 'days60', header: '31-60', align: 'right', format: formatCurrency },
      { key: 'days90', header: '61-90', align: 'right', format: formatCurrency },
      { key: 'over90', header: '90+', align: 'right', format: formatCurrency },
      { key: 'total', header: 'Total', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },

  // AR
  {
    id: 'ar-aging', name: 'AR Aging', module: 'Accounts Receivable', category: 'Aging',
    description: 'Receivables by aging bucket.',
    endpoint: '/ar/reports/aging', params: [],
    columns: [
      { key: 'customerName', header: 'Customer' },
      { key: 'current', header: 'Current', align: 'right', format: formatCurrency },
      { key: 'days30', header: '1-30', align: 'right', format: formatCurrency },
      { key: 'days60', header: '31-60', align: 'right', format: formatCurrency },
      { key: 'days90', header: '61-90', align: 'right', format: formatCurrency },
      { key: 'over90', header: '90+', align: 'right', format: formatCurrency },
      { key: 'total', header: 'Total', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },

  // Inventory
  {
    id: 'inv-valuation', name: 'Inventory Valuation', module: 'Inventory', category: 'Valuation',
    description: 'Item values by warehouse.',
    endpoint: '/inventory/reports/valuation', params: [],
    columns: [
      { key: 'itemCode', header: 'Item' }, { key: 'description', header: 'Description' },
      { key: 'warehouseName', header: 'Warehouse' },
      { key: 'quantityOnHand', header: 'Qty', align: 'right' },
      { key: 'unitCost', header: 'Unit Cost', align: 'right', format: formatCurrency },
      { key: 'totalValue', header: 'Total', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => Array.isArray(r) ? r : (r?.lines ?? []),
  },

  // Payroll
  {
    id: 'pay-register', name: 'Payroll Register', module: 'Payroll', category: 'Payroll',
    description: 'All employees: gross, taxes, net.',
    endpoint: '/payroll/reports/payroll-register', params: [],
    columns: [
      { key: 'employeeName', header: 'Employee' },
      { key: 'grossPay', header: 'Gross', align: 'right', format: formatCurrency },
      { key: 'taxes', header: 'Taxes', align: 'right', format: formatCurrency },
      { key: 'netPay', header: 'Net Pay', align: 'right', format: formatCurrency },
    ],
    adapter: (r) => r?.lines ?? (Array.isArray(r) ? r : []),
  },
]

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function ReportViewerPage() {
  const [selectedReportId, setSelectedReportId] = useState(REPORTS[0].id)
  const [paramValues, setParamValues] = useState<Record<string, string>>({})
  const [runId, setRunId] = useState(0)

  const report = useMemo(() => REPORTS.find(r => r.id === selectedReportId)!, [selectedReportId])

  const { data, isLoading, error } = useQuery({
    queryKey: ['reporting', 'viewer', selectedReportId, paramValues, runId],
    queryFn: async () => {
      const params = new URLSearchParams()
      params.set('companyId', companyId())
      for (const [k, v] of Object.entries(paramValues)) {
        if (v) params.set(k, v)
      }
      const res = await fetch(`/api/v1${report.endpoint}?${params.toString()}`, {
        headers: { Accept: 'application/json', Authorization: `Bearer ${useAuthStore.getState().accessToken}` },
      })
      if (!res.ok) throw new Error(`Report failed: ${res.status}`)
      const json = await res.json()
      const raw = json?.isSuccess !== undefined ? json.data : json
      return report.adapter(raw)
    },
    enabled: runId > 0,
    staleTime: 30_000,
  })

  const rows = (data as any[]) ?? []

  const handleExportCsv = () => {
    if (rows.length === 0) return
    const headers = report.columns.map(c => c.header)
    const csvRows = rows.map((row: any) =>
      report.columns.map(c => {
        const val = row[c.key]
        return typeof val === 'string' && val.includes(',') ? `"${val}"` : (val ?? '')
      }).join(',')
    )
    const csv = [headers.join(','), ...csvRows].join('\n')
    const blob = new Blob([csv], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url; a.download = `${report.id}.csv`; a.click()
    URL.revokeObjectURL(url)
  }

  const moduleGroups = useMemo(() => {
    const groups: Record<string, ReportConfig[]> = {}
    for (const r of REPORTS) {
      if (!groups[r.module]) groups[r.module] = []
      groups[r.module].push(r)
    }
    return groups
  }, [])

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Report Viewer</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">
          Select a report, set parameters, and run
        </p>
      </div>

      {/* Report Selector + Parameters */}
      <Card>
        <CardHeader title="Report Parameters" description="Select a report and configure filters" />
        <CardContent>
          <div className="space-y-4">
            <Select
              label="Select Report"
              options={REPORTS.map(r => ({ value: r.id, label: `${r.name} (${r.module})` }))}
              value={selectedReportId}
              onChange={e => { setSelectedReportId(e.target.value); setParamValues({}); setRunId(0) }}
            />

            <p className="text-sm text-gray-600 dark:text-gray-400">{report.description}</p>

            {/* Dynamic Parameter Inputs */}
            {report.params.length > 0 && (
              <div className="flex flex-wrap gap-4">
                {report.params.map(param => (
                  <div key={param.name} className="w-64">
                    {param.type === 'select' && param.fetchOptions ? (
                      <ParamSelect
                        param={param}
                        value={paramValues[param.name] ?? ''}
                        onChange={v => setParamValues(prev => ({ ...prev, [param.name]: v }))}
                      />
                    ) : param.type === 'date' ? (
                      <Input
                        label={param.label}
                        type="date"
                        value={paramValues[param.name] ?? ''}
                        onChange={e => setParamValues(prev => ({ ...prev, [param.name]: e.target.value }))}
                      />
                    ) : (
                      <Input
                        label={param.label}
                        value={paramValues[param.name] ?? ''}
                        onChange={e => setParamValues(prev => ({ ...prev, [param.name]: e.target.value }))}
                      />
                    )}
                  </div>
                ))}
              </div>
            )}

            <div className="flex gap-2">
              <Button
                variant="primary"
                size="sm"
                onClick={() => setRunId(id => id + 1)}
                leftIcon={<Play className="h-4 w-4" />}
              >
                Run Report
              </Button>
              {rows.length > 0 && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleExportCsv}
                  leftIcon={<Download className="h-4 w-4" />}
                >
                  Export CSV
                </Button>
              )}
              <Button
                variant="ghost"
                size="sm"
                onClick={() => { setParamValues({}); setRunId(0) }}
                leftIcon={<RefreshCcw className="h-4 w-4" />}
              >
                Reset
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Error */}
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300">
          <span className="text-sm">{getErrorMessage(error)}</span>
        </div>
      )}

      {/* Results */}
      {runId > 0 && (
        <Card>
          <CardHeader
            title={report.name}
            description={`${rows.length} row${rows.length !== 1 ? 's' : ''} returned`}
          />
          <CardContent className="p-0">
            {isLoading ? (
              <div className="p-6"><Skeleton className="h-64" /></div>
            ) : rows.length === 0 ? (
              <p className="text-sm text-gray-500 dark:text-gray-400 py-10 text-center">
                No data found for the selected parameters.
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                      {report.columns.map(col => (
                        <th key={col.key} className={`px-4 py-2.5 font-medium text-gray-500 dark:text-gray-400 ${col.align === 'right' ? 'text-right' : ''}`}>
                          {col.header}
                        </th>
                      ))}
                      {report.drillBack && <th className="px-4 py-2.5 font-medium text-gray-500 dark:text-gray-400 w-8"></th>}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                    {rows.map((row: any, ri: number) => (
                      <tr key={ri} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                        {report.columns.map(col => (
                          <td key={col.key} className={`px-4 py-2.5 ${col.align === 'right' ? 'text-right font-tabular tabular-nums' : ''}`}>
                            {col.format ? col.format(row[col.key]) : (row[col.key] ?? '—')}
                          </td>
                        ))}
                        {report.drillBack && (
                          <td className="px-4 py-2.5">
                            <a
                              href={report.drillBack(row)}
                              className="text-primary-600 hover:text-primary-700 dark:text-primary-400"
                              title="Drill back"
                            >
                              <ExternalLink className="h-3.5 w-3.5" />
                            </a>
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      {/* Initial state */}
      {runId === 0 && (
        <Card>
          <CardContent className="py-12 text-center">
            <FileText className="h-16 w-16 text-gray-200 dark:text-gray-700 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 dark:text-white">Select a Report</h3>
            <p className="text-sm text-gray-500 dark:text-gray-400 mt-2 max-w-md mx-auto">
              Choose a report from the dropdown above, configure any parameters, and click <strong>Run Report</strong> to generate results.
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// ParamSelect - async-loaded select for periods, etc.
// ---------------------------------------------------------------------------

function ParamSelect({ param, value, onChange }: {
  param: ReportParamDef; value: string; onChange: (v: string) => void
}) {
  const { data: options = [], isLoading } = useQuery({
    queryKey: ['reporting', 'param-options', param.name],
    queryFn: param.fetchOptions!,
    enabled: !!param.fetchOptions,
    staleTime: 60_000,
  })

  return (
    <Select
      label={param.label}
      placeholder={isLoading ? 'Loading...' : `All ${param.label}s`}
      options={options}
      value={value}
      onChange={e => onChange(e.target.value)}
    />
  )
}
