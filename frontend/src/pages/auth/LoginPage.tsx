import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Building2, Lock, Mail, Eye, EyeOff } from 'lucide-react'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { useAuth } from '@stores/authStore'
import { loginUser } from '@/api/auth'
import { getErrorMessage } from '@/api/client'

const loginSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  rememberMe: z.boolean().optional(),
})

type LoginForm = z.infer<typeof loginSchema>

export function LoginPage() {
  const navigate = useNavigate()
  const { setAuth, setLoading, setError } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)

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
      // Real auth: POST credentials to the local /auth/login endpoint, which
      // validates against the Platform user store and returns a JWT plus the
      // user's companies + open fiscal periods.
      const res = await loginUser({ username: data.email, password: data.password })

      setAuth({
        user: res.user,
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        companies: res.companies,
        fiscalPeriods: res.fiscalPeriods,
      })

      navigate('/dashboard')
    } catch (err) {
      setError(getErrorMessage(err) || 'Invalid email or password. Please try again.')
    } finally {
      setIsLoading(false)
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4 py-12">
      <div className="w-full max-w-md">
        {/* Logo */}
        <div className="text-center mb-8">
          <Link to="/" className="inline-flex items-center gap-2" aria-label="ERP Home">
            <div className="w-12 h-12 rounded-xl bg-primary-600 flex items-center justify-center">
              <Building2 className="w-7 h-7 text-white" aria-hidden="true" />
            </div>
            <span className="text-2xl font-bold text-gray-900 dark:text-white">ERP</span>
          </Link>
          <p className="mt-4 text-gray-600 dark:text-gray-400">Sign in to your account</p>
        </div>

        {/* Login Card */}
        <div className="bg-white dark:bg-gray-800 rounded-xl shadow-card border border-gray-200 dark:border-gray-700 p-8">
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
            {/* Email */}
            <div>
              <Input
                {...register('email')}
                type="email"
                label="Email"
                placeholder="you@company.com"
                leftIcon={<Mail className="h-5 w-5" aria-hidden="true" />}
                error={errors.email?.message}
                autoComplete="email"
                required
              />
            </div>

            {/* Password */}
            <div>
              <Input
                {...register('password')}
                type={showPassword ? 'text' : 'password'}
                label="Password"
                placeholder="Enter your password"
                leftIcon={<Lock className="h-5 w-5" aria-hidden="true" />}
                rightIcon={
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                  >
                    {showPassword ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
                  </button>
                }
                error={errors.password?.message}
                autoComplete="current-password"
                required
              />
            </div>

            {/* Remember Me & Forgot Password */}
            <div className="flex items-center justify-between">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  {...register('rememberMe')}
                  type="checkbox"
                  className="h-4 w-4 rounded border-gray-300 text-primary-600 focus:ring-2 focus:ring-primary-500"
                />
                <span className="text-sm text-gray-700 dark:text-gray-300">Remember me</span>
              </label>
              <Link to="/forgot-password" className="text-sm text-primary-600 hover:text-primary-700 dark:text-primary-400">
                Forgot password?
              </Link>
            </div>

            {/* Submit Button */}
            <Button
              type="submit"
              variant="primary"
              size="lg"
              fullWidth
              isLoading={isLoading}
              leftIcon={<Lock className="h-5 w-5" aria-hidden="true" />}
            >
              Sign In
            </Button>

            {/* Demo Credentials */}
            <div className="rounded-lg bg-gray-50 dark:bg-gray-900/50 p-4 border border-gray-200 dark:border-gray-700">
              <p className="text-xs text-gray-600 dark:text-gray-400 text-center">
                <strong>Demo Credentials:</strong><br />
                Email: demo@erp.com<br />
                Password: password123
              </p>
            </div>
          </form>

          {/* Divider */}
          <div className="relative my-6">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-200 dark:border-gray-700" />
            </div>
            <div className="relative flex justify-center text-sm">
              <span className="px-4 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400">Or continue with</span>
            </div>
          </div>

          {/* SSO Buttons */}
          <div className="grid grid-cols-2 gap-3">
            <Button
              type="button"
              variant="outline"
              size="md"
              onClick={() => {/* TODO: Azure AD */}}
            >
              <svg className="h-5 w-5" viewBox="0 0 24 24" aria-hidden="true">
                <path fill="currentColor" d="M12.6 2.4c-.9-1-2.6-1-3.6 0l-8.1 9c-.9 1-.3 2.4 1 3v9c0 .7.6 1.3 1.3 1.3h15.6c.7 0 1.3-.6 1.3-1.3v-9c1.3-.6 1.9-2 1-3zM12 6.7v7.7l3.9-3.8z" />
              </svg>
              <span className="hidden sm:inline">Azure AD</span>
            </Button>
            <Button
              type="button"
              variant="outline"
              size="md"
              onClick={() => {/* TODO: Google */}}
            >
              <svg className="h-5 w-5" viewBox="0 0 24 24" aria-hidden="true">
                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                <path fill="#FBBC05" d="M8.08 14.75c0 1.36.46 2.53 1.27 3.4H5.17v-2.84h2.91zm6.04 3.4H12v2.84h5.92c1.48-2.68 1.48-5.93 0-8.51H12V5.75h5.08c2.34 2.15 2.34 5.7 0 7.85z" />
                <path fill="#EA4335" d="M12 5.75c1.82 0 3.53.6 4.85 1.69l3.15-3.15C18.5 2.83 15.31 2 12 2 7.31 2 3.25 5.35 2.18 9.18h2.84c.87-2.45 3.3-4.48 6.98-4.48z" />
              </svg>
              <span className="hidden sm:inline">Google</span>
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}