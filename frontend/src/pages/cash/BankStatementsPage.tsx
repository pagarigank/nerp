import { useMemo, useRef, useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Search, Trash2, AlertCircle, Upload, Eye, FileCheck2, CloudDownload } from 'lucide-react'
import { formatCurrency, formatDate } from '@utils/helpers'
import { Card, CardHeader, CardContent } from '@components/ui/Card'
import { Button, IconButton } from '@components/ui/Button'
import { Input, Select, Textarea } from '@components/ui/Input'
import { Combobox } from '@components/ui/Combobox'
import { Modal, ConfirmDialog, Drawer } from '@components/ui/Modal'
import { SkeletonTable } from '@components/ui/LoadingSpinner'
import { MapStatusBadge } from '@components/ui/MapStatusBadge'
import { getErrorMessage } from '@api/client'
import {
  getBankStatements,
  getBankStatement,
  getBankAccounts,
  importBankStatement,
  runBankStatementDownload,
  validateBankStatement,
  deleteBankStatement,
  bankStatementFormats,
  DEMO_COMPANY_ID,
} from '@api/cash'
import type {
  CashBankStatement,
  CashBankStatementDetail,
  ImportStatementResponse,
  StatementDownloadReport,
} from '@/types/cash'
import { statementStatusMap, lineStatusMap } from './statusMaps'

const formatOptions = [
  { value: '0', label: 'CSV' },
  { value: '1', label: 'OFX' },
  { value: '2', label: 'BAI2' },
  { value: '3', label: 'QBO' },
]

const importSchema = z.object({
  bankAccountId: z.string().min(1, 'Select a bank account'),
  statementNumber: z.string().trim().min(1, 'Statement number is required'),
  statementDate: z.string().min(1, 'Statement date is required'),
  format: z.string().optional(),
  fileContent: z.string().min(1, 'Paste or upload the statement content'),
})

type ImportForm = z.infer<typeof importSchema>

function fieldError(message: string | undefined): { error?: string } {
  return message ? { error: message } : {}
}

export function BankStatementsPage() {
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [isModalOpen, setIsModalOpen] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [importResult, setImportResult] = useState<ImportStatementResponse | null>(null)
  const [downloadResult, setDownloadResult] = useState<StatementDownloadReport | null>(null)
  const [statementToDelete, setStatementToDelete] = useState<CashBankStatement | null>(null)
  const [detailStatement, setDetailStatement] = useState<CashBankStatement | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<ImportForm>({
    resolver: zodResolver(importSchema),
    defaultValues: {
      bankAccountId: '',
      statementNumber: '',
      statementDate: new Date().toISOString().slice(0, 10),
      format: '',
      fileContent: '',
    },
  })

  const { data: statements = [], isLoading } = useQuery({
    queryKey: ['cash', 'bankStatements'],
    queryFn: () => getBankStatements(),
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['cash', 'bankAccounts'],
    queryFn: () => getBankAccounts(),
  })

  const accountOptions = useMemo(
    () => accounts.map(a => ({ value: a.id, label: `${a.accountCode} - ${a.accountName}` })),
    [accounts]
  )

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['cash', 'bankStatements'] })
  }

  const importMutation = useMutation({
    mutationFn: importBankStatement,
    onSuccess: result => {
      invalidate()
      setImportResult(result)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const downloadMutation = useMutation({
    mutationFn: runBankStatementDownload,
    onSuccess: result => {
      invalidate()
      setDownloadResult(result)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const validateMutation = useMutation({
    mutationFn: validateBankStatement,
    onSuccess: () => invalidate(),
    onError: err => setFormError(getErrorMessage(err)),
  })

  const deleteMutation = useMutation({
    mutationFn: deleteBankStatement,
    onSuccess: () => {
      invalidate()
      setStatementToDelete(null)
    },
    onError: err => setFormError(getErrorMessage(err)),
  })

  const { data: detail } = useQuery({
    queryKey: ['cash', 'bankStatements', detailStatement?.id],
    queryFn: () => getBankStatement(detailStatement!.id),
    enabled: !!detailStatement,
  })

  const openImportForm = () => {
    setFormError(null)
    setImportResult(null)
    reset({
      bankAccountId: '',
      statementNumber: `STMT-2026-${String(statements.length + 1).padStart(3, '0')}`,
      statementDate: new Date().toISOString().slice(0, 10),
      format: '',
      fileContent: '',
    })
    setIsModalOpen(true)
  }

  const closeForm = () => {
    setIsModalOpen(false)
    setFormError(null)
    setImportResult(null)
  }

  const onSubmit = (data: ImportForm) => {
    setFormError(null)
    setImportResult(null)
    importMutation.mutate({
      companyId: DEMO_COMPANY_ID,
      bankAccountId: data.bankAccountId,
      statementNumber: data.statementNumber,
      statementDate: new Date(data.statementDate).toISOString(),
      fileContent: data.fileContent,
      format: data.format ? Number(data.format) : null,
    })
  }

  const handleFile = (file: File) => {
    const reader = new FileReader()
    reader.onload = () => setValue('fileContent', String(reader.result ?? ''), { shouldValidate: true })
    reader.readAsText(file)
  }

  const filteredStatements = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return statements
    return statements.filter(
      s => s.statementNumber.toLowerCase().includes(q) || s.format.toLowerCase().includes(q)
    )
  }, [statements, search])

  return (
    <div className="space-y-6">
      {formError && (
        <div
          className="flex items-center gap-2 p-4 rounded-lg bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/20 dark:border-red-800 dark:text-red-300"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0" aria-hidden="true" />
          <span className="text-sm">{formError}</span>
        </div>
      )}

      <Card>
        <CardHeader
          title="Bank Statements"
          description={`${statements.length} statement(s) on file`}
          action={
            <div className="flex items-center gap-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => downloadMutation.mutate()}
                disabled={downloadMutation.isPending}
                leftIcon={<CloudDownload className="h-4 w-4" />}
              >
                {downloadMutation.isPending ? 'Downloading...' : 'Download from Bank'}
              </Button>
              <Button variant="primary" size="sm" onClick={openImportForm} leftIcon={<Upload className="h-4 w-4" />}>
                Import Statement
              </Button>
            </div>
          }
        />
        <CardContent>
          {downloadResult && (
            <div className="mb-4 rounded-lg border border-blue-200 bg-blue-50 dark:border-blue-800 dark:bg-blue-900/20 p-3 text-sm">
              <p className="font-medium text-blue-800 dark:text-blue-300">
                Download complete: {downloadResult.imported} imported, {downloadResult.skippedExisting} skipped (already on file),
                {' '}{downloadResult.feedsProcessed} feed(s) processed.
              </p>
              {downloadResult.errors.length > 0 && (
                <ul className="mt-1 list-disc list-inside text-red-600 dark:text-red-400">
                  {downloadResult.errors.map(e => (
                    <li key={e}>{e}</li>
                  ))}
                </ul>
              )}
            </div>
          )}
          <div className="mb-4 max-w-md">
            <Input
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder="Search by statement number..."
              leftIcon={<Search className="h-4 w-4" aria-hidden="true" />}
              aria-label="Search bank statements"
            />
          </div>

          {isLoading ? (
            <SkeletonTable columns={7} />
          ) : filteredStatements.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-8 text-center">
              {search
                ? 'No statements match your search.'
                : 'No bank statements yet. Import a statement to begin reconciliation.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Statement #</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Bank Account</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Format</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Lines</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Ending Balance</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {filteredStatements.map(statement => (
                    <tr key={statement.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-3 font-mono text-xs font-medium text-gray-900 dark:text-white">
                        {statement.statementNumber}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">{formatDate(statement.statementDate)}</td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {accounts.find(a => a.id === statement.bankAccountId)?.accountName ?? '—'}
                      </td>
                      <td className="px-3 py-3 text-gray-700 dark:text-gray-300">
                        {bankStatementFormats[statement.format] ?? statement.format}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {statement.lineCount}
                      </td>
                      <td className="px-3 py-3 text-right font-tabular tabular-nums text-gray-900 dark:text-white">
                        {formatCurrency(statement.endingBalance)}
                      </td>
                      <td className="px-3 py-3">
                        <MapStatusBadge value={statement.status} mapping={statementStatusMap} />
                      </td>
                      <td className="px-3 py-3">
                        <div className="flex items-center justify-end gap-1">
                          {statement.status === 'Imported' && (
                            <Button
                              variant="outline"
                              size="sm"
                              leftIcon={<FileCheck2 className="h-4 w-4" />}
                              onClick={() => validateMutation.mutate(statement.id)}
                              isLoading={validateMutation.isPending && validateMutation.variables === statement.id}
                            >
                              Validate
                            </Button>
                          )}
                          <IconButton
                            size="sm"
                            variant="ghost"
                            aria-label={`View ${statement.statementNumber}`}
                            onClick={() => setDetailStatement(statement)}
                          >
                            <Eye className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                          <IconButton
                            size="sm"
                            variant="ghost"
                            className="text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20"
                            aria-label={`Delete ${statement.statementNumber}`}
                            onClick={() => setStatementToDelete(statement)}
                          >
                            <Trash2 className="h-4 w-4" aria-hidden="true" />
                          </IconButton>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      <Modal
        isOpen={isModalOpen}
        onClose={closeForm}
        title="Import Bank Statement"
        description="Paste statement content or upload a file. Format is auto-detected unless specified."
        size="lg"
        footer={
          <>
            <Button variant="secondary" onClick={closeForm} disabled={importMutation.isPending}>
              Cancel
            </Button>
            <Button variant="primary" onClick={handleSubmit(onSubmit)} isLoading={importMutation.isPending}>
              Import
            </Button>
          </>
        }
      >
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5" noValidate>
          {importResult && (
            <div className="rounded-lg bg-emerald-50 border border-emerald-200 p-4 dark:bg-emerald-900/20 dark:border-emerald-800">
              <p className="text-sm font-medium text-emerald-800 dark:text-emerald-300">
                Imported {importResult.lineCount} line(s) as {bankStatementFormats[importResult.format] ?? importResult.format}.
              </p>
              {(importResult.beginningBalance != null || importResult.endingBalance != null) && (
                <p className="mt-1 text-sm text-emerald-700 dark:text-emerald-400">
                  Beginning: {importResult.beginningBalance != null ? formatCurrency(importResult.beginningBalance) : '—'} ·{' '}
                  Ending: {importResult.endingBalance != null ? formatCurrency(importResult.endingBalance) : '—'}
                </p>
              )}
              {importResult.warnings.length > 0 && (
                <ul className="mt-2 list-disc pl-5 text-sm text-amber-700 dark:text-amber-400">
                  {importResult.warnings.map((warning, i) => (
                    <li key={`${warning}-${i}`}>{warning}</li>
                  ))}
                </ul>
              )}
            </div>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="sm:col-span-2">
              <Combobox
                label="Bank Account"
                placeholder="Select bank account..."
                options={accountOptions}
                value={watch('bankAccountId')}
                onChange={value => setValue('bankAccountId', value, { shouldValidate: true })}
                required
              />
            </div>
            <Input
              {...register('statementNumber')}
              label="Statement Number"
              {...fieldError(errors.statementNumber?.message)}
              required
            />
            <Input
              {...register('statementDate')}
              type="date"
              label="Statement Date"
              {...fieldError(errors.statementDate?.message)}
              required
            />
            <Select
              {...register('format')}
              label="Format (auto-detect if blank)"
              options={formatOptions}
              placeholder="Auto-detect"
              {...fieldError(errors.format?.message)}
            />
            <div className="flex items-end">
              <input
                ref={fileInputRef}
                type="file"
                accept=".csv,.ofx,.qfx,.bai,.bai2,.qbo,.txt"
                className="hidden"
                onChange={e => {
                  const file = e.target.files?.[0]
                  if (file) handleFile(file)
                  e.target.value = ''
                }}
                aria-label="Upload statement file"
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => fileInputRef.current?.click()}
                leftIcon={<Upload className="h-4 w-4" />}
              >
                Upload File
              </Button>
            </div>
            <div className="sm:col-span-2">
              <Textarea
                {...register('fileContent')}
                label="Statement Content"
                placeholder={'Paste bank statement content here (CSV, OFX, BAI2, or QBO)...\n\nCSV example:\ndate,description,amount,reference\n2026-07-01,PAYMENT,1500.00,CHK-1001'}
                className="min-h-[180px] font-mono text-xs"
                {...fieldError(errors.fileContent?.message)}
                required
              />
            </div>
          </div>
        </form>
      </Modal>

      <ConfirmDialog
        isOpen={!!statementToDelete}
        onClose={() => setStatementToDelete(null)}
        onConfirm={() => statementToDelete && deleteMutation.mutate(statementToDelete.id)}
        title="Delete Bank Statement"
        message={
          statementToDelete
            ? `Are you sure you want to delete statement "${statementToDelete.statementNumber}"?`
            : ''
        }
        confirmText="Delete"
        variant="danger"
        isLoading={deleteMutation.isPending}
      />

      <StatementDetailDrawer
        statement={detailStatement}
        detail={detail}
        onClose={() => setDetailStatement(null)}
      />
    </div>
  )
}

function StatementDetailDrawer({
  statement,
  detail,
  onClose,
}: {
  statement: CashBankStatement | null
  detail: CashBankStatementDetail | undefined
  onClose: () => void
}) {
  return (
    <Drawer isOpen={!!statement} onClose={onClose} title={statement ? `Statement ${statement.statementNumber}` : ''} size="lg">
      {!statement ? null : (
        <div className="space-y-5">
          <div className="rounded-lg bg-gray-50 dark:bg-gray-900/50 border border-gray-200 dark:border-gray-700 p-4 space-y-2">
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-500 dark:text-gray-400">Statement Date</span>
              <span className="font-medium text-gray-900 dark:text-white">{formatDate(statement.statementDate)}</span>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-500 dark:text-gray-400">Beginning Balance</span>
              <span className="font-medium font-tabular tabular-nums text-gray-900 dark:text-white">
                {formatCurrency(statement.beginningBalance)}
              </span>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-500 dark:text-gray-400">Ending Balance</span>
              <span className="font-semibold font-tabular tabular-nums text-gray-900 dark:text-white">
                {formatCurrency(statement.endingBalance)}
              </span>
            </div>
            <div className="flex items-center justify-between text-sm">
              <span className="text-gray-500 dark:text-gray-400">Lines</span>
              <span className="font-medium text-gray-900 dark:text-white">{statement.lineCount}</span>
            </div>
          </div>

          {!detail ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-6 text-center">Loading lines...</p>
          ) : detail.lines.length === 0 ? (
            <p className="text-sm text-gray-500 dark:text-gray-400 py-6 text-center">This statement has no lines.</p>
          ) : (
            <div className="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-lg">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700 text-left bg-gray-50 dark:bg-gray-900/50">
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Date</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Description</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Reference</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400 text-right">Amount</th>
                    <th className="px-3 py-2 font-medium text-gray-500 dark:text-gray-400">Status</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100 dark:divide-gray-700/60">
                  {detail.lines.map(line => (
                    <tr key={line.id} className="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
                      <td className="px-3 py-2.5 text-gray-700 dark:text-gray-300">{formatDate(line.transactionDate)}</td>
                      <td className="px-3 py-2.5 text-gray-900 dark:text-white">{line.description}</td>
                      <td className="px-3 py-2.5 font-mono text-xs text-gray-600 dark:text-gray-400">
                        {line.checkNumber ?? line.referenceNumber ?? '—'}
                      </td>
                      <td
                        className={`px-3 py-2.5 text-right font-tabular tabular-nums ${
                          line.amount < 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-900 dark:text-white'
                        }`}
                      >
                        {formatCurrency(line.amount)}
                      </td>
                      <td className="px-3 py-2.5">
                        <MapStatusBadge value={line.status} mapping={lineStatusMap} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </Drawer>
  )
}
