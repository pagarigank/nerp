import { useQueryClient } from '@tanstack/react-query'
import { PtoTab } from './PayrollPage'

export function PtoPage() {
  const qc = useQueryClient()
  return <PtoTab qc={qc} />
}
