// The signed-in app frame — a sticky sidebar (brand + section nav + the current
// business) and a header (section title + the "accounting terms" toggle). Ported
// from the shell shared by every authenticated screen in "Vbook.dc.html"; each
// page renders its own content as `children`.
//
// The business name and signed-in user are hard-coded to match the prototype;
// wiring them to the Identity/session is left as a TODO once auth is in place.

import { useState, type CSSProperties, type ReactNode } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { logout } from '../lib/identity'

export type Section = 'dashboard' | 'transactions' | 'invoices' | 'ledger' | 'reports' | 'credit' | 'settings'

// Report sub-tabs, revealed under "Reports" in the sidebar. Keys map to the
// /reports/:tab route.
export const REPORT_TABS: { key: string; label: string }[] = [
  { key: 'pl', label: 'Profit and Loss' },
  { key: 'bs', label: 'Balance Sheet' },
  { key: 'cf', label: 'Cash Flow' },
]

interface NavItem {
  id: Section
  label: string
  icon: ReactNode
}

// stroke inherits currentColor so the active/idle colour drives the icon too.
const svg = (children: ReactNode) => (
  <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
    {children}
  </svg>
)

const NAV: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: svg(<><rect x="3" y="3" width="7" height="9" /><rect x="14" y="3" width="7" height="5" /><rect x="14" y="12" width="7" height="9" /><rect x="3" y="16" width="7" height="5" /></>) },
  { id: 'transactions', label: 'Transactions', icon: svg(<><path d="M8 3 4 7l4 4" /><path d="M4 7h16" /><path d="m16 21 4-4-4-4" /><path d="M20 17H4" /></>) },
  { id: 'invoices', label: 'Invoices', icon: svg(<><path d="M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7z" /><path d="M14 2v5h6" /><path d="M9 13h6" /><path d="M9 17h4" /></>) },
  { id: 'ledger', label: 'Ledger', icon: svg(<><path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20" /><path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z" /></>) },
  { id: 'reports', label: 'Reports', icon: svg(<><path d="M3 3v18h18" /><rect x="7" y="11" width="3" height="7" /><rect x="13" y="7" width="3" height="11" /></>) },
  { id: 'credit', label: 'Credit Readiness', icon: svg(<><path d="M12 2 4 6v6c0 5 3.4 8.7 8 10 4.6-1.3 8-5 8-10V6z" /><path d="m9 12 2 2 4-4" /></>) },
  { id: 'settings', label: 'Settings', icon: svg(<><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.6 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.6h.09A1.65 1.65 0 0 0 10 3.09V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9v.09a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" /></>) },
]

// Shared look for every top-level sidebar button (background/colour set per item).
const navButton: CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: 11,
  width: '100%',
  padding: '10px 11px',
  border: 0,
  borderRadius: 'var(--radius-sm)',
  fontFamily: 'var(--font-body)',
  fontSize: 14.5,
  textAlign: 'left',
  cursor: 'pointer',
}

interface AppShellProps {
  active: Section
  title: string
  kicker: string
  showTerms: boolean
  onToggleTerms: () => void
  businessName?: string
  userName?: string
  children: ReactNode
}

// "Adaeze Okafor" -> "AO"; a single word yields its first two letters.
function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '—'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

export default function AppShell({
  active,
  title,
  kicker,
  showTerms,
  onToggleTerms,
  businessName = 'Okafor Logistics Ltd',
  userName = 'Adaeze Okafor',
  children,
}: AppShellProps) {
  const navigate = useNavigate()
  const location = useLocation()
  // The active report tab comes off the URL (/reports/pl -> "pl"); "" on other pages.
  const activeReport = location.pathname.startsWith('/reports/')
    ? location.pathname.slice('/reports/'.length)
    : ''
  // The Reports submenu starts open when the user is inside Reports.
  const [reportsOpen, setReportsOpen] = useState(active === 'reports')
  const [signingOut, setSigningOut] = useState(false)

  // Revokes the session (best-effort) and returns to the sign-in screen. logout()
  // clears the local session even if the server call fails, so navigation is safe.
  async function signOut() {
    if (signingOut) return
    setSigningOut(true)
    try {
      await logout()
    } finally {
      navigate('/signin', { replace: true })
    }
  }

  return (
    <div
      style={{
        display: 'flex',
        minHeight: '100vh',
        fontFamily: 'var(--font-body)',
        color: 'var(--color-text)',
        background: 'var(--color-bg)',
      }}
    >
      <aside
        style={{
          width: 244,
          flex: '0 0 244px',
          background: 'var(--color-accent-900)',
          color: 'var(--color-neutral-100)',
          display: 'flex',
          flexDirection: 'column',
          position: 'sticky',
          top: 0,
          height: '100vh',
        }}
      >
        <div style={{ padding: '26px 22px 20px 22px', display: 'flex', alignItems: 'baseline', gap: 8 }}>
          <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 27, letterSpacing: '0.02em', color: '#fff' }}>
            vbook
          </span>
          <span style={{ fontSize: 10, letterSpacing: '0.16em', textTransform: 'uppercase', color: 'var(--color-accent-400)' }}>
            books
          </span>
        </div>

        <nav style={{ display: 'flex', flexDirection: 'column', gap: 2, padding: '6px 12px' }}>
          {NAV.map((item) => {
            const on = item.id === active

            // Reports carries a collapsible submenu of the three statements.
            if (item.id === 'reports') {
              return (
                <div key={item.id} style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <button
                    onClick={() => {
                      setReportsOpen((o) => (active === 'reports' ? !o : true))
                      navigate('/reports')
                    }}
                    style={{ ...navButton, background: on ? 'var(--color-accent-700)' : 'transparent', color: on ? '#ffffff' : 'var(--color-accent-300)' }}
                  >
                    {item.icon}
                    {item.label}
                    <svg
                      width="15"
                      height="15"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="1.5"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      style={{ marginLeft: 'auto', transform: `rotate(${reportsOpen ? 180 : 0}deg)`, transition: 'transform 0.15s' }}
                    >
                      <path d="m6 9 6 6 6-6" />
                    </svg>
                  </button>
                  {reportsOpen && (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 2, padding: '2px 0 4px 30px' }}>
                      {REPORT_TABS.map((r) => {
                        const rOn = active === 'reports' && activeReport === r.key
                        return (
                          <button
                            key={r.key}
                            onClick={() => navigate(`/reports/${r.key}`)}
                            style={{
                              display: 'flex',
                              alignItems: 'center',
                              gap: 9,
                              width: '100%',
                              padding: '8px 11px',
                              border: 0,
                              borderRadius: 'var(--radius-sm)',
                              fontFamily: 'var(--font-body)',
                              fontSize: 13.5,
                              textAlign: 'left',
                              cursor: 'pointer',
                              background: rOn ? 'var(--color-accent-700)' : 'transparent',
                              color: rOn ? '#ffffff' : 'var(--color-accent-300)',
                            }}
                          >
                            <span style={{ width: 4, height: 4, flex: 'none', background: 'currentColor' }} />
                            {r.label}
                          </button>
                        )
                      })}
                    </div>
                  )}
                </div>
              )
            }

            return (
              <button
                key={item.id}
                onClick={() => navigate(`/${item.id}`)}
                style={{ ...navButton, background: on ? 'var(--color-accent-700)' : 'transparent', color: on ? '#ffffff' : 'var(--color-accent-300)' }}
              >
                {item.icon}
                {item.label}
              </button>
            )
          })}
        </nav>

        <div
          style={{
            marginTop: 'auto',
            padding: '16px 18px 20px 18px',
            borderTop: '1px solid var(--color-accent-800)',
            display: 'flex',
            alignItems: 'center',
            gap: 11,
          }}
        >
          <div
            style={{
              width: 32,
              height: 32,
              flex: '0 0 32px',
              background: 'var(--color-accent-700)',
              color: '#fff',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontFamily: 'var(--font-heading)',
              fontSize: 15,
            }}
          >
            {initials(userName)}
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.25, minWidth: 0 }}>
            <span style={{ fontSize: 13.5, color: '#fff', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {businessName}
            </span>
            <span style={{ fontSize: 11.5, color: 'var(--color-accent-400)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {userName}
            </span>
          </div>
          <button
            onClick={signOut}
            disabled={signingOut}
            title="Sign out"
            aria-label="Sign out"
            style={{
              marginLeft: 'auto',
              flex: '0 0 auto',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              padding: 7,
              background: 'none',
              border: '1px solid var(--color-accent-800)',
              borderRadius: 'var(--radius-sm)',
              color: 'var(--color-accent-300)',
              cursor: signingOut ? 'default' : 'pointer',
              opacity: signingOut ? 0.6 : 1,
            }}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
              <path d="m16 17 5-5-5-5" />
              <path d="M21 12H9" />
            </svg>
          </button>
        </div>
      </aside>

      <main style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <header
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'flex-end',
            justifyContent: 'space-between',
            gap: '18px 24px',
            padding: '30px 40px 22px 40px',
            borderBottom: '1px solid var(--color-divider)',
          }}
        >
          <div>
            <div style={{ fontSize: 12, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 6 }}>
              {kicker}
            </div>
            <h1 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 34, lineHeight: 1.05, margin: 0 }}>
              {title}
            </h1>
          </div>
          <button
            onClick={onToggleTerms}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              background: 'none',
              border: '1px solid var(--color-divider)',
              borderRadius: 'var(--radius-sm)',
              padding: '7px 11px',
              fontFamily: 'var(--font-body)',
              fontSize: 13,
              color: 'var(--color-neutral-700)',
              cursor: 'pointer',
            }}
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="12" cy="12" r="9" />
              <path d="M12 16v-4" />
              <path d="M12 8h.01" />
            </svg>
            {showTerms ? 'Hide accounting terms' : 'Show accounting terms'}
          </button>
        </header>

        {children}
      </main>
    </div>
  )
}
