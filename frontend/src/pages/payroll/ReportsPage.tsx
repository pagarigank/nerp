import { useQueryClient } from '@tanstack/react-query'
import { ReportsTab } from './PayrollPage'

export function ReportsPage() {
  const qc = useQueryClient()
  return <ReportsTab qc={qc} />
}
