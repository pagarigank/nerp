import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { FileText, BookOpen, CalendarDays, ShieldCheck } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { getCompanySetupReport, getChartOfAccountsReport, getFiscalCalendarReport, getSecurityMatrixReport } from '@api/platform'

type ReportKey = 'company-setup' | 'chart-of-accounts' | 'fiscal-calendar' | 'security-matrix'

const reports: { key: ReportKey; name: string; icon: typeof FileText; description: string }[] = [
  { key: 'company-setup', name: 'Company Setup', icon: FileText, description: 'Company, fiscal years, segments, currencies, sequences, users & roles' },
  { key: 'chart-of-accounts', name: 'Chart of Accounts', icon: BookOpen, description: 'Account master with segment types' },
  { key: 'fiscal-calendar', name: 'Fiscal Calendar', icon: CalendarDays, description: 'Fiscal years with periods' },
  { key: 'security-matrix', name: 'Security / Role Matrix', icon: ShieldCheck, description: 'Roles, users, permissions and assignments' },
]

export function PlatformReportsPage() {
  const [active, setActive] = useState<ReportKey>('company-setup')

  const companySetup = useQuery({ queryKey: ['platform', 'reports', 'company-setup'], queryFn: () => getCompanySetupReport(), enabled: active === 'company-setup' })
  const chartOfAccounts = useQuery({ queryKey: ['platform', 'reports', 'chart-of-accounts'], queryFn: () => getChartOfAccountsReport(), enabled: active === 'chart-of-accounts' })
  const fiscalCalendar = useQuery({ queryKey: ['platform', 'reports', 'fiscal-calendar'], queryFn: () => getFiscalCalendarReport(), enabled: active === 'fiscal-calendar' })
  const securityMatrix = useQuery({ queryKey: ['platform', 'reports', 'security-matrix'], queryFn: () => getSecurityMatrixReport(), enabled: active === 'security-matrix' })

  const current = reports.find(r => r.key === active)!

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Platform Reports</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">System setup & security reporting</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {reports.map(r => (
          <button key={r.key} onClick={() => setActive(r.key)}
            className={`text-left rounded-lg border p-4 transition-colors ${active === r.key ? 'border-primary-500 bg-primary-50 dark:bg-primary-900/20' : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'}`}>
            <div className="flex items-center gap-2"><r.icon className="h-5 w-5 text-primary-600 dark:text-primary-400" /><span className="font-medium text-gray-900 dark:text-white">{r.name}</span></div>
            <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{r.description}</p>
          </button>
        ))}
      </div>

      <Card>
        <CardHeader title={current.name} description={current.description} />
        <CardContent>
          {active === 'company-setup' && (
            companySetup.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !companySetup.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <div className="space-y-6">
                <Section title="Fiscal Years" rows={companySetup.data.fiscalYears.map(y => [y.fiscalYear.toString(), y.periodName])} />
                <Section title="Users" rows={companySetup.data.users.map(u => [u.fullName, u.email])} />
                <Section title="Roles" rows={companySetup.data.roles.map(r => [r.name, r.description])} />
              </div>
            )
          )}

          {active === 'chart-of-accounts' && (
            chartOfAccounts.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !chartOfAccounts.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <div className="overflow-x-auto"><table className="w-full text-sm">
                <thead><tr className="border-b border-gray-200 dark:border-gray-700 text-left"><th className="px-3 py-2 font-medium text-gray-500">#</th><th className="px-3 py-2 font-medium text-gray-500">Account</th><th className="px-3 py-2 font-medium text-gray-500">Name</th></tr></thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {chartOfAccounts.data.accounts.map(a => (
                    <tr key={a.id}><td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{a.accountNumber}</td><td className="px-3 py-3 text-gray-700 dark:text-gray-300" colSpan={2}>{a.accountName}</td></tr>
                  ))}
                </tbody>
              </table></div>
            )
          )}

          {active === 'fiscal-calendar' && (
            fiscalCalendar.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !fiscalCalendar.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <div className="space-y-4">
                {fiscalCalendar.data.fiscalYears.map(fy => (
                  <div key={fy.fiscalYear.id}>
                    <h4 className="font-medium text-gray-900 dark:text-white mb-2">{fy.fiscalYear.fiscalYear} — {fy.fiscalYear.periodName}</h4>
                    <div className="flex flex-wrap gap-2">
                      {fy.periods.map(p => (
                        <span key={p.id} className="px-2 py-1 text-xs rounded bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300">{p.periodNumber}: {new Date(p.startDate).toLocaleDateString()}–{new Date(p.endDate).toLocaleDateString()}</span>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            )
          )}

          {active === 'security-matrix' && (
            securityMatrix.isLoading ? <p className="text-sm text-gray-500 py-8 text-center">Loading…</p> :
            !securityMatrix.data ? <p className="text-sm text-gray-500 py-8 text-center">No data.</p> : (
              <div className="space-y-6">
                <Section title="Roles" rows={securityMatrix.data.roles.map(r => [r.name, r.description])} />
                <Section title="Users" rows={securityMatrix.data.users.map(u => [u.fullName, u.email])} />
              </div>
            )
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function Section({ title, rows }: { title: string; rows: string[][] }) {
  return (
    <div>
      <h4 className="text-sm font-semibold text-gray-900 dark:text-white mb-2">{title}</h4>
      {rows.length === 0 ? <p className="text-sm text-gray-500">None.</p> :
        <div className="overflow-x-auto"><table className="w-full text-sm">
          <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
            {rows.map((r, i) => (
              <tr key={i} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td className="px-3 py-2 font-medium text-gray-900 dark:text-white">{r[0]}</td>
                <td className="px-3 py-2 text-gray-700 dark:text-gray-300">{r[1] ?? '—'}</td>
              </tr>
            ))}
          </tbody>
        </table></div>}
    </div>
  )
}
