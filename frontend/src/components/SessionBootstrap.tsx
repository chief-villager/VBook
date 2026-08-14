// On first load (or a hard reload) the in-memory access token is gone, but a
// persisted refresh token may remain. This gate attempts one silent refresh before
// rendering the app, so a signed-in user who reloads keeps their session instead of
// being bounced to /signin by RequireAuth. See lib/api.ts `attemptRefresh` and
// CLAUDE.md "Authentication".

import { useEffect, useState, type ReactNode } from 'react'
import { attemptRefresh } from '../lib/api'
import { hasRefreshToken, isAuthenticated } from '../lib/auth'

export default function SessionBootstrap({ children }: { children: ReactNode }) {
  // Nothing to wait for when there's already a session, or no refresh token to try.
  const [ready, setReady] = useState(() => isAuthenticated() || !hasRefreshToken())

  useEffect(() => {
    if (ready) return
    let cancelled = false
    attemptRefresh().finally(() => {
      if (!cancelled) setReady(true)
    })
    return () => {
      cancelled = true
    }
  }, [ready])

  // Blank frame while the refresh is in flight — it's a single quick round-trip, and
  // it avoids a flash of the sign-in page before the session is restored.
  if (!ready) return <div style={{ minHeight: '100vh', background: 'var(--color-bg)' }} />

  return <>{children}</>
}
