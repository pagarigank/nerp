import { useState, useRef, useCallback, useMemo, useEffect } from 'react'

interface VirtualGridProps<T> {
  data: T[]
  rowHeight: number
  height: number
  renderRow: (item: T, index: number) => React.ReactNode
  header?: React.ReactNode
  className?: string
  overscan?: number
  onEndReached?: () => void
  endReachedThreshold?: number
}

/**
 * A virtualized grid component that only renders visible rows for performance
 * with large data sets (1000+ rows).
 */
export function VirtualGrid<T>({
  data,
  rowHeight,
  height,
  renderRow,
  header,
  className = '',
  overscan = 5,
  onEndReached,
  endReachedThreshold = 100
}: VirtualGridProps<T>) {
  const [scrollTop, setScrollTop] = useState(0)
  const containerRef = useRef<HTMLDivElement>(null)

  const totalHeight = data.length * rowHeight
  const startIndex = Math.max(0, Math.floor(scrollTop / rowHeight) - overscan)
  const endIndex = Math.min(
    data.length - 1,
    Math.floor((scrollTop + height) / rowHeight) + overscan
  )

  const visibleItems = useMemo(() => {
    const items = []
    for (let i = startIndex; i <= endIndex; i++) {
      items.push({ item: data[i], index: i })
    }
    return items
  }, [data, startIndex, endIndex])

  const handleScroll = useCallback(() => {
    if (containerRef.current) {
      setScrollTop(containerRef.current.scrollTop)

      // Check if we've reached the end
      if (onEndReached) {
        const { scrollTop: st, scrollHeight, clientHeight } = containerRef.current
        if (scrollHeight - st - clientHeight < endReachedThreshold) {
          onEndReached()
        }
      }
    }
  }, [onEndReached, endReachedThreshold])

  // Throttle scroll events for performance
  useEffect(() => {
    const container = containerRef.current
    if (!container) return

    let ticking = false
    const onScroll = () => {
      if (!ticking) {
        requestAnimationFrame(() => {
          handleScroll()
          ticking = false
        })
        ticking = true
      }
    }

    container.addEventListener('scroll', onScroll, { passive: true })
    return () => container.removeEventListener('scroll', onScroll)
  }, [handleScroll])

  // Only use virtualization for large data sets
  if (data.length <= 50) {
    return (
      <div className={className}>
        {header}
        <div>
          {data.map((item, index) => renderRow(item, index))}
        </div>
      </div>
    )
  }

  return (
    <div className={className}>
      {header}
      <div
        ref={containerRef}
        style={{ height, overflow: 'auto' }}
        className="relative"
      >
        <div style={{ height: totalHeight, position: 'relative' }}>
          {visibleItems.map(({ item, index }) => (
            <div
              key={index}
              style={{
                position: 'absolute',
                top: index * rowHeight,
                left: 0,
                right: 0,
                height: rowHeight
              }}
            >
              {renderRow(item, index)}
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
