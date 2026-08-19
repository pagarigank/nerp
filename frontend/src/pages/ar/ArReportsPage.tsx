import { useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { FileText, AlertCircle } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getErrorMessage } from '@api/client'
import { getArAgingReport, getCustomerTrialBalance, getCashReceiptsJournal, getSalesJournal, getArFinanceChargeReport } from '@api/ar'
import type { ArAgingReport, CustomerTrialBalanceReport, CashReceiptsJournalReport, SalesJournalReport, FinanceChargeReport } from '@/types/ar'

type ReportKey = 'aging' | 'customer-trial-balance' | 'cash-receipts-journal' | 'sales-journal' | 'finance-charge'

const reports = [
  { key: 'aging' as ReportKey, name: 'AR Aging', desc: 'Open balances by aging bucket' },
  { key: 'customer-trial-balance' as ReportKey, name: 'Customer Trial Balance', desc: 'Beginning/debits/credits/ending per customer' },
  { key: 'cash-receipts-journal' as ReportKey, name: 'Cash Receipts Journal', desc: 'Receipts in date range' },
  { key: 'sales-journal' as ReportKey, name: 'Sales Journal', desc: 'Invoices in date range' },
  { key: 'finance-charge' as ReportKey, name: 'Finance Charge Report', desc: 'Finance charges assessed' },
]

export function ArReportsPage() {
  const [active, setActive] = useState<ReportKey>('aging')
  const [formError, setFormError] = useState<string | null>(null)

  const aging = useQuery({ queryKey: ['ar', 'reports', 'aging'], queryFn: async () => getArAgingReport(), enabled: active === 'aging' })
  const ctb = useQuery({ queryKey: ['ar', 'reports', 'ctb'], queryFn: async () => getCustomerTrialBalance(), enabled: active === 'customer-trial-balance' })
  const crj = useQuery({ queryKey: ['ar', 'reports', 'crj'], queryFn: async () => getCashReceiptsJournal(), enabled: active === 'cash-receipts-journal' })
  const sj = useQuery({ queryKey: ['ar', 'reports', 'sj'], queryFn: async () => getSalesJournal(), enabled: active === 'sales-journal' })
  const fc = useQuery({ queryKey: ['ar', 'reports', 'fc'], queryFn: async () => getArFinanceChargeReport(), enabled: active === 'finance-charge' })

  const reportErr = aging.error ?? ctb.error ?? crj.error ?? sj.error ?? fc.error
  useEffect(() => { if (reportErr) setFormError(getErrorMessage(reportErr)) }, [reportErr])

  const current = reports.find(r => r.key === active)!

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">AR Reports</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">Receivables reporting and analytics</p>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
        {reports.map(r => (
          <button key={r.key} onClick={() => setActive(r.key)}
            className={`text-left rounded-lg border p-3 transition-colors ${active === r.key ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'}`}>
            <FileText className="h-4 w-4 text-primary-600 dark:text-primary-400" />
            <p className="mt-1 text-sm font-medium text-gray-900 dark:text-white">{r.name}</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">{r.desc}</p>
          </button>
        ))}
      </div>

      <Card>
        <CardHeader title={current.name} description={current.desc} />
        <CardContent>
          {active === 'aging' && (
            aging.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !aging.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <AggTable data={aging.data as ArAgingReport} />
            )
          )}
          {active === 'customer-trial-balance' && (
            ctb.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !ctb.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <CtbTable data={ctb.data as CustomerTrialBalanceReport} />
            )
          )}
          {active === 'cash-receipts-journal' && (
            crj.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !crj.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <CrjTable data={crj.data as CashReceiptsJournalReport} />
            )
          )}
          {active === 'sales-journal' && (
            sj.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !sj.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <SjTable data={sj.data as SalesJournalReport} />
            )
          )}
          {active === 'finance-charge' && (
            fc.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !fc.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <FcTable data={fc.data as FinanceChargeReport} />
            )
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function AggTable({ data }: { data: ArAgingReport }) {
  return (
    <div className="overflow-x-auto"><table className="w-full text-sm">
      <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
        <th className="px-3 py-2 font-medium text-gray-500">Customer</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Current</th>
        <th className="px-3 py-2 font-medium text-gray-500 text-right">1-30</th><th className="px-3 py-2 font-medium text-gray-500 text-right">31-60</th>
        <th className="px-3 py-2 font-medium text-gray-500 text-right">61-90</th><th className="px-3 py-2 font-medium text-gray-500 text-right">90+</th>
        <th className="px-3 py-2 font-medium text-gray-500 text-right">Total</th>
      </tr></thead>
      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
        {data.lines.map(l => (
          <tr key={l.customerId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{l.customerName}</td>
            <td className="px-3 py-3 text-right">{l.currentBalance.toFixed(2)}</td>
            <td className="px-3 py-3 text-right">{l.days1To30.toFixed(2)}</td>
            <td className="px-3 py-3 text-right">{l.days31To60.toFixed(2)}</td>
            <td className="px-3 py-3 text-right">{l.days61To90.toFixed(2)}</td>
            <td className="px-3 py-3 text-right">{l.over90Days.toFixed(2)}</td>
            <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.totalDue.toFixed(2)}</td>
          </tr>
        ))}
      </tbody>
    </table></div>
  )
}

function CtbTable({ data }: { data: CustomerTrialBalanceReport }) {
  return (
    <div className="overflow-x-auto"><table className="w-full text-sm">
      <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
        <th className="px-3 py-2 font-medium text-gray-500">Customer</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Beginning</th>
        <th className="px-3 py-2 font-medium text-gray-500 text-right">Debits</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Credits</th>
        <th className="px-3 py-2 font-medium text-gray-500 text-right">Ending</th>
      </tr></thead>
      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
        {data.lines.map(l => (
          <tr key={l.customerId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{l.customerName}</td>
            <td className="px-3 py-3 text-right">{l.beginningBalance.toFixed(2)}</td>
            <td className="px-3 py-3 text-right">{l.debits.toFixed(2)}</td>
            <td className="px-3 py-3 text-right">{l.credits.toFixed(2)}</td>
            <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.endingBalance.toFixed(2)}</td>
          </tr>
        ))}
      </tbody>
    </table></div>
  )
}

function CrjTable({ data }: { data: CashReceiptsJournalReport }) {
  return (
    <div className="overflow-x-auto"><table className="w-full text-sm">
      <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
        <th className="px-3 py-2 font-medium text-gray-500">Receipt</th><th className="px-3 py-2 font-medium text-gray-500">Customer</th>
        <th className="px-3 py-2 font-medium text-gray-500">Date</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
        <th className="px-3 py-2 font-medium text-gray-500">Method</th>
      </tr></thead>
      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
        {data.lines.map(l => (
          <tr key={l.receiptId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{l.receiptReference}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{l.customerName}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(l.receiptDate).toLocaleDateString()}</td>
            <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.amount.toFixed(2)}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{l.paymentMethod}</td>
          </tr>
        ))}
      </tbody>
    </table></div>
  )
}

function SjTable({ data }: { data: SalesJournalReport }) {
  return (
    <div className="overflow-x-auto"><table className="w-full text-sm">
      <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
        <th className="px-3 py-2 font-medium text-gray-500">Invoice</th><th className="px-3 py-2 font-medium text-gray-500">Customer</th>
        <th className="px-3 py-2 font-medium text-gray-500">Date</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
        <th className="px-3 py-2 font-medium text-gray-500">Status</th>
      </tr></thead>
      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
        {data.lines.map(l => (
          <tr key={l.invoiceId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{l.invoiceNumber}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{l.customerName}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(l.invoiceDate).toLocaleDateString()}</td>
            <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.amount.toFixed(2)}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{l.status}</td>
          </tr>
        ))}
      </tbody>
    </table></div>
  )
}

function FcTable({ data }: { data: FinanceChargeReport }) {
  return (
    <div className="overflow-x-auto"><table className="w-full text-sm">
      <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left">
        <th className="px-3 py-2 font-medium text-gray-500">Charge</th><th className="px-3 py-2 font-medium text-gray-500">Customer</th>
        <th className="px-3 py-2 font-medium text-gray-500">Date</th><th className="px-3 py-2 font-medium text-gray-500 text-right">Amount</th>
        <th className="px-3 py-2 font-medium text-gray-500">Rate</th>
      </tr></thead>
      <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
        {data.lines.map(l => (
          <tr key={l.chargeId} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{l.chargeNumber}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{l.customerName}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{new Date(l.chargeDate).toLocaleDateString()}</td>
            <td className="px-3 py-3 text-right text-gray-900 dark:text-white">{l.amount.toFixed(2)}</td>
            <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{(l.annualRate * 100).toFixed(2)}%</td>
          </tr>
        ))}
      </tbody>
    </table></div>
  )
}
