// Route guard for pages that need a signed-in user. It checks the in-memory token
// store and, if there's no session, redirects to /signin instead of letting the
// page render and then fail its API calls with 401s.
//
// The check is synchronous: by the time a route renders, SessionBootstrap has
// already awaited any silent refresh on load (components/SessionBootstrap.tsx), so
// the in-memory token reflects a restored session. Tokens that expire mid-session
// are handled transparently by the api.ts refresh-and-retry, not here.

import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { isAuthenticated } from '../lib/auth'

export default function RequireAuth({ children }: { children: ReactNode }) {
  return isAuthenticated() ? <>{children}</> : <Navigate to="/signin" replace />
}
