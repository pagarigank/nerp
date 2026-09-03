import { ShieldCheck, ExternalLink } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Badge } from '@components/ui/Badge'

export function ExemptZeroRatesPage() {
  return (
    <div className="space-y-6">
      <Card>
        <CardHeader
          title="Exempt & Zero-Rated Supplies"
          description="Configure which product/service categories are exempt or zero-rated from SST"
        />
        <CardContent>
          <div className="text-center py-12">
            <ShieldCheck className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
            <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
              Exempt/zero-rated supply configuration is managed through Tax Codes and Tax Determination Rules.
            </p>
            <p className="text-xs text-gray-400 dark:text-gray-500 mb-4">
              Use the Tax Codes page to create exempt (E) or zero-rated (ZRL) codes, then assign them
              via Tax Determination Rules based on item category, customer type, or place of supply.
            </p>
            <div className="flex justify-center gap-3">
              <a href="/tax/tax-codes" className="inline-flex items-center gap-1 text-sm text-emerald-600 hover:underline">
                Tax Codes <ExternalLink className="h-3 w-3" />
              </a>
              <a href="/tax/determination-rules" className="inline-flex items-center gap-1 text-sm text-emerald-600 hover:underline">
                Determination Rules <ExternalLink className="h-3 w-3" />
              </a>
            </div>
            <div className="mt-6 flex justify-center gap-2">
              <Badge variant="info" size="sm">E — Exempt</Badge>
              <Badge variant="success" size="sm">ZRL — Zero Rated</Badge>
              <Badge variant="default" size="sm">DS — Deemed Supply</Badge>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
