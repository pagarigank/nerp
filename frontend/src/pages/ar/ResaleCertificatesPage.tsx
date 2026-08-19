import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, FileBadge, Plus } from 'lucide-react'
import { formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Modal } from '@components/ui/Modal'
import { Input } from '@components/ui/Input'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import { getResaleCertificates, createResaleCertificate, getCustomers } from '@api/ar'
import type { CreateResaleCertificateRequest, ArCustomer } from '@/types/ar'

export function ResaleCertificatesPage() {
  const queryClient = useQueryClient()
  const [error, setError] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(false)

  const { data: certs = [], isLoading } = useQuery({
    queryKey: ['ar', 'resale-certificates'],
    queryFn: () => getResaleCertificates(),
  })
  const { data: customers = [] } = useQuery({
    queryKey: ['ar', 'customers'],
    queryFn: () => getCustomers(),
  })

  const createMutation = useMutation({
    mutationFn: (data: CreateResaleCertificateRequest) => createResaleCertificate(data),
    onSuccess: () => {
      setError(null)
      setIsOpen(false)
      queryClient.invalidateQueries({ queryKey: ['ar', 'resale-certificates'] })
    },
    onError: err => setError(getErrorMessage(err)),
  })

  return (
    <div className="space-y-6">
      {error && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Tax-Exempt Resale Certificates"
          description="Track customer resale certificates used for tax-exempt sales."
          action={
            <Button variant="primary" size="sm" leftIcon={<Plus className="h-4 w-4" />} onClick={() => setIsOpen(true)}>
              Add Certificate
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={5} />
          ) : certs.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">No resale certificates on file.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Certificate #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Customer</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Issued State</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Issue Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Expiry</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Active</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {certs.map(c => (
                    <tr key={c.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-medium text-primary-600 dark:text-primary-400">{c.certificateNumber}</td>
                      <td className="px-3 py-3">{customers.find(x => x.id === c.customerId)?.name ?? c.customerId.slice(0, 8)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{c.issuedState}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(c.issueDate)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(c.expiryDate)}</td>
                      <td className="px-3 py-3">{c.isActive ? <span className="text-emerald-600">Yes</span> : <span className="text-gray-400">No</span>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <NewCertModal isOpen={isOpen} customers={customers} onClose={() => setIsOpen(false)} onSubmit={data => createMutation.mutate(data)} isSubmitting={createMutation.isPending} />
    </div>
  )
}

interface NewCertModalProps {
  isOpen: boolean
  customers: ArCustomer[]
  onClose: () => void
  onSubmit: (data: CreateResaleCertificateRequest) => void
  isSubmitting: boolean
}

function NewCertModal({ isOpen, customers, onClose, onSubmit, isSubmitting }: NewCertModalProps) {
  const [customerId, setCustomerId] = useState('')
  const [certNumber, setCertNumber] = useState('')
  const [issuedState, setIssuedState] = useState('')
  const [issueDate, setIssueDate] = useState(new Date().toISOString().slice(0, 10))
  const [expiryDate, setExpiryDate] = useState(new Date(Date.now() + 365 * 864e5).toISOString().slice(0, 10))

  const submit = () => {
    if (!customerId || !certNumber.trim() || !issuedState.trim()) return
    onSubmit({
      companyId: customers[0]?.id ?? '',
      customerId,
      certificateNumber: certNumber.trim(),
      issuedState: issuedState.trim(),
      issueDate: new Date(issueDate).toISOString(),
      expiryDate: new Date(expiryDate).toISOString(),
    })
    setCustomerId(''); setCertNumber(''); setIssuedState('')
  }

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Add Resale Certificate">
      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Customer</label>
          <select value={customerId} onChange={e => setCustomerId(e.target.value)} className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm">
            <option value="">Select customer…</option>
            {customers.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <Input label="Certificate Number" value={certNumber} onChange={e => setCertNumber(e.target.value)} />
        <Input label="Issued State" value={issuedState} onChange={e => setIssuedState(e.target.value)} placeholder="e.g. CA" />
        <div className="grid grid-cols-2 gap-3">
          <Input label="Issue Date" type="date" value={issueDate} onChange={e => setIssueDate(e.target.value)} />
          <Input label="Expiry Date" type="date" value={expiryDate} onChange={e => setExpiryDate(e.target.value)} />
        </div>
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} isLoading={isSubmitting} disabled={!customerId || !certNumber.trim() || !issuedState.trim()} leftIcon={<FileBadge className="h-4 w-4" />}>Save</Button>
        </div>
      </div>
    </Modal>
  )
}
