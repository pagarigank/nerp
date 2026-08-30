import { useMemo } from 'react'
import { useAuthStore } from '@stores/authStore'

export interface PagePermissions {
  canView: boolean
  canCreate: boolean
  canEdit: boolean
  canDelete: boolean
}

// Resolve the four page-level actions for a given (module, page) from the
// current user's permission set. Use this inside pages to show/hide action
// buttons (New, Edit, Delete) per the role's RBAC grants.
//
// Permission codes follow "{module}.{page}.{action}" (view | create | edit |
// delete). The matcher also honors module-level ("module.*.action"), page-level
// ("module.page.*") and wildcard ("*.*") grants, so it works whether a role
// holds the new page-scoped codes or the legacy Module.Action codes.
export function usePagePermission(module: string, page: string): PagePermissions {
  const permissions = useAuthStore(s => s.permissions)
  const hasPermission = useAuthStore(s => s.hasPermission)

  return useMemo<PagePermissions>(() => {
    const code = (action: string) => `${module}.${page}.${action}`.toLowerCase()
    return {
      canView: hasPermission(code('view')),
      canCreate: hasPermission(code('create')),
      canEdit: hasPermission(code('edit')),
      canDelete: hasPermission(code('delete')),
    }
    // permissions included so the memo recomputes when the set changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [module, page, permissions, hasPermission])
}

// Derive module + page from a nav route like "/gl/journal-batches" or
// "/om/quotes/new" -> { module: "gl", page: "journal-batches" }.
export function modulePageFromRoute(to: string): { module: string; page: string } {
  const seg = to.split('/').filter(Boolean)
  if (seg.length === 0) return { module: '', page: '' }
  if (seg.length === 1) return { module: seg[0], page: '*' }
  return { module: seg[0], page: seg[1] }
}
