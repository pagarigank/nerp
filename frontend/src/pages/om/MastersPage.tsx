import { useEffect, useState } from 'react'
import { Settings2 } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { getErrorMessage } from '@api/client'
import {
  getPricingRules,
  getSalesOrderTypes,
  getSalesReps,
  getSalesTerritories,
  getShippingMethods,
  getTaxCodes,
  getTaxExemptions,
  createTaxExemption,
  revokeTaxExemption,
  deleteTaxExemption,
  companyId as currentCompanyId,
} from '@api/orderManagement'
import type {
  PricingRuleSummary,
  SalesOrderTypeSummary,
  SalesRepSummary,
  SalesTerritorySummary,
  ShippingMethodSummary,
  TaxCodeSummary,
  TaxExemptionCertificateSummary,
} from '@/types/orderManagement'

type MasterKey = 'shipping' | 'reps' | 'territories' | 'orderTypes' | 'pricing' | 'tax' | 'exemptions'

const TABS: { key: MasterKey; label: string }[] = [
  { key: 'shipping', label: 'Shipping Methods' },
  { key: 'reps', label: 'Sales Reps' },
  { key: 'territories', label: 'Territories' },
  { key: 'orderTypes', label: 'Order Types' },
  { key: 'pricing', label: 'Pricing Rules' },
  { key: 'tax', label: 'Tax Codes' },
  { key: 'exemptions', label: 'Tax Exemptions' },
]

export function MastersPage() {
  const [tab, setTab] = useState<MasterKey>('shipping')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [shipping, setShipping] = useState<ShippingMethodSummary[]>([])
  const [reps, setReps] = useState<SalesRepSummary[]>([])
  const [territories, setTerritories] = useState<SalesTerritorySummary[]>([])
  const [orderTypes, setOrderTypes] = useState<SalesOrderTypeSummary[]>([])
  const [pricing, setPricing] = useState<PricingRuleSummary[]>([])
  const [tax, setTax] = useState<TaxCodeSummary[]>([])
  const [exemptions, setExemptions] = useState<TaxExemptionCertificateSummary[]>([])

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const [s, r, t, o, p, c, e] = await Promise.all([
        getShippingMethods(),
        getSalesReps(),
        getSalesTerritories(),
        getSalesOrderTypes(),
        getPricingRules(),
        getTaxCodes(),
        getTaxExemptions(),
      ])
      setShipping(s)
      setReps(r)
      setTerritories(t)
      setOrderTypes(o)
      setPricing(p)
      setTax(c)
      setExemptions(e)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  const data = {
    shipping,
    reps,
    territories,
    orderTypes,
    pricing,
    tax,
    exemptions,
  }[tab]

  const columns = COLUMNS[tab]

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Order Management Masters</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Reference data, pricing rules and tax codes used by sales order entry
        </p>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">
          {error}
        </div>
      )}

      <div className="flex flex-wrap gap-2 border-b border-gray-200 dark:border-gray-700">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`flex items-center gap-1.5 px-3 py-2 text-sm font-medium ${
              tab === t.key
                ? 'border-b-2 border-blue-600 text-blue-600'
                : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white'
            }`}
          >
            <Settings2 className="h-3.5 w-3.5" /> {t.label}
          </button>
        ))}
      </div>

      {tab === 'exemptions' ? (
        <TaxExemptionsPanel
          rows={exemptions}
          onRevoke={async (id) => { await revokeTaxExemption(id); await load() }}
          onDelete={async (id) => { await deleteTaxExemption(id); await load() }}
          onCreate={async (body) => { await createTaxExemption(body); await load() }}
        />
      ) : (
        <DataTable columns={columns} data={data} loading={loading} emptyMessage="No records found" />
      )}
    </div>
  )
}

const COLUMNS: Record<MasterKey, DataTableColumn[]> = {
  shipping: [
    { key: 'code', header: 'Code', sortable: true },
    { key: 'description', header: 'Description' },
    { key: 'carrier', header: 'Carrier' },
    { key: 'baseCost', header: 'Base Cost', align: 'right' as const, render: (r: ShippingMethodSummary) => `$${r.baseCost.toFixed(2)}` },
    { key: 'isActive', header: 'Active', render: (r: ShippingMethodSummary) => (r.isActive ? 'Yes' : 'No') },
  ],
  reps: [
    { key: 'code', header: 'Code', sortable: true },
    { key: 'name', header: 'Name' },
    { key: 'commissionRate', header: 'Commission %', align: 'right' as const, render: (r: SalesRepSummary) => `${r.commissionRate}%` },
    { key: 'email', header: 'Email' },
    { key: 'isActive', header: 'Active', render: (r: SalesRepSummary) => (r.isActive ? 'Yes' : 'No') },
  ],
  territories: [
    { key: 'code', header: 'Code', sortable: true },
    { key: 'name', header: 'Name' },
    { key: 'region', header: 'Region' },
    { key: 'defaultCommissionRate', header: 'Def. Comm %', align: 'right' as const, render: (r: SalesTerritorySummary) => `${r.defaultCommissionRate}%` },
    { key: 'isActive', header: 'Active', render: (r: SalesTerritorySummary) => (r.isActive ? 'Yes' : 'No') },
  ],
  orderTypes: [
    { key: 'code', header: 'Code', sortable: true },
    { key: 'description', header: 'Description' },
    { key: 'typeCode', header: 'Type' },
    { key: 'isActive', header: 'Active', render: (r: SalesOrderTypeSummary) => (r.isActive ? 'Yes' : 'No') },
  ],
  pricing: [
    { key: 'code', header: 'Code', sortable: true },
    { key: 'scope', header: 'Scope' },
    { key: 'prioritySequence', header: 'Priority', align: 'right' as const },
    { key: 'discountPercent', header: 'Discount %', align: 'right' as const, render: (r: PricingRuleSummary) => `${r.discountPercent}%` },
    { key: 'unitPriceOverride', header: 'Override', align: 'right' as const, render: (r: PricingRuleSummary) => (r.unitPriceOverride != null ? `$${r.unitPriceOverride.toFixed(2)}` : '—') },
    { key: 'isActive', header: 'Active', render: (r: PricingRuleSummary) => (r.isActive ? 'Yes' : 'No') },
  ],
  tax: [
    { key: 'code', header: 'Code', sortable: true },
    { key: 'description', header: 'Description' },
    { key: 'jurisdiction', header: 'Jurisdiction' },
    { key: 'rate', header: 'Rate %', align: 'right' as const, render: (r: TaxCodeSummary) => `${r.rate}%` },
    { key: 'isTaxable', header: 'Taxable', render: (r: TaxCodeSummary) => (r.isTaxable ? 'Yes' : 'No') },
  ],
  exemptions: [
    { key: 'certificateNumber', header: 'Certificate #' },
    { key: 'jurisdiction', header: 'Jurisdiction' },
  ],
}

function TaxExemptionsPanel({
  rows,
  onRevoke,
  onDelete,
  onCreate,
}: {
  rows: TaxExemptionCertificateSummary[]
  onRevoke: (id: string) => Promise<void>
  onDelete: (id: string) => Promise<void>
  onCreate: (body: Record<string, unknown>) => Promise<void>
}) {
  const today = new Date().toISOString().slice(0, 10)
  const [cert, setCert] = useState('')
  const [jurisdiction, setJurisdiction] = useState('')
  const [validFrom, setValidFrom] = useState(today)
  const [validTo, setValidTo] = useState(today)
  const [busy, setBusy] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setFormError(null)
    try {
      await onCreate({
        companyId: currentCompanyId(),
        certificateNumber: cert,
        customerId: null,
        jurisdiction,
        validFrom,
        validTo,
        exemptItemsDescription: null,
        notes: null,
      })
      setCert('')
      setJurisdiction('')
    } catch (e) {
      setFormError(getErrorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-4">
      <form onSubmit={submit} className="grid grid-cols-1 gap-3 rounded-lg border border-gray-200 p-4 dark:border-gray-700 sm:grid-cols-5">
        <input className="rounded border border-gray-300 p-2 text-sm dark:bg-gray-800" placeholder="Certificate #" value={cert} onChange={(e) => setCert(e.target.value)} required />
        <input className="rounded border border-gray-300 p-2 text-sm dark:bg-gray-800" placeholder="Jurisdiction" value={jurisdiction} onChange={(e) => setJurisdiction(e.target.value)} required />
        <input type="date" className="rounded border border-gray-300 p-2 text-sm dark:bg-gray-800" value={validFrom} onChange={(e) => setValidFrom(e.target.value)} />
        <input type="date" className="rounded border border-gray-300 p-2 text-sm dark:bg-gray-800" value={validTo} onChange={(e) => setValidTo(e.target.value)} />
        <button type="submit" disabled={busy} className="rounded bg-blue-600 px-3 py-2 text-sm font-medium text-white disabled:opacity-50">
          {busy ? 'Saving…' : 'Add certificate'}
        </button>
        {formError && <p className="col-span-full text-sm text-red-600">{formError}</p>}
      </form>

      <div className="overflow-x-auto rounded-lg border border-gray-200 dark:border-gray-700">
        <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead className="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Certificate #</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Jurisdiction</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Valid From</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Valid To</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Status</th>
              <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {rows.length === 0 ? (
              <tr><td colSpan={6} className="px-3 py-6 text-center text-sm text-gray-400">No certificates found.</td></tr>
            ) : (
              rows.map((r) => (
                <tr key={r.id}>
                  <td className="px-3 py-2 text-sm">{r.certificateNumber}</td>
                  <td className="px-3 py-2 text-sm">{r.jurisdiction}</td>
                  <td className="px-3 py-2 text-sm">{new Date(r.validFrom).toLocaleDateString()}</td>
                  <td className="px-3 py-2 text-sm">{new Date(r.validTo).toLocaleDateString()}</td>
                  <td className="px-3 py-2 text-sm">{r.isActive ? 'Active' : 'Revoked'}</td>
                  <td className="px-3 py-2 text-right text-sm">
                    {r.isActive && (
                      <button onClick={() => void onRevoke(r.id)} className="mr-2 font-medium text-amber-600 hover:underline">Revoke</button>
                    )}
                    <button onClick={() => void onDelete(r.id)} className="font-medium text-red-600 hover:underline">Delete</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
