// <copyright file="company.ts" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useAuthStore } from '@stores/authStore'

// Returns the id of the currently selected company for the logged-in user.
// For super admins on the "All Companies" sentinel the id is '' (empty),
// which callers treat as "no company filter" (unbounded / cross-company).
// Use this everywhere instead of hardcoding a company GUID so the UI honors
// the tenant the user actually selected.
export function currentCompanyId(): string {
  return useAuthStore.getState().currentCompany?.id ?? ''
}
