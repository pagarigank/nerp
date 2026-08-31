import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Building2, Lock, Mail, User as UserIcon, Phone, FileText, CheckCircle2, AlertCircle } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { getPublicCompanies, getRoles, submitAccessRequest } from '@/api/platform'
import type { Role, PublicCompany } from '@/types'

const schema = z.object({
  fullName: z.string().min(2, 'Full name is required'),
  email: z.string().email('Enter a valid email address'),
  username: z.string().min(3, 'Username must be at least 3 characters'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  companyId: z.string().min(1, 'Please select a company'),
  requestedRole: z.string().optional(),
  phoneNumber: z.string().optional(),
  reason: z.string().optional(),
})
type Form = z.infer<typeof schema>

export function RegisterPage() {
  const navigate = useNavigate()
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [submitted, setSubmitted] = useState(false)

  const { data: companies = [] } = useQuery<PublicCompany[]>({
    queryKey: ['companies-public'],
    queryFn: getPublicCompanies,
  })
  const { data: roles = [] } = useQuery<Role[]>({
    queryKey: ['roles-public'],
    queryFn: getRoles,
  })

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<Form>({ resolver: zodResolver(schema) })

  const onSubmit = async (data: Form) => {
    setSubmitError(null)
    try {
      await submitAccessRequest({
        fullName: data.fullName,
        email: data.email,
        username: data.username,
        password: data.password,
        companyId: data.companyId,
        requestedRole: data.requestedRole || 'Staff',
        phoneNumber: data.phoneNumber || null,
        reason: data.reason || null,
      })
      setSubmitted(true)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Could not submit your request. Please try again.'
      setSubmitError(msg)
    }
  }

  if (submitted) {
    return (
      <div className="min-h-screen flex items-center justify-center px-6 bg-white dark:bg-gray-950">
        <div className="w-full max-w-md text-center">
          <div className="mx-auto w-14 h-14 rounded-full bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center mb-5">
            <CheckCircle2 className="w-7 h-7 text-emerald-600 dark:text-emerald-400" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Request submitted</h1>
          <p className="mt-3 text-sm text-gray-500 dark:text-gray-400">
            Your access request has been received. A company administrator (or the
            super admin) will review it and activate your account. You'll be able
            to sign in once it's approved.
          </p>
          <div className="mt-8 flex gap-3 justify-center">
            <Link
              to="/login"
              className="px-4 py-2.5 rounded-xl bg-primary-600 hover:bg-primary-700 text-white text-sm font-semibold transition-colors"
            >
              Back to sign in
            </Link>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex">
      {/* Left Panel — Branded Hero */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-primary-800 via-primary-700 to-primary-900">
          <div className="absolute inset-0 opacity-30">
            <div className="absolute top-0 -left-40 w-96 h-96 bg-primary-400 rounded-full mix-blend-multiply filter blur-3xl animate-pulse" />
            <div className="absolute bottom-0 -right-40 w-96 h-96 bg-primary-300 rounded-full mix-blend-multiply filter blur-3xl animate-pulse animation-delay-2000" />
          </div>
          <div
            className="absolute inset-0 opacity-[0.04]"
            style={{
              backgroundImage: `linear-gradient(rgba(255,255,255,0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.1) 1px, transparent 1px)`,
              backgroundSize: '40px 40px',
            }}
          />
        </div>
        <div className="relative z-10 flex flex-col justify-between p-12 w-full">
          <Link to="/" className="inline-flex items-center gap-3 group" aria-label="ERP Home">
            <div className="w-11 h-11 rounded-xl bg-white/10 backdrop-blur-sm border border-white/20 flex items-center justify-center group-hover:bg-white/20 transition-colors">
              <Building2 className="w-6 h-6 text-white" />
            </div>
            <span className="text-xl font-bold text-white tracking-tight">NERP</span>
          </Link>
          <div className="flex-1 flex flex-col justify-center max-w-md">
            <h1 className="text-4xl font-bold text-white leading-tight mb-4">
              Join your
              <br />
              <span className="text-primary-200">team's workspace</span>
            </h1>
            <p className="text-lg text-primary-200/80 leading-relaxed">
              Request access to NERP. Once a company administrator approves your
              request, you'll be able to sign in and start working.
            </p>
          </div>
          <p className="text-xs text-primary-300/50">© 2026 NERP. All rights reserved.</p>
        </div>
      </div>

      {/* Right Panel — Request Form */}
      <div className="flex-1 flex items-center justify-center px-6 py-12 bg-white dark:bg-gray-950 relative">
        <div className="absolute inset-0 opacity-[0.015] dark:opacity-[0.03]" style={{ backgroundImage: 'radial-gradient(circle at 1px 1px, currentColor 1px, transparent 0)', backgroundSize: '32px 32px' }} />
        <div className="w-full max-w-sm relative z-10">
          <div className="lg:hidden text-center mb-8">
            <Link to="/" className="inline-flex items-center gap-2.5" aria-label="ERP Home">
              <div className="w-10 h-10 rounded-xl bg-primary-600 flex items-center justify-center">
                <Building2 className="w-5 h-5 text-white" />
              </div>
              <span className="text-xl font-bold text-gray-900 dark:text-white">NERP</span>
            </Link>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white tracking-tight">Request access</h2>
            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">Fill in your details and we'll route it for approval.</p>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
            <div>
              <label htmlFor="fullName" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Full name</label>
              <div className="relative">
                <UserIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                <input id="fullName" {...register('fullName')} placeholder="Jane Doe" autoComplete="name"
                  className={`w-full h-12 pl-10 pr-4 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm outline-none ${errors.fullName ? 'border-red-300 dark:border-red-600 focus:border-red-500 focus:ring-2 focus:ring-red-500/20' : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20'}`} />
              </div>
              {errors.fullName && <p className="mt-1.5 text-xs text-red-500">{errors.fullName.message}</p>}
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Email address</label>
              <div className="relative">
                <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                <input id="email" {...register('email')} type="email" placeholder="you@company.com" autoComplete="email"
                  className={`w-full h-12 pl-10 pr-4 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm outline-none ${errors.email ? 'border-red-300 dark:border-red-600 focus:border-red-500 focus:ring-2 focus:ring-red-500/20' : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20'}`} />
              </div>
              {errors.email && <p className="mt-1.5 text-xs text-red-500">{errors.email.message}</p>}
            </div>

            <div>
              <label htmlFor="username" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Username</label>
              <div className="relative">
                <UserIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                <input id="username" {...register('username')} placeholder="jdoe" autoComplete="username"
                  className={`w-full h-12 pl-10 pr-4 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm outline-none ${errors.username ? 'border-red-300 dark:border-red-600 focus:border-red-500 focus:ring-2 focus:ring-red-500/20' : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20'}`} />
              </div>
              {errors.username && <p className="mt-1.5 text-xs text-red-500">{errors.username.message}</p>}
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Password</label>
              <div className="relative">
                <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                <input id="password" {...register('password')} type="password" placeholder="At least 8 characters" autoComplete="new-password"
                  className={`w-full h-12 pl-10 pr-4 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm outline-none ${errors.password ? 'border-red-300 dark:border-red-600 focus:border-red-500 focus:ring-2 focus:ring-red-500/20' : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20'}`} />
              </div>
              {errors.password && <p className="mt-1.5 text-xs text-red-500">{errors.password.message}</p>}
            </div>

            <div>
              <label htmlFor="companyId" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Company</label>
              <select id="companyId" {...register('companyId')}
                className={`w-full h-12 px-3 rounded-xl border bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white text-sm outline-none ${errors.companyId ? 'border-red-300 dark:border-red-600' : 'border-gray-200 dark:border-gray-800 focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20'}`}>
                <option value="">Select a company…</option>
                {companies.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              {errors.companyId && <p className="mt-1.5 text-xs text-red-500">{errors.companyId.message}</p>}
            </div>

            <div>
              <label htmlFor="requestedRole" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Requested role <span className="text-gray-400 font-normal">(optional)</span></label>
              <select id="requestedRole" {...register('requestedRole')}
                className="w-full h-12 px-3 rounded-xl border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20">
                <option value="Staff">Staff (default)</option>
                {roles.filter(r => !/admin/i.test(r.name)).map((r) => <option key={r.id} value={r.name}>{r.name}</option>)}
              </select>
            </div>

            <div>
              <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Phone <span className="text-gray-400 font-normal">(optional)</span></label>
              <div className="relative">
                <Phone className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
                <input id="phoneNumber" {...register('phoneNumber')} placeholder="+1 555 000 0000" autoComplete="tel"
                  className="w-full h-12 pl-10 pr-4 rounded-xl border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20" />
              </div>
            </div>

            <div>
              <label htmlFor="reason" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Reason <span className="text-gray-400 font-normal">(optional)</span></label>
              <div className="relative">
                <FileText className="absolute left-3 top-3 w-4 h-4 text-gray-400 pointer-events-none" />
                <textarea id="reason" {...register('reason')} rows={3} placeholder="Why do you need access?" 
                  className="w-full pl-10 pr-4 py-3 rounded-xl border border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 dark:placeholder-gray-500 text-sm outline-none focus:border-primary-500 focus:ring-2 focus:ring-primary-500/20" />
              </div>
            </div>

            {submitError && (
              <div className="flex items-start gap-2 rounded-xl bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 px-4 py-3 text-sm text-red-700 dark:text-red-300">
                <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
                <span>{submitError}</span>
              </div>
            )}

            <button type="submit" disabled={isSubmitting}
              className="w-full h-12 rounded-xl bg-primary-600 hover:bg-primary-700 active:bg-primary-800 text-white font-semibold text-sm transition-colors disabled:opacity-60 disabled:cursor-not-allowed">
              {isSubmitting ? 'Submitting…' : 'Submit request'}
            </button>
          </form>

          <p className="mt-8 text-center text-xs text-gray-400 dark:text-gray-500">
            Already have access?{' '}
            <Link to="/login" className="text-primary-600 hover:text-primary-700 dark:text-primary-400 font-medium transition-colors">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  )
}
