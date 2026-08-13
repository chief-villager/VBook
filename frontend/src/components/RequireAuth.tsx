// Route guard for pages that need a signed-in user. It checks the in-memory token
// store and, if there's no session, redirects to /signin instead of letting the
// page render and then fail its API calls with 401s.
//
// The check is synchronous today because the token lives only in memory (see
// lib/auth.ts). When a refresh-token flow is added, this is the place to grow a
// "checking…" state that awaits a silent token refresh before deciding.

import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { isAuthenticated } from '../lib/auth'

export default function RequireAuth({ children }: { children: ReactNode }) {
  return isAuthenticated() ? <>{children}</> : <Navigate to="/signin" replace />
}
