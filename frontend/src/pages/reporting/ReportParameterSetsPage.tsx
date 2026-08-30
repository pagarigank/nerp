import { currentCompanyId } from '@/api/company'
import { useState, useEffect, useCallback } from 'react'
import { Settings, Plus, Play, Star, Trash2 } from 'lucide-react'

interface ParameterSet {
  id: string
  reportDefinitionId: string
  name: string
  parametersJson: string
  isDefault: boolean
  description?: string
  runCount: number
}

export function ReportParameterSetsPage() {
  const [parameterSets, setParameterSets] = useState<ParameterSet[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedSet, setSelectedSet] = useState<ParameterSet | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [reportId, setReportId] = useState('')
  const [editForm, setEditForm] = useState({ name: '', parametersJson: '{}', isDefault: false, description: '' })

  const fetchParameterSets = useCallback(async () => {
    if (!reportId) return
    setLoading(true)
    try {
      const response = await fetch(`/api/v1/reporting/parameter-sets?reportDefinitionId=${reportId}`)
      const data = await response.json()
      setParameterSets(data.data || [])
    } catch (err) {
      console.error('Failed to fetch parameter sets:', err)
    } finally {
      setLoading(false)
    }
  }, [reportId])

  useEffect(() => { fetchParameterSets() }, [fetchParameterSets])

  const handleCreate = async () => {
    try {
      await fetch('/api/v1/reporting/parameter-sets', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ companyId: currentCompanyId(), reportDefinitionId: reportId, ...editForm })
      })
      setIsCreating(false)
      setEditForm({ name: '', parametersJson: '{}', isDefault: false, description: '' })
      fetchParameterSets()
    } catch (err) {
      console.error('Failed to create parameter set:', err)
    }
  }

  const handleRun = async (id: string) => {
    try {
      const response = await fetch(`/api/v1/reporting/parameter-sets/${id}/run`, { method: 'POST' })
      const data = await response.json()
      alert(`Report executed with ${data.data?.name} parameters (${data.data?.RunCount} total runs)`)
      fetchParameterSets()
    } catch (err) {
      console.error('Failed to run parameter set:', err)
    }
  }

  const handleSetDefault = async (id: string) => {
    try {
      await fetch(`/api/v1/reporting/parameter-sets/${id}/set-default`, { method: 'POST' })
      fetchParameterSets()
    } catch (err) {
      console.error('Failed to set default:', err)
    }
  }

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this parameter set?')) return
    try {
      await fetch(`/api/v1/reporting/parameter-sets/${id}`, { method: 'DELETE' })
      fetchParameterSets()
    } catch (err) {
      console.error('Failed to delete:', err)
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Report Parameter Sets</h1>
          <p className="text-gray-500 mt-1">Save and reuse parameter combinations for reports</p>
        </div>
        <button
          onClick={() => setIsCreating(true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 flex items-center gap-2"
        >
          <Plus size={16} /> New Parameter Set
        </button>
      </div>

      <div className="flex gap-4 items-center">
        <label className="text-sm font-medium">Report ID:</label>
        <input
          type="text"
          placeholder="Enter report definition ID"
          value={reportId}
          onChange={e => setReportId(e.target.value)}
          className="px-3 py-2 border rounded-lg text-sm flex-1 max-w-md"
        />
      </div>

      {isCreating && (
        <div className="bg-white dark:bg-gray-900 rounded-xl border p-4 space-y-3">
          <h3 className="font-semibold">New Parameter Set</h3>
          <input
            type="text"
            placeholder="Parameter set name"
            value={editForm.name}
            onChange={e => setEditForm({ ...editForm, name: e.target.value })}
            className="w-full px-3 py-2 border rounded-lg text-sm"
          />
          <textarea
            placeholder='Parameters JSON (e.g., {"CompanyId":"...","PeriodId":"..."})'
            value={editForm.parametersJson}
            onChange={e => setEditForm({ ...editForm, parametersJson: e.target.value })}
            className="w-full px-3 py-2 border rounded-lg text-sm font-mono"
            rows={4}
          />
          <input
            type="text"
            placeholder="Description (optional)"
            value={editForm.description}
            onChange={e => setEditForm({ ...editForm, description: e.target.value })}
            className="w-full px-3 py-2 border rounded-lg text-sm"
          />
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={editForm.isDefault} onChange={e => setEditForm({ ...editForm, isDefault: e.target.checked })} />
            Set as default for this report
          </label>
          <div className="flex gap-2">
            <button onClick={handleCreate} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm">Create</button>
            <button onClick={() => setIsCreating(false)} className="px-4 py-2 border rounded-lg text-sm">Cancel</button>
          </div>
        </div>
      )}

      {loading ? (
        <div className="text-center py-8 text-gray-500">Loading parameter sets...</div>
      ) : parameterSets.length === 0 ? (
        <div className="text-center py-12 bg-white dark:bg-gray-900 rounded-xl border">
          <Settings size={48} className="mx-auto text-gray-300 mb-4" />
          <p className="text-gray-500">No parameter sets for this report. Create one to save commonly-used parameters.</p>
        </div>
      ) : (
        <div className="grid grid-cols-2 lg:grid-cols-3 gap-4">
          {parameterSets.map(ps => (
            <div
              key={ps.id}
              className={`bg-white dark:bg-gray-900 rounded-xl border p-4 cursor-pointer hover:border-blue-300 transition ${selectedSet?.id === ps.id ? 'border-blue-500 ring-2 ring-blue-200' : ''}`}
              onClick={() => setSelectedSet(ps)}
            >
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold flex items-center gap-2">
                    {ps.name}
                    {ps.isDefault && <Star size={14} className="text-amber-500 fill-amber-500" />}
                  </h3>
                  {ps.description && <p className="text-xs text-gray-500 mt-1">{ps.description}</p>}
                </div>
              </div>
              <div className="mt-3 flex items-center gap-2 text-xs text-gray-500">
                <span>Run {ps.runCount}x</span>
                <span>•</span>
                <span>{ps.isDefault ? 'Default' : 'Custom'}</span>
              </div>
              <div className="mt-3 flex gap-1">
                <button onClick={(e) => { e.stopPropagation(); handleRun(ps.id) }} className="px-2 py-1 bg-green-100 text-green-700 rounded text-xs flex items-center gap-1">
                  <Play size={12} /> Run
                </button>
                {!ps.isDefault && (
                  <button onClick={(e) => { e.stopPropagation(); handleSetDefault(ps.id) }} className="px-2 py-1 bg-amber-100 text-amber-700 rounded text-xs">Default</button>
                )}
                <button onClick={(e) => { e.stopPropagation(); handleDelete(ps.id) }} className="px-2 py-1 bg-red-100 text-red-700 rounded text-xs flex items-center gap-1">
                  <Trash2 size={12} />
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
