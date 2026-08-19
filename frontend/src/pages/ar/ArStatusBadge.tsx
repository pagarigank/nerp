import { Badge } from '@components/ui/Badge'
import type { StatusMapEntry } from './statusMaps'

export interface ArStatusBadgeProps {
  value: string | number
  mapping: Record<string, StatusMapEntry>
  size?: 'sm' | 'md' | 'lg'
}

export function ArStatusBadge({ value, mapping, size = 'sm' }: ArStatusBadgeProps) {
  const config = mapping[String(value)]
  if (!config) {
    return (
      <Badge variant="neutral" size={size}>
        {value}
      </Badge>
    )
  }
  return (
    <Badge variant={config.variant} size={size} dot>
      {config.label}
    </Badge>
  )
}
