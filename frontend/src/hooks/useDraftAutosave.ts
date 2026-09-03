// <copyright file="useDraftAutosave.ts" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useEffect, useRef, useCallback, useState } from 'react'

interface DraftAutosaveOptions<T> {
  /** Unique key for this draft (e.g. batch ID or form identifier). */
  draftKey: string
  /** The data to auto-save. */
  data: T
  /** How often to save in milliseconds. Default: 30000 (30s). */
  intervalMs?: number
  /** Whether auto-save is enabled. Default: true. */
  enabled?: boolean
  /** Callback when draft is saved. */
  onSaved?: (timestamp: number) => void
}

interface DraftAutosaveResult<T> {
  /** The last saved timestamp (epoch ms). */
  lastSavedAt: number | null
  /** Human-readable "X minutes ago" string. */
  lastSavedLabel: string
  /** Whether a save is in progress. */
  isSaving: boolean
  /** Manually trigger a save. */
  saveNow: () => void
  /** Load the draft from localStorage. Returns null if none exists. */
  loadDraft: () => T | null
  /** Delete the draft from localStorage. */
  deleteDraft: () => void
  /** Whether a draft exists on disk. */
  hasDraft: boolean
}

const STORAGE_PREFIX = 'erp-draft-'

/**
 * Auto-saves form data to localStorage at regular intervals.
 * Useful for multi-line batch entry forms where data loss on refresh is costly.
 *
 * Usage:
 * ```tsx
 * const { lastSavedLabel, saveNow } = useDraftAutosave({
 *   draftKey: `journal-batch-${batchId}`,
 *   data: lines,
 *   intervalMs: 30000,
 * })
 * ```
 */
export function useDraftAutosave<T>({
  draftKey,
  data,
  intervalMs = 30_000,
  enabled = true,
  onSaved,
}: DraftAutosaveOptions<T>): DraftAutosaveResult<T> {
  const [lastSavedAt, setLastSavedAt] = useState<number | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const dataRef = useRef(data)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // Keep data ref current
  dataRef.current = data

  const saveNow = useCallback(() => {
    try {
      setIsSaving(true)
      const payload = JSON.stringify({
        data: dataRef.current,
        savedAt: Date.now(),
        version: 1,
      })
      localStorage.setItem(`${STORAGE_PREFIX}${draftKey}`, payload)
      const now = Date.now()
      setLastSavedAt(now)
      onSaved?.(now)
    } catch (e) {
      console.error('Draft autosave failed:', e)
    } finally {
      setIsSaving(false)
    }
  }, [draftKey, onSaved])

  const loadDraft = useCallback((): T | null => {
    try {
      const raw = localStorage.getItem(`${STORAGE_PREFIX}${draftKey}`)
      if (!raw) return null
      const parsed = JSON.parse(raw)
      return parsed.data as T
    } catch {
      return null
    }
  }, [draftKey])

  const deleteDraft = useCallback(() => {
    localStorage.removeItem(`${STORAGE_PREFIX}${draftKey}`)
    setLastSavedAt(null)
  }, [draftKey])

  // Check if a draft exists
  const hasDraft = useCallback(() => {
    return localStorage.getItem(`${STORAGE_PREFIX}${draftKey}`) !== null
  }, [draftKey])()

  // Set up interval
  useEffect(() => {
    if (!enabled) {
      if (intervalRef.current) clearInterval(intervalRef.current)
      return
    }

    intervalRef.current = setInterval(() => {
      saveNow()
    }, intervalMs)

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [enabled, intervalMs, saveNow])

  // Save on unmount
  useEffect(() => {
    return () => {
      saveNow()
    }
  }, [saveNow])

  // Compute label
  const lastSavedLabel = lastSavedAt
    ? getTimeAgo(lastSavedAt)
    : 'Not saved'

  return {
    lastSavedAt,
    lastSavedLabel,
    isSaving,
    saveNow,
    loadDraft,
    deleteDraft,
    hasDraft,
  }
}

function getTimeAgo(timestamp: number): string {
  const seconds = Math.floor((Date.now() - timestamp) / 1000)
  if (seconds < 10) return 'Just now'
  if (seconds < 60) return `${seconds}s ago`
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes}m ago`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours}h ago`
  return `${Math.floor(hours / 24)}d ago`
}
