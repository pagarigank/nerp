// <copyright file="DrillBackLink.tsx" company="ERP Project">
// Copyright (c) ERP Project. All rights reserved.
// </copyright>

import { useNavigate } from 'react-router-dom'
import { ExternalLink } from 'lucide-react'
import { cn } from '@utils/helpers'

interface DrillBackLinkProps {
  /** The route to navigate to (e.g. /gl/journal-batches?account=1200). */
  to: string
  /** The display text (e.g. account number, invoice number). */
  children: React.ReactNode
  /** Optional className. */
  className?: string
  /** Whether to open in a new tab instead of navigating. */
  external?: boolean
}

/**
 * A clickable link that navigates to a filtered source transaction list.
 * Used on report figures to enable drill-back from summary → detail.
 *
 * Styling: blue text with underline on hover, external-link icon on hover.
 */
export function DrillBackLink({ to, children, className, external }: DrillBackLinkProps) {
  const navigate = useNavigate()

  const handleClick = (e: React.MouseEvent) => {
    if (external) return // Let the <a> tag handle it
    e.preventDefault()
    navigate(to)
  }

  if (external) {
    return (
      <a
        href={to}
        target="_blank"
        rel="noopener noreferrer"
        className={cn(
          'inline-flex items-center gap-1 text-primary-600 dark:text-primary-400',
          'hover:text-primary-700 dark:hover:text-primary-300',
          'hover:underline transition-colors font-mono text-xs',
          className
        )}
      >
        {children}
        <ExternalLink className="h-3 w-3 opacity-0 group-hover:opacity-100 transition-opacity" />
      </a>
    )
  }

  return (
    <button
      type="button"
      onClick={handleClick}
      className={cn(
        'inline-flex items-center gap-1 text-primary-600 dark:text-primary-400',
        'hover:text-primary-700 dark:hover:text-primary-300',
        'hover:underline transition-colors font-mono text-xs text-left',
        className
      )}
    >
      {children}
      <ExternalLink className="h-3 w-3 opacity-0 group-hover:opacity-100 transition-opacity" />
    </button>
  )
}
