/**
 * Creates a debounced function that delays invoking the provided function
 * until after `delay` milliseconds have elapsed since the last invocation.
 */
export function debounce<T extends (...args: never[]) => void>(
  fn: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timer: ReturnType<typeof setTimeout> | null = null

  return (...args: Parameters<T>) => {
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => {
      fn(...args)
      timer = null
    }, delay)
  }
}

/**
 * Creates a throttled function that only invokes the provided function
 * at most once per `limit` milliseconds.
 */
export function throttle<T extends (...args: never[]) => void>(
  fn: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle = false
  let lastArgs: Parameters<T> | null = null

  return (...args: Parameters<T>) => {
    if (!inThrottle) {
      fn(...args)
      inThrottle = true
      setTimeout(() => {
        inThrottle = false
        if (lastArgs) {
          fn(...lastArgs)
          lastArgs = null
        }
      }, limit)
    } else {
      lastArgs = args
    }
  }
}

/**
 * React hook for debounced value - useful for search inputs.
 */
export function useDebouncedValue<T>(value: T, delay: number): T {
  // This would typically use useState + useEffect, but as a standalone utility:
  const [debouncedValue, setDebouncedValue] = useState(value)

  useEffect(() => {
    const handler = setTimeout(() => setDebouncedValue(value), delay)
    return () => clearTimeout(handler)
  }, [value, delay])

  return debouncedValue
}

// Need to import React hooks for useDebouncedValue
import { useState, useEffect } from 'react'
