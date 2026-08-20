import { useQueryClient } from '@tanstack/react-query'
import { DeductionsTab } from './PayrollPage'

export function DeductionsPage() {
  const qc = useQueryClient()
  return <DeductionsTab qc={qc} />
}
