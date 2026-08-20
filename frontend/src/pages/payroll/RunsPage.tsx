import { useQueryClient } from '@tanstack/react-query'
import { RunsTab } from './PayrollPage'

export function RunsPage() {
  const qc = useQueryClient()
  return <RunsTab qc={qc} />
}
