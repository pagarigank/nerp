// Reusable UOM selector sourced from the global Unit of Measure master.
// Forwards all native <select> props (including react-hook-form register() output)
// so it can drop-in replace a plain <select> bound to a unitOfMeasure field.
import { forwardRef, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getUoms } from '@api/uom'
import type { SelectHTMLAttributes } from 'react'

interface UomSelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  /** Optional explicit list of UOM codes to include (e.g. an item's allowed UOMs). */
  codes?: string[]
}

export const UomSelect = forwardRef<HTMLSelectElement, UomSelectProps>(
  ({ codes, className, value, ...props }, ref) => {
    const { data: uoms = [] } = useQuery({
      queryKey: ['inventory', 'uoms'],
      queryFn: () => getUoms(),
    })

    const options = useMemo(() => {
      const set = new Set<string>(codes ?? [])
      uoms.forEach((u) => set.add(u.code))
      if (value) set.add(String(value)) // ensure current/selected value is selectable
      if (set.size === 0) set.add('EA')
      return Array.from(set).map((c) => ({ value: c, label: c }))
    }, [uoms, codes, value])

    return (
      <select
        ref={ref}
        value={value}
        className={
          className ??
          'w-20 text-sm rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-1 py-1.5'
        }
        {...props}
      >
        {options.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    )
  },
)

UomSelect.displayName = 'UomSelect'
