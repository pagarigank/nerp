// <copyright file="PdfPreviewModal.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useEffect, useRef, useState } from 'react'
import { ApiError, getErrorMessage } from '@api/client'

export interface PdfPreviewModalProps {
  /** The entity id used to fetch the PDF. */
  entityId: string
  /** Human-readable label shown in the modal title. */
  title: string
  /** Loader that returns the PDF as a Blob. */
  loader: (id: string) => Promise<Blob>
  /** Shown while the PDF is being fetched. */
  open: boolean
  /** Called when the user dismisses the modal. */
  onClose: () => void
}

/**
 * Simple before-print preview: fetches a PDF Blob, renders it in an <iframe>,
 * and exposes a Print button that triggers the browser print dialog.
 */
export function PdfPreviewModal({
  entityId,
  title,
  loader,
  open,
  onClose,
}: PdfPreviewModalProps) {
  const [state, setState] = useState<{
    status: 'loading' | 'ready' | 'error'
    url: string | null
    error: string | null
  }>({ status: 'loading', url: null, error: null })
  const iframeRef = useRef<HTMLIFrameElement>(null)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    setState({ status: 'loading', url: null, error: null })
    loader(entityId)
      .then((blob) => {
        if (cancelled) return
        const url = URL.createObjectURL(blob)
        setState({ status: 'ready', url, error: null })
      })
      .catch((error: unknown) => {
        if (cancelled) return
        setState({
          status: 'error',
          url: null,
          error:
            error instanceof ApiError
              ? error.message
              : getErrorMessage(error) || 'Failed to load PDF.',
        })
      })
    return () => {
      cancelled = true
      setState((prev) => {
        if (prev.url) URL.revokeObjectURL(prev.url)
        return prev
      })
    }
  }, [open, entityId, loader])

  const closeAndRevoke = () => {
    if (state.url) URL.revokeObjectURL(state.url)
    onClose()
  }

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 backdrop-blur-sm"
      onClick={closeAndRevoke}
      role="dialog"
      aria-modal="true"
      aria-label={`${title} preview`}
    >
      <div
        className="relative flex max-h-[90vh] w-11/12 max-w-4xl flex-col rounded-lg bg-white shadow-xl"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b px-4 py-2">
          <h2 className="text-lg font-semibold">{title}</h2>
          <button
            type="button"
            onClick={closeAndRevoke}
            className="rounded p-1 text-gray-500 hover:bg-gray-100"
            aria-label="Close preview"
          >
            ✕
          </button>
        </div>
        {state.status === 'loading' && (
          <div className="flex items-center justify-center py-12 text-gray-500">
            Loading PDF…
          </div>
        )}
        {state.status === 'error' && (
          <div className="p-4 text-red-600">{state.error}</div>
        )}
        {state.status === 'ready' && state.url && (
          <>
            <iframe
              ref={iframeRef}
              title={title}
              src={state.url}
              className="h-[calc(90vh-110px)] w-full border-0"
            />
            <div className="flex justify-end gap-2 border-t px-4 py-2">
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation()
                  iframeRef.current?.contentWindow?.print()
                }}
                className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
              >
                Print
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
