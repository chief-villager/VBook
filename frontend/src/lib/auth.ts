// In-memory access-token store. Kept in a module variable (not localStorage) so a
// stolen XSS payload can't read it off disk and it clears on tab close. On reload
// the token is gone by design — call a refresh flow to obtain a new one when the
// API supports it. See CLAUDE.md "Authentication".

let accessToken: string | null = null

export function setAccessToken(token: string | null): void {
  accessToken = token
}

export function getAccessToken(): string | null {
  return accessToken
}

export function clearAccessToken(): void {
  accessToken = null
}

export function isAuthenticated(): boolean {
  return accessToken !== null
}

// The API issues a JWT whose `business_role` claim(s) carry "{businessId}:{role}"
// per business the user belongs to (see Infrastructure/Auth/JwtTokenIssuer.cs).
// Login only returns the token, so the businessId the pages need is read back out
// of it here rather than kept as separate state.

interface JwtPayload {
  sub?: string
  email?: string
  business_role?: string | string[]
}

function decodeToken(token: string): JwtPayload | null {
  const segment = token.split('.')[1]
  if (!segment) return null
  try {
    const base64 = segment.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join(''),
    )
    return JSON.parse(json) as JwtPayload
  } catch {
    return null
  }
}

export interface BusinessMembership {
  businessId: string
  role: string
}

export function getMemberships(): BusinessMembership[] {
  if (!accessToken) return []
  const payload = decodeToken(accessToken)
  if (!payload?.business_role) return []
  const claims = Array.isArray(payload.business_role) ? payload.business_role : [payload.business_role]
  return claims.map((value) => {
    // businessId is a GUID (no colon) and role has none either, so split on the first ':'.
    const i = value.indexOf(':')
    return { businessId: value.slice(0, i), role: value.slice(i + 1) }
  })
}

// The user's primary business — the one the dashboard reports on. Users in this
// flow own exactly one business; the first membership is that business.
export function getBusinessId(): string | null {
  return getMemberships()[0]?.businessId ?? null
}
