import { useQueryClient } from '@tanstack/react-query'
import { PayCodesTab } from './PayrollPage'

export function PayCodesPage() {
  const qc = useQueryClient()
  return <PayCodesTab qc={qc} />
}
