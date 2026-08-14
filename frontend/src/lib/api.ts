// The single HTTP entry point for talking to the Bookkeeping API. Every call goes
// through `api()` so the base URL and the `Authorization: Bearer` header are applied
// in exactly one place.

import { getAccessToken } from './auth'

const BASE_URL = import.meta.env.VITE_API_URL

if (!BASE_URL) {
  // Fail loudly in dev rather than firing requests at the wrong origin.
  console.warn('VITE_API_URL is not set — copy frontend/.env.example to .env')
}

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly body?: unknown,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

interface ApiOptions extends Omit<RequestInit, 'body'> {
  /** Plain object — serialized as JSON. Omit for GET/DELETE without a body. */
  body?: unknown
  /** Set false for endpoints that must be called without a token (login, register). */
  auth?: boolean
}

// Controllers report expected failures as `{ error }` and unexpected ones may carry
// `{ message }`; pull whichever is present so callers get a human-readable reason.
function errorMessage(payload: unknown, status: number): string {
  if (payload && typeof payload === 'object') {
    const { message, error } = payload as { message?: unknown; error?: unknown }
    if (typeof message === 'string') return message
    if (typeof error === 'string') return error
  }
  return `Request failed with ${status}`
}

export async function api<T = unknown>(path: string, options: ApiOptions = {}): Promise<T> {
  const { body, auth = true, headers, ...rest } = options

  const finalHeaders = new Headers(headers)
  if (body !== undefined) finalHeaders.set('Content-Type', 'application/json')

  if (auth) {
    const token = getAccessToken()
    if (token) finalHeaders.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    ...rest,
    headers: finalHeaders,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  const isJson = response.headers.get('content-type')?.includes('application/json')
  const payload = isJson ? await response.json().catch(() => undefined) : await response.text()

  if (!response.ok) {
    throw new ApiError(response.status, errorMessage(payload, response.status), payload)
  }

  return payload as T
}

export const get = <T>(path: string, options?: ApiOptions) =>
  api<T>(path, { ...options, method: 'GET' })

export const post = <T>(path: string, body?: unknown, options?: ApiOptions) =>
  api<T>(path, { ...options, method: 'POST', body })

export const put = <T>(path: string, body?: unknown, options?: ApiOptions) =>
  api<T>(path, { ...options, method: 'PUT', body })

export const patch = <T>(path: string, body?: unknown, options?: ApiOptions) =>
  api<T>(path, { ...options, method: 'PATCH', body })

export const del = <T>(path: string, options?: ApiOptions) =>
  api<T>(path, { ...options, method: 'DELETE' })
