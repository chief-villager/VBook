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
