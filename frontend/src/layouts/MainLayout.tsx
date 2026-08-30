import { Fragment, useState, useEffect, useRef, useMemo } from 'react'
import { Link, useLocation, Outlet, useNavigate } from 'react-router-dom'
import {
  ChevronRight,
  ChevronLeft,
  ChevronDown,
  Home,
  User,
  LogOut,
  Building2,
  HelpCircle,
  Sun,
  Moon,
  Search,
  Grid,
  Menu,
  Bell,
  Settings,
  FileText,
  AlertTriangle,
  CheckCircle,
  Clock,
  X,
  Command,
} from 'lucide-react'
import { cn } from '@utils/helpers'
import { useAuth, useAuthStore, ALL_COMPANIES } from '@stores/authStore'
import { modulePageFromRoute } from '@hooks/usePagePermission'
import { Button } from '@components/ui/Button'
import { Combobox, type SelectOption } from '@components/ui/Combobox'
import { mainNavigation, resolveNav, NAV_CHIP_STYLES, NAV_TEXT_STYLES, getSubIcon } from '@/navigation'

interface Notification {
  id: string
  type: string
  title: string
  message: string
  module: string
  actionUrl?: string
  isRead: boolean
  createdOn: string
  priority?: string
}

export function MainLayout() {
  const location = useLocation()
  const navigate = useNavigate()
  const [isSidebarOpen, setIsSidebarOpen] = useState(true)
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [isDarkMode, setIsDarkMode] = useState(false)
  const [openModule, setOpenModule] = useState<string | null>(null)
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false)
  const [isNotificationOpen, setIsNotificationOpen] = useState(false)
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [commandOpen, setCommandOpen] = useState(false)
  const [commandQuery, setCommandQuery] = useState('')
  const [hoveredCollapsed, setHoveredCollapsed] = useState<string | null>(null)
  const userMenuRef = useRef<HTMLDivElement>(null)
  const notifMenuRef = useRef<HTMLDivElement>(null)
  const commandInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const saved = localStorage.getItem('erp-dark-mode')
    if (saved) {
      setIsDarkMode(JSON.parse(saved))
      document.documentElement.classList.toggle('dark', JSON.parse(saved))
    }
    const sb = localStorage.getItem('erp-sidebar-open')
    if (sb !== null) setIsSidebarOpen(JSON.parse(sb))
  }, [])

  useEffect(() => {
    localStorage.setItem('erp-sidebar-open', JSON.stringify(isSidebarOpen))
  }, [isSidebarOpen])

  useEffect(() => {
    const resolved = resolveNav(location.pathname)
    if (resolved && resolved.module.sub.length > 0) {
      setOpenModule(resolved.module.href)
    }
    setIsUserMenuOpen(false)
    setIsNotificationOpen(false)
    setCommandOpen(false)
  }, [location.pathname])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault()
        setCommandOpen(v => !v)
      }
      if (e.key === 'Escape') {
        setCommandOpen(false)
        setIsMobileMenuOpen(false)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  useEffect(() => {
    if (commandOpen) setTimeout(() => commandInputRef.current?.focus(), 50)
    else setCommandQuery('')
  }, [commandOpen])

  const { user, currentCompany, currentPeriod, companies, fiscalPeriods, setCurrentCompany, setCurrentPeriod, logout, isSuperAdmin } = useAuth()
  const hasPermission = useAuthStore(s => s.hasPermission)

  // A nav sub-item is visible if the user can view that page. A module stays
  // visible if it has at least one visible sub-page (or the user holds a
  // module-level view grant). Roles gate (item.roles) still applies.
  const canViewRoute = (to: string): boolean => {
    const { module, page } = modulePageFromRoute(to)
    return hasPermission(`${module}.${page}.view`)
  }
  const roleAllowed = (item: { roles: string[] }): boolean =>
    item.roles.includes('*') || Boolean(user?.roles?.some((r: { name: string }) => item.roles.includes(r.name)))

  const filteredCommandItems = useMemo(() => {
    const q = commandQuery.toLowerCase().trim()
    const base = mainNavigation.flatMap(m => [{ label: m.name, to: m.href, module: m.name, isModule: true }, ...m.sub.map(s => ({ label: s.label, to: s.to, module: m.name, isModule: false }))])
    const visible = base.filter(i => i.isModule ? true : canViewRoute(i.to))
    if (!q) return visible
    return visible.filter(i => i.label.toLowerCase().includes(q) || i.module.toLowerCase().includes(q))
  }, [commandQuery, canViewRoute])

  useEffect(() => {
    const fetchNotifications = async () => {
      try {
        const token = useAuthStore.getState().accessToken
        const headers: Record<string, string> = {}
        if (token) headers['Authorization'] = `Bearer ${token}`
        const res = await fetch('/api/v1/platform/notifications', { headers })
        if (res.ok) {
          const json = await res.json()
          const data = json.data
          setNotifications(data?.notifications || [])
          setUnreadCount(data?.unreadCount || 0)
        }
      } catch {
      }
    }
    fetchNotifications()
    const interval = setInterval(fetchNotifications, 60000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) setIsUserMenuOpen(false)
      if (notifMenuRef.current && !notifMenuRef.current.contains(e.target as Node)) setIsNotificationOpen(false)
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const toggleDarkMode = () => {
    const newMode = !isDarkMode
    setIsDarkMode(newMode)
    localStorage.setItem('erp-dark-mode', JSON.stringify(newMode))
    document.documentElement.classList.toggle('dark', newMode)
  }

  const filteredNavigation = mainNavigation
    .map(item => ({
      ...item,
      sub: item.sub.filter(s => canViewRoute(s.to)),
    }))
    // A module is visible when it has at least one RBAC-permitted sub-page, OR
    // the user passes the (legacy) coarse role gate, OR holds a module-level
    // view wildcard. The legacy `roles` gate must not override page-scoped RBAC:
    // e.g. a user granted `platform.roles.view` must see the Platform → Roles
    // sub-page even if their role name is not in the module's `roles` list.
    .filter(item =>
      item.sub.length > 0 ||
      roleAllowed(item) ||
      hasPermission(`${modulePageFromRoute(item.href).module}.*.view`))

  const resolved = resolveNav(location.pathname)
  const resolvedModule = resolved?.module
  const resolvedSub = resolved?.sub

  const companyOptions: SelectOption[] = [
    ...(isSuperAdmin ? [{ value: '', label: 'All Companies' }] : []),
    ...companies.map(c => ({ value: c.id, label: `${c.code} - ${c.name}` })),
  ]

  const periodOptions: SelectOption[] = fiscalPeriods.map(p => ({
    value: p.id,
    label: `${p.periodName} (${p.fiscalYear} P${p.periodNumber})`,
  }))

  const handleCompanyChange = (value: string) => {
    if (isSuperAdmin && value === '') { setCurrentCompany(ALL_COMPANIES); return }
    const company = companies.find(c => c.id === value)
    if (company) setCurrentCompany(company)
  }

  const handlePeriodChange = (value: string) => {
    const period = fiscalPeriods.find(p => p.id === value)
    if (period) setCurrentPeriod(period)
  }

  const markAsRead = async (id: string) => {
    try {
      const token = useAuthStore.getState().accessToken
      const headers: Record<string, string> = {}
      if (token) headers['Authorization'] = `Bearer ${token}`
      await fetch(`/api/v1/platform/notifications/${id}/read`, { method: 'POST', headers })
      setNotifications(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n))
      setUnreadCount(prev => Math.max(0, prev - 1))
    } catch { }
  }

  const markAllAsRead = async () => {
    try {
      const token = useAuthStore.getState().accessToken
      const headers: Record<string, string> = {}
      if (token) headers['Authorization'] = `Bearer ${token}`
      await fetch('/api/v1/platform/notifications/read-all', { method: 'POST', headers })
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })))
      setUnreadCount(0)
    } catch { }
  }

  const pageTitle = resolvedModule?.name ?? 'Dashboard'
  const pageSubtitle = resolvedSub?.label
  const userInitials = user?.firstName && user?.lastName ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : user?.email?.[0]?.toUpperCase() || 'U'

  const getNotifIcon = (type: string) => {
    switch (type) {
      case 'approval': return <Clock className="h-4 w-4 text-blue-500" />
      case 'alert': return <AlertTriangle className="h-4 w-4 text-amber-500" />
      case 'deadline': return <FileText className="h-4 w-4 text-red-500" />
      case 'success': return <CheckCircle className="h-4 w-4 text-emerald-500" />
      default: return <Bell className="h-4 w-4 text-gray-500" />
    }
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col">
      {commandOpen && (
        <div className="fixed inset-0 z-[300] flex items-start justify-center pt-[20vh] p-4">
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm animate-fade-in" onClick={() => setCommandOpen(false)} />
          <div className="relative w-full max-w-lg bg-white dark:bg-gray-800 rounded-2xl shadow-2xl border border-gray-200 dark:border-gray-700 overflow-hidden animate-scale-in">
            <div className="flex items-center gap-3 px-4 py-3 border-b border-gray-200 dark:border-gray-700">
              <Search className="h-5 w-5 text-gray-400 flex-shrink-0" />
              <input ref={commandInputRef} value={commandQuery} onChange={e => setCommandQuery(e.target.value)} placeholder="Search modules, pages..." className="flex-1 bg-transparent outline-none text-sm placeholder:text-gray-400" />
              <kbd className="hidden sm:inline-flex items-center gap-1 px-2 py-1 text-xs bg-gray-100 dark:bg-gray-700 rounded border">ESC</kbd>
            </div>
            <div className="max-h-80 overflow-y-auto p-2 scrollbar-thin">
              {filteredCommandItems.slice(0, 20).map(item => (
                <button key={item.to} onClick={() => { navigate(item.to); setCommandOpen(false) }} className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm hover:bg-gray-100 dark:hover:bg-gray-700 text-left transition-colors">
                  <span className="text-xs text-gray-400 w-24 truncate text-right">{item.module}</span>
                  <span className={cn('flex-1 truncate', item.isModule && 'font-semibold')}>{item.label}</span>
                  <ChevronRight className="h-3.5 w-3.5 text-gray-300" />
                </button>
              ))}
              {filteredCommandItems.length === 0 && <p className="text-sm text-gray-400 text-center py-8">No results</p>}
            </div>
          </div>
        </div>
      )}

      {isMobileMenuOpen && (
        <div className="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm lg:hidden animate-fade-in" onClick={() => setIsMobileMenuOpen(false)} aria-hidden="true" />
      )}

      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col will-change-[width] transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)]',
          isSidebarOpen ? 'w-64' : 'w-[72px]',
          isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
        )}
        aria-label="Main navigation"
      >
        <div className="flex items-center justify-between h-16 px-3 border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
          <Link to="/dashboard" className="flex items-center gap-2.5 min-w-0 group" aria-label="ERP Home">
            <div className="w-8 h-8 rounded-lg bg-primary-600 flex items-center justify-center flex-shrink-0 group-hover:scale-105 transition-transform duration-200 shadow-sm">
              <Grid className="w-5 h-5 text-white" aria-hidden="true" />
            </div>
            <span className={cn('text-xl font-bold text-gray-900 dark:text-white transition-all duration-300', !isSidebarOpen && 'lg:hidden lg:opacity-0 lg:w-0 overflow-hidden')}>ERP</span>
          </Link>
          <Button variant="ghost" size="sm" onClick={() => setIsSidebarOpen(!isSidebarOpen)} aria-label={isSidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'} className="hidden lg:flex h-7 w-7 p-0 flex-shrink-0">
            {isSidebarOpen ? <ChevronLeft className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
          </Button>
          <Button variant="ghost" size="sm" onClick={() => setIsMobileMenuOpen(false)} className="lg:hidden h-7 w-7 p-0" aria-label="Close menu"><X className="h-5 w-5" /></Button>
        </div>

        <nav className="flex-1 overflow-y-auto overflow-x-hidden p-2.5 space-y-0.5 scrollbar-thin" role="navigation" aria-label="Modules">
          {filteredNavigation.map(item => {
            const isModuleActive = location.pathname === item.href || location.pathname.startsWith(item.href + '/')
            const hasSub = item.sub.length > 0
            const isExpanded = openModule === item.href
            const Icon = item.icon

            if (!isSidebarOpen) {
              const showFlyout = hoveredCollapsed === item.href && hasSub
              return (
                <div key={item.href} className="relative" onMouseEnter={() => setHoveredCollapsed(item.href)} onMouseLeave={() => setHoveredCollapsed(null)}>
                  <Link
                    to={item.href}
                    title={item.name}
                    aria-current={isModuleActive ? 'page' : undefined}
                    className={cn(
                      'flex items-center justify-center py-1 rounded-xl transition-all duration-200 hover:scale-[1.02] active:scale-[0.98]',
                      isModuleActive ? 'bg-primary-50 dark:bg-primary-900/30' : 'hover:bg-gray-100 dark:hover:bg-gray-700'
                    )}
                  >
                    <span className={cn('w-10 h-10 rounded-xl flex items-center justify-center transition-all duration-200', NAV_CHIP_STYLES[item.color], isModuleActive && 'shadow-sm ring-1 ring-black/5')}>
                      <Icon className="h-5 w-5" aria-hidden="true" />
                    </span>
                  </Link>
                  {showFlyout && (
                    <div className="absolute left-[68px] top-0 z-50 w-56 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl shadow-xl p-2 animate-scale-in">
                      <p className="px-3 py-1.5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">{item.name}</p>
                      {item.sub.slice(0, 12).map(sub => {
                        const isSubActive = location.pathname === sub.to || location.pathname.startsWith(sub.to + '/')
                        const SubIcon = getSubIcon(sub.label)
                        return (
                          <Link key={sub.to} to={sub.to} className={cn('flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-colors', isSubActive ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-700 dark:text-primary-300 font-medium' : 'hover:bg-gray-50 dark:hover:bg-gray-700/50 text-gray-600 dark:text-gray-300')}>
                            <SubIcon className={cn('h-3.5 w-3.5 flex-shrink-0', NAV_TEXT_STYLES[item.color])} />
                            <span className="truncate">{sub.label}</span>
                          </Link>
                        )
                      })}
                      {item.sub.length > 12 && <p className="px-3 py-1 text-xs text-gray-400">+{item.sub.length - 12} more — open module to see all</p>}
                    </div>
                  )}
                </div>
              )
            }

            return (
              <div key={item.href}>
                {hasSub ? (
                  <button
                    type="button"
                    onClick={() => setOpenModule(isExpanded ? null : item.href)}
                    aria-expanded={isExpanded}
                    className={cn(
                      'group w-full flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium transition-all duration-200 relative overflow-hidden',
                      isModuleActive
                        ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300 shadow-sm'
                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white hover:translate-x-0.5'
                    )}
                  >
                    {isModuleActive && <span className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-6 bg-primary-600 dark:bg-primary-400 rounded-r-full" />}
                    <span className={cn('w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 transition-all duration-200 group-hover:scale-105', NAV_CHIP_STYLES[item.color], isModuleActive && 'shadow-sm')}>
                      <Icon className="h-[18px] w-[18px]" aria-hidden="true" />
                    </span>
                    <span className="flex-1 text-left truncate">{item.name}</span>
                    <span className="text-xs text-gray-400 font-normal hidden xl:inline">{item.sub.length}</span>
                    <ChevronDown className={cn('h-4 w-4 transition-transform duration-300 ease-[cubic-bezier(0.16,1,0.3,1)] text-gray-400', isExpanded && 'rotate-180 text-gray-600 dark:text-gray-300')} aria-hidden="true" />
                  </button>
                ) : (
                  <Link
                    to={item.href}
                    aria-current={isModuleActive ? 'page' : undefined}
                    className={cn(
                      'group flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium transition-all duration-200 relative',
                      isModuleActive
                        ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300 shadow-sm'
                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white hover:translate-x-0.5'
                    )}
                  >
                    {isModuleActive && <span className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-6 bg-primary-600 dark:bg-primary-400 rounded-r-full" />}
                    <span className={cn('w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 transition-all duration-200 group-hover:scale-105', NAV_CHIP_STYLES[item.color], isModuleActive && 'shadow-sm')}>
                      <Icon className="h-[18px] w-[18px]" aria-hidden="true" />
                    </span>
                    <span className="truncate">{item.name}</span>
                  </Link>
                )}

                {hasSub && (
                  <div className={cn('grid transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)]', isExpanded ? 'grid-rows-[1fr] opacity-100 mt-1' : 'grid-rows-[0fr] opacity-0')}>
                    <div className="overflow-hidden">
                      <ul className="ml-4 pl-3 border-l border-gray-200 dark:border-gray-700 space-y-0.5 py-1">
                        {item.sub.map((sub, idx) => {
                          const subUrl = new URL(sub.to, window.location.origin)
                          const isSubActive = subUrl.search
                            ? (location.pathname === subUrl.pathname && location.search.includes(subUrl.search))
                            : (location.pathname === sub.to || location.pathname.startsWith(sub.to + '/'))
                          const SubIcon = getSubIcon(sub.label)
                          return (
                            <li key={sub.to} className="animate-stagger" style={{ animationDelay: `${idx * 18}ms` }}>
                              <Link
                                to={sub.to}
                                aria-current={isSubActive ? 'page' : undefined}
                                className={cn(
                                  'flex items-center gap-2.5 px-3 py-1.5 rounded-lg text-sm transition-all duration-200 group/sub',
                                  isSubActive
                                    ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-800 dark:text-primary-200 font-medium shadow-sm translate-x-0.5'
                                    : 'text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white hover:translate-x-0.5'
                                )}
                              >
                                <SubIcon className={cn('h-3.5 w-3.5 flex-shrink-0 transition-transform duration-200 group-hover/sub:scale-110', isSubActive ? NAV_TEXT_STYLES[item.color] : cn(NAV_TEXT_STYLES[item.color], 'opacity-60 group-hover/sub:opacity-100'))} aria-hidden="true" />
                                <span className="truncate">{sub.label}</span>
                              </Link>
                            </li>
                          )
                        })}
                      </ul>
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </nav>

        <div className="p-2.5 border-t border-gray-200 dark:border-gray-700 space-y-2 flex-shrink-0">
          {isSidebarOpen && (
            <Fragment>
              <Combobox label="Company" placeholder="Select company..." options={companyOptions} value={currentCompany?.id ?? ''} onChange={handleCompanyChange} className="mb-1" />
              <Combobox label="Period" placeholder="Select period..." options={periodOptions} value={currentPeriod?.id} onChange={handlePeriodChange} className="mb-1" />
            </Fragment>
          )}

          <Button variant="ghost" size="sm" className={cn('w-full justify-start gap-3 rounded-xl transition-colors', !isSidebarOpen && 'justify-center')} onClick={toggleDarkMode} aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'} title={isSidebarOpen ? undefined : (isDarkMode ? 'Light mode' : 'Dark mode')}>
            {isDarkMode ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            {isSidebarOpen && <span className="text-sm">{isDarkMode ? 'Light' : 'Dark'} Mode</span>}
          </Button>

          <div className="relative" ref={userMenuRef}>
            <Button variant="ghost" size="sm" className={cn('w-full justify-start gap-3 rounded-xl', !isSidebarOpen && 'justify-center')} onClick={() => setIsUserMenuOpen(!isUserMenuOpen)} aria-expanded={isUserMenuOpen} aria-haspopup="true" title={isSidebarOpen ? undefined : 'User menu'}>
              <div className="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center flex-shrink-0 ring-2 ring-primary-100 dark:ring-primary-900/30">
                <span className="text-sm font-bold text-primary-600 dark:text-primary-400">{userInitials}</span>
              </div>
              {isSidebarOpen && (
                <div className="flex-1 text-left min-w-0">
                  <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{user?.fullName || user?.email}</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{currentCompany?.name || 'No company'}</p>
                </div>
              )}
              {isSidebarOpen && <ChevronDown className={cn('h-4 w-4 transition-transform duration-300', isUserMenuOpen && 'rotate-180')} />}
            </Button>

            {isUserMenuOpen && (
              <div className="absolute bottom-full left-0 right-0 mb-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl shadow-xl z-50 overflow-hidden animate-scale-in">
                <div className="p-3 border-b border-gray-200 dark:border-gray-700">
                  <p className="text-sm font-semibold text-gray-900 dark:text-white truncate">{user?.fullName || user?.email}</p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{user?.email}</p>
                  {user?.roles && user.roles.length > 0 && (
                    <div className="flex flex-wrap gap-1 mt-2">
                      {user.roles.map((r: { name: string }) => (
                        <span key={r.name} className="px-2 py-0.5 text-xs bg-primary-100 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300 rounded-full">{r.name}</span>
                      ))}
                    </div>
                  )}
                </div>
                <div className="py-1">
                  <Link to="/platform/users" className="flex items-center gap-3 px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors" onClick={() => setIsUserMenuOpen(false)}><User className="h-4 w-4" />My Profile</Link>
                  <Link to="/platform/roles" className="flex items-center gap-3 px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors" onClick={() => setIsUserMenuOpen(false)}><Settings className="h-4 w-4" />Settings</Link>
                  <Link to="/platform/api-keys" className="flex items-center gap-3 px-3 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors" onClick={() => setIsUserMenuOpen(false)}><Grid className="h-4 w-4" />API Keys</Link>
                </div>
                <div className="border-t border-gray-200 dark:border-gray-700 py-1">
                  <button onClick={() => { setIsUserMenuOpen(false); logout() }} className="flex items-center gap-3 px-3 py-2 text-sm text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 w-full transition-colors"><LogOut className="h-4 w-4" />Sign Out</button>
                </div>
              </div>
            )}
          </div>
        </div>
      </aside>

      <div className={cn('flex flex-col flex-1 min-h-screen transition-all duration-300 ease-[cubic-bezier(0.16,1,0.3,1)]', isSidebarOpen ? 'lg:ml-64' : 'lg:ml-[72px]')}>
        <header className="sticky top-0 z-30 bg-white/80 dark:bg-gray-900/80 backdrop-blur-xl border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between h-16 px-4 sm:px-6 lg:px-8 gap-4">
            <div className="flex items-center gap-3 min-w-0 flex-1">
              <Button variant="ghost" size="sm" className="lg:hidden h-8 w-8 p-0 flex-shrink-0" onClick={() => setIsMobileMenuOpen(true)} aria-label="Open menu"><Menu className="h-5 w-5" /></Button>
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-1.5 text-xs text-gray-500 dark:text-gray-400 animate-fade-in" key={location.pathname}>
                  <Link to="/dashboard" className="hover:text-gray-700 dark:hover:text-gray-200 p-1 hover:bg-gray-100 dark:hover:bg-gray-800 rounded transition-colors" aria-label="Dashboard"><Home className="h-3.5 w-3.5" /></Link>
                  {resolvedModule && (
                    <>
                      <ChevronRight className="h-3 w-3 flex-shrink-0" />
                      <span className="truncate font-medium">{resolvedModule.name}</span>
                      {pageSubtitle && (
                        <>
                          <ChevronRight className="h-3 w-3 flex-shrink-0" />
                          <span className="truncate font-semibold text-gray-700 dark:text-gray-200">{pageSubtitle}</span>
                        </>
                      )}
                    </>
                  )}
                </div>
                <h1 className="text-[15px] font-semibold text-gray-900 dark:text-white truncate leading-tight">
                  {pageTitle}
                  {pageSubtitle ? <span className="font-normal text-gray-500 dark:text-gray-400"> · {pageSubtitle}</span> : ''}
                </h1>
              </div>
            </div>

            <div className="flex items-center gap-1.5 flex-shrink-0">
              <button onClick={() => setCommandOpen(true)} className="hidden md:flex items-center gap-2 pl-3 pr-2 py-2 text-sm border border-gray-200 dark:border-gray-600 rounded-xl bg-white dark:bg-gray-800 text-gray-500 hover:border-gray-300 dark:hover:border-gray-500 hover:shadow-sm transition-all duration-200 group min-w-[200px]">
                <Search className="h-4 w-4 flex-shrink-0 group-hover:text-gray-700 dark:group-hover:text-gray-300 transition-colors" />
                <span className="flex-1 text-left text-xs">Search...</span>
                <kbd className="hidden lg:inline-flex items-center gap-1 px-1.5 py-0.5 text-[11px] bg-gray-100 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded font-mono"><Command className="h-3 w-3" />K</kbd>
              </button>
              <Button variant="ghost" size="sm" aria-label="Search" className="md:hidden h-8 w-8 p-0" onClick={() => setCommandOpen(true)}><Search className="h-4 w-4" /></Button>

              <Button variant="ghost" size="sm" aria-label="Help" className="hidden sm:flex h-8 w-8 p-0 hover:rotate-12 transition-transform duration-200"><HelpCircle className="h-5 w-5" /></Button>

              <div className="relative" ref={notifMenuRef}>
                <Button variant="ghost" size="sm" aria-label="Notifications" className="relative h-8 w-8 p-0" onClick={() => setIsNotificationOpen(!isNotificationOpen)}>
                  <Bell className={cn('h-5 w-5 transition-transform duration-200', isNotificationOpen && 'rotate-12')} />
                  {unreadCount > 0 && (
                    <span className="absolute -top-1 -right-1 w-5 h-5 bg-red-500 text-white text-[11px] rounded-full flex items-center justify-center font-bold shadow-sm animate-pulse-soft ring-2 ring-white dark:ring-gray-900">
                      {unreadCount > 99 ? '99+' : unreadCount}
                    </span>
                  )}
                </Button>

                {isNotificationOpen && (
                  <div className="absolute right-0 top-full mt-2 w-80 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl shadow-xl z-50 overflow-hidden animate-scale-in">
                    <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700">
                      <h3 className="text-sm font-semibold text-gray-900 dark:text-white">Notifications</h3>
                      {unreadCount > 0 && <button onClick={markAllAsRead} className="text-xs text-primary-600 hover:text-primary-700 dark:text-primary-400 font-medium">Mark all read</button>}
                    </div>
                    <div className="max-h-80 overflow-y-auto scrollbar-thin">
                      {notifications.length === 0 ? (
                        <div className="p-8 text-center">
                          <div className="w-12 h-12 mx-auto mb-3 rounded-full bg-gray-100 dark:bg-gray-700 flex items-center justify-center"><Bell className="h-6 w-6 text-gray-400" /></div>
                          <p className="text-sm text-gray-500 dark:text-gray-400">No notifications</p>
                          <p className="text-xs text-gray-400 mt-1">You're all caught up</p>
                        </div>
                      ) : (
                        notifications.slice(0, 10).map(notif => (
                          <div key={notif.id} className={cn('flex items-start gap-3 p-3 hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer border-b border-gray-100 dark:border-gray-700 last:border-0 transition-colors', !notif.isRead && 'bg-blue-50/50 dark:bg-blue-900/10')} onClick={() => { if (!notif.isRead) markAsRead(notif.id); if (notif.actionUrl) window.location.href = notif.actionUrl }}>
                            <div className="flex-shrink-0 mt-0.5">{getNotifIcon(notif.type)}</div>
                            <div className="flex-1 min-w-0">
                              <p className={cn('text-sm leading-snug', notif.isRead ? 'text-gray-600 dark:text-gray-400' : 'text-gray-900 dark:text-white font-medium')}>{notif.title}</p>
                              <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5 line-clamp-2">{notif.message}</p>
                              <div className="flex items-center gap-2 mt-1.5">
                                <span className="inline-flex px-1.5 py-0.5 text-[11px] bg-gray-100 dark:bg-gray-700 rounded font-medium">{notif.module}</span>
                                <span className="text-xs text-gray-400">{new Date(notif.createdOn).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                              </div>
                            </div>
                            {!notif.isRead && <div className="w-2 h-2 bg-blue-500 rounded-full flex-shrink-0 mt-2 animate-pulse-soft" />}
                          </div>
                        ))
                      )}
                    </div>
                    {notifications.length > 0 && (
                      <div className="p-2 border-t border-gray-200 dark:border-gray-700">
                        <Link to="/platform/audit-logs" className="block text-center text-xs text-primary-600 hover:text-primary-700 dark:text-primary-400 py-1.5 rounded-lg hover:bg-primary-50 dark:hover:bg-primary-900/20 transition-colors" onClick={() => setIsNotificationOpen(false)}>View all activity →</Link>
                      </div>
                    )}
                  </div>
                )}
              </div>

              <div className="hidden sm:block lg:hidden">
                <Combobox placeholder="Company..." options={companyOptions} value={currentCompany?.id ?? ''} onChange={handleCompanyChange} />
              </div>
            </div>
          </div>
        </header>

        <main className="flex-1 p-4 sm:p-6 lg:p-8 animate-fade-in" key={location.pathname}>
          <Outlet />
        </main>

        <footer className="border-t border-gray-200 dark:border-gray-700 bg-white/50 dark:bg-gray-800/50 backdrop-blur-sm">
          <div className="px-4 sm:px-6 lg:px-8 py-3 flex flex-col sm:flex-row items-center justify-between gap-2 text-xs text-gray-500 dark:text-gray-400">
            <p className="flex items-center gap-2"><span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse-soft" />© {new Date().getFullYear()} ERP Project</p>
            <div className="flex items-center gap-3">
              <span className="inline-flex items-center gap-1.5 px-2.5 py-1 bg-gray-100 dark:bg-gray-700 rounded-full text-xs font-medium">
                <Building2 className="h-3 w-3" />{currentCompany ? currentCompany.name : 'No company'}
              </span>
              <span className="hidden sm:inline-flex items-center gap-1 text-xs"><Clock className="h-3 w-3" />{currentPeriod ? currentPeriod.periodName : 'No period'}</span>
              <span className="hidden sm:inline text-gray-300 dark:text-gray-600">·</span>
              <span className="hidden sm:inline font-mono">v1.0.0</span>
            </div>
          </div>
        </footer>
      </div>
    </div>
  )
}
