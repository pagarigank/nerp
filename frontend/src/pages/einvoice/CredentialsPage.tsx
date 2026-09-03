import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  Plus,
  Key,
  Eye,
  EyeOff,
  Copy,
  Trash2,
  AlertCircle,
  CheckCircle,
  RotateCw,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Modal, ConfirmDialog } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { getErrorMessage } from '@api/client'
import {
  getCredentials,
  createCredential,
  rotateSecret,
  deactivateCredential,
  type ApiCredential,
} from '@api/einvoice'

const ENV_OPTIONS = [
  { value: 'sandbox', label: 'Sandbox' },
  { value: 'production', label: 'Production' },
]

export function CredentialsPage() {
  const queryClient = useQueryClient()

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [clientId, setClientId] = useState('')
  const [clientSecret, setClientSecret] = useState('')
  const [environment, setEnvironment] = useState('sandbox')
  const [createError, setCreateError] = useState<string | null>(null)

  const [showSecret, setShowSecret] = useState<string | null>(null)
  const [newSecret, setNewSecret] = useState<string | null>(null)
  const [credToRotate, setCredToRotate] = useState<ApiCredential | null>(null)
  const [credToDeactivate, setCredToDeactivate] = useState<ApiCredential | null>(null)

  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const { data: credentials = [], isLoading } = useQuery({
    queryKey: ['einvoice', 'credentials', companyId],
    queryFn: () => getCredentials(companyId),
  })

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['einvoice', 'credentials', companyId] })
  }

  const createMutation = useMutation({
    mutationFn: () => createCredential(companyId, clientId, clientSecret, environment),
    onSuccess: (result) => {
      invalidate()
      setNewSecret(result.clientSecret ?? null)
      closeCreateModal()
    },
    onError: (err) => setCreateError(getErrorMessage(err)),
  })

  const rotateMutation = useMutation({
    mutationFn: (id: string) => rotateSecret(id),
    onSuccess: (result) => {
      invalidate()
      setNewSecret(result.clientSecret)
      setCredToRotate(null)
    },
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateCredential(id),
    onSuccess: () => {
      invalidate()
      setCredToDeactivate(null)
    },
  })

  const closeCreateModal = () => {
    setIsCreateModalOpen(false)
    setClientId('')
    setClientSecret('')
    setEnvironment('sandbox')
    setCreateError(null)
  }

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text)
  }

  return (
    <div className="space-y-6">
      {/* New Secret Display */}
      {newSecret && (
        <div className="p-4 rounded-lg bg-amber-50 border border-amber-200 dark:bg-amber-900/20 dark:border-amber-800">
          <div className="flex items-start gap-3">
            <Key className="h-5 w-5 text-amber-600 dark:text-amber-400 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium text-amber-800 dark:text-amber-200">
                Client Secret — Save this now!
              </p>
              <p className="text-xs text-amber-700 dark:text-amber-300 mt-1 mb-3">
                This secret will not be shown again. Copy it and store it securely.
              </p>
              <div className="flex items-center gap-2">
                <code className="flex-1 px-3 py-2 bg-white dark:bg-gray-800 rounded text-sm font-mono text-gray-900 dark:text-white border border-amber-300 dark:border-amber-700">
                  {showSecret === 'new' ? newSecret : '•'.repeat(40)}
                </code>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => setShowSecret(showSecret === 'new' ? null : 'new')}
                >
                  {showSecret === 'new' ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => copyToClipboard(newSecret)}
                >
                  <Copy className="h-4 w-4" />
                </Button>
              </div>
            </div>
            <button onClick={() => setNewSecret(null)} className="text-amber-500 hover:text-amber-700">
              <span className="sr-only">Dismiss</span>
              <span className="text-lg">&times;</span>
            </button>
          </div>
        </div>
      )}

      {/* Create Modal */}
      <Modal
        isOpen={isCreateModalOpen}
        onClose={closeCreateModal}
        title="Add API Credentials"
        description="Add a new MyInvois API credential pair for a specific environment."
        size="md"
        footer={
          <>
            <Button variant="secondary" onClick={closeCreateModal} disabled={createMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="primary"
              onClick={() => createMutation.mutate()}
              isLoading={createMutation.isPending}
              leftIcon={<Plus className="h-4 w-4" />}
            >
              Add Credential
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {createError && (
            <div className="flex items-center gap-2 p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300 text-sm">
              <AlertCircle className="h-4 w-4 flex-shrink-0" />
              {createError}
            </div>
          )}
          <Input
            label="Client ID"
            value={clientId}
            onChange={(e) => setClientId(e.target.value)}
            placeholder="MyInvois Client ID"
          />
          <Input
            label="Client Secret"
            type="password"
            value={clientSecret}
            onChange={(e) => setClientSecret(e.target.value)}
            placeholder="MyInvois Client Secret"
          />
          <Select
            label="Environment"
            value={environment}
            onChange={(e) => setEnvironment(e.target.value)}
            options={ENV_OPTIONS}
          />
        </div>
      </Modal>

      {/* Credentials Table */}
      <Card>
        <CardHeader
          title="API Credentials"
          description="Manage MyInvois Client ID/Secret pairs per environment"
          action={
            <Button
              variant="primary"
              size="sm"
              onClick={() => setIsCreateModalOpen(true)}
              leftIcon={<Plus className="h-4 w-4" />}
            >
              Add Credential
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <SkeletonTable columns={5} />
          ) : credentials.length === 0 ? (
            <div className="text-center py-12">
              <Key className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                No API credentials configured yet.
              </p>
              <Button
                variant="primary"
                size="sm"
                onClick={() => setIsCreateModalOpen(true)}
                leftIcon={<Plus className="h-4 w-4" />}
              >
                Add First Credential
              </Button>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Environment</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Client ID</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Secret</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Created</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {credentials.map((cred) => (
                    <tr
                      key={cred.id}
                      className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
                    >
                      <td className="px-3 py-3">
                        {cred.isActive ? (
                          <Badge variant="success" size="sm" dot>Active</Badge>
                        ) : (
                          <Badge variant="neutral" size="sm">Inactive</Badge>
                        )}
                      </td>
                      <td className="px-3 py-3">
                        <Badge
                          variant={cred.environment === 'production' ? 'warning' : 'info'}
                          size="sm"
                        >
                          {cred.environment}
                        </Badge>
                      </td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">
                        {cred.clientId}
                      </td>
                      <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                        {showSecret === cred.id ? (
                          <span className="font-mono">{cred.clientSecretHint || '••••••••'}</span>
                        ) : (
                          <span>••••••••••••</span>
                        )}
                      </td>
                      <td className="px-3 py-3 text-xs text-gray-500 dark:text-gray-400">
                        {new Date(cred.createdAt).toLocaleDateString()}
                      </td>
                      <td className="px-3 py-3 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => setShowSecret(showSecret === cred.id ? null : cred.id)}
                          >
                            {showSecret === cred.id ? (
                              <EyeOff className="h-3 w-3" />
                            ) : (
                              <Eye className="h-3 w-3" />
                            )}
                          </Button>
                          {cred.isActive && (
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => setCredToRotate(cred)}
                              title="Rotate Secret"
                            >
                              <RotateCw className="h-3 w-3" />
                            </Button>
                          )}
                          {cred.isActive && (
                            <Button
                              size="sm"
                              variant="ghost"
                              onClick={() => setCredToDeactivate(cred)}
                              className="text-red-600 hover:text-red-700"
                              title="Deactivate"
                            >
                              <Trash2 className="h-3 w-3" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Rotate Confirmation */}
      <ConfirmDialog
        isOpen={!!credToRotate}
        onClose={() => setCredToRotate(null)}
        onConfirm={() => credToRotate && rotateMutation.mutate(credToRotate.id)}
        title="Rotate Client Secret"
        message={
          credToRotate
            ? `Generate a new client secret for "${credToRotate.clientId}"? The old secret will be immediately invalidated.`
            : ''
        }
        confirmText="Rotate Secret"
        variant="warning"
        isLoading={rotateMutation.isPending}
      />

      {/* Deactivate Confirmation */}
      <ConfirmDialog
        isOpen={!!credToDeactivate}
        onClose={() => setCredToDeactivate(null)}
        onConfirm={() => credToDeactivate && deactivateMutation.mutate(credToDeactivate.id)}
        title="Deactivate Credential"
        message={
          credToDeactivate
            ? `Deactivate credential "${credToDeactivate.clientId}"? It will no longer be usable for API authentication.`
            : ''
        }
        confirmText="Deactivate"
        variant="danger"
        isLoading={deactivateMutation.isPending}
      />
    </div>
  )
}
