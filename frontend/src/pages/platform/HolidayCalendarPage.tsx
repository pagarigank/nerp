import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Plus, AlertCircle, CalendarDays, Trash2, Calculator } from 'lucide-react'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button } from '@components/ui/Button'
import { Input } from '@components/ui/Input'
import { Modal } from '@components/ui/Modal'
import { Badge } from '@components/ui/Badge'
import { getErrorMessage } from '@api/client'
import { getHolidayCalendar, createHolidayEntry, deleteHolidayEntry, advanceHolidayDate, companyId } from '@api/platform'
import type { HolidayCalendarEntry, CreateHolidayCalendarRequest } from '@/types/platform'

export function HolidayCalendarPage() {
  const qc = useQueryClient()
  const cid = companyId()
  const year = new Date().getFullYear()
  const [open, setOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [date, setDate] = useState('')
  const [description, setDescription] = useState('')
  const [isWorkingDay, setIsWorkingDay] = useState(false)

  const [calcFrom, setCalcFrom] = useState('')
  const [calcDays, setCalcDays] = useState('5')
  const [calcResult, setCalcResult] = useState<string | null>(null)

  const { data: rows = [], isLoading } = useQuery({
    queryKey: ['platform', 'holiday-calendar', cid, year],
    queryFn: () => getHolidayCalendar(cid, year),
  })

  const createMut = useMutation({
    mutationFn: (d: CreateHolidayCalendarRequest) => createHolidayEntry(d),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['platform', 'holiday-calendar', cid, year] })
      setOpen(false)
    },
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const deleteMut = useMutation({
    mutationFn: (id: string) => deleteHolidayEntry(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['platform', 'holiday-calendar', cid, year] }),
    onError: (e) => setFormError(getErrorMessage(e)),
  })
  const calcMut = useMutation({
    mutationFn: () => advanceHolidayDate(cid, calcFrom, Number(calcDays)),
    onSuccess: (r) => setCalcResult(r),
    onError: (e) => setFormError(getErrorMessage(e)),
  })

  const submit = () => {
    setFormError(null)
    if (!date || !description) {
      setFormError('Date and description are required')
      return
    }
    createMut.mutate({ companyId: cid, date, description, isWorkingDay })
  }

  return (
    <div className="space-y-6">
      {formError && (
        <div className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm" role="alert">
          <AlertCircle className="h-5 w-5" /> <span>{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Holiday Calendar"
          description={`Working days & holidays for ${year} (pay-date / delivery date math)`}
          action={
            <Button variant="primary" size="sm" onClick={() => { setFormError(null); setDate(''); setDescription(''); setIsWorkingDay(false); setOpen(true) }} leftIcon={<Plus className="h-4 w-4" />}>
              Add Date
            </Button>
          }
        />
        <CardContent>
          {isLoading ? (
            <p className="text-sm text-gray-500 py-8 text-center">Loading…</p>
          ) : rows.length === 0 ? (
            <p className="text-sm text-gray-500 py-8 text-center">No calendar entries yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500">Type</th>
                    <th className="px-3 py-2 font-medium text-gray-500 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {rows.map((h: HolidayCalendarEntry) => (
                    <tr key={h.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                      <td className="px-3 py-3 text-gray-900 dark:text-white">{new Date(h.date).toLocaleDateString()}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{h.description}</td>
                      <td className="px-3 py-3">
                        <Badge variant={h.isWorkingDay ? 'info' : 'neutral'} size="sm">
                          {h.isWorkingDay ? 'Working day' : 'Holiday'}
                        </Badge>
                      </td>
                      <td className="px-3 py-3 text-right">
                        <Button size="sm" variant="ghost" className="text-red-600" disabled={deleteMut.isPending} onClick={() => deleteMut.mutate(h.id)}>
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Business-Day Calculator" description="Advance a date by N working days (skips holidays)" />
        <CardContent>
          <div className="flex flex-wrap items-end gap-3">
            <Input type="date" value={calcFrom} onChange={(e) => setCalcFrom(e.target.value)} label="From" />
            <Input value={calcDays} onChange={(e) => setCalcDays(e.target.value)} label="Working days" className="w-32" />
            <Button variant="primary" onClick={() => calcMut.mutate()} isLoading={calcMut.isPending} leftIcon={<Calculator className="h-4 w-4" />}>
              Calculate
            </Button>
            {calcResult && (
              <Badge variant="success" size="md">
                <CalendarDays className="h-3.5 w-3.5 mr-1" /> {new Date(calcResult).toLocaleDateString()}
              </Badge>
            )}
          </div>
        </CardContent>
      </Card>

      <Modal isOpen={open} onClose={() => setOpen(false)} title="Add Calendar Date"
        footer={<><Button variant="secondary" onClick={() => setOpen(false)} disabled={createMut.isPending}>Cancel</Button><Button variant="primary" onClick={submit} isLoading={createMut.isPending}>Add</Button></>}>
        <div className="space-y-4">
          <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} label="Date" required />
          <Input value={description} onChange={(e) => setDescription(e.target.value)} label="Description" required />
          <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <input type="checkbox" checked={isWorkingDay} onChange={(e) => setIsWorkingDay(e.target.checked)} />
            Mark as working day (e.g. weekend made up for a holiday)
          </label>
        </div>
      </Modal>
    </div>
  )
}
