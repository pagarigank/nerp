const CACHE_NAME = 'erp-v1';
const STATIC_CACHE = 'erp-static-v1';
const LOOKUP_CACHE = 'erp-lookups-v1';

// Static assets to pre-cache on install
const STATIC_ASSETS = [
  '/',
  '/index.html',
];

// Lookup API paths that should be cached for offline use
const LOOKUP_PATHS = [
  '/api/v1/platform/companies',
  '/api/v1/platform/segment-types',
  '/api/v1/platform/segment-values',
  '/api/v1/platform/fiscal-years',
  '/api/v1/platform/fiscal-periods',
  '/api/v1/platform/currencies',
  '/api/v1/platform/exchange-rates',
  '/api/v1/gl/accounts',
  '/api/v1/inventory/items',
  '/api/v1/ap/vendors',
  '/api/v1/ar/customers',
  '/api/v1/payroll/employees',
];

// Paths that should always go to network (never cached)
const NETWORK_ONLY = [
  '/api/v1/platform/auth',
  '/api/v1/platform/account',
  '/api/v1/platform/users/me',
  '/api/v1/platform/notifications',
];

// Install: pre-cache static assets
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(STATIC_CACHE).then((cache) => {
      return cache.addAll(STATIC_ASSETS).catch(() => {
        // Silently fail for offline-first — assets will be fetched when online
      });
    }),
  );
  self.skipWaiting();
});

// Activate: clean up old caches
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames
          .filter((name) => name !== STATIC_CACHE && name !== LOOKUP_CACHE && name !== CACHE_NAME)
          .map((name) => caches.delete(name)),
      );
    }),
  );
  self.clients.claim();
});

// Fetch: implement caching strategies
self.addEventListener('fetch', (event) => {
  const { request } = event;
  const url = new URL(request.url);

  // Skip non-GET requests
  if (request.method !== 'GET') return;

  // Skip network-only paths
  if (NETWORK_ONLY.some((path) => url.pathname.startsWith(path))) return;

  // Skip cross-origin requests
  if (url.origin !== self.location.origin) return;

  // Lookup data: stale-while-revalidate (serve from cache, update in background)
  if (LOOKUP_PATHS.some((path) => url.pathname.startsWith(path))) {
    event.respondWith(staleWhileRevalidate(request, LOOKUP_CACHE));
    return;
  }

  // Static assets: cache-first
  if (url.pathname.match(/\.(js|css|png|jpg|jpeg|gif|svg|ico|woff|woff2)$/)) {
    event.respondWith(cacheFirst(request, STATIC_CACHE));
    return;
  }

  // HTML pages: network-first (SPA navigation)
  if (request.headers.get('accept')?.includes('text/html')) {
    event.respondWith(networkFirst(request, CACHE_NAME));
    return;
  }

  // API requests: network-first with cache fallback
  if (url.pathname.startsWith('/api/')) {
    event.respondWith(networkFirst(request, CACHE_NAME));
    return;
  }

  // Default: network-first
  event.respondWith(networkFirst(request, CACHE_NAME));
});

// Strategy: Cache First (for static assets)
async function cacheFirst(request, cacheName) {
  const cached = await caches.match(request);
  if (cached) return cached;

  try {
    const response = await fetch(request);
    if (response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    return new Response('Offline', { status: 503 });
  }
}

// Strategy: Network First (for API/HTML)
async function networkFirst(request, cacheName) {
  try {
    const response = await fetch(request);
    if (response.ok) {
      const cache = await caches.open(cacheName);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    const cached = await caches.match(request);
    if (cached) return cached;
    return new Response('Offline', { status: 503 });
  }
}

// Strategy: Stale While Revalidate (for lookup data)
async function staleWhileRevalidate(request, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(request);

  const fetchPromise = fetch(request)
    .then((response) => {
      if (response.ok) {
        cache.put(request, response.clone());
      }
      return response;
    })
    .catch(() => cached);

  return cached || fetchPromise;
}

// Listen for messages from the main thread
self.addEventListener('message', (event) => {
  if (event.data === 'skipWaiting') {
    self.skipWaiting();
  }

  // Invalidate specific lookup caches
  if (event.data?.type === 'INVALIDATE_LOOKUP') {
    const path = event.data.path;
    caches.open(LOOKUP_CACHE).then((cache) => {
      cache.keys().then((requests) => {
        requests.forEach((req) => {
          if (new URL(req.url).pathname.startsWith(path)) {
            cache.delete(req);
          }
        });
      });
    });
  }

  // Clear all caches
  if (event.data === 'CLEAR_CACHES') {
    caches.keys().then((names) => {
      names.forEach((name) => caches.delete(name));
    });
  }
});
