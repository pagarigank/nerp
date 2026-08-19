import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'
import { useShallow } from 'zustand/react/shallow'
import type { User, Company, FiscalPeriod, Role } from '../types/index'

interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  companies: Company[]
  currentCompany: Company | null
  fiscalPeriods: FiscalPeriod[]
  currentPeriod: FiscalPeriod | null
  roles: Role[]
  permissions: string[]
  isAuthenticated: boolean
  isLoading: boolean
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
      fiscalPeriods: [],
      currentPeriod: null,
      roles: [],
      permissions: [],
      isAuthenticated: false,
      isLoading: false,
      error: null,

      setAuth: ({ user, accessToken, refreshToken, companies, fiscalPeriods, roles, permissions }) => {
        const safeCompanies = companies ?? []
        const safePeriods = fiscalPeriods ?? []
        const firstCompany = safeCompanies.length > 0 ? safeCompanies[0]! : null
        const firstPeriod = safePeriods.length > 0
          ? (safePeriods.find(p => p.isCurrent) ?? safePeriods[0]!)
          : null
        set({
          user,
          accessToken,
          refreshToken,
          companies: safeCompanies,
          currentCompany: firstCompany,
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
          currentCompany: companies[0] || null,
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

      hasPermission: (permission) => get().permissions.includes(permission),

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
        fiscalPeriods: state.fiscalPeriods,
        currentPeriod: state.currentPeriod,
        roles: state.roles,
        permissions: state.permissions,
      }),
      merge: (persistedState, currentState) => {
        const persisted = persistedState as Partial<AuthState> | undefined
        return {
          ...currentState,
          ...persisted,
          isAuthenticated: persisted?.isAuthenticated || Boolean(persisted?.accessToken),
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