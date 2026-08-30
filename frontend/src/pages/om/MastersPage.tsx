import { useEffect, useState } from 'react'
import { Settings2, Pencil, Trash2, Plus } from 'lucide-react'
import { DataTable, type DataTableColumn } from '@components/ui/DataTable'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Card } from '@components/ui/Card'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getCustomers } from '@api/ar'
import { getItems, getItemCategories } from '@api/inventory'
import { getAccounts } from '@api/platform'
import {
  getShippingMethods,
  getSalesReps,
  getSalesTerritories,
  getSalesOrderTypes,
  getPricingRules,
  getTaxCodes,
  getTaxExemptions,
  createTaxExemption,
  revokeTaxExemption,
  deleteTaxExemption,
  createShippingMethod,
  updateShippingMethod,
  deleteShippingMethod,
  createSalesRep,
  updateSalesRep,
  deleteSalesRep,
  createSalesTerritory,
  updateSalesTerritory,
  deleteSalesTerritory,
  createSalesOrderType,
  updateSalesOrderType,
  deleteSalesOrderType,
  createPricingRule,
  updatePricingRule,
  deletePricingRule,
  createTaxCode,
  updateTaxCode,
  deleteTaxCode,
  companyId as currentCompanyId,
} from '@api/orderManagement'
import type {
  ShippingMethodSummary,
  SalesRepSummary,
  SalesTerritorySummary,
  SalesOrderTypeSummary,
  PricingRuleSummary,
  TaxCodeSummary,
  TaxExemptionCertificateSummary,
  CreateShippingMethodRequest,
  UpdateShippingMethodRequest,
  CreateSalesRepRequest,
  UpdateSalesRepRequest,
  CreateSalesTerritoryRequest,
  UpdateSalesTerritoryRequest,
  CreateSalesOrderTypeRequest,
  UpdateSalesOrderTypeRequest,
  CreatePricingRuleRequest,
  UpdatePricingRuleRequest,
  CreateTaxCodeRequest,
  UpdateTaxCodeRequest,
  SalesOrderTypeCodeValue,
  PricingRuleScopeValue,
} from '@/types/orderManagement'

type FormValue = string | number | boolean | null
type FormState = Record<string, FormValue>

type FieldType = 'text' | 'number' | 'date' | 'checkbox' | 'select' | 'guid' | 'lookup'
interface FieldDef {
  name: string
  label: string
  type: FieldType
  required?: boolean
  options?: { value: string; label: string }[]
  loadOptions?: () => Promise<{ value: string; label: string }[]>
  excludeFromUpdate?: boolean
  step?: string
}

interface MasterConfig<T> {
  key: string
  label: string
  load: () => Promise<T[]>
  create: (data: unknown) => Promise<string>
  update: (id: string, data: unknown) => Promise<string>
  remove: (id: string) => Promise<string>
  toCreate: (f: FormState) => unknown
  toUpdate: (f: FormState) => unknown
  columns: DataTableColumn<T>[]
  fields: FieldDef[]
  emptyForm: () => FormState
}

const YESNO = (v: boolean) => (v ? 'Yes' : 'No')
const MONEY = (v: number) => `$${Number(v).toFixed(2)}`

const ORDER_TYPE_OPTIONS = (['Quote', 'Order', 'Return', 'CreditOnly'] as SalesOrderTypeCodeValue[]).map((v) => ({ value: v, label: v }))
const PRICING_SCOPE_OPTIONS = (['Standard', 'CustomerSpecific', 'QuantityBreak', 'Promotional'] as PricingRuleScopeValue[]).map((v) => ({ value: v, label: v }))

const DEMO_COMPANY = currentCompanyId()
const resolveCompanyId = () => currentCompanyId() || DEMO_COMPANY

const MASTERS: MasterConfig<any>[] = [
  {
    key: 'shipping',
    label: 'Shipping Methods',
    load: () => getShippingMethods() as unknown as Promise<any[]>,
    create: (d) => createShippingMethod(d as CreateShippingMethodRequest),
    update: (id, d) => updateShippingMethod(id, d as UpdateShippingMethodRequest),
    remove: (id) => deleteShippingMethod(id),
    toCreate: (f) => ({ companyId: resolveCompanyId(), code: f.code, description: f.description, carrier: f.carrier || null, baseCost: Number(f.baseCost), trackingUrlTemplate: (f.trackingUrlTemplate as string) || null }),
    toUpdate: (f) => ({ description: f.description, carrier: f.carrier || null, baseCost: Number(f.baseCost), isActive: Boolean(f.isActive), trackingUrlTemplate: (f.trackingUrlTemplate as string) || null }),
    columns: [
      { key: 'code', header: 'Code', sortable: true },
      { key: 'description', header: 'Description' },
      { key: 'carrier', header: 'Carrier' },
      { key: 'baseCost', header: 'Base Cost', align: 'right' as const, render: (r: ShippingMethodSummary) => MONEY(r.baseCost) },
      { key: 'isActive', header: 'Active', render: (r: ShippingMethodSummary) => YESNO(r.isActive) },
    ],
    fields: [
      { name: 'code', label: 'Code', type: 'text', required: true, excludeFromUpdate: true },
      { name: 'description', label: 'Description', type: 'text' },
      { name: 'carrier', label: 'Carrier', type: 'text' },
      { name: 'baseCost', label: 'Base Cost', type: 'number', step: '0.01' },
      { name: 'trackingUrlTemplate', label: 'Tracking URL Template', type: 'text' },
      { name: 'isActive', label: 'Active', type: 'checkbox' },
    ],
    emptyForm: () => ({ code: '', description: '', carrier: '', baseCost: 0, trackingUrlTemplate: '', isActive: true }),
  },
  {
    key: 'reps',
    label: 'Sales Reps',
    load: () => getSalesReps() as unknown as Promise<any[]>,
    create: (d) => createSalesRep(d as CreateSalesRepRequest),
    update: (id, d) => updateSalesRep(id, d as UpdateSalesRepRequest),
    remove: (id) => deleteSalesRep(id),
    toCreate: (f) => ({ companyId: resolveCompanyId(), code: f.code, name: f.name, commissionRate: Number(f.commissionRate), territoryId: (f.territoryId as string) || null, email: (f.email as string) || null }),
    toUpdate: (f) => ({ name: f.name, commissionRate: Number(f.commissionRate), territoryId: (f.territoryId as string) || null, isActive: Boolean(f.isActive), email: (f.email as string) || null }),
    columns: [
      { key: 'code', header: 'Code', sortable: true },
      { key: 'name', header: 'Name' },
      { key: 'commissionRate', header: 'Commission %', align: 'right' as const, render: (r: SalesRepSummary) => `${r.commissionRate}%` },
      { key: 'email', header: 'Email' },
      { key: 'isActive', header: 'Active', render: (r: SalesRepSummary) => YESNO(r.isActive) },
    ],
    fields: [
      { name: 'code', label: 'Code', type: 'text', required: true, excludeFromUpdate: true },
      { name: 'name', label: 'Name', type: 'text', required: true },
      { name: 'commissionRate', label: 'Commission %', type: 'number', step: '0.01' },
      { name: 'territoryId', label: 'Territory', type: 'lookup', loadOptions: async () => { const rows = await getSalesTerritories(); return rows.map((t) => ({ value: t.id, label: `${t.code} - ${t.name}` })) } },
      { name: 'email', label: 'Email', type: 'text' },
      { name: 'isActive', label: 'Active', type: 'checkbox' },
    ],
    emptyForm: () => ({ code: '', name: '', commissionRate: 0, territoryId: '', email: '', isActive: true }),
  },
  {
    key: 'territories',
    label: 'Territories',
    load: () => getSalesTerritories() as unknown as Promise<any[]>,
    create: (d) => createSalesTerritory(d as CreateSalesTerritoryRequest),
    update: (id, d) => updateSalesTerritory(id, d as UpdateSalesTerritoryRequest),
    remove: (id) => deleteSalesTerritory(id),
    toCreate: (f) => ({ companyId: resolveCompanyId(), code: f.code, name: f.name, region: (f.region as string) || null, defaultCommissionRate: Number(f.defaultCommissionRate) }),
    toUpdate: (f) => ({ name: f.name, region: (f.region as string) || null, defaultCommissionRate: Number(f.defaultCommissionRate), isActive: Boolean(f.isActive) }),
    columns: [
      { key: 'code', header: 'Code', sortable: true },
      { key: 'name', header: 'Name' },
      { key: 'region', header: 'Region' },
      { key: 'defaultCommissionRate', header: 'Def. Comm %', align: 'right' as const, render: (r: SalesTerritorySummary) => `${r.defaultCommissionRate}%` },
      { key: 'isActive', header: 'Active', render: (r: SalesTerritorySummary) => YESNO(r.isActive) },
    ],
    fields: [
      { name: 'code', label: 'Code', type: 'text', required: true, excludeFromUpdate: true },
      { name: 'name', label: 'Name', type: 'text', required: true },
      { name: 'region', label: 'Region', type: 'text' },
      { name: 'defaultCommissionRate', label: 'Default Commission %', type: 'number', step: '0.01' },
      { name: 'isActive', label: 'Active', type: 'checkbox' },
    ],
    emptyForm: () => ({ code: '', name: '', region: '', defaultCommissionRate: 0, isActive: true }),
  },
  {
    key: 'orderTypes',
    label: 'Order Types',
    load: () => getSalesOrderTypes() as unknown as Promise<any[]>,
    create: (d) => createSalesOrderType(d as CreateSalesOrderTypeRequest),
    update: (id, d) => updateSalesOrderType(id, d as UpdateSalesOrderTypeRequest),
    remove: (id) => deleteSalesOrderType(id),
    toCreate: (f) => ({ companyId: resolveCompanyId(), code: f.code, description: f.description, typeCode: f.typeCode as SalesOrderTypeCodeValue, revenueAccountId: (f.revenueAccountId as string) || null }),
    toUpdate: (f) => ({ description: f.description, typeCode: f.typeCode as SalesOrderTypeCodeValue, revenueAccountId: (f.revenueAccountId as string) || null, isActive: Boolean(f.isActive) }),
    columns: [
      { key: 'code', header: 'Code', sortable: true },
      { key: 'description', header: 'Description' },
      { key: 'typeCode', header: 'Type' },
      { key: 'isActive', header: 'Active', render: (r: SalesOrderTypeSummary) => YESNO(r.isActive) },
    ],
    fields: [
      { name: 'code', label: 'Code', type: 'text', required: true, excludeFromUpdate: true },
      { name: 'description', label: 'Description', type: 'text' },
      { name: 'typeCode', label: 'Type Code', type: 'select', required: true, options: ORDER_TYPE_OPTIONS },
      { name: 'revenueAccountId', label: 'Revenue Account', type: 'lookup', loadOptions: async () => { const rows = await getAccounts(); return rows.map((a) => ({ value: a.id, label: `${a.accountNumber} - ${a.description}` })) } },
      { name: 'isActive', label: 'Active', type: 'checkbox' },
    ],
    emptyForm: () => ({ code: '', description: '', typeCode: 'Order', revenueAccountId: '', isActive: true }),
  },
  {
    key: 'pricing',
    label: 'Pricing Rules',
    load: () => getPricingRules() as unknown as Promise<any[]>,
    create: (d) => createPricingRule(d as CreatePricingRuleRequest),
    update: (id, d) => updatePricingRule(id, d as UpdatePricingRuleRequest),
    remove: (id) => deletePricingRule(id),
    toCreate: (f) => ({
      companyId: resolveCompanyId(), code: f.code, description: f.description, scope: f.scope as PricingRuleScopeValue,
      prioritySequence: Number(f.prioritySequence), discountPercent: Number(f.discountPercent),
      unitPriceOverride: f.unitPriceOverride ? Number(f.unitPriceOverride) : null,
      customerId: (f.customerId as string) || null, itemId: (f.itemId as string) || null,
      itemCategoryId: (f.itemCategoryId as string) || null,
      minimumQuantity: f.minimumQuantity ? Number(f.minimumQuantity) : null,
      effectiveFrom: (f.effectiveFrom as string) || null, effectiveTo: (f.effectiveTo as string) || null,
    }),
    toUpdate: (f) => ({
      description: f.description, prioritySequence: Number(f.prioritySequence), discountPercent: Number(f.discountPercent),
      unitPriceOverride: f.unitPriceOverride ? Number(f.unitPriceOverride) : null,
      itemCategoryId: (f.itemCategoryId as string) || null,
      minimumQuantity: f.minimumQuantity ? Number(f.minimumQuantity) : null,
      effectiveFrom: (f.effectiveFrom as string) || null, effectiveTo: (f.effectiveTo as string) || null, isActive: Boolean(f.isActive),
    }),
    columns: [
      { key: 'code', header: 'Code', sortable: true },
      { key: 'scope', header: 'Scope' },
      { key: 'prioritySequence', header: 'Priority', align: 'right' as const },
      { key: 'discountPercent', header: 'Discount %', align: 'right' as const, render: (r: PricingRuleSummary) => `${r?.discountPercent ?? 0}%` },
      { key: 'unitPriceOverride', header: 'Override', align: 'right' as const, render: (r: PricingRuleSummary) => (r?.unitPriceOverride != null ? MONEY(r.unitPriceOverride) : '—') },
      { key: 'isActive', header: 'Active', render: (r: PricingRuleSummary) => YESNO(r?.isActive) },
    ],
    fields: [
      { name: 'code', label: 'Code', type: 'text', required: true, excludeFromUpdate: true },
      { name: 'description', label: 'Description', type: 'text' },
      { name: 'scope', label: 'Scope', type: 'select', required: true, options: PRICING_SCOPE_OPTIONS },
      { name: 'prioritySequence', label: 'Priority Sequence', type: 'number' },
      { name: 'discountPercent', label: 'Discount %', type: 'number', step: '0.01' },
      { name: 'unitPriceOverride', label: 'Unit Price Override', type: 'number', step: '0.01' },
      { name: 'customerId', label: 'Customer', type: 'lookup', loadOptions: async () => { const rows = await getCustomers(); return rows.map((c) => ({ value: c.id, label: `${c.customerId} - ${c.name}` })) } },
      { name: 'itemId', label: 'Item', type: 'lookup', loadOptions: async () => { const rows = await getItems(); return rows.map((i) => ({ value: i.id, label: `${i.itemCode} - ${i.description}` })) } },
      { name: 'itemCategoryId', label: 'Item Category', type: 'lookup', loadOptions: async () => { const rows = await getItemCategories(); return rows.map((c) => ({ value: c.id, label: `${c.categoryCode} - ${c.description}` })) } },
      { name: 'minimumQuantity', label: 'Minimum Quantity', type: 'number', step: '0.01' },
      { name: 'effectiveFrom', label: 'Effective From', type: 'date' },
      { name: 'effectiveTo', label: 'Effective To', type: 'date' },
      { name: 'isActive', label: 'Active', type: 'checkbox' },
    ],
    emptyForm: () => ({ code: '', description: '', scope: 'Standard', prioritySequence: 0, discountPercent: 0, unitPriceOverride: '', customerId: '', itemId: '', itemCategoryId: '', minimumQuantity: '', effectiveFrom: '', effectiveTo: '', isActive: true }),
  },
  {
    key: 'tax',
    label: 'Tax Codes',
    load: () => getTaxCodes() as unknown as Promise<any[]>,
    create: (d) => createTaxCode(d as CreateTaxCodeRequest),
    update: (id, d) => updateTaxCode(id, d as UpdateTaxCodeRequest),
    remove: (id) => deleteTaxCode(id),
    toCreate: (f) => ({ companyId: resolveCompanyId(), code: f.code, description: f.description, jurisdiction: f.jurisdiction, rate: Number(f.rate), isTaxable: Boolean(f.isTaxable), effectiveFrom: (f.effectiveFrom as string) || null, effectiveTo: (f.effectiveTo as string) || null }),
    toUpdate: (f) => ({ description: f.description, rate: Number(f.rate), isTaxable: Boolean(f.isTaxable), effectiveFrom: (f.effectiveFrom as string) || null, effectiveTo: (f.effectiveTo as string) || null, isActive: Boolean(f.isActive) }),
    columns: [
      { key: 'code', header: 'Code', sortable: true },
      { key: 'description', header: 'Description' },
      { key: 'jurisdiction', header: 'Jurisdiction' },
      { key: 'rate', header: 'Rate %', align: 'right' as const, render: (r: TaxCodeSummary) => `${r.rate}%` },
      { key: 'isTaxable', header: 'Taxable', render: (r: TaxCodeSummary) => YESNO(r.isTaxable) },
      { key: 'isActive', header: 'Active', render: (r: TaxCodeSummary) => YESNO(r.isActive) },
    ],
    fields: [
      { name: 'code', label: 'Code', type: 'text', required: true, excludeFromUpdate: true },
      { name: 'description', label: 'Description', type: 'text' },
      { name: 'jurisdiction', label: 'Jurisdiction', type: 'text' },
      { name: 'rate', label: 'Rate %', type: 'number', step: '0.01' },
      { name: 'isTaxable', label: 'Taxable', type: 'checkbox' },
      { name: 'effectiveFrom', label: 'Effective From', type: 'date' },
      { name: 'effectiveTo', label: 'Effective To', type: 'date' },
      { name: 'isActive', label: 'Active', type: 'checkbox' },
    ],
    emptyForm: () => ({ code: '', description: '', jurisdiction: '', rate: 0, isTaxable: true, effectiveFrom: '', effectiveTo: '', isActive: true }),
  },
  {
    key: 'exemptions',
    label: 'Tax Exemptions',
    load: () => getTaxExemptions() as unknown as Promise<any[]>,
    create: () => Promise.resolve(''),
    update: () => Promise.resolve(''),
    remove: () => Promise.resolve(''),
    toCreate: () => ({}),
    toUpdate: () => ({}),
    columns: [],
    fields: [],
    emptyForm: () => ({}),
  },
]

export function MastersPage() {
  const [tab, setTab] = useState('shipping')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [rowsByKey, setRowsByKey] = useState<Record<string, any[]>>({})
  const [modalOpen, setModalOpen] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>({})
  const [saving, setSaving] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [deleteId, setDeleteId] = useState<string | null>(null)
  const [lookupOptions, setLookupOptions] = useState<Record<string, { value: string; label: string }[]>>({})

  const config = MASTERS.find((m) => m.key === tab)!

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const entries = await Promise.all(MASTERS.map(async (m) => [m.key, await m.load()] as const))
      const map: Record<string, any[]> = {}
      for (const [k, v] of entries) map[k] = (v as any[])?.filter(Boolean) ?? []
      setRowsByKey(map)
    } catch (e) {
      setError(getErrorMessage(e))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function loadLookupOptions(fields: FieldDef[]) {
    const lookupFields = fields.filter((f) => f.type === 'lookup' && f.loadOptions)
    if (lookupFields.length === 0) return
    const entries = await Promise.all(
      lookupFields.map(async (f) => [f.name, await f.loadOptions!()] as const)
    )
    const map: Record<string, { value: string; label: string }[]> = {}
    for (const [k, v] of entries) map[k] = v
    setLookupOptions(map)
  }

  function openCreate() {
    setEditingId(null)
    setForm(config.emptyForm())
    setFormError(null)
    setModalOpen(true)
    void loadLookupOptions(config.fields)
  }

  function openEdit(row: any) {
    const f: FormState = {}
    for (const field of config.fields) {
      const raw = row[field.name]
      f[field.name] = field.type === 'checkbox' ? Boolean(raw) : raw ?? (field.type === 'number' ? 0 : '')
    }
    setEditingId(row.id)
    setForm(f)
    setFormError(null)
    setModalOpen(true)
    void loadLookupOptions(config.fields)
  }

  async function save() {
    setSaving(true)
    setFormError(null)
    try {
      const payload = editingId ? config.toUpdate(form) : config.toCreate(form)
      if (editingId) await config.update(editingId, payload)
      else await config.create(payload)
      setModalOpen(false)
      await load()
    } catch (e) {
      setFormError(getErrorMessage(e))
    } finally {
      setSaving(false)
    }
  }

  async function confirmDelete() {
    if (!deleteId) return
    try {
      await config.remove(deleteId)
      setDeleteId(null)
      await load()
    } catch (e) {
      setError(getErrorMessage(e))
      setDeleteId(null)
    }
  }

  function setField(name: string, value: FormValue) {
    setForm((p) => ({ ...p, [name]: value }))
  }

  const actionColumn: DataTableColumn<any> = {
    key: 'actions',
    header: 'Actions',
    render: (_: unknown, row: any) => (
      <div className="flex gap-2">
        <Button size="sm" variant="outline" onClick={() => openEdit(row)}><Pencil className="h-3.5 w-3.5" /> Edit</Button>
        <Button size="sm" variant="destructive" onClick={() => setDeleteId(row.id)}><Trash2 className="h-3.5 w-3.5" /> Delete</Button>
      </div>
    ),
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Order Management Masters</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Reference data, pricing rules and tax codes used by sales order entry
        </p>
      </div>

      {error && (
        <div className="rounded-md bg-red-50 p-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-300">{error}</div>
      )}

      <div className="flex flex-wrap gap-2 border-b border-gray-200 dark:border-gray-700">
        {MASTERS.map((m) => (
          <button
            key={m.key}
            onClick={() => setTab(m.key)}
            className={`flex items-center gap-1.5 px-3 py-2 text-sm font-medium ${
              tab === m.key ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white'
            }`}
          >
            <Settings2 className="h-3.5 w-3.5" /> {m.label}
          </button>
        ))}
      </div>

      {tab === 'exemptions' ? (
        <TaxExemptionsPanel
          rows={(rowsByKey['exemptions'] as TaxExemptionCertificateSummary[]) ?? []}
          onRevoke={async (id) => { await revokeTaxExemption(id); await load() }}
          onDelete={async (id) => { await deleteTaxExemption(id); await load() }}
          onCreate={async (body) => { await createTaxExemption(body); await load() }}
        />
      ) : (
        <Card className="p-4 space-y-4">
          <div className="flex justify-end">
            <Button variant="primary" onClick={openCreate}><Plus className="h-4 w-4" /> New {config.label.replace(/s$/, '')}</Button>
          </div>
          <DataTable columns={[...config.columns, actionColumn]} data={rowsByKey[tab] ?? []} loading={loading} emptyMessage="No records found" />
        </Card>
      )}

      <Modal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        title={editingId ? `Edit ${config.label}` : `New ${config.label}`}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModalOpen(false)} disabled={saving}>Cancel</Button>
            <Button variant="primary" onClick={save} isLoading={saving}>Save</Button>
          </>
        }
      >
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {config.fields.map((field) => (
            <div key={field.name} className={field.type === 'checkbox' ? 'flex items-center gap-2' : 'flex flex-col'}>
              {field.type === 'checkbox' ? (
                <>
                  <label className="text-sm font-medium">{field.label}</label>
                  <input type="checkbox" checked={Boolean(form[field.name])} onChange={(e) => setField(field.name, e.target.checked)} />
                </>
              ) : field.type === 'select' || field.type === 'lookup' ? (
                <>
                  <label className="text-sm font-medium">{field.label}</label>
                  <select
                    className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm"
                    value={(form[field.name] as string) ?? ''}
                    onChange={(e) => setField(field.name, e.target.value || null)}
                  >
                    <option value="">— None —</option>
                    {(field.type === 'lookup' ? lookupOptions[field.name] ?? [] : field.options ?? []).map((o) => (
                      <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                  </select>
                </>
              ) : (
                <>
                  <label className="text-sm font-medium">{field.label}</label>
                  <Input
                    type={field.type === 'number' ? 'number' : field.type === 'date' ? 'date' : 'text'}
                    step={field.step}
                    value={(form[field.name] as string | number) ?? ''}
                    onChange={(e) => setField(field.name, field.type === 'number' ? (e.target.value === '' ? '' : Number(e.target.value)) : e.target.value)}
                  />
                </>
              )}
            </div>
          ))}
        </div>
        {formError && <p className="mt-3 text-sm text-red-600">{formError}</p>}
      </Modal>

      <ConfirmDialog
        isOpen={deleteId !== null}
        onClose={() => setDeleteId(null)}
        onConfirm={confirmDelete}
        title={`Delete ${config.label}`}
        message="Are you sure you want to delete this record? This cannot be undone."
        confirmText="Delete"
        variant="danger"
      />
    </div>
  )
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
  const [customerId, setCustomerId] = useState('')
  const [jurisdiction, setJurisdiction] = useState('')
  const [validFrom, setValidFrom] = useState(today)
  const [validTo, setValidTo] = useState(today)
  const [busy, setBusy] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [customers, setCustomers] = useState<{ id: string; customerId: string; name: string }[]>([])

  useEffect(() => {
    getCustomers().then(setCustomers).catch(() => {})
  }, [])

  async function submit(e: React.FormEvent) {
    e.preventDefault()
    setBusy(true)
    setFormError(null)
    try {
      await onCreate({
        companyId: resolveCompanyId(),
        certificateNumber: cert,
        customerId: customerId || null,
        jurisdiction,
        validFrom,
        validTo,
        exemptItemsDescription: null,
        notes: null,
      })
      setCert('')
      setCustomerId('')
      setJurisdiction('')
    } catch (e) {
      setFormError(getErrorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-4">
      <form onSubmit={submit} className="grid grid-cols-1 gap-3 rounded-lg border border-gray-200 p-4 dark:border-gray-700 sm:grid-cols-3 lg:grid-cols-6">
        <input className="rounded border border-gray-300 p-2 text-sm dark:bg-gray-800" placeholder="Certificate #" value={cert} onChange={(e) => setCert(e.target.value)} required />
        <select className="rounded border border-gray-300 p-2 text-sm dark:bg-gray-800" value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
          <option value="">— Any Customer —</option>
          {customers.map((c) => (<option key={c.id} value={c.id}>{c.customerId} - {c.name}</option>))}
        </select>
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
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Customer</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Jurisdiction</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Valid From</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Valid To</th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase text-gray-500">Status</th>
              <th className="px-3 py-2 text-right text-xs font-medium uppercase text-gray-500">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            {rows.length === 0 ? (
              <tr><td colSpan={7} className="px-3 py-6 text-center text-sm text-gray-400">No certificates found.</td></tr>
            ) : (
              rows.map((r) => (
                <tr key={r.id}>
                  <td className="px-3 py-2 text-sm">{r.certificateNumber}</td>
                  <td className="px-3 py-2 text-sm">{customers.find((c) => c.id === r.customerId)?.name ?? '—'}</td>
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
