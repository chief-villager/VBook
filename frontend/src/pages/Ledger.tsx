// Ledger — React port of the "ledger" section of "Vbook.dc.html", wired to the live
// API. A full-width, grouped overview of every account the business keeps (Income /
// Expenses / Assets / Liabilities / Equity): each group carries a plain-words note and
// the balance it stands at today, with its accounts listed beneath. The design keeps
// this a summary — no per-entry drill-down.
//
// Source is the trial balance (GET .../trial-balance), which returns every account
// signed as debits-minus-credits; naturalBalance folds each onto the side it normally
// sits on so the figures read as a business owner expects. Visuals come from
// src/styles/industry.css. The "Show accounting terms" toggle reveals each account's
// code and statement line.

import { useEffect, useMemo, useState } from 'react'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import {
  AccountType,
  getTrialBalance,
  naturalBalance,
  type AccountBalance,
  type TrialBalance,
} from '../lib/ledger'

const NAIRA = '₦'
const MINUS = '−'

// "₦1,200" / "−₦900".
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(Math.round(n)).toLocaleString('en-NG')

// The order the groups read in, with the plain-words note the design shows under each.
const GROUPS: { type: number; title: string; note: string }[] = [
  { type: AccountType.Income, title: 'Income', note: 'Money you earn' },
  { type: AccountType.Expense, title: 'Expenses', note: 'Money you spend to run the business' },
  { type: AccountType.Asset, title: 'Assets', note: 'What the business owns' },
  { type: AccountType.Liability, title: 'Liabilities', note: 'What the business owes' },
  { type: AccountType.Equity, title: 'Equity', note: 'What the business is worth to you' },
]

// "2026-08-15" -> "15 Aug 2026". Parse by hand so the day never shifts across a
// timezone boundary (new Date("yyyy-MM-dd") is parsed as UTC).
function formatDate(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number)
  if (!y || !m || !d) return iso
  return new Date(y, m - 1, d).toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: 'numeric' })
}

export default function Ledger() {
  const [showTerms, setShowTerms] = useState(false)

  const businessId = useMemo(() => getBusinessId(), [])

  const [trial, setTrial] = useState<TrialBalance | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!businessId) {
      setError('We could not find a business on your account.')
      setLoading(false)
      return
    }
    let cancelled = false
    setLoading(true)
    getTrialBalance(businessId)
      .then((data) => {
        if (!cancelled) {
          setTrial(data)
          setError(null)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiError ? err.message : 'Could not load your ledger.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [businessId])

  // Accounts split by group, with each group's natural-side total. Accounts standing
  // at zero are dropped from the listing (they'd add nothing and the API returns the
  // full reference chart), but the group total still sums every account.
  const groups = useMemo(() => {
    const accounts = trial?.accounts ?? []
    return GROUPS.map((g) => {
      const inGroup = accounts.filter((a) => a.type === g.type)
      const total = inGroup.reduce((s, a) => s + naturalBalance(a.type, a.balance), 0)
      const rows = inGroup
        .map((a) => ({ account: a, balance: naturalBalance(a.type, a.balance) }))
        .filter((r) => Math.round(r.balance) !== 0)
        .sort((x, y) => Math.abs(y.balance) - Math.abs(x.balance))
      return { ...g, total, rows }
    }).filter((g) => g.rows.length > 0 || Math.round(g.total) !== 0)
  }, [trial])

  return (
    <AppShell
      active="ledger"
      title="Ledger"
      kicker="Every account, where it stands"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '26px 40px 48px 40px', display: 'flex', flexWrap: 'wrap', gap: 26, alignItems: 'flex-start' }}>
        <aside style={{ flex: '1 1 100%', width: '100%', display: 'flex', flexDirection: 'column', gap: 22 }}>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'baseline', justifyContent: 'space-between' }}>
            <p style={{ margin: 0, fontSize: 14, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '60ch' }}>
              Every account your business keeps, and what each one stands at today.
            </p>
            {trial && (
              <span style={{ fontSize: 12.5, color: 'var(--color-neutral-600)' }}>
                As of {formatDate(trial.asOf)}
                {!trial.isBalanced && (
                  <span className="tag tag-outline" style={{ fontSize: 11, marginLeft: 8 }}>
                    Out of balance
                  </span>
                )}
              </span>
            )}
          </div>

          {error && (
            <p role="alert" style={{ margin: 0, fontSize: 13.5, color: '#b3261e' }}>
              {error}
            </p>
          )}

          {loading && !trial ? (
            <p style={{ margin: 0, fontSize: 15, color: 'var(--color-neutral-700)' }}>Loading your ledger…</p>
          ) : !error && groups.length === 0 ? (
            <p style={{ margin: 0, fontSize: 15, color: 'var(--color-neutral-700)' }}>
              Nothing has been posted yet. Record or import a transaction and it will show here.
            </p>
          ) : (
            groups.map((g) => (
              <div key={g.type}>
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'baseline',
                    justifyContent: 'space-between',
                    gap: 12,
                    paddingBottom: 7,
                    borderBottom: '1px solid var(--color-divider)',
                  }}
                >
                  <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 17 }}>{g.title}</span>
                  <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 15 }}>{amt(g.total)}</span>
                </div>
                <div style={{ fontSize: 12.5, color: 'var(--color-neutral-600)', padding: '6px 0 4px 0' }}>{g.note}</div>

                {g.rows.map(({ account, balance }) => (
                  <AccountRow key={account.id.value} account={account} balance={balance} showTerms={showTerms} />
                ))}
              </div>
            ))
          )}
        </aside>
      </div>
    </AppShell>
  )
}

function AccountRow({
  account,
  balance,
  showTerms,
}: {
  account: AccountBalance
  balance: number
  showTerms: boolean
}) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'baseline',
        justifyContent: 'space-between',
        gap: 12,
        padding: '9px 0',
        borderBottom: '1px solid var(--color-divider)',
      }}
    >
      <span style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
        <span style={{ fontSize: 14.5 }}>{account.name}</span>
        {showTerms && (
          <span style={{ fontSize: 12, color: 'var(--color-neutral-600)' }}>
            {account.code} · {account.statementLine}
          </span>
        )}
      </span>
      <span style={{ fontSize: 14.5, whiteSpace: 'nowrap', color: 'var(--color-neutral-800)' }}>{amt(balance)}</span>
    </div>
  )
}
