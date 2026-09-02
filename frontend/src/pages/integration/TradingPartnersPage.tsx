import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  getTradingPartners,
  createTradingPartner,
  updateTradingPartner,
  activateTradingPartner,
  deactivateTradingPartner,
  deleteTradingPartner,
} from '@/api/integration'

function PartnerForm({ onSave, onCancel }: { onSave: (data: any) => void; onCancel: () => void }) {
  const [form, setForm] = useState({
    companyId: '11111111-1111-1111-1111-111111111111',
    partnerCode: '',
    name: '',
    isaId: '',
    gsSenderId: '',
    gsReceiverId: '',
    protocol: 'AS2',
    ediVersion: '5010',
  })
  return (
    <Card>
      <CardHeader><CardTitle>New Trading Partner</CardTitle></CardHeader>
      <CardContent className="space-y-3">
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="text-sm font-medium">Partner Code *</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.partnerCode} onChange={e => setForm({ ...form, partnerCode: e.target.value })} />
          </div>
          <div>
            <label className="text-sm font-medium">Name *</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} />
          </div>
          <div>
            <label className="text-sm font-medium">ISA ID</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.isaId} onChange={e => setForm({ ...form, isaId: e.target.value })} placeholder="Sender/Receiver ID" />
          </div>
          <div>
            <label className="text-sm font-medium">Protocol</label>
            <select className="w-full border rounded px-3 py-2 text-sm" value={form.protocol} onChange={e => setForm({ ...form, protocol: e.target.value })}>
              <option value="AS2">AS2</option>
              <option value="SFTP">SFTP</option>
              <option value="OFTP">OFTP</option>
              <option value="VAN">VAN (Value Added Network)</option>
              <option value="API">REST API</option>
            </select>
          </div>
          <div>
            <label className="text-sm font-medium">GS Sender ID</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.gsSenderId} onChange={e => setForm({ ...form, gsSenderId: e.target.value })} />
          </div>
          <div>
            <label className="text-sm font-medium">GS Receiver ID</label>
            <input className="w-full border rounded px-3 py-2 text-sm" value={form.gsReceiverId} onChange={e => setForm({ ...form, gsReceiverId: e.target.value })} />
          </div>
          <div>
            <label className="text-sm font-medium">EDI Version</label>
            <select className="w-full border rounded px-3 py-2 text-sm" value={form.ediVersion} onChange={e => setForm({ ...form, ediVersion: e.target.value })}>
              <option value="4010">4010</option>
              <option value="5010">5010</option>
            </select>
          </div>
        </div>
        <div className="flex gap-2 pt-2">
          <Button size="sm" onClick={() => onSave(form)}>Create</Button>
          <Button size="sm" variant="outline" onClick={onCancel}>Cancel</Button>
        </div>
      </CardContent>
    </Card>
  )
}

export default function TradingPartnersPage() {
  const [partners, setPartners] = useState<any[]>([])
  const [showForm, setShowForm] = useState(false)
  const [loading, setLoading] = useState(true)

  const load = () => {
    getTradingPartners().then(setPartners).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleCreate = async (data: any) => {
    await createTradingPartner(data)
    setShowForm(false)
    load()
  }

  const handleToggle = async (id: string, isActive: boolean) => {
    if (isActive) await deactivateTradingPartner(id); else await activateTradingPartner(id)
    load()
  }

  const handleDelete = async (id: string) => {
    if (confirm('Delete this trading partner?')) {
      await deleteTradingPartner(id)
      load()
    }
  }

  if (loading) return <div className="p-8 text-gray-500">Loading...</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Trading Partners</h1>
        <Button onClick={() => setShowForm(true)}>+ New Partner</Button>
      </div>

      {showForm && <PartnerForm onSave={handleCreate} onCancel={() => setShowForm(false)} />}

      <Card>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left p-3">Code</th>
                <th className="text-left p-3">Name</th>
                <th className="text-left p-3">Protocol</th>
                <th className="text-left p-3">EDI Version</th>
                <th className="text-center p-3">Status</th>
                <th className="text-right p-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {partners.map((p: any) => (
                <tr key={p.id} className="border-b hover:bg-gray-50">
                  <td className="p-3 font-mono">{p.partnerCode}</td>
                  <td className="p-3">{p.name}</td>
                  <td className="p-3">{p.protocol || '-'}</td>
                  <td className="p-3">{p.ediVersion}</td>
                  <td className="p-3 text-center">
                    <Badge className={p.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}>
                      {p.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                  </td>
                  <td className="p-3 text-right space-x-2">
                    <Button size="sm" variant="outline" onClick={() => handleToggle(p.id, p.isActive)}>
                      {p.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                    <Button size="sm" variant="destructive" onClick={() => handleDelete(p.id)}>Delete</Button>
                  </td>
                </tr>
              ))}
              {partners.length === 0 && (
                <tr><td colSpan={6} className="p-8 text-center text-gray-500">No trading partners configured.</td></tr>
              )}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  )
}
