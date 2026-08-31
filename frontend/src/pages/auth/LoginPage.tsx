import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Building2,
  Lock,
  Mail,
  Eye,
  EyeOff,
  ArrowRight,
  Shield,
  BarChart3,
  Zap,
  CheckCircle2,
} from 'lucide-react'
import { useAuth } from '@stores/authStore'
import { loginUser } from '@/api/auth'
import { getErrorMessage } from '@/api/client'

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  rememberMe: z.any().optional(),
})

type LoginForm = z.infer<typeof loginSchema>

const features = [
  {
    icon: BarChart3,
    title: 'Real-Time Analytics',
    description: 'Live dashboards with financial insights across all modules.',
  },
  {
    icon: Shield,
    title: 'Enterprise Security',
    description: 'SOC 2 compliant with role-based access and audit trails.',
  },
  {
    icon: Zap,
    title: 'Lightning Fast',
    description: 'Optimized performance for high-volume transaction processing.',
  },
]

export function LoginPage() {
  const { setAuth, setLoading, setError, error: authError } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [mounted, setMounted] = useState(false)

  useEffect(() => {
    setMounted(true)
  }, [])

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
      rememberMe: false,
    },
  })

  const onSubmit = async (data: LoginForm) => {
    setIsLoading(true)
    setLoading(true)
    setError(null)

    try {
      const raw = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({ username: data.email, password: data.password }),
      })
      const json = await raw.json()
      if (!raw.ok || !json.isSuccess) throw new Error(json.errors?.[0] || json.message || 'Login failed')
      const res = json.data as { user: any; accessToken: string; refreshToken: string; companies: any[]; fiscalPeriods: any[]; isSuperAdmin: boolean }

      setAuth({
        user: res.user,
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        companies: res.companies,
        fiscalPeriods: res.fiscalPeriods,
        isSuperAdmin: res.isSuperAdmin,
      })

      window.location.href = '/dashboard'
    } catch (err) {
      console.error('LOGIN ERR', err)
      setError(getErrorMessage(err) || 'Invalid email or password. Please try again.')
    } finally {
      setIsLoading(false)
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex">
      {/* Left Panel — Branded Hero */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden">
        {/* Animated gradient background */}
        <div className="absolute inset-0 bg-gradient-to-br from-primary-800 via-primary-700 to-primary-900">
          <div className="absolute inset-0 opacity-30">
            <div className="absolute top-0 -left-40 w-96 h-96 bg-primary-400 rounded-full mix-blend-multiply filter blur-3xl animate-pulse" />
            <div className="absolute bottom-0 -right-40 w-96 h-96 bg-primary-300 rounded-full mix-blend-multiply filter blur-3xl animate-pulse animation-delay-2000" />
            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-80 h-80 bg-primary-500 rounded-full mix-blend-multiply filter blur-3xl animate-pulse animation-delay-4000" />
          </div>
          {/* Grid pattern overlay */}
          <div
            className="absolute inset-0 opacity-[0.04]"
            style={{
              backgroundImage: `linear-gradient(rgba(255,255,255,0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.1) 1px, transparent 1px)`,
              backgroundSize: '40px 40px',
            }}
          />
        </div>

        {/* Content */}
        <div className="relative z-10 flex flex-col justify-between p-12 w-full">
          {/* Logo */}
          <div className={`transition-all duration-700 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 -translate-y-4'}`}>
            <Link to="/" className="inline-flex items-center gap-3 group" aria-label="ERP Home">
              <div className="w-11 h-11 rounded-xl bg-white/10 backdrop-blur-sm border border-white/20 flex items-center justify-center group-hover:bg-white/20 transition-colors">
                <Building2 className="w-6 h-6 text-white" />
              </div>
              <span className="text-xl font-bold text-white tracking-tight">NERP</span>
            </Link>
          </div>

          {/* Center Content */}
          <div className={`flex-1 flex flex-col justify-center max-w-md transition-all duration-700 delay-200 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-8'}`}>
            <h1 className="text-4xl font-bold text-white leading-tight mb-4">
              Modern ERP for
              <br />
              <span className="text-primary-200">Modern Business</span>
            </h1>
            <p className="text-lg text-primary-200/80 mb-10 leading-relaxed">
              Streamline your financial operations with real-time insights,
              automated workflows, and enterprise-grade security.
            </p>

            {/* Feature List */}
            <div className="space-y-5">
              {features.map((feature, index) => (
                <div
                  key={feature.title}
                  className={`flex items-start gap-4 transition-all duration-500 ${mounted ? 'opacity-100 translate-x-0' : 'opacity-0 -translate-x-8'}`}
                  style={{ transitionDelay: `${400 + index * 150}ms` }}
                >
                  <div className="w-10 h-10 rounded-lg bg-white/10 backdrop-blur-sm border border-white/10 flex items-center justify-center flex-shrink-0">
                    <feature.icon className="w-5 h-5 text-primary-200" />
                  </div>
                  <div>
                    <h3 className="text-sm font-semibold text-white mb-0.5">{feature.title}</h3>
                    <p className="text-sm text-primary-200/70">{feature.description}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Footer */}
          <div className={`transition-all duration-700 delay-700 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'}`}>
            <p className="text-xs text-primary-300/50">
              © 2026 NERP. All rights reserved. Built with modern architecture.
            </p>
          </div>
        </div>
      </div>

      {/* Right Panel — Login Form */}
      <div className="flex-1 flex items-center justify-center px-6 py-12 bg-white dark:bg-gray-950 relative">
        {/* Subtle background pattern */}
        <div
          className="absolute inset-0 opacity-[0.015] dark:opacity-[0.03]"
          style={{
            backgroundImage: `radial-gradient(circle at 1px 1px, currentColor 1px, transparent 0)`,
            backgroundSize: '32px 32px',
          }}
        />

        <div className={`w-full max-w-sm relative z-10 transition-all duration-700 delay-300 ${mounted ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-8'}`}>
          {/* Mobile Logo */}
          <div className="lg:hidden text-center mb-8">
            <Link to="/" className="inline-flex items-center gap-2.5" aria-label="ERP Home">
              <div className="w-10 h-10 rounded-xl bg-primary-600 flex items-center justify-center">
                <Building2 className="w-5 h-5 text-white" />
              </div>
              <span className="text-xl font-bold text-gray-900 dark:text-white">NERP</span>
            </Link>
          </div>

          {/* Header */}
          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white tracking-tight">
              Welcome back
            </h2>
            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
              Sign in to your account to continue
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
            {/* Email Field */}
            <div className="relative group">
              <label
                htmlFor="email"
                className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
              >
                Email address
              </label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 group-focus-within:text-primary-500 transition-colors pointer-events-none" />
                <input
                  id="email"
                  {...register('email')}
                  type="email"
                  placeholder="you@company.com"
                  autoComplete="email"
                  required
                  className={`w-full h-12 pl-10 pr-4 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm transition-all duration-200 outline-none
                    ${errors.email
                      ? 'border-red-300 dark:border-red-600 focus:border-red-500 focus:ring-2 focus:ring-red-500/20'
                      : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 hover:border-gray-300 dark:hover:border-gray-700'
                    }`}
                />
              </div>
              {errors.email && (
                <p className="mt-1.5 text-xs text-red-500 dark:text-red-400 flex items-center gap-1">
                  <span className="inline-block w-1 h-1 rounded-full bg-red-500" />
                  {errors.email.message}
                </p>
              )}
            </div>

            {/* Password Field */}
            <div className="relative group">
              <label
                htmlFor="password"
                className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
              >
                Password
              </label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 group-focus-within:text-primary-500 transition-colors pointer-events-none" />
                <input
                  id="password"
                  {...register('password')}
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Enter your password"
                  autoComplete="current-password"
                  required
                  className={`w-full h-12 pl-10 pr-12 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm transition-all duration-200 outline-none
                    ${errors.password
                      ? 'border-red-300 dark:border-red-600 focus:border-red-500 focus:ring-2 focus:ring-red-500/20'
                      : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20 hover:border-gray-300 dark:hover:border-gray-700'
                    }`}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1.5 text-xs text-red-500 dark:text-red-400 flex items-center gap-1">
                  <span className="inline-block w-1 h-1 rounded-full bg-red-500" />
                  {errors.password.message}
                </p>
              )}
            </div>

            {/* Remember Me & Forgot Password */}
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2.5 cursor-pointer group">
                <div className="relative">
                  <input
                    {...register('rememberMe')}
                    type="checkbox"
                    className="peer sr-only"
                  />
                  <div className="w-4 h-4 rounded border-2 border-gray-300 dark:border-gray-600 peer-checked:border-primary-500 peer-checked:bg-primary-500 transition-all duration-200 flex items-center justify-center">
                    <CheckCircle2 className="w-3 h-3 text-white opacity-0 peer-checked:opacity-100 transition-opacity" />
                  </div>
                </div>
                <span className="text-sm text-gray-600 dark:text-gray-400 group-hover:text-gray-900 dark:group-hover:text-gray-200 transition-colors">
                  Remember me
                </span>
              </label>
              <Link
                to="/forgot-password"
                className="text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300 font-medium transition-colors"
              >
                Forgot password?
              </Link>
            </div>

            {authError && (
              <div className="rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-300">
                {authError}
              </div>
            )}

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className="group relative w-full h-12 rounded-xl bg-primary-600 hover:bg-primary-700 active:bg-primary-800 text-white font-semibold text-sm transition-all duration-200 disabled:opacity-60 disabled:cursor-not-allowed overflow-hidden"
            >
              <span className={`flex items-center justify-center gap-2 transition-all duration-200 ${isLoading ? 'opacity-0' : 'opacity-100'}`}>
                Sign In
                <ArrowRight className="w-4 h-4 group-hover:translate-x-0.5 transition-transform" />
              </span>
              {isLoading && (
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                </div>
              )}
              {/* Hover gradient */}
              <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-full group-hover:translate-x-full transition-transform duration-700" />
            </button>
          </form>

          {/* Footer */}
          <p className="mt-8 text-center text-xs text-gray-400 dark:text-gray-500">
            Don't have an account?{' '}
            <Link
              to="/register"
              className="text-primary-600 hover:text-primary-700 dark:text-primary-400 font-medium transition-colors"
            >
              Request access
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}

