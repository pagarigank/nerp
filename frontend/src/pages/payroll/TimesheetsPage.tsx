import { useQueryClient } from '@tanstack/react-query'
import { TimesheetsTab } from './PayrollPage'

export function TimesheetsPage() {
  const qc = useQueryClient()
  return <TimesheetsTab qc={qc} />
}
