import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Check, X, Loader2, Inbox, AlertCircle, ShieldCheck } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Badge } from '@components/ui/Badge'
import { Modal } from '@components/ui/Modal'
import { getErrorMessage } from '@api/client'
import { getAccessRequests, approveAccessRequest, rejectAccessRequest, getRoles } from '@api/platform'
import type { AccessRequest } from '@api/platform'
import { useAuth } from '@stores/authStore'

export function AccessRequestsPage() {
  const queryClient = useQueryClient()
  const { isCompanyAdmin } = useAuth()
  const [reviewTarget, setReviewTarget] = useState<{ req: AccessRequest; action: 'approve' | 'reject' } | null>(null)
  const [notes, setNotes] = useState('')
  const [roleOverride, setRoleOverride] = useState('')
  const [error, setError] = useState<string | null>(null)

  const { data: requests = [], isLoading } = useQuery<AccessRequest[]>({
    queryKey: ['access-requests'],
    queryFn: getAccessRequests,
    enabled: isCompanyAdmin,
  })
  const { data: roles = [] } = useQuery({ queryKey: ['roles-for-approval'], queryFn: getRoles, enabled: isCompanyAdmin })

  const reviewMutation = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'approve' | 'reject' }) => {
      if (action === 'approve') {
        return approveAccessRequest(id, roleOverride || null, notes || null)
      }
      return rejectAccessRequest(id, notes || null)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['access-requests'] })
      setReviewTarget(null)
      setNotes('')
      setRoleOverride('')
    },
    onError: (err) => setError(getErrorMessage(err)),
  })

  if (!isCompanyAdmin) {
    return (
      <div className="p-8">
        <Card>
          <CardContent className="flex items-center gap-3 text-amber-600 dark:text-amber-400">
            <ShieldCheck className="w-5 h-5" />
            <span className="text-sm">Only a company administrator or the super admin can review access requests.</span>
          </CardContent>
        </Card>
      </div>
    )
  }

  const pending = requests.filter((r) => r.status === 'Pending')
  const decided = requests.filter((r) => r.status !== 'Pending')

  const openReview = (req: AccessRequest, action: 'approve' | 'reject') => {
    setReviewTarget({ req, action })
    setNotes(req.reviewNotes ?? '')
    setRoleOverride('')
    setError(null)
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Access Requests</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          Review and approve requests for system access. Approving provisions the user account in the requested company.
        </p>
      </div>

      {error && (
        <div className="flex items-center gap-2 rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-300">
          <AlertCircle className="w-4 h-4" /> <span>{error}</span>
        </div>
      )}

      <Card>
        <CardHeader title={`Pending (${pending.length})`} />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-400">Loading…</p>
          ) : pending.length === 0 ? (
            <div className="flex items-center gap-2 text-sm text-gray-400 py-6">
              <Inbox className="w-5 h-5" /> No pending requests.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-800">
                    <th className="py-2 pr-4 font-medium">Name</th>
                    <th className="py-2 pr-4 font-medium">Email</th>
                    <th className="py-2 pr-4 font-medium">Company</th>
                    <th className="py-2 pr-4 font-medium">Requested role</th>
                    <th className="py-2 pr-4 font-medium">Submitted</th>
                    <th className="py-2 pr-4 font-medium text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {pending.map((r) => (
                    <tr key={r.id} className="border-b border-gray-100 dark:border-gray-800/60">
                      <td className="py-3 pr-4 text-gray-900 dark:text-white">{r.fullName}</td>
                      <td className="py-3 pr-4 text-gray-600 dark:text-gray-300">{r.email}</td>
                      <td className="py-3 pr-4 text-gray-600 dark:text-gray-300">{r.companyName ?? r.companyId}</td>
                      <td className="py-3 pr-4 text-gray-600 dark:text-gray-300">{r.requestedRole}</td>
                      <td className="py-3 pr-4 text-gray-400">{new Date(r.createdOn).toLocaleDateString()}</td>
                      <td className="py-3 pr-4 text-right whitespace-nowrap">
                        <Button size="sm" variant="success" className="mr-2" onClick={() => openReview(r, 'approve')}>
                          <Check className="w-4 h-4 mr-1" /> Approve
                        </Button>
                        <Button size="sm" variant="destructive" onClick={() => openReview(r, 'reject')}>
                          <X className="w-4 h-4 mr-1" /> Reject
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

      {decided.length > 0 && (
        <Card>
          <CardHeader title={`Reviewed (${decided.length})`} />
          <CardContent>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-left text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-800">
                    <th className="py-2 pr-4 font-medium">Name</th>
                    <th className="py-2 pr-4 font-medium">Email</th>
                    <th className="py-2 pr-4 font-medium">Company</th>
                    <th className="py-2 pr-4 font-medium">Role</th>
                    <th className="py-2 pr-4 font-medium">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {decided.map((r) => (
                    <tr key={r.id} className="border-b border-gray-100 dark:border-gray-800/60">
                      <td className="py-3 pr-4 text-gray-900 dark:text-white">{r.fullName}</td>
                      <td className="py-3 pr-4 text-gray-600 dark:text-gray-300">{r.email}</td>
                      <td className="py-3 pr-4 text-gray-600 dark:text-gray-300">{r.companyName ?? r.companyId}</td>
                      <td className="py-3 pr-4 text-gray-600 dark:text-gray-300">{r.requestedRole}</td>
                      <td className="py-3 pr-4">
                        <Badge variant={r.status === 'Approved' ? 'success' : 'error'}>{r.status}</Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </CardContent>
        </Card>
      )}

      <Modal
        isOpen={reviewTarget !== null}
        onClose={() => setReviewTarget(null)}
        title={reviewTarget?.action === 'approve' ? 'Approve access request' : 'Reject access request'}
      >
        {reviewTarget && (
          <div className="space-y-4">
            <p className="text-sm text-gray-600 dark:text-gray-300">
              <span className="font-medium text-gray-900 dark:text-white">{reviewTarget.req.fullName}</span>{' '}
              ({reviewTarget.req.email}) · {reviewTarget.req.companyName ?? reviewTarget.req.companyId}
            </p>
            {reviewTarget.action === 'approve' && (
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Assign role</label>
                <select
                  value={roleOverride}
                  onChange={(e) => setRoleOverride(e.target.value)}
                  className="w-full h-11 px-3 rounded-xl border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white text-sm outline-none focus:border-primary-500"
                >
                  <option value="">Requested role ({reviewTarget.req.requestedRole})</option>
                  {roles.filter((r) => !/admin/i.test(r.name)).map((r) => (
                    <option key={r.id} value={r.id}>{r.name}</option>
                  ))}
                </select>
              </div>
            )}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Notes</label>
              <textarea
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 rounded-xl border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white text-sm outline-none focus:border-primary-500"
                placeholder="Optional notes…"
              />
            </div>
            <div className="flex justify-end gap-3">
              <Button variant="ghost" onClick={() => setReviewTarget(null)}>Cancel</Button>
              <Button
                variant={reviewTarget.action === 'approve' ? 'success' : 'destructive'}
                disabled={reviewMutation.isPending}
                onClick={() => reviewMutation.mutate({ id: reviewTarget.req.id, action: reviewTarget.action })}
              >
                {reviewMutation.isPending && <Loader2 className="w-4 h-4 mr-1 animate-spin" />}
                {reviewTarget.action === 'approve' ? 'Approve & provision' : 'Reject'}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
