import { useState, useEffect, useMemo, useRef } from 'react'
import { useParams, useNavigate, useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Clock,
  Search,
  ShoppingCart,
  User,
  MapPin,
  Phone,
  ArrowLeft,
  CheckCircle,
  AlertTriangle,
  Minus,
  Plus,
  Save,
  Wifi,
  WifiOff,
  Database,
  Trash2,
} from 'lucide-react'
import { useAuthStore } from '@stores/authStore'
import { sqliteService, type ProductRecord, type RouteRecord } from '@/utils/sqliteDb'
import { getItems } from '@/api/inventory'
import { getCustomers } from '@/api/ar'
import { uploadOfflineSalesCalls } from '@/api/salesperson'

interface OrderLineState {
  itemId: string
  itemCode: string
  description: string
  basePrice: number
  sellingPrice: number
  unitOfMeasure: string
  discountPercent: number
  orderQty: number
  lineTotal: number
  ruleApplied?: string
}

export function SalesOrderCallPage() {
  const { customerId } = useParams<{ customerId: string }>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const currentCompany = useAuthStore((s) => s.currentCompany)

  const repIdFromUrl = searchParams.get('repId') || localStorage.getItem('nerp_selected_salesrep_id') || ''
  const routeIdFromUrl = searchParams.get('routeId') || undefined

  // Network state
  const [isOnline, setIsOnline] = useState(navigator.onLine)
  useEffect(() => {
    const onOnline = () => setIsOnline(true)
    const onOffline = () => setIsOnline(false)
    window.addEventListener('online', onOnline)
    window.addEventListener('offline', onOffline)
    return () => {
      window.removeEventListener('online', onOnline)
      window.removeEventListener('offline', onOffline)
    }
  }, [])

  // Call session timer
  const [callSeconds, setCallSeconds] = useState(0)
  const [isTimerRunning, setIsTimerRunning] = useState(true)
  const callStartTimeRef = useRef(new Date().toISOString())

  useEffect(() => {
    let timer: NodeJS.Timeout
    if (isTimerRunning) {
      timer = setInterval(() => {
        setCallSeconds((prev) => prev + 1)
      }, 1000)
    }
    return () => clearInterval(timer)
  }, [isTimerRunning])

  const formattedTimer = useMemo(() => {
    const mins = Math.floor(callSeconds / 60)
    const secs = callSeconds % 60
    return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`
  }, [callSeconds])

  // Customer state
  const [customerInfo, setCustomerInfo] = useState<RouteRecord | null>(null)
  const [callNotes, setCallNotes] = useState('')
  const [orderLines, setOrderLines] = useState<Record<string, OrderLineState>>({})
  const [searchTerm, setSearchTerm] = useState('')
  const [activeTab, setActiveTab] = useState<'all' | 'ordered'>('all')
  const [isSaving, setIsSaving] = useState(false)
  const [saveSuccess, setSaveSuccess] = useState<string | null>(null)

  // 1. Load customer details (offline from SQLite first, fallback to online query)
  useEffect(() => {
    if (!customerId) return
    const loadCustomer = async () => {
      const fromSqlite = await sqliteService.getCustomerById(customerId)
      if (fromSqlite) {
        setCustomerInfo(fromSqlite)
      }
    }
    loadCustomer()
  }, [customerId])

  // Also query online customers if available
  const { data: onlineCustomers = [] } = useQuery({
    queryKey: ['ar', 'customers', currentCompany?.id],
    queryFn: () => getCustomers(),
    enabled: !customerInfo && isOnline,
  })

  useEffect(() => {
    if (!customerInfo && onlineCustomers.length > 0 && customerId) {
      const found = onlineCustomers.find((c) => c.id === customerId)
      if (found) {
        setCustomerInfo({
          id: found.id,
          salesRepId: repIdFromUrl,
          customerId: found.id,
          customerCode: found.customerId || '',
          customerName: found.name,
          contactName: '',
          phone: '',
          address: found.billingAddress || '',
          dayOfWeek: new Date().getDay(),
          sequenceOrder: 1,
          creditLimit: found.creditLimit || 0,
          currentBalance: found.currentBalance || 0,
        })
      }
    }
  }, [onlineCustomers, customerId, customerInfo, repIdFromUrl])

  // 2. Load product catalogue from SQLite
  const [products, setProducts] = useState<ProductRecord[]>([])

  useEffect(() => {
    const loadProducts = async () => {
      const prods = await sqliteService.getAllProducts()
      if (prods.length > 0) {
        setProducts(prods)
      }
    }
    loadProducts()
  }, [])

  // If SQLite has no products yet and online, fetch from backend
  const { data: onlineItems = [] } = useQuery({
    queryKey: ['inv', 'items', currentCompany?.id],
    queryFn: () => getItems(),
    enabled: products.length === 0 && isOnline,
  })

  useEffect(() => {
    if (products.length === 0 && onlineItems.length > 0) {
      setProducts(
        onlineItems.map((item) => ({
          id: item.id,
          itemCode: item.itemCode,
          description: item.description,
          basePrice: item.standardCost ? Math.round(item.standardCost * 1.35 * 100) / 100 : 25.0,
          unitOfMeasure: item.baseUnitOfMeasure || 'EA',
          itemCategoryId: item.itemCategoryId,
          onHandQty: 100,
        })),
      )
    }
  }, [onlineItems, products.length])

  // 3. Initialize line states with SQLite evaluated pricing rules
  useEffect(() => {
    if (products.length === 0 || !customerId) return

    const initLines = async () => {
      const initial: Record<string, OrderLineState> = {}

      for (const prod of products) {
        const { sellingPrice, discountPercent, ruleApplied } = await sqliteService.calculateItemPrice(
          customerId,
          prod.id,
          prod.itemCategoryId,
          1,
          prod.basePrice,
        )

        initial[prod.id] = {
          itemId: prod.id,
          itemCode: prod.itemCode,
          description: prod.description,
          basePrice: prod.basePrice,
          sellingPrice,
          unitOfMeasure: prod.unitOfMeasure,
          discountPercent,
          orderQty: 0,
          lineTotal: 0,
          ruleApplied,
        }
      }

      setOrderLines((prev) => ({ ...initial, ...prev }))
    }

    initLines()
  }, [products, customerId])

  // Update quantity handler
  const handleQtyChange = (itemId: string, newQty: number) => {
    const sanitizedQty = Math.max(0, isNaN(newQty) ? 0 : newQty)
    setOrderLines((prev) => {
      const line = prev[itemId]
      if (!line) return prev
      const lineTotal = Math.round(sanitizedQty * line.sellingPrice * 100) / 100
      return {
        ...prev,
        [itemId]: {
          ...line,
          orderQty: sanitizedQty,
          lineTotal,
        },
      }
    })
  }

  // Quick increment/decrement
  const handleStepQty = (itemId: string, delta: number) => {
    const current = orderLines[itemId]?.orderQty || 0
    handleQtyChange(itemId, current + delta)
  }

  // Clear specific line
  const handleClearLine = (itemId: string) => {
    handleQtyChange(itemId, 0)
  }

  // Computed totals
  const activeOrderedLines = useMemo(() => {
    return Object.values(orderLines).filter((l) => l.orderQty > 0)
  }, [orderLines])

  const grandTotal = useMemo(() => {
    return activeOrderedLines.reduce((sum, l) => sum + l.lineTotal, 0)
  }, [activeOrderedLines])

  const totalUnits = useMemo(() => {
    return activeOrderedLines.reduce((sum, l) => sum + l.orderQty, 0)
  }, [activeOrderedLines])

  // Filtered products list
  const filteredProducts = useMemo(() => {
    const term = searchTerm.toLowerCase().trim()
    return products.filter((p) => {
      const matchesSearch =
        !term ||
        p.description.toLowerCase().includes(term) ||
        p.itemCode.toLowerCase().includes(term)

      if (!matchesSearch) return false

      if (activeTab === 'ordered') {
        return (orderLines[p.id]?.orderQty || 0) > 0
      }
      return true
    })
  }, [products, searchTerm, activeTab, orderLines])

  // End Sales Call & Store Details
  const handleEndSalesCall = async () => {
    if (!customerId || !currentCompany?.id) return
    setIsSaving(true)
    setIsTimerRunning(false)

    try {
      const callId = crypto.randomUUID()
      const now = new Date().toISOString()

      const callData = {
        id: callId,
        companyId: currentCompany.id,
        salesRepId: repIdFromUrl || '00000000-0000-0000-0000-000000000000',
        customerId,
        customerName: customerInfo?.customerName || 'Customer',
        callDate: new Date().toISOString().slice(0, 10),
        startTime: callStartTimeRef.current,
        endTime: now,
        status: 'Completed',
        notes: callNotes,
        grandTotal,
        lines: activeOrderedLines.map((l) => ({
          itemId: l.itemId,
          itemCode: l.itemCode,
          description: l.description,
          sellingPrice: l.sellingPrice,
          orderQty: l.orderQty,
          discountPercent: l.discountPercent,
          lineTotal: l.lineTotal,
        })),
      }

      // 1. Store in local SQLite database immediately (offline-safe!)
      await sqliteService.saveSalesCall(callData)

      // 2. If online, attempt live server synchronization right away
      if (isOnline && activeOrderedLines.length > 0) {
        try {
          const res = await uploadOfflineSalesCalls({
            companyId: currentCompany.id,
            salesRepId: repIdFromUrl || '00000000-0000-0000-0000-000000000000',
            calls: [
              {
                clientCallId: callId,
                customerId,
                routeId: routeIdFromUrl,
                callDate: callData.callDate,
                startTime: callData.startTime,
                endTime: callData.endTime,
                notes: callData.notes,
                lines: callData.lines.map((l) => ({
                  ...l,
                  unitOfMeasure: 'EA',
                })),
                createSalesOrder: true,
              },
            ],
          })

          if (res.items[0]) {
            await sqliteService.markCallSynced(
              callId,
              res.items[0].salesOrderId,
              res.items[0].salesOrderNumber,
            )
            setSaveSuccess(
              `Call completed and live Sales Order ${res.items[0].salesOrderNumber || ''} created!`,
            )
          }
        } catch {
          // If server sync fails, offline SQLite copy is already secured!
          setSaveSuccess('Call completed and saved safely to SQLite (pending sync).')
        }
      } else {
        setSaveSuccess('Sales call completed and saved to offline SQLite database.')
      }

      setTimeout(() => {
        navigate('/om/salesperson/portal')
      }, 1500)
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : 'Failed to save sales call.')
      setIsSaving(false)
      setIsTimerRunning(true)
    }
  }

  return (
    <div className="space-y-4 pb-28">
      {/* Session Navigation Bar */}
      <div className="flex items-center justify-between">
        <button
          onClick={() => {
            if (activeOrderedLines.length > 0) {
              if (confirm('You have active order lines. Are you sure you want to exit the call?')) {
                navigate('/om/salesperson/portal')
              }
            } else {
              navigate('/om/salesperson/portal')
            }
          }}
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Exit Sales Call</span>
        </button>

        {/* Live Call Duration Timer */}
        <div className="flex items-center gap-2 px-3 py-1.5 bg-slate-900 text-white rounded-full text-xs font-mono font-semibold shadow-sm">
          <Clock className="w-3.5 h-3.5 text-emerald-400 animate-pulse" />
          <span>Call Duration: {formattedTimer}</span>
        </div>
      </div>

      {/* Customer Session Card */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl p-5 shadow-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-start gap-3">
            <div className="w-12 h-12 rounded-xl bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 text-indigo-700 dark:text-indigo-300 flex items-center justify-center shrink-0">
              <User className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2 flex-wrap">
                <span className="text-xs font-mono px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">
                  {customerInfo?.customerCode || 'CUST'}
                </span>
                <h1 className="text-xl font-bold text-slate-900 dark:text-white">
                  {customerInfo?.customerName || 'Customer Sales Call'}
                </h1>
                <span
                  className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-semibold ${
                    isOnline
                      ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400'
                      : 'bg-amber-50 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400'
                  }`}
                >
                  {isOnline ? <Wifi className="w-3 h-3" /> : <WifiOff className="w-3 h-3" />}
                  {isOnline ? 'Online Entry' : 'Offline SQLite Entry'}
                </span>
              </div>

              <div className="flex items-center gap-4 text-xs text-slate-500 dark:text-slate-400 mt-1 flex-wrap">
                {customerInfo?.address && (
                  <span className="flex items-center gap-1">
                    <MapPin className="w-3.5 h-3.5 text-slate-400" />
                    {customerInfo.address}
                  </span>
                )}
                {customerInfo?.phone && (
                  <span className="flex items-center gap-1">
                    <Phone className="w-3.5 h-3.5 text-slate-400" />
                    {customerInfo.phone}
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* AR Credit Info */}
          <div className="bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700/60 rounded-lg p-3 text-right">
            <span className="text-[11px] text-slate-500 uppercase tracking-wider font-semibold block">
              AR Balance / Limit
            </span>
            <div className="text-sm font-bold text-slate-800 dark:text-slate-200 tabular-nums">
              ${(customerInfo?.currentBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2 })}
              <span className="text-slate-400 font-normal">
                {' '}
                / ${(customerInfo?.creditLimit || 0).toLocaleString('en-US')}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Success Notification */}
      {saveSuccess && (
        <div className="p-4 bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-300 dark:border-emerald-700 rounded-xl text-emerald-900 dark:text-emerald-200 text-sm font-semibold flex items-center gap-2 shadow-sm">
          <CheckCircle className="w-5 h-5 text-emerald-600" />
          <span>{saveSuccess}</span>
        </div>
      )}

      {/* Search & Tabs Controls */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3">
        {/* Search */}
        <div className="relative w-full sm:w-80">
          <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            placeholder="Search products by code or description..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-9 pr-4 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg text-xs font-medium text-slate-900 dark:text-white placeholder-slate-400 outline-none focus:border-indigo-500 shadow-sm"
          />
        </div>

        {/* Filter Pills */}
        <div className="flex items-center gap-1.5 p-1 bg-slate-100 dark:bg-slate-800/80 rounded-lg">
          <button
            onClick={() => setActiveTab('all')}
            className={`px-3 py-1.5 rounded-md text-xs font-semibold transition-all ${
              activeTab === 'all'
                ? 'bg-white dark:bg-slate-900 text-indigo-600 dark:text-indigo-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900'
            }`}
          >
            All Products ({products.length})
          </button>
          <button
            onClick={() => setActiveTab('ordered')}
            className={`px-3 py-1.5 rounded-md text-xs font-semibold transition-all flex items-center gap-1.5 ${
              activeTab === 'ordered'
                ? 'bg-white dark:bg-slate-900 text-indigo-600 dark:text-indigo-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900'
            }`}
          >
            <ShoppingCart className="w-3.5 h-3.5" />
            <span>Ordered Items ({activeOrderedLines.length})</span>
          </button>
        </div>
      </div>

      {/* Products Listview Table */}
      <div className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl overflow-hidden shadow-sm">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs">
            <thead>
              <tr className="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-300 font-bold uppercase tracking-wider text-[11px]">
                <th className="py-3 px-4 w-12">#</th>
                <th className="py-3 px-4 min-w-[260px]">Product Description</th>
                <th className="py-3 px-3 w-20 text-center">UOM</th>
                <th className="py-3 px-4 w-32 text-right">Selling Price</th>
                <th className="py-3 px-4 w-44 text-center">Order Qty</th>
                <th className="py-3 px-4 w-36 text-right">Line Total</th>
                <th className="py-3 px-3 w-12 text-center"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {filteredProducts.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    No products found matching your filter.
                  </td>
                </tr>
              ) : (
                filteredProducts.map((prod, idx) => {
                  const line = orderLines[prod.id] || {
                    sellingPrice: prod.basePrice,
                    orderQty: 0,
                    lineTotal: 0,
                    discountPercent: 0,
                  }
                  const hasOrder = line.orderQty > 0

                  return (
                    <tr
                      key={prod.id}
                      className={`transition-colors ${
                        hasOrder
                          ? 'bg-emerald-50/50 dark:bg-emerald-950/20 font-medium'
                          : 'hover:bg-slate-50/80 dark:hover:bg-slate-800/40'
                      }`}
                    >
                      {/* Row # */}
                      <td className="py-3 px-4 text-slate-400 font-mono text-[11px]">
                        {idx + 1}
                      </td>

                      {/* Product Description */}
                      <td className="py-3 px-4">
                        <div className="flex flex-col">
                          <span className="text-slate-900 dark:text-white font-semibold">
                            {prod.description}
                          </span>
                          <span className="text-[11px] font-mono text-slate-400 dark:text-slate-500">
                            {prod.itemCode}
                          </span>
                        </div>
                      </td>

                      {/* UOM */}
                      <td className="py-3 px-3 text-center">
                        <span className="px-2 py-0.5 rounded text-[11px] font-mono bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300">
                          {prod.unitOfMeasure}
                        </span>
                      </td>

                      {/* Selling Price */}
                      <td className="py-3 px-4 text-right tabular-nums">
                        <div className="flex flex-col items-end">
                          <span className="font-semibold text-slate-900 dark:text-white">
                            ${line.sellingPrice.toFixed(2)}
                          </span>
                          {line.discountPercent > 0 && (
                            <span className="text-[10px] text-emerald-600 dark:text-emerald-400 font-medium">
                              {line.discountPercent}% off rule
                            </span>
                          )}
                        </div>
                      </td>

                      {/* Order Quantity Input with Quick Stepper */}
                      <td className="py-3 px-4">
                        <div className="flex items-center justify-center gap-1.5">
                          <button
                            type="button"
                            onClick={() => handleStepQty(prod.id, -1)}
                            disabled={line.orderQty <= 0}
                            className="w-7 h-7 rounded border border-slate-200 dark:border-slate-700 flex items-center justify-center text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-30 disabled:pointer-events-none"
                          >
                            <Minus className="w-3.5 h-3.5" />
                          </button>

                          <input
                            type="number"
                            min="0"
                            step="1"
                            value={line.orderQty === 0 ? '' : line.orderQty}
                            placeholder="0"
                            onChange={(e) => handleQtyChange(prod.id, parseFloat(e.target.value) || 0)}
                            onFocus={(e) => e.target.select()}
                            className="w-16 py-1 px-2 text-center text-xs font-bold text-slate-900 dark:text-white bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded outline-none focus:border-indigo-500 tabular-nums shadow-inner"
                          />

                          <button
                            type="button"
                            onClick={() => handleStepQty(prod.id, 1)}
                            className="w-7 h-7 rounded border border-slate-200 dark:border-slate-700 flex items-center justify-center text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800"
                          >
                            <Plus className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>

                      {/* Line Total */}
                      <td className="py-3 px-4 text-right tabular-nums font-bold">
                        <span className={hasOrder ? 'text-emerald-600 dark:text-emerald-400' : 'text-slate-400'}>
                          ${line.lineTotal.toFixed(2)}
                        </span>
                      </td>

                      {/* Clear line action */}
                      <td className="py-3 px-3 text-center">
                        {hasOrder && (
                          <button
                            type="button"
                            onClick={() => handleClearLine(prod.id)}
                            className="text-slate-400 hover:text-red-500 p-1"
                            title="Clear order quantity"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Sticky Bottom Bar: Grand Total & End Sales Call */}
      <div className="fixed bottom-0 left-0 right-0 z-40 bg-white/95 dark:bg-slate-900/95 backdrop-blur-md border-t border-slate-200 dark:border-slate-800 py-3.5 px-6 shadow-2xl">
        <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
          {/* Notes input */}
          <div className="w-full sm:w-1/3">
            <input
              type="text"
              placeholder="Visit notes / customer special delivery instructions..."
              value={callNotes}
              onChange={(e) => setCallNotes(e.target.value)}
              className="w-full px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-medium text-slate-900 dark:text-white placeholder-slate-400 outline-none focus:border-indigo-500"
            />
          </div>

          {/* Grand Total & Action */}
          <div className="flex items-center justify-between sm:justify-end gap-6 w-full sm:w-auto">
            {/* Summary figures */}
            <div className="text-right">
              <div className="text-[11px] text-slate-500 dark:text-slate-400 uppercase tracking-wider font-semibold">
                {activeOrderedLines.length} Item{activeOrderedLines.length === 1 ? '' : 's'} ({totalUnits} Units)
              </div>
              <div className="text-xl sm:text-2xl font-black text-slate-900 dark:text-white tabular-nums tracking-tight text-emerald-600 dark:text-emerald-400">
                ${grandTotal.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </div>
            </div>

            {/* End Sales Call Button */}
            <button
              onClick={handleEndSalesCall}
              disabled={isSaving}
              className="py-3 px-6 bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white font-bold text-sm rounded-xl shadow-lg shadow-emerald-600/25 flex items-center gap-2 transition-all disabled:opacity-50 shrink-0"
            >
              <Save className="w-4 h-4" />
              <span>{isSaving ? 'Storing Call...' : 'End Sales Call & Save Order'}</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
