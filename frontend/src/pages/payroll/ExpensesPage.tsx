import { useQueryClient } from '@tanstack/react-query'
import { ExpensesTab } from './PayrollPage'

export function ExpensesPage() {
  const qc = useQueryClient()
  return <ExpensesTab qc={qc} />
}
