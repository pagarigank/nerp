import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  Save,
  CheckCircle,
  AlertCircle,
  Shield,
  Building2,
  MapPin,
  Hash,
  RefreshCw,
} from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input, Select } from '@components/ui/Input'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import {
  getTaxpayerProfile,
  saveTaxpayerProfile,
  validateTin,
  type TaxpayerProfile,
  type TinValidationResult,
} from '@api/einvoice'

const profileSchema = z.object({
  tin: z.string().trim().min(1, 'TIN is required').max(14, 'TIN must be 14 characters or less'),
  brn: z.string().trim().min(1, 'BRN is required'),
  salesTaxNumber: z.string().optional(),
  serviceTaxNumber: z.string().optional(),
  msicCode: z.string().optional(),
  address: z.string().optional(),
  city: z.string().optional(),
  state: z.string().optional(),
  postCode: z.string().optional(),
})

type ProfileForm = z.infer<typeof profileSchema>

const malaysiaStates = [
  { value: 'JHR', label: 'Johor' },
  { value: 'KDH', label: 'Kedah' },
  { value: 'KEL', label: 'Kelantan' },
  { value: 'MLK', label: 'Melaka' },
  { value: 'NSN', label: 'Negeri Sembilan' },
  { value: 'PHG', label: 'Pahang' },
  { value: 'PRK', label: 'Perak' },
  { value: 'PLS', label: 'Perlis' },
  { value: 'PNG', label: 'Pulau Pinang' },
  { value: 'SBH', label: 'Sabah' },
  { value: 'SWK', label: 'Sarawak' },
  { value: 'SGR', label: 'Selangor' },
  { value: 'TRG', label: 'Terengganu' },
  { value: 'WP', label: 'Wilayah Persekutuan' },
]

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function CompanyEInvoiceProfilePage() {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const [saveSuccess, setSaveSuccess] = useState(false)
  const [tinValidation, setTinValidation] = useState<TinValidationResult | null>(null)

  // TODO: Replace with actual companyId from auth context
  const companyId = '00000000-0000-0000-0000-000000000001'

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isDirty },
  } = useForm<ProfileForm>({
    resolver: zodResolver(profileSchema),
    defaultValues: {
      tin: '',
      brn: '',
      salesTaxNumber: '',
      serviceTaxNumber: '',
      msicCode: '',
      address: '',
      city: '',
      state: '',
      postCode: '',
    },
  })

  const { data: profile, isLoading } = useQuery({
    queryKey: ['einvoice', 'taxpayer-profile', companyId],
    queryFn: () => getTaxpayerProfile(companyId),
  })

  // Reset form when profile loads
  const profileLoaded = profile && !isDirty
  if (profile && watch('tin') === '' && profile.tin) {
    reset({
      tin: profile.tin,
      brn: profile.brn,
      salesTaxNumber: profile.salesTaxNumber ?? '',
      serviceTaxNumber: profile.serviceTaxNumber ?? '',
      msicCode: profile.msicCode ?? '',
      address: profile.address ?? '',
      city: profile.city ?? '',
      state: profile.state ?? '',
      postCode: profile.postCode ?? '',
    })
  }

  const saveMutation = useMutation({
    mutationFn: (data: ProfileForm) =>
      saveTaxpayerProfile({
        companyId,
        ...data,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['einvoice', 'taxpayer-profile', companyId] })
      setSaveSuccess(true)
      setFormError(null)
      setTimeout(() => setSaveSuccess(false), 3000)
    },
    onError: (err) => {
      setFormError(getErrorMessage(err))
      setSaveSuccess(false)
    },
  })

  const tinMutation = useMutation({
    mutationFn: validateTin,
    onSuccess: (result) => {
      setTinValidation(result)
    },
    onError: () => {
      setTinValidation(null)
    },
  })

  const onSubmit = (data: ProfileForm) => {
    setFormError(null)
    setSaveSuccess(false)
    saveMutation.mutate(data)
  }

  const handleValidateTin = () => {
    const tin = watch('tin')
    if (tin) {
      tinMutation.mutate(tin)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Card>
          <CardContent>
            <div className="animate-pulse space-y-4">
              <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded w-1/3" />
              <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
              <div className="grid grid-cols-2 gap-4">
                {Array.from({ length: 8 }).map((_, i) => (
                  <div key={i} className="h-10 bg-gray-200 dark:bg-gray-700 rounded" />
                ))}
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Status Messages */}
      {formError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      {saveSuccess && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-green-50 border border-green-200 text-green-700 dark:bg-green-900/20 dark:border-green-800 dark:text-green-300"
          role="status"
        >
          <CheckCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">Company e-Invoice profile saved successfully.</span>
        </div>
      )}

      {/* Profile Header */}
      <Card>
        <CardHeader
          title="Company e-Invoice Profile"
          description="Configure your company's MyInvois registration details. These fields are used as the Supplier party on every e-Invoice document."
          action={
            <Button
              variant="primary"
              size="sm"
              onClick={handleSubmit(onSubmit)}
              isLoading={saveMutation.isPending}
              leftIcon={<Save className="h-4 w-4" />}
            >
              {profile ? 'Save Changes' : 'Create Profile'}
            </Button>
          }
        />
      </Card>

      {/* TIN & Registration */}
      <Card>
        <CardHeader
          title="Tax Registration"
          description="Your company's LHDN tax identification details"
        />
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-6" noValidate>
            {/* TIN Section */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="md:col-span-2">
                <Input
                  {...register('tin')}
                  label="Tax Identification Number (TIN)"
                  placeholder="e.g., C230601D123456"
                  {...fieldError(errors.tin?.message)}
                  required
                />
              </div>
              <div className="flex items-end">
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={handleValidateTin}
                  isLoading={tinMutation.isPending}
                  leftIcon={<Shield className="h-4 w-4" />}
                  className="w-full"
                >
                  Validate TIN
                </Button>
              </div>
            </div>

            {/* TIN Validation Result */}
            {tinValidation && (
              <div
                className={`p-4 rounded-lg border ${
                  tinValidation.isValid
                    ? 'bg-green-50 border-green-200 dark:bg-green-900/20 dark:border-green-800'
                    : 'bg-yellow-50 border-yellow-200 dark:bg-yellow-900/20 dark:border-yellow-800'
                }`}
              >
                <div className="flex items-start gap-3">
                  {tinValidation.isValid ? (
                    <CheckCircle className="h-5 w-5 text-green-600 dark:text-green-400 mt-0.5" />
                  ) : (
                    <AlertCircle className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
                  )}
                  <div className="text-sm">
                    <p className="font-medium">
                      {tinValidation.isValid ? 'TIN is valid' : 'TIN validation failed'}
                    </p>
                    {tinValidation.entityName && (
                      <p className="mt-1 text-gray-600 dark:text-gray-400">
                        Entity: {tinValidation.entityName}
                      </p>
                    )}
                    {tinValidation.businessDescription && (
                      <p className="text-gray-600 dark:text-gray-400">
                        Business: {tinValidation.businessDescription}
                      </p>
                    )}
                    {tinValidation.errorMessage && (
                      <p className="mt-1 text-yellow-700 dark:text-yellow-300">
                        {tinValidation.errorMessage}
                      </p>
                    )}
                  </div>
                </div>
              </div>
            )}

            <Input
              {...register('brn')}
              label="Business Registration Number (BRN)"
              placeholder="e.g., 202301001234"
              {...fieldError(errors.brn?.message)}
              required
            />

            {/* SST Numbers */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Input
                {...register('salesTaxNumber')}
                label="Sales Tax Registration Number"
                placeholder="e.g., A-01-2304-56789012"
              />
              <Input
                {...register('serviceTaxNumber')}
                label="Service Tax Registration Number"
                placeholder="e.g., B-01-2304-56789012"
              />
            </div>

            <Input
              {...register('msicCode')}
              label="MSIC Code"
              placeholder="5-digit MSIC code (e.g., 62021)"
            />

            {/* Address Section */}
            <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
              <h3 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3 flex items-center gap-2">
                <MapPin className="h-4 w-4" aria-hidden="true" />
                Registered Address
              </h3>
              <div className="space-y-4">
                <Input
                  {...register('address')}
                  label="Address Line"
                  placeholder="Street address, building, unit number"
                />
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <Input {...register('city')} label="City" placeholder="e.g., Kuala Lumpur" />
                  <Select
                    {...register('state')}
                    label="State"
                    options={malaysiaStates}
                  />
                  <Input
                    {...register('postCode')}
                    label="Postcode"
                    placeholder="e.g., 50000"
                  />
                </div>
              </div>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Profile Status Card */}
      {profile && (
        <Card>
          <CardHeader title="Profile Status" />
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div>
                <span className="text-gray-500 dark:text-gray-400">Status</span>
                <div className="mt-1">
                  <Badge variant="success" size="sm" dot>
                    Active
                  </Badge>
                </div>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">Created</span>
                <p className="mt-1 text-gray-900 dark:text-white">
                  {new Date(profile.createdAt).toLocaleDateString()}
                </p>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">Last Updated</span>
                <p className="mt-1 text-gray-900 dark:text-white">
                  {new Date(profile.updatedAt).toLocaleDateString()}
                </p>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">TIN</span>
                <p className="mt-1 font-mono text-gray-900 dark:text-white">{profile.tin}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
