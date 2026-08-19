import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle, Check, KeyRound, Trash2, EyeOff, Eye } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getApiKeys, createApiKey, activateApiKey, deactivateApiKey, deleteApiKey, companyId } from '@api/platform'
import type { ApiKey, ApiKeyCreated, CreateApiKeyRequest } from '@/types/platform'

const COMMON_SCOPES = ['platform:read', 'platform:write', 'gl:read', 'gl:write', 'ap:read', 'ar:read', 'om:read', 'inv:read']

export function ApiKeysPage() {
  const qc = useQueryClient()
  const cid = companyId()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [revealed, setRevealed] = useState<ApiKeyCreated | null>(null)
  const [name, setName] = useState('')
  const [scopes, setScopes] = useState<string[]>(['platform:read'])
  const [expiresOn, setExpiresOn] = useState('')

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['platform', 'api-keys', cid],
    queryFn: () => getApiKeys(cid),
  })

  const createMut = useMutation({
    mutationFn: (d: CreateApiKeyRequest) => createApiKey(d),
    onSuccess: (created) => {
      qc.invalidateQueries({ queryKey: ['platform', 'api-keys', cid] })
      setRevealed(created)
      setOpen(false)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const toggleMut = useMutation({
    mutationFn: (p: { id: string; op: 'activate' | 'deactivate' }) =>
      p.op === 'activate' ? activateApiKey(p.id) : deactivateApiKey(p.id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'api-keys', cid] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const deleteMut = useMutation({
    mutationFn: (id: string) => deleteApiKey(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'api-keys', cid] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const close = () => {
    setOpen(false)
    setFormError(null)
  }
  const toggleScope = (s: string) =>
    setScopes((cur) => (cur.includes(s) ? cur.filter((x) => x !== s) : [...cur, s]))
  const submit = () => {
    setFormError(null)
    if (!name) {
      setFormError('Name is required')
      return
    }
    createMut.mutate({
      companyId: cid,
      name,
      scopes,
      expiresOn: expiresOn ? new Date(expiresOn).toISOString() : null,
    })
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}
      {revealed && (
        <div className="flex items-start gap-2 p-4 rounded-lg bg-green-50 border border-green-200 text-green-800 text-sm">
          <Check className="h-5 w-5 mt-0.5" />
          <div>
            <p className="font-medium">API key created. Copy it now — it will not be shown again.</p>
            <code className="block mt-1 break-all rounded bg-white px-2 py-1 text-xs">{revealed.secret}</code>
            <Button size="sm" variant="ghost" className="mt-2" onClick={() => setRevealed(null)}>
              Done
            </Button>
          </div>
        </div>
      )}

      <Card>
        <CardHeader
          title="API Keys"
          description="Scoped machine identities for integrations, EDI and webhooks"
          action={
            <Button variant="primary" size="sm" onClick={() => { setFormError(null); setName(''); setScopes(['platform:read']); setExpiresOn(''); setOpen(true) }} leftIcon={<Plus className="h-4 w-4" />}>
              New Key
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading…</p>
          ) : rows.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No API keys yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Name</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Prefix</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Scopes</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((k: ApiKey) => (
                    <tr key={k.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 font-medium text-gray-900 dark:text-white">{k.name}</td>
                      <td className="px-3 py-3 font-mono text-xs text-gray-500">{k.keyPrefix}</td>
                      <td className="px-3 py-3">
                        <div className="flex flex-wrap gap-1">
                          {k.scopes.map((s) => (
                            <Badge key={s} variant="info" size="sm">{s}</Badge>
                          ))}
                        </div>
                      </td>
                      <td className="px-3 py-3">
                        <Badge variant={k.isActive ? 'success' : 'neutral'} size="sm" dot>
                          {k.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3 text-right">
                        {k.isActive ? (
                          <Button size="sm" variant="ghost" className="text-amber-600" disabled={toggleMut.isPending} onClick={() => toggleMut.mutate({ id: k.id, op: 'deactivate' })}>
                            <EyeOff className="h-3.5 w-3.5" /> Revoke
                          </Button>
                        ) : (
                          <Button size="sm" variant="ghost" disabled={toggleMut.isPending} onClick={() => toggleMut.mutate({ id: k.id, op: 'activate' })}>
                            <Eye className="h-3.5 w-3.5" /> Activate
                          </Button>
                        )}
                        <Button size="sm" variant="ghost" className="text-red-600" disabled={deleteMut.isPending} onClick={() => deleteMut.mutate(k.id)}>
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={open}
        onClose={close}
        title="New API Key"
        footer={
          <>
            <Button variant="secondary" onClick={close} disabled={createMut.isPending}>Cancel</Button>
            <Button variant="primary" onClick={submit} isLoading={createMut.isPending} leftIcon={<KeyRound className="h-4 w-4" />}>
              Generate
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <Input value={name} onChange={(e) => setName(e.target.value)} label="Name" required />
          <Input type="date" value={expiresOn} onChange={(e) => setExpiresOn(e.target.value)} label="Expires on (optional)" />
          <div>
            <p className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Scopes</p>
            <div className="flex flex-wrap gap-2">
              {COMMON_SCOPES.map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => toggleScope(s)}
                  className={`px-2 py-1 rounded-full text-xs border ${
                    scopes.includes(s)
                      ? 'bg-blue-600 text-white border-blue-600'
                      : 'bg-white text-gray-600 border-gray-300 dark:bg-gray-800 dark:text-gray-300'
                  }`}
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        </div>
      </Modal>
    </div>
  )
}
