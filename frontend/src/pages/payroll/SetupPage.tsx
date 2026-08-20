import { useQueryClient } from '@tanstack/react-query'
import { SetupTab } from './PayrollPage'

export function SetupPage() {
  const qc = useQueryClient()
  return <SetupTab qc={qc} />
}
