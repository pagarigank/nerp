// Inventory > UOM (global Unit of Measure master).
import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import {
  getUoms,
  createUom,
  updateUom,
  deleteUom,
  companyId,
  type UnitOfMeasureSummary,
} from '@api/uom'

export function UnitOfMeasuresPage() {
  const qc = useQueryClient()
  const [err, setErr] = useState<string | null>(null)
  const [ok, setOk] = useState<string | null>(null)
  const [editing, setEditing] = useState<UnitOfMeasureSummary | null>(null)
  const [code, setCode] = useState('')
  const [description, setDescription] = useState('')
  const [baseUOM, setBaseUOM] = useState('EA')
  const [factor, setFactor] = useState('1')

  const { data: uoms = [], isLoading } = useQuery({
    queryKey: ['inventory', 'uoms'],
    queryFn: () => getUoms(),
  })

  const save = useMutation({
    mutationFn: () => {
      const body = {
        companyId: companyId(),
        code,
        description,
        baseUOM,
        factorToBase: Number(factor),
      }
      return editing ? updateUom(editing.id, body) : createUom(body)
    },
    onSuccess: () => {
      setOk(editing ? 'UOM updated' : 'UOM created')
      setErr(null)
      setEditing(null)
      setCode('')
      setDescription('')
      setBaseUOM('EA')
      setFactor('1')
      void qc.invalidateQueries({ queryKey: ['inventory', 'uoms'] })
    },
    onError: (e) => {
      setErr(getErrorMessage(e))
      setOk(null)
    },
  })

  const remove = useMutation({
    mutationFn: (id: string) => deleteUom(id),
    onSuccess: () => {
      setOk('UOM deleted')
      setErr(null)
      void qc.invalidateQueries({ queryKey: ['inventory', 'uoms'] })
    },
    onError: (e) => {
      setErr(getErrorMessage(e))
      setOk(null)
    },
  })

  const startEdit = (u: UnitOfMeasureSummary) => {
    setEditing(u)
    setCode(u.code)
    setDescription(u.description)
    setBaseUOM(u.baseUOM)
    setFactor(String(u.factorToBase))
  }

  const uomOptions = [...new Set([...uoms.map((u) => u.code), baseUOM])].map((c) => ({ value: c, label: c }))

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-slate-800 dark:text-slate-100">Unit of Measures</h1>
        <p className="text-sm text-slate-500">
          Define the UOM codes and the quantity they represent in terms of a base UOM (e.g. 1 Case = 12 EA).
        </p>
      </div>

      {err && <div className="p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">{err}</div>}
      {ok && <div className="p-4 rounded-lg bg-green-50 border border-green-200 text-green-700 text-sm">{ok}</div>}

      <Card>
        <CardHeader>{editing ? 'Edit UOM' : 'New UOM'}</CardHeader>
        <CardContent>
          <div className="flex gap-4 items-end flex-wrap">
            <Input label="Code" value={code} onChange={(e) => setCode(e.target.value.toUpperCase())} disabled={!!editing} />
            <Input label="Description" value={description} onChange={(e) => setDescription(e.target.value)} />
            <Select label="Base UOM" options={uomOptions} value={baseUOM} onChange={(e) => setBaseUOM(e.target.value)} />
            <Input label="Factor to Base (1 Code = ? Base)" value={factor} onChange={(e) => setFactor(e.target.value)} />
            <Button onClick={() => save.mutate()} disabled={!code || !baseUOM || !factor}>
              {editing ? 'Update' : 'Create'}
            </Button>
            {editing && (
              <Button variant="ghost" onClick={() => { setEditing(null); setCode(''); setDescription(''); setBaseUOM('EA'); setFactor('1') }}>
                Cancel
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>Defined UOMs</CardHeader>
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-slate-500">Loading…</p>
          ) : uoms.length === 0 ? (
            <p className="text-sm text-slate-500">No UOMs defined yet.</p>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="text-left border-b border-slate-200 dark:border-slate-700">
                  <th className="py-2">Code</th>
                  <th className="py-2">Description</th>
                  <th className="py-2">Base UOM</th>
                  <th className="py-2">Factor</th>
                  <th className="py-2">Active</th>
                  <th className="py-2"></th>
                </tr>
              </thead>
              <tbody>
                {uoms.map((u) => (
                  <tr key={u.id} className="border-b border-slate-100 dark:border-slate-800">
                    <td className="py-2 font-medium">{u.code}</td>
                    <td className="py-2">{u.description}</td>
                    <td className="py-2">{u.baseUOM}</td>
                    <td className="py-2">{u.factorToBase}</td>
                    <td className="py-2">{u.isActive ? 'Yes' : 'No'}</td>
                    <td className="py-2 text-right space-x-2">
                      <Button variant="ghost" size="sm" onClick={() => startEdit(u)}>Edit</Button>
                      <Button variant="ghost" size="sm" onClick={() => remove.mutate(u.id)}>Delete</Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
