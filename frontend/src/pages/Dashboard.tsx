// Dashboard — React port of the "dashboard" section of "Vbook.dc.html". The home
// screen after sign-in: a bank-connection banner, the "are you making money"
// profit card with a money-in vs money-out split donut, the credit-readiness
// checklist, and a banner nudging the user to the review queue.
//
// On mount it loads this month's Profit & Loss for the signed-in user's business
// (from the 1st of the current month through today) and drives the profit card and
// split donut from it. Visuals come from src/styles/industry.css. The remaining
// figures are still the prototype's mock values; TODOs mark where real data comes from:
//   review banner  -> count of Pending bank-imports
//   credit card    -> credit-readiness checklist state

import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { useNavigate } from 'react-router-dom'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import { getProfitAndLoss, type ProfitAndLoss } from '../lib/reporting'
import { linkBankAccount, listBankAccounts, type LinkedBankAccount } from '../lib/bankAccounts'
import { MonoNotConfiguredError, openMonoConnect } from '../lib/mono'

const NAIRA = '₦'

// TODO: source from the count of Pending bank-imports.
const REVIEW_COUNT = 12

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

// Local-date yyyy-MM-dd (the API binds these to DateOnly). Local, not UTC, so "today"
// matches the user's calendar day.
function toDateParam(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function formatNaira(amount: number): string {
  return NAIRA + Math.round(amount).toLocaleString('en-NG')
}

// Compact form for the tight donut centre, e.g. ₦7.1m, ₦950k.
function formatCompact(amount: number): string {
  const abs = Math.abs(amount)
  if (abs >= 1_000_000) return `${NAIRA}${(amount / 1_000_000).toFixed(1)}m`
  if (abs >= 1_000) return `${NAIRA}${Math.round(amount / 1_000)}k`
  return formatNaira(amount)
}

export default function Dashboard() {
  const [showTerms, setShowTerms] = useState(false)
  const navigate = useNavigate()

  const businessId = useMemo(() => getBusinessId(), [])

  const [pnl, setPnl] = useState<ProfitAndLoss | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Linked bank accounts drive the connection banner. `linking` covers the widget +
  // code exchange so the button can show progress and block double-clicks.
  const [banks, setBanks] = useState<LinkedBankAccount[] | null>(null)
  const [linking, setLinking] = useState(false)
  const [bankError, setBankError] = useState<string | null>(null)

  // Fixed at mount (≈ login time): 1st of the current month → today.
  const period = useMemo(() => {
    const now = new Date()
    const first = new Date(now.getFullYear(), now.getMonth(), 1)
    return {
      from: toDateParam(first),
      to: toDateParam(now),
      monthLabel: now.toLocaleDateString('en-NG', { month: 'long' }),
      rangeLabel: `${first.getDate()}–${now.toLocaleDateString('en-NG', { day: 'numeric', month: 'long' })}`,
    }
  }, [])

  useEffect(() => {
    if (!businessId) {
      setError('We could not find a business on your account.')
      setLoading(false)
      return
    }

    let cancelled = false
    setLoading(true)
    getProfitAndLoss(businessId, period.from, period.to)
      .then((data) => {
        if (!cancelled) {
          setPnl(data)
          setError(null)
        }
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiError ? err.message : 'Could not load your figures.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [businessId, period.from, period.to])

  useEffect(() => {
    if (!businessId) return
    let cancelled = false
    listBankAccounts(businessId)
      .then((data) => {
        if (!cancelled) setBanks(data)
      })
      .catch(() => {
        // Non-fatal for the page; the banner just falls back to its prompt state.
        if (!cancelled) setBanks([])
      })
    return () => {
      cancelled = true
    }
  }, [businessId])

  // Opens the Mono widget; on a successful link, exchanges the returned code for a
  // durable account via the API, then refreshes the banner from the server.
  function handleAddBank() {
    if (!businessId || linking) return
    setBankError(null)
    try {
      openMonoConnect({
        onCode: async (code) => {
          setLinking(true)
          try {
            await linkBankAccount(businessId, code)
            setBanks(await listBankAccounts(businessId))
          } catch (err) {
            setBankError(err instanceof ApiError ? err.message : 'Could not link that account.')
          } finally {
            setLinking(false)
          }
        },
      })
    } catch (err) {
      setBankError(
        err instanceof MonoNotConfiguredError ? err.message : 'Could not open the bank connection.',
      )
    }
  }

  const totalRevenue = pnl?.totalRevenue ?? 0
  const totalExpenses = pnl?.totalExpenses ?? 0
  const netProfit = pnl?.netProfit ?? 0
  const totalMovement = totalRevenue + totalExpenses
  // Guard against divide-by-zero for a brand-new business with no transactions yet.
  const inPct = totalMovement > 0 ? Math.round((totalRevenue / totalMovement) * 100) : 0
  const outPct = totalMovement > 0 ? 100 - inPct : 0
  const inShare = totalMovement > 0 ? ((totalRevenue / totalMovement) * 100).toFixed(1) : '0'

  const readySteps = [
    { label: 'Connect a bank account', go: '/dashboard' },
    { label: 'Record your transactions', go: '/transactions' },
    { label: 'Complete your business details', go: '/dashboard' },
  ]

  return (
    <AppShell
      active="dashboard"
      title="Good morning"
      kicker={period.rangeLabel}
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '30px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 30 }}>
        {/* Bank connection banner */}
        <section
          className="card blueprint"
          style={{
            position: 'relative',
            display: 'flex',
            flexDirection: 'row',
            flexWrap: 'wrap',
            gap: '10px 14px',
            alignItems: 'center',
            padding: '10px 14px',
            background: 'transparent',
          }}
        >
          {cornerMarks}
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent-700)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ flex: '0 0 16px' }}>
            <rect x="3" y="6" width="18" height="13" />
            <path d="M3 10h18" />
            <path d="M7 15h4" />
          </svg>
          <span style={{ flex: '1 1 auto', minWidth: 180, fontSize: 13.5, color: 'var(--color-neutral-700)' }}>
            {bankError ? (
              <span style={{ color: 'var(--color-danger, #b42318)' }}>{bankError}</span>
            ) : banks === null ? (
              'Checking your connected accounts…'
            ) : banks.length === 0 ? (
              'No bank account connected yet — connect one to record money automatically.'
            ) : banks.length === 1 ? (
              `${banks[0].institutionName} connected`
            ) : (
              `${banks.length} bank accounts connected`
            )}
          </span>
          <button
            onClick={handleAddBank}
            disabled={linking || !businessId}
            className="btn btn-ghost"
            style={{ flex: '0 0 auto', fontSize: 13, opacity: linking ? 0.7 : 1 }}
          >
            {linking ? 'Connecting…' : 'Add a bank account'}
          </button>
        </section>

        <section style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))', gap: 26, alignItems: 'stretch' }}>
          {/* Profit card */}
          <div className="card blueprint" style={{ position: 'relative', padding: '26px 26px 24px 26px' }}>
            {cornerMarks}
            <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 16, marginBottom: 22 }}>
              <h2 style={{ ...heading, fontSize: 23 }}>Are you making money this month?</h2>
              <span style={{ fontSize: 12.5, color: 'var(--color-neutral-600)' }}>{period.rangeLabel}</span>
            </div>

            {error ? (
              <p role="alert" style={{ margin: 0, fontSize: 14.5, lineHeight: 1.5, color: 'var(--color-danger, #b42318)' }}>
                {error}
              </p>
            ) : loading ? (
              <p style={{ margin: 0, fontSize: 14.5, color: 'var(--color-neutral-600)' }}>Loading your figures…</p>
            ) : (
              <>
                <div style={{ display: 'flex', alignItems: 'baseline', gap: 14, marginBottom: 4 }}>
                  <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 52, lineHeight: 1, color: 'var(--color-accent-800)' }}>
                    {formatNaira(netProfit)}
                  </span>
                </div>
                <p style={{ margin: '0 0 24px 0', fontSize: 14.5, color: 'var(--color-neutral-700)' }}>
                  Money left over after everything you spent.
                  {showTerms && <span style={{ color: 'var(--color-neutral-500)' }}>&nbsp;&middot; Net profit</span>}
                </p>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 22, marginBottom: 26 }}>
                  <div style={{ borderLeft: '2px solid var(--color-accent)', paddingLeft: 13 }}>
                    <div style={{ ...miniLabel, marginBottom: 5 }}>
                      Money in
                      {showTerms && <span style={termHint}>&nbsp;(revenue)</span>}
                    </div>
                    <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 27 }}>{formatNaira(totalRevenue)}</div>
                  </div>
                  <div style={{ borderLeft: '2px solid var(--color-neutral-400)', paddingLeft: 13 }}>
                    <div style={{ ...miniLabel, marginBottom: 5 }}>
                      Money out
                      {showTerms && <span style={termHint}>&nbsp;(expenses)</span>}
                    </div>
                    <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 27 }}>{formatNaira(totalExpenses)}</div>
                  </div>
                </div>

                {/* Money in vs money out — split donut */}
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 28, alignItems: 'center', paddingTop: 4 }}>
                  <div
                    style={{
                      position: 'relative',
                      width: 168,
                      height: 168,
                      flex: '0 0 168px',
                      borderRadius: '50%',
                      background: `conic-gradient(var(--color-accent) 0 ${inShare}%, var(--color-neutral-300) ${inShare}% 100%)`,
                    }}
                  >
                    <div
                      style={{
                        position: 'absolute',
                        inset: 34,
                        borderRadius: '50%',
                        background: 'var(--color-bg)',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: 1,
                      }}
                    >
                      <span style={{ fontSize: 10.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>{period.monthLabel}</span>
                      <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 26, lineHeight: 1, color: 'var(--color-accent-800)' }}>
                        {formatCompact(totalMovement)}
                      </span>
                      <span style={{ fontSize: 11, color: 'var(--color-neutral-600)' }}>total movement</span>
                    </div>
                  </div>
                  <div style={{ flex: '1 1 200px', minWidth: 190, display: 'flex', flexDirection: 'column', gap: 14 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 11 }}>
                      <span style={{ width: 12, height: 12, flex: 'none', background: 'var(--color-accent)' }} />
                      <span style={{ flex: '1 1 auto', fontSize: 14.5 }}>Money in</span>
                      <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 20 }}>{inPct}%</span>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 11 }}>
                      <span style={{ width: 12, height: 12, flex: 'none', background: 'var(--color-neutral-300)' }} />
                      <span style={{ flex: '1 1 auto', fontSize: 14.5 }}>Money out</span>
                      <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 20 }}>{outPct}%</span>
                    </div>
                    <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5, color: 'var(--color-neutral-700)' }}>
                      {totalMovement > 0 ? (
                        <>
                          Of every {NAIRA}100 that moved through the business this month, {NAIRA}
                          {inPct} came in and {NAIRA}
                          {outPct} went out.
                        </>
                      ) : (
                        <>No money has moved through the business yet this month. Connect a bank account to start your records.</>
                      )}
                    </p>
                  </div>
                </div>
              </>
            )}
          </div>

          {/* Credit readiness card */}
          <div className="card blueprint" style={{ position: 'relative', padding: 26, display: 'flex', flexDirection: 'column' }}>
            {cornerMarks}
            <div style={{ fontSize: 12, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 10 }}>
              Credit readiness
            </div>
            <h2 style={{ ...heading, fontSize: 23, margin: '0 0 6px 0' }}>Three things make you loan-ready</h2>
            <p style={{ margin: '0 0 18px 0', fontSize: 14, color: 'var(--color-neutral-700)' }}>
              Lenders want to see clean, complete records. Do these and you have them.
            </p>

            <div style={{ display: 'flex', flexDirection: 'column', marginBottom: 22 }}>
              {readySteps.map((s, i) => (
                <button
                  key={s.label}
                  onClick={() => navigate(s.go)}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 12,
                    width: '100%',
                    textAlign: 'left',
                    fontFamily: 'var(--font-body)',
                    background: 'none',
                    border: 0,
                    borderTop: '1px solid var(--color-divider)',
                    padding: '13px 4px',
                    cursor: 'pointer',
                  }}
                >
                  <span
                    style={{
                      fontFamily: 'var(--font-heading)',
                      fontSize: 13,
                      width: 22,
                      height: 22,
                      flex: '0 0 22px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      border: '1px solid var(--color-accent)',
                      color: 'var(--color-accent-800)',
                    }}
                  >
                    {i + 1}
                  </span>
                  <span style={{ flex: '1 1 auto', minWidth: 0, fontSize: 15 }}>{s.label}</span>
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="var(--color-neutral-600)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ flex: '0 0 15px' }}>
                    <path d="m9 18 6-6-6-6" />
                  </svg>
                </button>
              ))}
            </div>

            <button onClick={() => navigate('/credit')} className="btn btn-secondary btn-block" style={{ marginTop: 'auto', padding: '16px 20px', fontSize: 17 }}>
              See what lenders will see
            </button>
          </div>
        </section>

        {/* Review nudge */}
        <section
          style={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: '16px 22px',
            alignItems: 'center',
            border: '1px solid var(--color-accent)',
            background: 'var(--color-accent-100)',
            padding: '18px 22px',
          }}
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent-700)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" style={{ flex: '0 0 20px' }}>
            <path d="M10.3 21a1.94 1.94 0 0 0 3.4 0" />
            <path d="M18 8a6 6 0 0 0-12 0c0 7-3 8-3 8h18s-3-1-3-8" />
          </svg>
          <div style={{ flex: '1 1 320px', minWidth: 240 }}>
            <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 20, marginBottom: 3 }}>
              {REVIEW_COUNT} transactions are waiting for you
            </div>
            <p style={{ margin: 0, fontSize: 14, lineHeight: 1.5, color: 'var(--color-neutral-700)' }}>
              Your bank feed brought them in automatically. vbook just needs to know what each one was for &mdash; about 6 minutes of work.
              {showTerms && <span style={{ color: 'var(--color-neutral-500)' }}>&nbsp;&middot; Uncategorised and unreconciled entries</span>}
            </p>
          </div>
          <button onClick={() => navigate('/transactions')} className="btn btn-primary blueprint" style={{ position: 'relative', flex: '0 0 auto' }}>
            {cornerMarks}
            Go to Transactions
          </button>
        </section>
      </div>
    </AppShell>
  )
}

const heading: CSSProperties = {
  fontFamily: 'var(--font-heading)',
  fontWeight: 600,
  margin: 0,
}

const miniLabel: CSSProperties = {
  fontSize: 12,
  letterSpacing: '0.1em',
  textTransform: 'uppercase',
  color: 'var(--color-neutral-600)',
}

const termHint: CSSProperties = {
  textTransform: 'none',
  letterSpacing: 0,
  color: 'var(--color-neutral-500)',
}
