import { useState, useRef } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Upload,
  Shield,
  ShieldCheck,
  ShieldOff,
  Trash2,
  AlertCircle,
  CheckCircle,
  Key,
  Calendar,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getCertificates,
  uploadCertificate,
  activateCertificate,
  revokeCertificate,
  deleteCertificate,
  type DigitalCertificate,
} from '@api/einvoice'

export function CertificatesPage() {
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)

  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false)
  const [certName, setCertName] = useState('')
  const [certFile, setCertFile] = useState<File | null>(null)
  const [isActive, setIsActive] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)

  const [certToRevoke, setCertToRevoke] = useState<DigitalCertificate | null>(null)
  const [revokeReason, setRevokeReason] = useState('')
  const [certToDelete, setCertToDelete] = useState<DigitalCertificate | null>(null)

  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const { data: certificates = [], isLoading } = useQuery({
    queryKey: ['einvoice', 'certificates', companyId],
    queryFn: () => getCertificates(companyId),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['einvoice', 'certificates', companyId] })
  }

  const uploadMutation = useMutation({
    mutationFn: () => {
      if (!certFile || !certName.trim()) throw new Error('Name and file are required')
      return uploadCertificate(companyId, certName.trim(), certFile, isActive)
    },
    onSuccess: () => {
      invalidate()
      closeUploadModal()
    },
    onError: (err) => setUploadError(getErrorMessage(err)),
  })

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateCertificate(id),
    onSuccess: invalidate,
  })

  const revokeMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => revokeCertificate(id, reason),
    onSuccess: () => {
      invalidate()
      setCertToRevoke(null)
      setRevokeReason('')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCertificate(id),
    onSuccess: () => {
      invalidate()
      setCertToDelete(null)
    },
  })

  const closeUploadModal = () => {
    setIsUploadModalOpen(false)
    setCertName('')
    setCertFile(null)
    setIsActive(false)
    setUploadError(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  }

  const isExpired = (notAfter: string) => new Date(notAfter) < new Date()
  const daysUntilExpiry = (notAfter: string) => {
    const diff = new Date(notAfter).getTime() - Date.now()
    return Math.ceil(diff / (1000 * 60 * 60 * 24))
  }

  return (
    <div className="space-y-6">
      {/* Upload Modal */}
      <Modal
        isOpen={isUploadModalOpen}
        onClose={closeUploadModal}
        title="Upload Digital Certificate"
        description="Upload a PFX/PKCS#12 certificate file. The private key must be included."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeUploadModal} disabled={uploadMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={() => uploadMutation.mutate()}
              isLoading={uploadMutation.isPending}
              leftIcon={<Upload className="h-4 w-4" />}
            >
              Upload Certificate
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {uploadError && (
            <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm">
              <AlertCircle className="h-4 w-4 flex-shrink-0" />
              {uploadError}
            </div>
          )}
          <Input
            label="Certificate Name"
            value={certName}
            onChange={(e) => setCertName(e.target.value)}
            placeholder="e.g., Production Signing Cert"
          />
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Certificate File (PFX/PKCS#12)
            </label>
            <input
              ref={fileInputRef}
              type="file"
              accept=".pfx,.p12"
              onChange={(e) => setCertFile(e.target.files?.[0] ?? null)}
              className="block w-full text-sm text-gray-500 dark:text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-emerald-50 file:text-emerald-700 hover:file:bg-emerald-100 dark:file:bg-emerald-900/20 dark:file:text-emerald-400"
            />
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="rounded border-gray-300 text-emerald-600 focus:ring-emerald-500"
            />
            <span className="text-gray-700 dark:text-gray-300">Activate immediately</span>
          </label>
        </div>
      </Modal>

      {/* Revoke Modal */}
      <Modal
        isOpen={!!certToRevoke}
        onClose={() => setCertToRevoke(null)}
        title="Revoke Certificate"
        description={`Revoking "${certToRevoke?.certificateName}" will prevent it from signing new documents.`}
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={() => setCertToRevoke(null)}>
              Cancel
            </Button>
            <Button
              variant="danger"
              onClick={() => certToRevoke && revokeMutation.mutate({ id: certToRevoke.id, reason: revokeReason })}
              isLoading={revokeMutation.isPending}
            >
              Revoke Certificate
            </Button>
          </>
        }
      >
        <Input
          label="Revocation Reason"
          value={revokeReason}
          onChange={(e) => setRevokeReason(e.target.value)}
          placeholder="e.g., Compromised, expired, replaced"
        />
      </Modal>

      {/* Certificates List */}
      <Card>
        <CardHeader
          title="Digital Certificates"
          description="Manage PFX certificates used for XAdES signing of e-Invoice documents"
          action={
            <Button
              variant="primary"
              size="sm"
              onClick={() => setIsUploadModalOpen(true)}
              leftIcon={<Upload className="h-4 w-4" />}
            >
              Upload Certificate
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={6} />
          ) : certificates.length === 0 ? (
            <div className="text-center py-12">
              <Shield className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                No digital certificates uploaded yet.
              </p>
              <Button
                variant="primary"
                size="sm"
                onClick={() => setIsUploadModalOpen(true)}
                leftIcon={<Upload className="h-4 w-4" />}
              >
                Upload First Certificate
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {certificates.map((cert) => {
                const expired = isExpired(cert.notAfter)
                const expiring = !expired && daysUntilExpiry(cert.notAfter) <= 30
                const days = daysUntilExpiry(cert.notAfter)

                return (
                  <div
                    key={cert.id}
                    className={`p-4 rounded-lg border transition-colors ${
                      cert.isActive
                        ? 'border-emerald-200 bg-emerald-50/50 dark:border-emerald-800 dark:bg-emerald-900/10'
                        : expired
                          ? 'border-red-200 bg-red-50/50 dark:border-red-800 dark:bg-red-900/10'
                          : 'border-gray-200 dark:border-gray-700'
                    }`}
                  >
                    <div className="flex items-start justify-between mb-3">
                      <div className="flex items-center gap-2">
                        {cert.isActive ? (
                          <ShieldCheck className="h-5 w-5 text-emerald-600 dark:text-emerald-400" />
                        ) : expired ? (
                          <ShieldOff className="h-5 w-5 text-red-500" />
                        ) : (
                          <Shield className="h-5 w-5 text-gray-400" />
                        )}
                        <span className="font-medium text-gray-900 dark:text-white">{cert.certificateName}</span>
                      </div>
                      <div className="flex gap-1">
                        {cert.isActive && (
                          <Badge variant="success" size="sm" dot>Active</Badge>
                        )}
                        {cert.isRevoked && (
                          <Badge variant="error" size="sm">Revoked</Badge>
                        )}
                        {!cert.isActive && !cert.isRevoked && !expired && (
                          <Badge variant="neutral" size="sm">Inactive</Badge>
                        )}
                      </div>
                    </div>

                    <div className="space-y-1 text-xs text-gray-500 dark:text-gray-400 mb-3">
                      <p><span className="font-medium">Issuer:</span> {cert.issuer}</p>
                      <p><span className="font-medium">Subject:</span> {cert.subject}</p>
                      <p className="font-mono"><span className="font-medium">Serial:</span> {cert.serialNumber}</p>
                      <div className="flex items-center gap-1">
                        <Calendar className="h-3 w-3" />
                        <span>
                          Valid: {new Date(cert.notBefore).toLocaleDateString()} — {new Date(cert.notAfter).toLocaleDateString()}
                        </span>
                        {expired && <span className="text-red-500 font-medium">(EXPIRED)</span>}
                        {expiring && <span className="text-amber-500 font-medium">({days} days left)</span>}
                      </div>
                    </div>

                    <div className="flex gap-2 pt-2 border-t border-gray-100 dark:border-gray-700/60">
                      {!cert.isActive && !cert.isRevoked && !expired && (
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => activateMutation.mutate(cert.id)}
                          isLoading={activateMutation.isPending}
                          leftIcon={<Key className="h-3 w-3" />}
                        >
                          Activate
                        </Button>
                      )}
                      {cert.isActive && !cert.isRevoked && (
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => setCertToRevoke(cert)}
                          className="text-amber-600 hover:text-amber-700"
                        >
                          Revoke
                        </Button>
                      )}
                      {!cert.isActive && (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => setCertToDelete(cert)}
                          className="text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-900/20"
                          leftIcon={<Trash2 className="h-3 w-3" />}
                        >
                          Delete
                        </Button>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <ConfirmDialog
        isOpen={!!certToDelete}
        onClose={() => setCertToDelete(null)}
        onConfirm={() => certToDelete && deleteMutation.mutate(certToDelete.id)}
        title="Delete Certificate"
        message={
          certToDelete
            ? `Are you sure you want to delete "${certToDelete.certificateName}"? This action cannot be undone.`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />
    </div>
  )
}
