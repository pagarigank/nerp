import { Suspense, type ComponentType, type LazyExoticComponent } from 'react'
import { LoadingSpinner } from './LoadingSpinner'

interface LazyRouteProps {
  component: LazyExoticComponent<ComponentType>
}

/**
 * Wrapper component for lazy-loaded routes that shows a loading spinner
 * while the route chunk is being loaded.
 */
export function LazyRoute({ component: Component }: LazyRouteProps) {
  return (
    <Suspense fallback={
      <div className="min-h-[400px] flex items-center justify-center">
        <LoadingSpinner size="lg" />
      </div>
    }>
      <Component />
    </Suspense>
  )
}
