const SW_MODULE_PATH = '/sw.js';
const SW_SCOPE = '/';

let registration: ServiceWorkerRegistration | null = null;

/**
 * Register the service worker for offline-first caching.
 * Only registers in production or when explicitly enabled.
 */
export async function registerServiceWorker(): Promise<ServiceWorkerRegistration | null> {
  if (!('serviceWorker' in navigator)) {
    console.warn('[SW] Service workers not supported');
    return null;
  }

  // Only register in production
  if (import.meta.env.DEV) {
    console.info('[SW] Skipping registration in development mode');
    return null;
  }

  try {
    registration = await navigator.serviceWorker.register(SW_MODULE_PATH, {
      scope: SW_SCOPE,
    });

    console.info('[SW] Registered successfully', registration.scope);

    // Listen for updates
    registration.addEventListener('updatefound', () => {
      const newWorker = registration?.installing;
      if (!newWorker) return;

      newWorker.addEventListener('statechange', () => {
        if (newWorker.state === 'activated') {
          console.info('[SW] New version activated');
          // Notify the user they can refresh
          window.dispatchEvent(new CustomEvent('sw-update'));
        }
      });
    });

    return registration;
  } catch (error) {
    console.error('[SW] Registration failed:', error);
    return null;
  }
}

/**
 * Unregister the service worker and clear all caches.
 */
export async function unregisterServiceWorker(): Promise<void> {
  if (!('serviceWorker' in navigator)) return;

  const regs = await navigator.serviceWorker.getRegistrations();
  await Promise.all(regs.map((reg) => reg.unregister()));

  // Clear all caches
  if ('caches' in window) {
    const cacheNames = await caches.keys();
    await Promise.all(cacheNames.map((name) => caches.delete(name)));
  }

  registration = null;
  console.info('[SW] Unregistered and caches cleared');
}

/**
 * Force the service worker to check for updates.
 */
export async function checkForSWUpdate(): Promise<boolean> {
  if (!registration) return false;

  try {
    await registration.update();
    return !!registration.waiting;
  } catch {
    return false;
  }
}

/**
 * Skip waiting and activate the new service worker.
 */
export function skipWaiting(): void {
  registration?.waiting?.postMessage('skipWaiting');
}

/**
 * Invalidate a specific lookup path in the service worker cache.
 * Call this after creating/updating master data.
 */
export function invalidateLookupCache(path: string): void {
  if (navigator.serviceWorker?.controller) {
    navigator.serviceWorker.controller.postMessage({
      type: 'INVALIDATE_LOOKUP',
      path,
    });
  }
}

/**
 * Clear all caches (useful for logout).
 */
export function clearAllCaches(): void {
  if (navigator.serviceWorker?.controller) {
    navigator.serviceWorker.controller.postMessage('CLEAR_CACHES');
  }
}

/**
 * Get cache storage stats (for debugging).
 */
export async function getCacheStats(): Promise<Record<string, number>> {
  if (!('caches' in window)) return {};

  const stats: Record<string, number> = {};
  const cacheNames = await caches.keys();

  for (const name of cacheNames) {
    const cache = await caches.open(name);
    const keys = await cache.keys();
    stats[name] = keys.length;
  }

  return stats;
}
