import { useQueryClient } from '@tanstack/react-query'
import { GarnishmentsTab } from './PayrollPage'

export function GarnishmentsPage() {
  const qc = useQueryClient()
  return <GarnishmentsTab qc={qc} />
}
