import { Badge } from '@components/ui/Badge'
import type { StatusMapEntry } from '@pages/platform/statusMaps'

export interface MapStatusBadgeProps {
  value: string | number | undefined | null
  mapping: Record<string, StatusMapEntry>
  size?: 'sm' | 'md' | 'lg'
  fallback?: string
}

export function MapStatusBadge({ value, mapping, size = 'sm', fallback = '—' }: MapStatusBadgeProps) {
  if (value === undefined || value === null || value === '') {
    return (
      <Badge variant="neutral" size={size}>
        {fallback}
      </Badge>
    )
  }

  const config = mapping[String(value)]
  if (!config) {
    return (
      <Badge variant="neutral" size={size}>
        {String(value)}
      </Badge>
    )
  }

  return (
    <Badge variant={config.variant} size={size} dot>
      {config.label}
    </Badge>
  )
}
