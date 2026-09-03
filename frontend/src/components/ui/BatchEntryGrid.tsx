// <copyright file="BatchEntryGrid.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import {
  useState,
  useRef,
  useCallback,
  useMemo,
  useEffect,
  type KeyboardEvent,
  type ClipboardEvent,
} from 'react'
import { cn } from '@utils/helpers'
import { useDensity } from '@stores/densityStore'
import { Plus, Trash2, Copy, ClipboardPaste } from 'lucide-react'
import { Button, IconButton } from '@components/ui/Button'

/**
 * Column definition for a batch-entry grid cell.
 * Each column maps to an editable or display field on the row.
 */
export interface BatchGridColumn<T = any> {
  /** Unique key matching a property on the row object. */
  key: string
  /** Column header label. */
  header: string
  /** Whether this column is editable inline. Defaults to true. */
  editable?: boolean
  /** HTML input type for editing. Defaults to 'text'. */
  inputType?: 'text' | 'number' | 'date' | 'select'
  /** Options for select-type columns. */
  selectOptions?: { value: string; label: string }[]
  /** Placeholder text shown in the edit input. */
  placeholder?: string
  /** Right-align numeric columns. */
  align?: 'left' | 'center' | 'right'
  /** Render a custom display cell (read-only mode). */
  render?: (row: T, value: any) => React.ReactNode
  /** Render a custom edit cell (replaces the default input). */
  renderEdit?: (row: T, value: any, onChange: (val: any) => void) => React.ReactNode
  /** Width class, e.g. 'w-32', 'w-48', 'flex-1'. */
  width?: string
  /** Validate a value before committing. Return error string or null. */
  validate?: (value: any, row: T) => string | null
  /** Whether the column is read-only (display only). Overrides editable. */
  readOnly?: boolean
}

interface BatchEntryGridProps<T extends Record<string, any>> {
  /** Column definitions. */
  columns: BatchGridColumn<T>[]
  /** Row data. The parent component owns the state. */
  rows: T[]
  /** Called when any row is changed. Parent should update state. */
  onRowsChange: (rows: T[]) => void
  /** Create a new blank row (called when user clicks "Add Row" or presses Enter on last row). */
  createEmptyRow: () => T
  /** Unique key field name on each row. Defaults to 'id'. */
  rowKey?: string
  /** Whether the grid is read-only (no add/delete/edit). */
  readOnly?: boolean
  /** Show add row button. Defaults to true when not readOnly. */
  showAddRow?: boolean
  /** Show delete button on each row. Defaults to true when not readOnly. */
  showDeleteRow?: boolean
  /** Placeholder shown when there are no rows. */
  emptyMessage?: string
  /** Extra class on the wrapper. */
  className?: string
  /** Show row numbers. Defaults to true. */
  showRowNumbers?: boolean
  /** Whether to auto-add a new row when the last row starts being edited. */
  autoAddRow?: boolean
  /** Callback when a row is deleted. */
  onRowDelete?: (row: T, index: number) => void
  /** Footer content (e.g. totals row). */
  footer?: React.ReactNode
}

/**
 * A keyboard-navigable batch-entry grid for accounting data entry.
 *
 * Key behaviors:
 * - Tab / Shift+Tab: move between editable cells (left to right, wrapping to next row)
 * - Enter: move to the cell below (or create new row if on last row)
 * - Escape: cancel edit, revert to previous value
 * - Arrow keys: navigate cells when not editing
 * - Ctrl+C / Ctrl+V: copy/paste rows or cell ranges
 * - Delete: clear cell or delete row (with confirmation)
 */
export function BatchEntryGrid<T extends Record<string, any>>({
  columns,
  rows,
  onRowsChange,
  createEmptyRow,
  rowKey = 'id',
  readOnly = false,
  showAddRow = !readOnly,
  showDeleteRow = !readOnly,
  emptyMessage = 'No lines yet. Click "Add Row" to start.',
  className,
  showRowNumbers = true,
  autoAddRow = true,
  onRowDelete,
  footer,
}: BatchEntryGridProps<T>) {
  const density = useDensity()
  const [editingCell, setEditingCell] = useState<{ row: number; col: number } | null>(null)
  const [editValue, setEditValue] = useState<any>(null)
  const [selectedRow, setSelectedRow] = useState<number | null>(null)
  const [clipboard, setClipboard] = useState<any[] | null>(null)
  const [cellErrors, setCellErrors] = useState<Record<string, string>>({})
  const gridRef = useRef<HTMLDivElement>(null)
  const inputRefs = useRef<Map<string, HTMLInputElement | HTMLSelectElement>>(new Map())

  const editableColumns = useMemo(
    () => columns.filter(c => c.editable !== false && !c.readOnly),
    [columns]
  )

  // Get the column index in the editable-only list for a given column index
  const getEditableColIndex = useCallback(
    (colIdx: number) => {
      let count = 0
      for (let i = 0; i < colIdx; i++) {
        if (columns[i].editable !== false && !columns[i].readOnly) count++
      }
      return count
    },
    [columns]
  )

  const focusCell = useCallback(
    (rowIdx: number, editableColIdx: number) => {
      const editableCols = columns
        .map((c, i) => ({ c, i }))
        .filter(({ c }) => c.editable !== false && !c.readOnly)
      if (editableColIdx < 0 || editableColIdx >= editableCols.length) return
      const colIdx = editableCols[editableColIdx].i
      const key = `${rowIdx}-${colIdx}`
      setTimeout(() => {
        const el = inputRefs.current.get(key)
        if (el) {
          el.focus()
          if (el instanceof HTMLInputElement) el.select()
        }
      }, 0)
    },
    [columns]
  )

  const startEditing = useCallback(
    (rowIdx: number, colIdx: number) => {
      if (readOnly) return
      const col = columns[colIdx]
      if (col.editable === false || col.readOnly) return
      setEditingCell({ row: rowIdx, col: colIdx })
      setEditValue(rows[rowIdx]?.[col.key] ?? '')
    },
    [readOnly, columns, rows]
  )

  const commitEdit = useCallback(() => {
    if (!editingCell) return
    const { row: rowIdx, col: colIdx } = editingCell
    const col = columns[colIdx]
    const row = rows[rowIdx]
    if (!row) return

    // Validate
    if (col.validate) {
      const error = col.validate(editValue, row)
      if (error) {
        setCellErrors(prev => ({ ...prev, [`${rowIdx}-${colIdx}`]: error }))
        return
      }
    }

    // Clear error
    setCellErrors(prev => {
      const next = { ...prev }
      delete next[`${rowIdx}-${colIdx}`]
      return next
    })

    // Commit
    const newRows = rows.map((r, i) => (i === rowIdx ? { ...r, [col.key]: editValue } : r))
    onRowsChange(newRows)
    setEditingCell(null)
    setEditValue(null)
  }, [editingCell, columns, rows, onRowsChange, editValue])

  const cancelEdit = useCallback(() => {
    setEditingCell(null)
    setEditValue(null)
    setCellErrors({})
  }, [])

  const addRow = useCallback(() => {
    const newRow = createEmptyRow()
    onRowsChange([...rows, newRow])
    setSelectedRow(rows.length)
    // Focus the first editable cell of the new row
    if (editableColumns.length > 0) {
      setTimeout(() => focusCell(rows.length, 0), 50)
    }
  }, [rows, onRowsChange, createEmptyRow, editableColumns.length, focusCell])

  const deleteRow = useCallback(
    (rowIdx: number) => {
      const row = rows[rowIdx]
      if (onRowDelete) {
        onRowDelete(row, rowIdx)
      }
      const newRows = rows.filter((_, i) => i !== rowIdx)
      onRowsChange(newRows)
      setEditingCell(null)
      setSelectedRow(null)
    },
    [rows, onRowsChange, onRowDelete]
  )

  const duplicateRow = useCallback(
    (rowIdx: number) => {
      const source = rows[rowIdx]
      if (!source) return
      const newRow = { ...source }
      // Assign a new id if present
      if (newRow[rowKey]) newRow[rowKey] = crypto.randomUUID?.() ?? `${Date.now()}`
      const newRows = [...rows.slice(0, rowIdx + 1), newRow, ...rows.slice(rowIdx + 1)]
      onRowsChange(newRows)
    },
    [rows, onRowsChange, rowKey]
  )

  const copyRows = useCallback(
    (rowIndices: number[]) => {
      const copied = rowIndices.map(i => ({ ...rows[i] }))
      setClipboard(copied)
    },
    [rows]
  )

  const pasteRows = useCallback(
    (afterRowIdx: number) => {
      if (!clipboard || clipboard.length === 0) return
      const newRows = clipboard.map(r => {
        const row = { ...r }
        if (row[rowKey]) row[rowKey] = crypto.randomUUID?.() ?? `${Date.now()}`
        return row
      })
      const insertIdx = afterRowIdx + 1
      const result = [...rows.slice(0, insertIdx), ...newRows, ...rows.slice(insertIdx)]
      onRowsChange(result)
    },
    [clipboard, rows, onRowsChange, rowKey]
  )

  // Keyboard handler for the grid
  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLDivElement>) => {
      if (!editingCell) {
        // Navigate with arrows when not editing
        if (selectedRow !== null) {
          if (e.key === 'ArrowDown' && selectedRow < rows.length - 1) {
            e.preventDefault()
            setSelectedRow(selectedRow + 1)
          } else if (e.key === 'ArrowUp' && selectedRow > 0) {
            e.preventDefault()
            setSelectedRow(selectedRow - 1)
          } else if (e.key === 'Enter' || e.key === 'F2') {
            e.preventDefault()
            // Start editing the first editable cell of the selected row
            const editColIdx = columns.findIndex(c => c.editable !== false && !c.readOnly)
            if (editColIdx >= 0) startEditing(selectedRow, editColIdx)
          } else if (e.key === 'Delete' && showDeleteRow) {
            e.preventDefault()
            deleteRow(selectedRow)
          }
        }
        return
      }

      const { row: rowIdx, col: colIdx } = editingCell

      // Ctrl shortcuts
      if ((e.ctrlKey || e.metaKey) && !e.altKey) {
        if (e.key === 'c') {
          e.preventDefault()
          copyRows([rowIdx])
          return
        }
        if (e.key === 'v') {
          e.preventDefault()
          commitEdit()
          pasteRows(rowIdx)
          return
        }
        if (e.key === 'z') {
          e.preventDefault()
          cancelEdit()
          return
        }
      }

      switch (e.key) {
        case 'Tab': {
          e.preventDefault()
          commitEdit()
          const editCols = columns
            .map((c, i) => ({ c, i }))
            .filter(({ c }) => c.editable !== false && !c.readOnly)
          const currentEditableIdx = editCols.findIndex(ec => ec.i === colIdx)
          const editableColCount = editCols.length

          if (e.shiftKey) {
            // Shift+Tab: go left (or up to last column of previous row)
            if (currentEditableIdx > 0) {
              focusCell(rowIdx, currentEditableIdx - 1)
            } else if (rowIdx > 0) {
              focusCell(rowIdx - 1, editableColCount - 1)
            }
          } else {
            // Tab: go right (or down to first column of next row)
            if (currentEditableIdx < editableColCount - 1) {
              focusCell(rowIdx, currentEditableIdx + 1)
            } else if (rowIdx < rows.length - 1) {
              focusCell(rowIdx + 1, 0)
            } else if (autoAddRow) {
              // Last cell of last row — add a new row
              addRow()
            }
          }
          break
        }
        case 'Enter': {
          e.preventDefault()
          commitEdit()
          if (rowIdx < rows.length - 1) {
            // Move down to same column
            const editCols = columns
              .map((c, i) => ({ c, i }))
              .filter(({ c }) => c.editable !== false && !c.readOnly)
            const currentEditableIdx = editCols.findIndex(ec => ec.i === colIdx)
            focusCell(rowIdx + 1, currentEditableIdx)
          } else if (autoAddRow) {
            addRow()
          }
          break
        }
        case 'Escape': {
          e.preventDefault()
          cancelEdit()
          break
        }
        case 'ArrowDown': {
          e.preventDefault()
          commitEdit()
          if (rowIdx < rows.length - 1) {
            const editCols = columns
              .map((c, i) => ({ c, i }))
              .filter(({ c }) => c.editable !== false && !c.readOnly)
            const currentEditableIdx = editCols.findIndex(ec => ec.i === colIdx)
            focusCell(rowIdx + 1, currentEditableIdx)
          }
          break
        }
        case 'ArrowUp': {
          e.preventDefault()
          commitEdit()
          if (rowIdx > 0) {
            const editCols = columns
              .map((c, i) => ({ c, i }))
              .filter(({ c }) => c.editable !== false && !c.readOnly)
            const currentEditableIdx = editCols.findIndex(ec => ec.i === colIdx)
            focusCell(rowIdx - 1, currentEditableIdx)
          }
          break
        }
      }
    },
    [
      editingCell,
      selectedRow,
      rows,
      columns,
      startEditing,
      commitEdit,
      cancelEdit,
      focusCell,
      deleteRow,
      addRow,
      copyRows,
      pasteRows,
      autoAddRow,
      showDeleteRow,
    ]
  )

  // Handle double-click on a cell to start editing
  const handleCellDoubleClick = useCallback(
    (rowIdx: number, colIdx: number) => {
      setSelectedRow(rowIdx)
      startEditing(rowIdx, colIdx)
    },
    [startEditing]
  )

  // Handle click on row to select it
  const handleRowClick = useCallback(
    (rowIdx: number) => {
      if (editingCell?.row !== rowIdx) {
        commitEdit()
      }
      setSelectedRow(rowIdx)
    },
    [editingCell, commitEdit]
  )

  // Render an editable cell
  const renderEditableCell = (row: T, rowIdx: number, col: BatchGridColumn<T>, colIdx: number) => {
    const isEditing = editingCell?.row === rowIdx && editingCell?.col === colIdx
    const cellKey = `${rowIdx}-${colIdx}`
    const error = cellErrors[cellKey]
    const value = row[col.key]

    if (isEditing) {
      if (col.renderEdit) {
        return col.renderEdit(row, editValue, (val: any) => {
          setEditValue(val)
          // Clear error on change
          setCellErrors(prev => {
            const next = { ...prev }
            delete next[cellKey]
            return next
          })
        })
      }

      if (col.inputType === 'select' && col.selectOptions) {
        return (
          <select
            ref={el => {
              if (el) inputRefs.current.set(cellKey, el)
            }}
            value={editValue ?? ''}
            onChange={e => {
              setEditValue(e.target.value)
              setCellErrors(prev => {
                const next = { ...prev }
                delete next[cellKey]
                return next
              })
            }}
            onBlur={commitEdit}
            onKeyDown={e => {
              if (e.key === 'Enter') {
                e.preventDefault()
                commitEdit()
              }
              if (e.key === 'Escape') {
                e.preventDefault()
                cancelEdit()
              }
            }}
            className={cn(
              'w-full bg-white dark:bg-gray-800 border border-primary-500 rounded px-2 py-1 text-sm',
              'focus:outline-none focus:ring-2 focus:ring-primary-500/30',
              error && 'border-red-500 focus:ring-red-500/30'
            )}
          >
            <option value="">—</option>
            {col.selectOptions.map(opt => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        )
      }

      return (
        <input
          ref={el => {
            if (el) inputRefs.current.set(cellKey, el)
          }}
          type={col.inputType ?? 'text'}
          value={editValue ?? ''}
          placeholder={col.placeholder}
          onChange={e => {
            const val = col.inputType === 'number' ? (e.target.value === '' ? '' : Number(e.target.value)) : e.target.value
            setEditValue(val)
            setCellErrors(prev => {
              const next = { ...prev }
              delete next[cellKey]
              return next
            })
          }}
          onBlur={commitEdit}
          onKeyDown={e => {
            if (e.key === 'Enter') {
              e.preventDefault()
              commitEdit()
            }
            if (e.key === 'Escape') {
              e.preventDefault()
              cancelEdit()
            }
          }}
          className={cn(
            'w-full bg-white dark:bg-gray-800 border border-primary-500 rounded px-2 py-1 text-sm',
            'focus:outline-none focus:ring-2 focus:ring-primary-500/30 tabular-nums',
            col.align === 'right' && 'text-right',
            error && 'border-red-500 focus:ring-red-500/30'
          )}
          step={col.inputType === 'number' ? '0.01' : undefined}
        />
      )
    }

    // Read-only display
    return (
      <div
        className={cn(
          'px-2 py-1.5 text-sm min-h-[32px] flex items-center cursor-pointer rounded',
          'hover:bg-gray-100 dark:hover:bg-gray-700/50 transition-colors',
          col.align === 'right' && 'text-right justify-end',
          col.align === 'center' && 'text-center justify-center',
          error && 'bg-red-50 dark:bg-red-900/20'
        )}
        title={error ?? undefined}
      >
        {col.render ? (
          col.render(row, value)
        ) : (
          <span className={cn('truncate', value == null || value === '' ? 'text-gray-400 dark:text-gray-500' : '')}>
            {value == null || value === '' ? (col.placeholder ?? '—') : String(value)}
          </span>
        )}
      </div>
    )
  }

  // Render a read-only cell
  const renderReadOnlyCell = (row: T, col: BatchGridColumn<T>) => {
    const value = row[col.key]
    return (
      <div
        className={cn(
          'px-2 py-1.5 text-sm min-h-[32px] flex items-center',
          col.align === 'right' && 'text-right justify-end',
          col.align === 'center' && 'text-center justify-center'
        )}
      >
        {col.render ? (
          col.render(row, value)
        ) : (
          <span className="truncate">{value == null || value === '' ? '—' : String(value)}</span>
        )}
      </div>
    )
  }

  return (
    <div
      ref={gridRef}
      className={cn(
        'rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden',
        density === 'compact' ? 'density-compact' : 'density-comfortable',
        className
      )}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="grid"
      aria-label="Batch entry grid"
    >
      {/* Header */}
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
              {showRowNumbers && (
                <th className="px-2 py-2 w-10 text-center text-xs font-medium text-gray-500 dark:text-gray-400">#</th>
              )}
              {columns.map(col => (
                <th
                  key={col.key}
                  className={cn(
                    'px-2 py-2 text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider border-b border-gray-200 dark:border-gray-700',
                    col.align === 'right' && 'text-right',
                    col.align === 'center' && 'text-center',
                    col.width
                  )}
                >
                  {col.header}
                </th>
              ))}
              {showDeleteRow && <th className="px-2 py-2 w-10" />}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100 dark:divide-gray-800">
            {rows.length === 0 ? (
              <tr>
                <td
                  colSpan={columns.length + (showRowNumbers ? 1 : 0) + (showDeleteRow ? 1 : 0)}
                  className="px-4 py-12 text-center text-gray-500 dark:text-gray-400"
                >
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              rows.map((row, rowIdx) => {
                const isSelected = selectedRow === rowIdx
                return (
                  <tr
                    key={row[rowKey] ?? rowIdx}
                    onClick={() => handleRowClick(rowIdx)}
                    onDoubleClick={() => {
                      const editCol = columns.findIndex(c => c.editable !== false && !c.readOnly)
                      if (editCol >= 0) handleCellDoubleClick(rowIdx, editCol)
                    }}
                    className={cn(
                      'transition-colors cursor-pointer',
                      isSelected
                        ? 'bg-primary-50 dark:bg-primary-900/20 ring-1 ring-inset ring-primary-300 dark:ring-primary-700'
                        : 'hover:bg-gray-50 dark:hover:bg-gray-800/50'
                    )}
                  >
                    {showRowNumbers && (
                      <td className="px-2 py-1 text-center text-xs text-gray-400 dark:text-gray-500 tabular-nums">
                        {rowIdx + 1}
                      </td>
                    )}
                    {columns.map((col, colIdx) => (
                      <td
                        key={col.key}
                        className={cn(
                          'border-b border-r border-gray-100 dark:border-gray-800',
                          isSelected && 'border-primary-200 dark:border-primary-800'
                        )}
                        onDoubleClick={() => handleCellDoubleClick(rowIdx, colIdx)}
                      >
                        {readOnly || col.readOnly || col.editable === false
                          ? renderReadOnlyCell(row, col)
                          : renderEditableCell(row, rowIdx, col, colIdx)}
                      </td>
                    ))}
                    {showDeleteRow && (
                      <td className="px-1 py-1 text-center border-b border-gray-100 dark:border-gray-800">
                        <IconButton
                          size="sm"
                          variant="ghost"
                          className="text-gray-400 hover:text-red-600 dark:hover:text-red-400"
                          aria-label={`Delete row ${rowIdx + 1}`}
                          onClick={e => {
                            e.stopPropagation()
                            deleteRow(rowIdx)
                          }}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </IconButton>
                      </td>
                    )}
                  </tr>
                )
              })
            )}
          </tbody>
          {footer && (
            <tfoot>
              <tr className="border-t-2 border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/80">
                {showRowNumbers && <td />}
                {footer}
                {showDeleteRow && <td />}
              </tr>
            </tfoot>
          )}
        </table>
      </div>

      {/* Toolbar */}
      {!readOnly && showAddRow && (
        <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800/50 border-t border-gray-200 dark:border-gray-700">
          <Button
            variant="ghost"
            size="sm"
            onClick={addRow}
            leftIcon={<Plus className="h-3.5 w-3.5" />}
            className="text-xs"
          >
            Add Row
          </Button>
          {clipboard && clipboard.length > 0 && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => pasteRows(selectedRow ?? rows.length - 1)}
              leftIcon={<ClipboardPaste className="h-3.5 w-3.5" />}
              className="text-xs"
            >
              Paste ({clipboard.length} row{clipboard.length > 1 ? 's' : ''})
            </Button>
          )}
          <div className="flex-1" />
          <span className="text-xs text-gray-400 dark:text-gray-500">
            Tab → next cell · Enter ↓ next row · Esc cancel · Ctrl+C/V copy/paste
          </span>
        </div>
      )}
    </div>
  )
}
