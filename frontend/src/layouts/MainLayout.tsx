import { Fragment, useState, useEffect } from 'react'
import { Link, useLocation, Outlet } from 'react-router-dom'
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
} from 'lucide-react'
import { cn } from '@utils/helpers'
import { useAuth } from '@stores/authStore'
import { Button } from '@components/ui/Button'
import { Combobox, type SelectOption } from '@components/ui/Combobox'
import { mainNavigation, resolveNav } from '@/navigation'

export function MainLayout() {
  const location = useLocation()
  const { user, currentCompany, currentPeriod, companies, fiscalPeriods, setCurrentCompany, setCurrentPeriod, logout } = useAuth()
  const [isSidebarOpen, setIsSidebarOpen] = useState(true)
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [isDarkMode, setIsDarkMode] = useState(false)
  const [openModule, setOpenModule] = useState<string | null>(null)

  useEffect(() => {
    const saved = localStorage.getItem('erp-dark-mode')
    if (saved) {
      setIsDarkMode(JSON.parse(saved))
      document.documentElement.classList.toggle('dark', JSON.parse(saved))
    }
  }, [])

  useEffect(() => {
    // Auto-expand the module matching the current route when navigating.
    const resolved = resolveNav(location.pathname)
    if (resolved && resolved.module.sub.length > 0) {
      setOpenModule(resolved.module.href)
    }
  }, [location.pathname])

  const toggleDarkMode = () => {
    const newMode = !isDarkMode
    setIsDarkMode(newMode)
    localStorage.setItem('erp-dark-mode', JSON.stringify(newMode))
    document.documentElement.classList.toggle('dark', newMode)
  }

  const filteredNavigation = mainNavigation.filter(item =>
    item.roles.includes('*') || user?.roles?.some((r: { name: string }) => item.roles.includes(r.name))
  )

  const resolved = resolveNav(location.pathname)
  const resolvedModule = resolved?.module
  const resolvedSub = resolved?.sub

  const companyOptions: SelectOption[] = companies.map(c => ({
    value: c.id,
    label: `${c.code} - ${c.name}`,
  }))

  const periodOptions: SelectOption[] = fiscalPeriods.map(p => ({
    value: p.id,
    label: `${p.periodName} (${p.fiscalYear} P${p.periodNumber})`,
  }))

  const handleCompanyChange = (value: string) => {
    const company = companies.find(c => c.id === value)
    if (company) setCurrentCompany(company)
  }

  const handlePeriodChange = (value: string) => {
    const period = fiscalPeriods.find(p => p.id === value)
    if (period) setCurrentPeriod(period)
  }

  const pageTitle = resolvedModule?.name ?? 'Dashboard'
  const pageSubtitle = resolvedSub?.label

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex flex-col">
      {/* Mobile menu overlay */}
      {isMobileMenuOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => setIsMobileMenuOpen(false)}
          aria-hidden="true"
        />
      )}

      {/* Sidebar (main + sub menu) */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 transition-all duration-300',
          'flex flex-col',
          isSidebarOpen ? 'w-64' : 'w-20',
          isMobileMenuOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'
        )}
        aria-label="Main navigation"
      >
        {/* Logo & Toggle */}
        <div className="flex items-center justify-between h-16 px-4 border-b border-gray-200 dark:border-gray-700">
          <Link to="/dashboard" className="flex items-center gap-2" aria-label="ERP Home">
            <div className="w-8 h-8 rounded-lg bg-primary-600 flex items-center justify-center">
              <Grid className="w-5 h-5 text-white" aria-hidden="true" />
            </div>
            {isSidebarOpen && (
              <span className="text-xl font-bold text-gray-900 dark:text-white">ERP</span>
            )}
          </Link>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsSidebarOpen(!isSidebarOpen)}
            aria-label={isSidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'}
            className={cn('lg:hidden', !isSidebarOpen && 'ml-auto')}
          >
            {isSidebarOpen ? <ChevronLeft className="h-5 w-5" /> : <ChevronRight className="h-5 w-5" />}
          </Button>
        </div>

        {/* Main + Sub Navigation */}
        <nav className="flex-1 overflow-y-auto p-3 space-y-1" role="navigation" aria-label="Modules">
          {filteredNavigation.map(item => {
            const isModuleActive =
              location.pathname === item.href || location.pathname.startsWith(item.href + '/')
            const hasSub = item.sub.length > 0
            const isExpanded = openModule === item.href
            const Icon = item.icon

            if (!isSidebarOpen) {
              // Collapsed: single icon button linking to module index.
              return (
                <Link
                  key={item.href}
                  to={item.href}
                  title={item.name}
                  aria-current={isModuleActive ? 'page' : undefined}
                  className={cn(
                    'flex items-center justify-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors duration-fast',
                    isModuleActive
                      ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                      : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                  )}
                >
                  <Icon className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
                </Link>
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
                      'w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors duration-fast',
                      isModuleActive
                        ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white'
                    )}
                  >
                    <Icon className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
                    <span className="flex-1 text-left truncate">{item.name}</span>
                    <ChevronDown
                      className={cn('h-4 w-4 transition-transform', isExpanded && 'rotate-180')}
                      aria-hidden="true"
                    />
                  </button>
                ) : (
                  <Link
                    to={item.href}
                    aria-current={isModuleActive ? 'page' : undefined}
                    className={cn(
                      'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors duration-fast',
                      isModuleActive
                        ? 'bg-primary-50 dark:bg-primary-900/30 text-primary-700 dark:text-primary-300'
                        : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white'
                    )}
                  >
                    <Icon className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
                    <span className="truncate">{item.name}</span>
                  </Link>
                )}

                {/* Sub menu */}
                {hasSub && isExpanded && (
                  <ul className="mt-1 ml-4 pl-3 border-l border-gray-200 dark:border-gray-700 space-y-0.5">
                    {item.sub.map(sub => {
                      const isSubActive =
                        location.pathname === sub.to || location.pathname.startsWith(sub.to + '/')
                      return (
                        <li key={sub.to}>
                          <Link
                            to={sub.to}
                            aria-current={isSubActive ? 'page' : undefined}
                            className={cn(
                              'flex items-center gap-2 px-3 py-1.5 rounded-md text-sm transition-colors duration-fast',
                              isSubActive
                                ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-800 dark:text-primary-200 font-medium'
                                : 'text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white'
                            )}
                          >
                            <ChevronRight className="h-3.5 w-3.5 flex-shrink-0 opacity-60" aria-hidden="true" />
                            <span className="truncate">{sub.label}</span>
                          </Link>
                        </li>
                      )
                    })}
                  </ul>
                )}
              </div>
            )
          })}
        </nav>

        {/* User & Settings */}
        <div className="p-3 border-t border-gray-200 dark:border-gray-700 space-y-2">
          {isSidebarOpen && (
            <Fragment>
              <Combobox
                label="Company"
                placeholder="Select company..."
                options={companyOptions}
                value={currentCompany?.id}
                onChange={handleCompanyChange}
                className="mb-2"
              />
              <Combobox
                label="Period"
                placeholder="Select period..."
                options={periodOptions}
                value={currentPeriod?.id}
                onChange={handlePeriodChange}
                className="mb-2"
              />
            </Fragment>
          )}

          <Button
            variant="ghost"
            size="sm"
            className={cn('w-full justify-start gap-3', !isSidebarOpen && 'justify-center')}
            onClick={toggleDarkMode}
            aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
            title={isSidebarOpen ? undefined : (isDarkMode ? 'Light mode' : 'Dark mode')}
          >
            {isDarkMode ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
            {isSidebarOpen && <span>{isDarkMode ? 'Light' : 'Dark'} Mode</span>}
          </Button>

          <div className="relative">
            <Button
              variant="ghost"
              size="sm"
              className={cn('w-full justify-start gap-3', !isSidebarOpen && 'justify-center')}
              onClick={() => {/* TODO: dropdown */}}
              aria-expanded="false"
              aria-haspopup="true"
              title={isSidebarOpen ? undefined : 'User menu'}
            >
              <div className="w-8 h-8 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center flex-shrink-0">
                <User className="h-4 w-4 text-primary-600 dark:text-primary-400" aria-hidden="true" />
              </div>
              {isSidebarOpen && (
                <div className="flex-1 text-left min-w-0">
                  <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
                    {user?.fullName || user?.email}
                  </p>
                  <p className="text-xs text-gray-500 dark:text-gray-400 truncate">
                    {currentCompany?.name}
                  </p>
                </div>
              )}
            </Button>
          </div>

          <Button
            variant="ghost"
            size="sm"
            className={cn('w-full justify-start gap-3 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20', !isSidebarOpen && 'justify-center')}
            onClick={logout}
            title={isSidebarOpen ? undefined : 'Logout'}
          >
            <LogOut className="h-5 w-5" aria-hidden="true" />
            {isSidebarOpen && <span>Logout</span>}
          </Button>
        </div>
      </aside>

      {/* Right side: header + content + footer */}
      <div
        className={cn(
          'flex flex-col flex-1 min-h-screen transition-all duration-300',
          isSidebarOpen ? 'lg:ml-64' : 'lg:ml-20'
        )}
      >
        {/* Top Header */}
        <header className="sticky top-0 z-30 bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between h-16 px-4 sm:px-6 lg:px-8">
            <div className="flex items-center gap-4 min-w-0">
              <Button
                variant="ghost"
                size="sm"
                className="lg:hidden"
                onClick={() => setIsMobileMenuOpen(true)}
                aria-label="Open menu"
              >
                <Menu className="h-6 w-6" aria-hidden="true" />
              </Button>
              <div className="min-w-0">
                <div className="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
                  <Link to="/dashboard" className="hover:text-gray-700 dark:hover:text-gray-200" aria-label="Dashboard">
                    <Home className="h-4 w-4" aria-hidden="true" />
                  </Link>
                  {resolvedModule && (
                    <>
                      <ChevronRight className="h-3.5 w-3.5" aria-hidden="true" />
                      <span className="truncate">{resolvedModule.name}</span>
                      {pageSubtitle && (
                        <>
                          <ChevronRight className="h-3.5 w-3.5" aria-hidden="true" />
                          <span className="truncate font-medium text-gray-700 dark:text-gray-200">{pageSubtitle}</span>
                        </>
                      )}
                    </>
                  )}
                </div>
                <h1 className="text-lg font-semibold text-gray-900 dark:text-white truncate">
                  {pageTitle}
                  {pageSubtitle ? ` › ${pageSubtitle}` : ''}
                </h1>
              </div>
            </div>

            <div className="flex items-center gap-2">
              <div className="hidden md:block relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" aria-hidden="true" />
                <input
                  type="search"
                  placeholder="Search... (Ctrl+K)"
                  className="w-64 pl-10 pr-4 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  aria-label="Global search"
                />
              </div>
              <Button variant="ghost" size="sm" aria-label="Help">
                <HelpCircle className="h-5 w-5" aria-hidden="true" />
              </Button>
              <Button variant="ghost" size="sm" aria-label="Notifications" className="relative">
                <Building2 className="h-5 w-5" aria-hidden="true" />
                <span className="absolute -top-1 -right-1 w-4 h-4 bg-red-500 text-white text-xs rounded-full flex items-center justify-center">3</span>
              </Button>
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>

        {/* Footer */}
        <footer className="border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <div className="px-4 sm:px-6 lg:px-8 py-4 flex flex-col sm:flex-row items-center justify-between gap-2 text-xs text-gray-500 dark:text-gray-400">
            <p>© {new Date().getFullYear()} ERP Project. All rights reserved.</p>
            <div className="flex items-center gap-4">
              <span>
                {currentCompany ? `${currentCompany.name}` : 'No company'} ·{' '}
                {currentPeriod ? `${currentPeriod.periodName}` : 'No period'}
              </span>
              <span className="hidden sm:inline">v1.0.0</span>
            </div>
          </div>
        </footer>
      </div>
    </div>
  )
}
