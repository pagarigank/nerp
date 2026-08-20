import { useQueryClient } from '@tanstack/react-query'
import { EmployeesTab } from './PayrollPage'

export function EmployeesPage() {
  const qc = useQueryClient()
  return <EmployeesTab qc={qc} />
}
