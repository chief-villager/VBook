// The single HTTP entry point for talking to the Bookkeeping API. Every call goes
// through `api()` so the base URL and the `Authorization: Bearer` header are applied
// in exactly one place.

import { clearSession, getAccessToken, getRefreshToken, storeSession } from './auth'

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
  /** Plain object — serialized as JSON. Pass a FormData to send multipart (e.g. a
   *  file upload); the browser sets its own Content-Type + boundary. Omit for
   *  GET/DELETE without a body. */
  body?: unknown
  /** Set false for endpoints that must be called without a token (login, register). */
  auth?: boolean
  /** Internal: set once after a 401-triggered refresh so a retry can't loop. */
  _retry?: boolean
}

// Exchanges the stored refresh token for a new token pair. Single-flighted: if a
// refresh is already running (e.g. several calls 401 at once), everyone awaits the
// same request rather than each rotating the refresh token — which would look like
// token reuse to the API and revoke the whole session. Returns whether it succeeded.
let refreshInFlight: Promise<boolean> | null = null

export function attemptRefresh(): Promise<boolean> {
  if (refreshInFlight) return refreshInFlight

  const refreshToken = getRefreshToken()
  if (!refreshToken) return Promise.resolve(false)

  refreshInFlight = (async () => {
    try {
      const response = await fetch(`${BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })
      if (!response.ok) {
        // Expired, revoked, or reuse-detected: the session is over.
        clearSession()
        return false
      }
      const data = (await response.json()) as { accessToken: string; refreshToken: string }
      storeSession({ accessToken: data.accessToken, refreshToken: data.refreshToken })
      return true
    } catch {
      return false
    } finally {
      refreshInFlight = null
    }
  })()

  return refreshInFlight
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
  const { body, auth = true, headers, _retry, ...rest } = options

  // FormData carries its own multipart Content-Type (with a boundary the browser
  // fills in), so we only set the JSON header — and only JSON-serialize — for plain
  // object bodies.
  const isForm = body instanceof FormData
  const finalHeaders = new Headers(headers)
  if (body !== undefined && !isForm) finalHeaders.set('Content-Type', 'application/json')

  if (auth) {
    const token = getAccessToken()
    if (token) finalHeaders.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${BASE_URL}${path}`, {
    ...rest,
    headers: finalHeaders,
    body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
  })

  // Access token expired mid-session: silently refresh once and replay the request.
  if (response.status === 401 && auth && !_retry && getRefreshToken()) {
    if (await attemptRefresh()) return api<T>(path, { ...options, _retry: true })
  }

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

// Fetches a binary response (e.g. a generated PDF) as a Blob, with the same bearer
// auth and refresh-on-401 replay as api(). Kept separate because api() assumes a
// JSON/text body.
export async function getBlob(path: string, options: ApiOptions = {}): Promise<Blob> {
  const { auth = true, headers, _retry } = options

  const finalHeaders = new Headers(headers)
  if (auth) {
    const token = getAccessToken()
    if (token) finalHeaders.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${BASE_URL}${path}`, { method: 'GET', headers: finalHeaders })

  if (response.status === 401 && auth && !_retry && getRefreshToken()) {
    if (await attemptRefresh()) return getBlob(path, { ...options, _retry: true })
  }

  if (!response.ok) throw new ApiError(response.status, `Request failed with ${response.status}`)
  return response.blob()
}
