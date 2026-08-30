import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { useShallow } from 'zustand/react/shallow'
import type { User, Company, FiscalPeriod, Role } from '../types/index'

// Sentinel used as the "current company" for super admins who want to see
// data across every company (unbounded). Its empty id means "no companyId
// filter is sent to the API", so the backend returns all companies' data.
export const ALL_COMPANIES: Company = {
  id: '',
  code: '',
  name: 'All Companies',
  legalName: '',
  taxId: '',
  baseCurrency: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
  phone: '',
  email: '',
  website: '',
  isActive: true,
  createdAt: '',
  updatedAt: '',
}

// Normalize a permission code into { module, page, action } (lowercase).
// Supports: "module.page.action" (page-scoped), "module.action" (legacy, page="*"),
// and "*.*" / "*.*.*" (wildcards).
function normalizeCode(code: string): { module: string; page: string; action: string } {
  const parts = code.toLowerCase().split('.')
  if (parts.length === 3) return { module: parts[0], page: parts[1], action: parts[2] }
  if (parts.length === 2) return { module: parts[0], page: '*', action: parts[1] }
  return { module: parts[0] ?? '*', page: '*', action: '*' }
}

// Returns true if `held` permissions grant `requested` (page-scoped) permission,
// accounting for module-level, page-level, and wildcard grants.
function matchesPermission(held: string[], requested: string): boolean {
  const req = normalizeCode(requested)
  for (const h of held) {
    const p = normalizeCode(h)
    if (p.module === '*' || p.action === '*') return true // full wildcard
    if (p.module !== req.module) continue
    if (p.action !== '*' && p.action !== req.action) continue
    // Same module + (any action or matching action). Now check page scope.
    if (p.page === '*' || p.page === req.page) return true
  }
  return false
}

interface AuthState {
  error: string | null
  
  // Actions
  setAuth: (data: {
    user: User
    accessToken: string
    refreshToken: string
    companies?: Company[]
    fiscalPeriods?: FiscalPeriod[]
    roles?: Role[]
    permissions?: string[]
    isSuperAdmin?: boolean
  }) => void
  setUser: (user: User) => void
  setCompanies: (companies: Company[]) => void
  setCurrentCompany: (company: Company) => void
  setFiscalPeriods: (periods: FiscalPeriod[]) => void
  setCurrentPeriod: (period: FiscalPeriod) => void
  setRoles: (roles: Role[]) => void
  setPermissions: (permissions: string[]) => void
  logout: () => void
  setLoading: (loading: boolean) => void
  setError: (error: string | null) => void
  hasPermission: (permission: string) => boolean
  hasRole: (role: string) => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      companies: [],
      currentCompany: null,
      isSuperAdmin: false,
      fiscalPeriods: [],
      currentPeriod: null,
      roles: [],
      permissions: [],
      isAuthenticated: false,
      isLoading: false,
      error: null,

      setAuth: ({ user, accessToken, refreshToken, companies, fiscalPeriods, roles, permissions, isSuperAdmin }) => {
        const safeCompanies = companies ?? []
        const safePeriods = fiscalPeriods ?? []
        const superAdmin = isSuperAdmin ?? false
        // Super admins default to "All Companies" (unbounded). Company-scoped
        // users default to their first company.
        const firstCompany = superAdmin ? ALL_COMPANIES : (safeCompanies.length > 0 ? safeCompanies[0]! : null)
        const firstPeriod = safePeriods.length > 0
          ? (safePeriods.find(p => p.isCurrent) ?? safePeriods[0]!)
          : null
        set({
          user,
          accessToken,
          refreshToken,
          companies: safeCompanies,
          currentCompany: firstCompany,
          isSuperAdmin: superAdmin,
          fiscalPeriods: safePeriods,
          currentPeriod: firstPeriod,
          roles: roles ?? user.roles ?? [],
          permissions: permissions ?? user.permissions ?? [],
          isAuthenticated: true,
          isLoading: false,
          error: null,
        })
      },

      setUser: (user) => set({ user }),

      setCompanies: (companies) =>
        set({
          companies,
          // If currently on "All Companies" (super admin) keep that; otherwise
          // re-default to the first company.
          currentCompany: get().isSuperAdmin ? ALL_COMPANIES : (companies[0] || null),
        }),

      setCurrentCompany: (company) => set({ currentCompany: company }),

      setFiscalPeriods: (periods) =>
        set({
          fiscalPeriods: periods,
          currentPeriod: periods.find(p => p.isCurrent) || periods[0] || null,
        }),

      setCurrentPeriod: (period) => set({ currentPeriod: period }),

      setRoles: (roles) => set({ roles }),

      setPermissions: (permissions) => set({ permissions }),

      logout: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          companies: [],
          currentCompany: null,
          fiscalPeriods: [],
          currentPeriod: null,
          roles: [],
          permissions: [],
          isAuthenticated: false,
          error: null,
        }),

      setLoading: (isLoading) => set({ isLoading }),

      setError: (error) => set({ error }),

      hasPermission: (permission) => {
        const held = get().permissions
        if (held.length === 0) return false
        return matchesPermission(held, permission)
      },

      hasRole: (role) => get().roles.some(r => r.name === role),
    }),
    {
      name: 'erp-auth-storage',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        isAuthenticated: state.isAuthenticated,
        companies: state.companies,
        currentCompany: state.currentCompany,
        isSuperAdmin: state.isSuperAdmin,
        fiscalPeriods: state.fiscalPeriods,
        currentPeriod: state.currentPeriod,
        roles: state.roles,
        permissions: state.permissions,
      }),
      merge: (persistedState, currentState) => {
        // zustand persist stores `{ state: {...}, version }`; unwrap it, but
        // also tolerate an already-unwrapped payload for safety.
        const raw = persistedState as { state?: Partial<AuthState> } | Partial<AuthState> | undefined
        const persisted = (raw && 'state' in raw && raw.state ? raw.state : (raw as Partial<AuthState>)) ?? {}
        return {
          ...currentState,
          ...persisted,
          isAuthenticated: persisted.isAuthenticated || Boolean(persisted.accessToken),
        }
      },
    }
  )
)

// Selectors for performance
export const useAuth = () => useAuthStore(useShallow(state => ({
  user: state.user,
  accessToken: state.accessToken,
  refreshToken: state.refreshToken,
  companies: state.companies,
  currentCompany: state.currentCompany,
  isSuperAdmin: state.isSuperAdmin,
  fiscalPeriods: state.fiscalPeriods,
  currentPeriod: state.currentPeriod,
  roles: state.roles,
  permissions: state.permissions,
  isAuthenticated: state.isAuthenticated,
  isLoading: state.isLoading,
  error: state.error,
  hasPermission: state.hasPermission,
  hasRole: state.hasRole,
  setAuth: state.setAuth,
  setUser: state.setUser,
  setCompanies: state.setCompanies,
  setCurrentCompany: state.setCurrentCompany,
  setFiscalPeriods: state.setFiscalPeriods,
  setCurrentPeriod: state.setCurrentPeriod,
  setRoles: state.setRoles,
  setPermissions: state.setPermissions,
  logout: state.logout,
  setLoading: state.setLoading,
  setError: state.setError,
})))