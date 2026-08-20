import { useQueryClient } from '@tanstack/react-query'
import { TaxTab } from './PayrollPage'

export function TaxPage() {
  const qc = useQueryClient()
  return <TaxTab qc={qc} />
}
