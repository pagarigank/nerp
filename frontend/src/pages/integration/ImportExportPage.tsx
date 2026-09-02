import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  getImportJobs,
  getImportJob,
  createImportJob,
  addImportLines,
  validateImportJob,
  commitImportJob,
  resetImportJob,
  deleteImportJob,
} from '@/api/integration'

const statusColors: Record<string, string> = {
  Draft: 'bg-gray-100 text-gray-800',
  Validating: 'bg-blue-100 text-blue-800',
  Validated: 'bg-green-100 text-green-800',
  Committing: 'bg-blue-100 text-blue-800',
  Committed: 'bg-green-100 text-green-800',
  Failed: 'bg-red-100 text-red-800',
}

function ImportWizard({ onComplete }: { onComplete: () => void }) {
  const [step, setStep] = useState<'config' | 'upload' | 'validate' | 'commit'>('config')
  const [jobId, setJobId] = useState<string | null>(null)
  const [jobDetail, setJobDetail] = useState<any>(null)
  const [form, setForm] = useState({ jobName: '', fileFormat: 'csv', destination: 'vendors' })
  const [textInput, setTextInput] = useState('')
  const [processing, setProcessing] = useState(false)

  const handleCreate = async () => {
    const job = await createImportJob({
      companyId: '11111111-1111-1111-1111-111111111111',
      ...form,
    })
    setJobId(job.id)
    setStep('upload')
  }

  const handleUpload = async () => {
    if (!jobId || !textInput.trim()) return
    setProcessing(true)
    const rows = textInput.split('\n').filter(r => r.trim()).map(r => JSON.stringify({ raw: r }))
    await addImportLines(jobId, rows)
    const detail = await getImportJob(jobId)
    setJobDetail(detail)
    setProcessing(false)
    setStep('validate')
  }

  const handleValidate = async () => {
    if (!jobId) return
    setProcessing(true)
    const result = await validateImportJob(jobId)
    const detail = await getImportJob(jobId)
    setJobDetail(detail)
    setProcessing(false)
    if (result.errorRows === 0) setStep('commit')
  }

  const handleCommit = async () => {
    if (!jobId) return
    setProcessing(true)
    await commitImportJob(jobId)
    const detail = await getImportJob(jobId)
    setJobDetail(detail)
    setProcessing(false)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Import Wizard — Step: {step.toUpperCase()}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {step === 'config' && (
          <div className="space-y-3">
            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="text-sm font-medium">Job Name *</label>
                <input className="w-full border rounded px-3 py-2 text-sm" value={form.jobName} onChange={e => setForm({ ...form, jobName: e.target.value })} />
              </div>
              <div>
                <label className="text-sm font-medium">Format</label>
                <select className="w-full border rounded px-3 py-2 text-sm" value={form.fileFormat} onChange={e => setForm({ ...form, fileFormat: e.target.value })}>
                  <option value="csv">CSV</option>
                  <option value="json">JSON</option>
                  <option value="edi">EDI</option>
                </select>
              </div>
              <div>
                <label className="text-sm font-medium">Destination</label>
                <select className="w-full border rounded px-3 py-2 text-sm" value={form.destination} onChange={e => setForm({ ...form, destination: e.target.value })}>
                  <option value="vendors">Vendors</option>
                  <option value="customers">Customers</option>
                  <option value="items">Items</option>
                  <option value="employees">Employees</option>
                  <option value="projects">Projects</option>
                </select>
              </div>
            </div>
            <Button onClick={handleCreate} disabled={!form.jobName}>Next: Upload Data</Button>
          </div>
        )}

        {step === 'upload' && (
          <div className="space-y-3">
            <p className="text-sm text-gray-600">Paste your data (one JSON object per line):</p>
            <textarea
              className="w-full border rounded px-3 py-2 text-sm font-mono h-48"
              value={textInput}
              onChange={e => setTextInput(e.target.value)}
              placeholder='{"code": "V001", "name": "Acme Corp"}&#10;{"code": "V002", "name": "Widget Inc"}'
            />
            <Button onClick={handleUpload} disabled={processing || !textInput.trim()}>
              {processing ? 'Uploading...' : 'Upload & Parse'}
            </Button>
          </div>
        )}

        {step === 'validate' && jobDetail && (
          <div className="space-y-3">
            <div className="grid grid-cols-4 gap-4 text-center">
              <div><div className="text-2xl font-bold">{jobDetail.totalRows}</div><div className="text-xs text-gray-500">Total Rows</div></div>
              <div><div className="text-2xl font-bold text-green-600">{jobDetail.validatedRows}</div><div className="text-xs text-gray-500">Valid</div></div>
              <div><div className="text-2xl font-bold text-red-600">{jobDetail.errorRows}</div><div className="text-xs text-gray-500">Errors</div></div>
              <div><Badge className={statusColors[jobDetail.status] || ''}>{jobDetail.status}</Badge></div>
            </div>
            {jobDetail.lines?.filter((l: any) => l.validationStatus === 'Invalid').length > 0 && (
              <div className="bg-red-50 border border-red-200 rounded p-3">
                <div className="text-sm font-semibold text-red-800">Validation Errors:</div>
                {jobDetail.lines.filter((l: any) => l.validationStatus === 'Invalid').map((l: any) => (
                  <div key={l.id} className="text-xs text-red-700 mt-1">Row {l.rowNumber}: {l.errors}</div>
                ))}
              </div>
            )}
            <div className="flex gap-2">
              {jobDetail.errorRows === 0 && (
                <Button onClick={handleCommit} disabled={processing}>
                  {processing ? 'Committing...' : 'Commit Import'}
                </Button>
              )}
              <Button variant="outline" onClick={() => { resetImportJob(jobId!).then(() => { setStep('upload'); setTextInput('') }) }}>
                Reset & Re-upload
              </Button>
            </div>
          </div>
        )}

        {step === 'commit' && jobDetail && (
          <div className="space-y-3">
            <div className="bg-green-50 border border-green-200 rounded p-4 text-center">
              <div className="text-lg font-bold text-green-800">✓ Import Committed</div>
              <div className="text-sm text-green-700 mt-1">{jobDetail.committedRows} rows imported successfully.</div>
            </div>
            <Button onClick={onComplete}>Done</Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

export default function ImportExportPage() {
  const [jobs, setJobs] = useState<any[]>([])
  const [showWizard, setShowWizard] = useState(false)
  const [loading, setLoading] = useState(true)

  const load = () => {
    getImportJobs().then(setJobs).finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleDelete = async (id: string) => {
    if (confirm('Delete this import job?')) {
      await deleteImportJob(id)
      load()
    }
  }

  if (loading) return <div className="p-8 text-gray-500">Loading...</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Import / Export Jobs</h1>
        <Button onClick={() => setShowWizard(true)}>+ New Import</Button>
      </div>

      {showWizard && (
        <ImportWizard onComplete={() => { setShowWizard(false); load() }} />
      )}

      <Card>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-gray-50">
                <th className="text-left p-3">Job Name</th>
                <th className="text-left p-3">Format</th>
                <th className="text-left p-3">Destination</th>
                <th className="text-center p-3">Rows</th>
                <th className="text-center p-3">Status</th>
                <th className="text-right p-3">Actions</th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((j: any) => (
                <tr key={j.id} className="border-b hover:bg-gray-50">
                  <td className="p-3 font-medium">{j.jobName}</td>
                  <td className="p-3 uppercase text-xs">{j.fileFormat}</td>
                  <td className="p-3">{j.destination}</td>
                  <td className="p-3 text-center">{j.totalRows}</td>
                  <td className="p-3 text-center">
                    <Badge className={statusColors[j.status] || ''}>{j.status}</Badge>
                  </td>
                  <td className="p-3 text-right">
                    {j.status !== 'Committed' && (
                      <Button size="sm" variant="destructive" onClick={() => handleDelete(j.id)}>Delete</Button>
                    )}
                  </td>
                </tr>
              ))}
              {jobs.length === 0 && (
                <tr><td colSpan={6} className="p-8 text-center text-gray-500">No import jobs yet.</td></tr>
              )}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  )
}
