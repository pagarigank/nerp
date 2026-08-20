import { useQueryClient } from '@tanstack/react-query'
import { ManualChecksTab } from './PayrollPage'

export function ManualChecksPage() {
  const qc = useQueryClient()
  return <ManualChecksTab qc={qc} />
}
