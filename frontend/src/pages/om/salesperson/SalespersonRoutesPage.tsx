import { useState, useMemo } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Route as RouteIcon,
  Plus,
  Trash2,
  User,
  Search,
} from 'lucide-react'
import { useAuthStore } from '@stores/authStore'
import {
  getSalespersonRoutes,
  deleteSalespersonRoute,
  bulkAssignRoutes,
  reorderRoutes,
} from '@/api/salesperson'
import { getSalesReps } from '@/api/orderManagement'
import type { SalesRepSummary } from '@/types/orderManagement'
import { getCustomers } from '@/api/ar'

const DAYS_OF_WEEK = [
  { id: 1, name: 'Monday' },
  { id: 2, name: 'Tuesday' },
  { id: 3, name: 'Wednesday' },
  { id: 4, name: 'Thursday' },
  { id: 5, name: 'Friday' },
  { id: 6, name: 'Saturday' },
  { id: 0, name: 'Sunday' },
]

export function SalespersonRoutesPage() {
  const queryClient = useQueryClient()
  const currentCompany = useAuthStore((s) => s.currentCompany)

  const [selectedRepId, setSelectedRepId] = useState<string>(() => {
    return localStorage.getItem('nerp_selected_salesrep_id') || ''
  })
  const [selectedDay, setSelectedDay] = useState<number>(1) // Monday default
  const [showAssignModal, setShowAssignModal] = useState(false)
  const [selectedCustomerIds, setSelectedCustomerIds] = useState<string[]>([])
  const [custSearch, setCustSearch] = useState('')

  // Query Sales Reps
  const { data: salesReps = [] } = useQuery<SalesRepSummary[]>({
    queryKey: ['om', 'salesReps'],
    queryFn: () => getSalesReps(),
  })

  // Set default rep if none selected
  if (!selectedRepId) {
    const firstRep = salesReps[0]
    if (firstRep) setSelectedRepId(firstRep.id)
  }

  // Query Routes for selected rep and day
  const { data: routes = [] } = useQuery({
    queryKey: ['om', 'salespersonRoutes', currentCompany?.id, selectedRepId, selectedDay],
    queryFn: () => getSalespersonRoutes(currentCompany?.id, selectedRepId, selectedDay),
    enabled: Boolean(selectedRepId),
  })

  // Query Customers for assignment modal
  const { data: allCustomers = [] } = useQuery({
    queryKey: ['ar', 'customers', currentCompany?.id],
    queryFn: () => getCustomers(),
    enabled: showAssignModal,
  })

  // Delete mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteSalespersonRoute(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['om', 'salespersonRoutes'] })
    },
  })

  // Bulk assign mutation
  const bulkAssignMutation = useMutation({
    mutationFn: (customerIds: string[]) => {
      if (!currentCompany?.id || !selectedRepId) throw new Error('Missing company or rep')
      return bulkAssignRoutes({
        companyId: currentCompany.id,
        salesRepId: selectedRepId,
        dayOfWeek: selectedDay,
        customerIds,
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['om', 'salespersonRoutes'] })
      setShowAssignModal(false)
      setSelectedCustomerIds([])
    },
  })

  // Move stop up/down
  const handleMoveStop = async (index: number, direction: 'up' | 'down') => {
    const targetIndex = direction === 'up' ? index - 1 : index + 1
    if (targetIndex < 0 || targetIndex >= routes.length) return

    const newOrder = [...routes]
    const current = newOrder[index]
    const swap = newOrder[targetIndex]
    if (current === undefined || swap === undefined) return
    newOrder[targetIndex] = current
    newOrder[index] = swap

    await reorderRoutes(newOrder.map((r) => r.id))
    queryClient.invalidateQueries({ queryKey: ['om', 'salespersonRoutes'] })
  }

  // Filtered customer list for modal
  const filteredCustomersForModal = useMemo(() => {
    const existingCustIds = new Set(routes.map((r) => r.customerId))
    const term = custSearch.toLowerCase().trim()
    return allCustomers.filter((c) => {
      if (existingCustIds.has(c.id)) return false
      if (!term) return true
      return (
        c.name.toLowerCase().includes(term) ||
        (c.customerId && c.customerId.toLowerCase().includes(term))
      )
    })
  }, [allCustomers, routes, custSearch])

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-12 h-12 rounded-xl bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 text-indigo-600 dark:text-indigo-400 flex items-center justify-center">
              <RouteIcon className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900 dark:text-white">
                Salesperson Daily Visit Route Planning
              </h1>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                Assign and order customer stops on a weekly day-to-day schedule
              </p>
            </div>
          </div>

          {/* Rep Selector & Action */}
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg px-3 py-1.5">
              <User className="w-4 h-4 text-slate-400" />
              <select
                aria-label="Active Sales Rep"
                className="bg-transparent text-xs font-semibold text-slate-800 dark:text-slate-200 outline-none"
                value={selectedRepId}
                onChange={(e) => {
                  setSelectedRepId(e.target.value)
                  localStorage.setItem('nerp_selected_salesrep_id', e.target.value)
                }}
              >
                {salesReps.map((r) => (
                  <option key={r.id} value={r.id} className="dark:bg-slate-900">
                    {r.name} ({r.code})
                  </option>
                ))}
              </select>
            </div>

            <button
              onClick={() => setShowAssignModal(true)}
              className="px-3.5 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-xs font-bold flex items-center gap-1.5 shadow-sm"
            >
              <Plus className="w-4 h-4" />
              <span>Assign Customers</span>
            </button>
          </div>
        </div>
      </div>

      {/* Weekday Switcher Tabs */}
      <div className="flex overflow-x-auto gap-2 p-1.5 bg-slate-100 dark:bg-slate-800/80 rounded-xl">
        {DAYS_OF_WEEK.map((d) => (
          <button
            key={d.id}
            onClick={() => setSelectedDay(d.id)}
            className={`flex-1 py-2 px-3 rounded-lg text-xs font-semibold transition-all ${
              selectedDay === d.id
                ? 'bg-white dark:bg-slate-900 text-indigo-600 dark:text-indigo-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900'
            }`}
          >
            {d.name}
          </button>
        ))}
      </div>

      {/* Route Stops Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-slate-500 font-bold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4 w-16 text-center">Stop #</th>
                <th className="py-3 px-4">Customer Name & Code</th>
                <th className="py-3 px-4">Address</th>
                <th className="py-3 px-4 w-32">Est. Time</th>
                <th className="py-3 px-4">Visit Notes / Instructions</th>
                <th className="py-3 px-4 w-28 text-right">AR Balance</th>
                <th className="py-3 px-4 w-32 text-center">Reorder</th>
                <th className="py-3 px-3 w-16 text-center"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {routes.length === 0 ? (
                <tr>
                  <td colSpan={8} className="py-12 text-center text-slate-400">
                    No customers assigned to {DAYS_OF_WEEK.find((d) => d.id === selectedDay)?.name}.
                    Click &quot;Assign Customers&quot; to build the route.
                  </td>
                </tr>
              ) : (
                routes.map((stop, idx) => (
                  <tr key={stop.id} className="hover:bg-slate-50/80 dark:hover:bg-slate-800/50">
                    <td className="py-3 px-4 text-center">
                      <span className="w-6 h-6 rounded bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold text-xs inline-flex items-center justify-center">
                        #{stop.sequenceOrder}
                      </span>
                    </td>
                    <td className="py-3 px-4">
                      <div className="font-bold text-slate-900 dark:text-white">{stop.customerName}</div>
                      <div className="text-[11px] font-mono text-slate-400">{stop.customerCode}</div>
                    </td>
                    <td className="py-3 px-4 text-slate-600 dark:text-slate-300">
                      {stop.customerAddress || '—'}
                    </td>
                    <td className="py-3 px-4 text-slate-600 dark:text-slate-300">
                      {stop.estimatedTime || '—'}
                    </td>
                    <td className="py-3 px-4 text-slate-600 dark:text-slate-300">
                      {stop.notes || '—'}
                    </td>
                    <td className="py-3 px-4 text-right tabular-nums font-semibold text-slate-800 dark:text-slate-200">
                      ${stop.currentBalance.toFixed(2)}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <div className="inline-flex items-center gap-1">
                        <button
                          disabled={idx === 0}
                          onClick={() => handleMoveStop(idx, 'up')}
                          className="p-1 rounded hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-20"
                          title="Move Up"
                        >
                          ▲
                        </button>
                        <button
                          disabled={idx === routes.length - 1}
                          onClick={() => handleMoveStop(idx, 'down')}
                          className="p-1 rounded hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-20"
                          title="Move Down"
                        >
                          ▼
                        </button>
                      </div>
                    </td>
                    <td className="py-3 px-3 text-center">
                      <button
                        onClick={() => deleteMutation.mutate(stop.id)}
                        className="text-slate-400 hover:text-red-600 p-1"
                        title="Remove Stop"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Customer Bulk Assignment Modal */}
      {showAssignModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
          <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-6 max-w-xl w-full shadow-2xl max-h-[85vh] flex flex-col">
            <h2 className="text-lg font-bold text-slate-900 dark:text-white mb-1">
              Assign Customers to {DAYS_OF_WEEK.find((d) => d.id === selectedDay)?.name}
            </h2>
            <p className="text-xs text-slate-500 mb-4">
              Select one or more customers to add to this day&apos;s visit schedule
            </p>

            <div className="relative mb-3">
              <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
              <input
                type="text"
                placeholder="Search customers by name or code..."
                value={custSearch}
                onChange={(e) => setCustSearch(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-xs"
              />
            </div>

            <div className="overflow-y-auto flex-1 divide-y divide-slate-100 dark:divide-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg mb-4">
              {filteredCustomersForModal.length === 0 ? (
                <div className="p-8 text-center text-slate-400 text-xs">
                  No matching unassigned customers found.
                </div>
              ) : (
                filteredCustomersForModal.map((c) => {
                  const isChecked = selectedCustomerIds.includes(c.id)
                  return (
                    <label
                      key={c.id}
                      className="p-3 flex items-center gap-3 hover:bg-slate-50 dark:hover:bg-slate-800 cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => {
                          setSelectedCustomerIds((prev) =>
                            isChecked ? prev.filter((id) => id !== c.id) : [...prev, c.id],
                          )
                        }}
                        className="rounded border-slate-300 text-indigo-600 focus:ring-indigo-500"
                      />
                      <div className="flex-1">
                        <div className="font-semibold text-xs text-slate-900 dark:text-white">
                          {c.name}
                        </div>
                        <div className="text-[11px] text-slate-400 font-mono">
                          {c.customerId} {c.billingAddress ? `• ${c.billingAddress}` : ''}
                        </div>
                      </div>
                    </label>
                  )
                })
              )}
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                onClick={() => setShowAssignModal(false)}
                className="px-4 py-2 border border-slate-300 dark:border-slate-700 rounded-lg text-xs font-semibold text-slate-700 dark:text-slate-300"
              >
                Cancel
              </button>
              <button
                disabled={selectedCustomerIds.length === 0 || bulkAssignMutation.isPending}
                onClick={() => bulkAssignMutation.mutate(selectedCustomerIds)}
                className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white rounded-lg text-xs font-bold disabled:opacity-50"
              >
                {bulkAssignMutation.isPending
                  ? 'Assigning...'
                  : `Assign ${selectedCustomerIds.length} Customer${selectedCustomerIds.length === 1 ? '' : 's'}`}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
