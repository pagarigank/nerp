import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { AlertCircle, Calculator } from 'lucide-react'
import { formatDate, formatNumber } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { getErrorMessage } from '@api/client'
import { calculateFinanceCharges } from '@api/ar'

interface ChargeResult {
  count: number
  asOfDate: string
  annualRate: number
}

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function FinanceChargesPage() {
  const [rate, setRate] = useState('18')
  const [error, setError] = useState<string | null>(null)

  const calculateMutation = useMutation({
    mutationFn: () => calculateFinanceCharges({ annualRate: Number(rate) }),
    onSuccess: () => setError(null),
    onError: err => setError(getErrorMessage(err)),
  })

  const result: ChargeResult | undefined = calculateMutation.data
  const parsedRate = Number(rate)
  const isRateValid = !Number.isNaN(parsedRate) && parsedRate > 0

  return (
    <div className="space-y-6">
      {error && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{error}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Finance Charges"
          description="Calculate finance charges on overdue invoices for all customers."
        />
        <CardContent>
          <div className="max-w-sm space-y-4">
            <Input
              label="Annual Rate (%)"
              type="number"
              step="0.01"
              min="0"
              value={rate}
              onChange={e => setRate(e.target.value)}
              rightAddon="%"
              {...fieldError(isRateValid ? undefined : 'Enter a valid annual rate greater than zero')}
            />
            <Button
              variant="primary"
              leftIcon={<Calculator className="h-4 w-4" />}
              onClick={() => calculateMutation.mutate()}
              isLoading={calculateMutation.isPending}
              disabled={!isRateValid}
            >
              Calculate Charges
            </Button>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              This is a preview calculation and does not post any journal entries.
            </p>
          </div>
        </CardContent>
      </Card>

      {result && (
        <Card>
          <CardHeader title="Calculation Result" description="Finance charges calculated" />
          <CardContent>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm">
              <div>
                <p className="text-gray-500 dark:text-gray-400">Invoices Charged</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1 tabular-nums">
                  {formatNumber(result.count, 0)}
                </p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">Annual Rate</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1 tabular-nums">
                  {formatNumber(result.annualRate, 2)}%
                </p>
              </div>
              <div>
                <p className="text-gray-500 dark:text-gray-400">As Of</p>
                <p className="text-2xl font-bold text-gray-900 dark:text-white mt-1">{formatDate(result.asOfDate)}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
