import { useAuthStore } from '@stores/authStore'

export class ApiError extends Error {
  readonly status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  if (error instanceof Error) return error.message
  if (typeof error === 'string') return error
  return 'An unexpected error occurred'
}

const BASE_URL = '/api/v1'

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH'
  body?: unknown
  query?: Record<string, string | number | boolean | undefined | null>
}

async function parseErrorBody(res: Response): Promise<string | null> {
  const contentType = res.headers.get('content-type') ?? ''
  try {
    if (contentType.includes('application/json')) {
      const body = (await res.json()) as unknown
      if (typeof body === 'string') return body
      if (body && typeof body === 'object') {
        const record = body as Record<string, unknown>
        if (typeof record.message === 'string') return record.message
        if (typeof record.title === 'string') return record.title
        const errors = record.errors
        if (Array.isArray(errors) && typeof errors[0] === 'string') return errors[0]
      }
      return null
    }
    const text = await res.text()
    return text || null
  } catch {
    return null
  }
}

async function unwrap<T>(res: Response): Promise<T> {
  if (res.status === 204) return undefined as T

  const contentType = res.headers.get('content-type') ?? ''
  if (!contentType.includes('application/json')) {
    return (await res.text()) as unknown as T
  }

  const json = (await res.json()) as unknown

  if (
    json &&
    typeof json === 'object' &&
    'isSuccess' in json &&
    typeof (json as { isSuccess?: unknown }).isSuccess === 'boolean'
  ) {
    const envelope = json as { isSuccess: boolean; data?: T }
    if (envelope.isSuccess && envelope.data !== undefined) return envelope.data
  }

  return json as T
}

function buildQuery(query?: RequestOptions['query']): string {
  if (!query) return ''
  const params = new URLSearchParams()
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') continue
    params.set(key, String(value))
  }
  const str = params.toString()
  return str ? `?${str}` : ''
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = useAuthStore.getState().accessToken

  const headers: Record<string, string> = {
    Accept: 'application/json',
  }
  if (token) headers['Authorization'] = `Bearer ${token}`
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  const init: RequestInit = {
    method: options.method ?? 'GET',
    headers,
  }
  if (options.body !== undefined) {
    init.body = JSON.stringify(options.body)
  }

  const res = await fetch(`${BASE_URL}${path}${buildQuery(options.query)}`, init)

  if (!res.ok) {
    const message =
      ((await parseErrorBody(res)) ?? res.statusText) || `Request failed with status ${res.status}`
    throw new ApiError(message, res.status)
  }

  return unwrap<T>(res)
}

export function get<T>(path: string, query?: RequestOptions['query']): Promise<T> {
  return request<T>(path, query ? { query } : {})
}

export function post<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, { method: 'POST', ...(body === undefined ? {} : { body }) })
}

export function put<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, { method: 'PUT', ...(body === undefined ? {} : { body }) })
}

export function del<T>(path: string): Promise<T> {
  return request<T>(path, { method: 'DELETE' })
}
