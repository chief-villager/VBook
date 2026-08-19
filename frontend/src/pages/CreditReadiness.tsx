// Credit Readiness — React port of the "credit" section of "Vbook.dc.html", wired to
// the live API. Two calls back the page:
//   - the dashboard endpoint fills the "How far you have come" journey stats;
//   - the evaluation endpoint (5Cs) fills the readiness card once the user says they
//     are preparing for a loan and picks a period.
//
// No score is invented client-side: the two bars come straight from the API's
// recordKeepingScore / observable-strength values, and each factor row shows the
// API's own band, description and suggested action. Visuals come from
// src/styles/industry.css.

import { useEffect, useMemo, useState } from 'react'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import {
  evaluateCreditReadiness,
  factorName,
  getCreditReadinessDashboard,
  ratingLabel,
  type CreditReadinessDashboard,
  type FiveCsRating,
} from '../lib/creditReadiness'

// Local-date yyyy-MM-dd (the API binds these to DateOnly), computed in local time so
// "today" matches the user's calendar.
function toDateParam(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function monthsAgo(n: number): Date {
  const d = new Date()
  d.setMonth(d.getMonth() - n)
  return d
}

// "2026-08-15" -> "15 Aug 2026", parsed by hand so the day never shifts across a timezone.
function formatDate(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number)
  if (!y || !m || !d) return iso
  return new Date(y, m - 1, d).toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: 'numeric' })
}

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

type Period = '3' | '6' | 'custom'

// The evaluation returns a single-element list; the page reads the first entry.
type Evaluation = FiveCsRating

export default function CreditReadiness() {
  const [showTerms, setShowTerms] = useState(false)
  const businessId = useMemo(() => getBusinessId(), [])
  const today = useMemo(() => toDateParam(new Date()), [])

  // "How far you have come" — loaded once against a wide window so the journey reflects
  // the whole trading history, not just the loan period the user picks below.
  const [dashboard, setDashboard] = useState<CreditReadinessDashboard | null>(null)
  const [dashError, setDashError] = useState<string | null>(null)

  // Loan-prep flow.
  const [loanPrep, setLoanPrep] = useState<'yes' | 'no' | null>(null)
  const [period, setPeriod] = useState<Period>('3')
  const [customFrom, setCustomFrom] = useState('')
  const [customTo, setCustomTo] = useState(today)

  const [evaluation, setEvaluation] = useState<Evaluation | null>(null)
  const [evalLoading, setEvalLoading] = useState(false)
  const [evalError, setEvalError] = useState<string | null>(null)

  // The window the evaluation runs over. Null when a custom range isn't filled in yet.
  const window = useMemo<{ from: string; to: string } | null>(() => {
    if (period === 'custom') {
      if (!customFrom || !customTo) return null
      return { from: customFrom, to: customTo }
    }
    return { from: toDateParam(monthsAgo(Number(period))), to: today }
  }, [period, customFrom, customTo, today])

  useEffect(() => {
    if (!businessId) {
      setDashError('We could not find a business on your account.')
      return
    }
    // Wide window (last 5 years) so the journey totals capture everything recorded.
    const from = toDateParam(monthsAgo(60))
    getCreditReadinessDashboard(businessId, from, today)
      .then((d) => {
        setDashboard(d)
        setDashError(null)
      })
      .catch((err) => setDashError(err instanceof ApiError ? err.message : 'Could not load your record summary.'))
  }, [businessId, today])

  useEffect(() => {
    if (loanPrep !== 'yes' || !businessId || !window) {
      setEvaluation(null)
      return
    }
    let cancelled = false
    setEvalLoading(true)
    setEvalError(null)
    evaluateCreditReadiness(businessId, window.from, window.to)
      .then((list) => {
        if (cancelled) return
        setEvaluation(list[0] ?? null)
      })
      .catch((err) => {
        if (cancelled) return
        setEvaluation(null)
        setEvalError(err instanceof ApiError ? err.message : 'Could not assess your records for this period.')
      })
      .finally(() => {
        if (!cancelled) setEvalLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [loanPrep, businessId, window])

  // The four journey stats: three from the API, plus the three IFRS statements the
  // records make available (always ready once there's activity).
  const journey = dashboard
    ? [
        { value: String(dashboard.monthsOfHistory), label: 'Months of history recorded' },
        { value: dashboard.numberOfTransactions.toLocaleString('en-NG'), label: 'Transactions recorded' },
        { value: String(dashboard.numberOfInvoices), label: 'Invoices issued from vbook' },
        { value: '3', label: 'Statements ready to share' },
      ]
    : []

  const completeness = evaluation ? clampPct(evaluation.recordKeepingScore) : 0
  const strength = evaluation ? clampPct(evaluation.obeservableStrengthScore) : 0

  return (
    <AppShell
      active="credit"
      title="Credit Readiness"
      kicker="Getting loan-ready"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '30px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 30 }}>
        {/* How far you have come */}
        <section
          className="card blueprint"
          style={{ position: 'relative', padding: '26px 28px', display: 'flex', flexDirection: 'column', gap: 16 }}
        >
          {cornerMarks}
          <div style={{ fontSize: 11.5, letterSpacing: '0.16em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>
            How far you have come
          </div>
          {dashError ? (
            <ErrorLine message={dashError} />
          ) : !dashboard ? (
            <p style={{ margin: 0, fontSize: 14.5, color: 'var(--color-neutral-700)' }}>Loading your record summary…</p>
          ) : (
            <>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(140px, 1fr))', gap: '18px 14px' }}>
                {journey.map((j) => (
                  <div key={j.label}>
                    <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 30, lineHeight: 1, color: 'var(--color-accent-800)' }}>
                      {j.value}
                    </div>
                    <div style={{ fontSize: 13, lineHeight: 1.35, color: 'var(--color-neutral-700)', marginTop: 5 }}>{j.label}</div>
                  </div>
                ))}
              </div>
              <div style={{ marginTop: 4, borderTop: '1px solid var(--color-divider)', paddingTop: 14, fontSize: 13.5, lineHeight: 1.5, color: 'var(--color-accent-800)' }}>
                Every record you keep is something a lender can read without you preparing anything.
              </div>
            </>
          )}
        </section>

        {/* Are you preparing for a loan? */}
        <section
          className="card blueprint"
          style={{ position: 'relative', padding: '26px 28px', display: 'flex', flexDirection: 'column', gap: 18 }}
        >
          {cornerMarks}
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 18, alignItems: 'center', justifyContent: 'space-between' }}>
            <div>
              <h2 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 24, margin: '0 0 5px 0' }}>
                Are you preparing for a loan?
              </h2>
              <p style={{ margin: 0, fontSize: 14, color: 'var(--color-neutral-700)' }}>
                Tell us and we will assess how a lender may read your records for the period.
              </p>
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <ToggleButton label="Yes, I am" active={loanPrep === 'yes'} onClick={() => setLoanPrep('yes')} />
              <ToggleButton label="Not right now" active={loanPrep === 'no'} onClick={() => setLoanPrep('no')} />
            </div>
          </div>

          {loanPrep === 'yes' && (
            <div style={{ borderTop: '1px solid var(--color-divider)', paddingTop: 18, display: 'flex', flexDirection: 'column', gap: 16 }}>
              {/* Period picker */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'flex-end' }}>
                <div className="field" style={{ flex: '0 0 230px' }}>
                  <label htmlFor="creditWindow">Period to check</label>
                  <select
                    id="creditWindow"
                    className="input"
                    value={period}
                    onChange={(e) => setPeriod(e.target.value as Period)}
                    style={{ fontSize: 14 }}
                  >
                    <option value="3">Last 3 months</option>
                    <option value="6">Last 6 months</option>
                    <option value="custom">From a date I choose</option>
                  </select>
                </div>
                {period === 'custom' && (
                  <>
                    <div className="field" style={{ flex: '0 0 200px' }}>
                      <label htmlFor="creditFrom">Records from</label>
                      <input id="creditFrom" className="input" type="date" value={customFrom} onChange={(e) => setCustomFrom(e.target.value)} style={{ fontSize: 14 }} />
                    </div>
                    <div className="field" style={{ flex: '0 0 200px' }}>
                      <label htmlFor="creditTo">Records to</label>
                      <input id="creditTo" className="input" type="date" value={customTo} onChange={(e) => setCustomTo(e.target.value)} style={{ fontSize: 14 }} />
                    </div>
                  </>
                )}
              </div>

              {/* Readiness card */}
              <div className="card blueprint" style={{ position: 'relative', padding: '22px 24px', display: 'flex', flexDirection: 'column', gap: 0 }}>
                {cornerMarks}
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center', justifyContent: 'space-between', marginBottom: 10 }}>
                  <span style={{ fontSize: 11.5, letterSpacing: '0.16em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>
                    Credit readiness
                  </span>
                  {window && (
                    <span style={{ fontSize: 12.5, color: 'var(--color-neutral-600)' }}>
                      Based on records to {formatDate(window.to)}
                    </span>
                  )}
                </div>

                {period === 'custom' && !window ? (
                  <p style={{ margin: 0, fontSize: 14, color: 'var(--color-neutral-700)' }}>
                    Choose both a start and an end date to run the assessment.
                  </p>
                ) : evalError ? (
                  <ErrorLine message={evalError} />
                ) : evalLoading || !evaluation ? (
                  <p style={{ margin: 0, fontSize: 14, color: 'var(--color-neutral-700)' }}>Assessing your records…</p>
                ) : (
                  <>
                    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'baseline' }}>
                      <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 30, lineHeight: 1.1 }}>
                        {verdict(strength)}
                      </span>
                      <span className="tag tag-accent" style={{ fontSize: 12.5 }}>
                        Observable strength {strength}%
                      </span>
                    </div>
                    <div style={{ fontSize: 13.5, color: 'var(--color-neutral-700)', margin: '4px 0 20px 0' }}>
                      A guide from what you have recorded — not a lender's decision or an offer.
                    </div>

                    {/* Two bars */}
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 14 }}>
                      <MeterCard label="Record completeness" pct={completeness} note="how complete your books are" color="var(--color-accent)" />
                      <MeterCard label="Observable strength" pct={strength} note="what your figures show" color="var(--color-accent-600)" />
                    </div>

                    <div style={{ display: 'flex', alignItems: 'flex-start', gap: 8, fontSize: 12.5, lineHeight: 1.5, color: 'var(--color-neutral-600)', padding: '14px 0 16px 0' }}>
                      <span>Strength is only meaningful once your records are complete enough to judge. Where they are not, we say so rather than guess.</span>
                    </div>

                    {/* Factor list */}
                    <div style={{ borderTop: '1px solid var(--color-divider)', paddingTop: 16 }}>
                      <div style={{ fontSize: 11.5, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 14 }}>
                        What a lender reads — and your next move on each
                      </div>
                      <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                        {evaluation.ratings.map((r) => (
                          <div key={r.factor} style={{ display: 'flex', alignItems: 'flex-start', gap: 13 }}>
                            <div
                              style={{
                                width: 34,
                                height: 34,
                                flex: '0 0 34px',
                                border: `1px ${r.rating === 0 ? 'dashed' : 'solid'} ${r.rating === 0 ? 'var(--color-neutral-400)' : 'var(--color-accent)'}`,
                                background: r.rating === 0 ? 'transparent' : 'var(--color-accent-100)',
                                color: r.rating === 0 ? 'var(--color-neutral-600)' : 'var(--color-accent-800)',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                fontFamily: 'var(--font-heading)',
                                fontSize: 15,
                              }}
                            >
                              {factorName(r.factor).charAt(0)}
                            </div>
                            <div style={{ flex: '1 1 0', minWidth: 0 }}>
                              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, justifyContent: 'space-between', alignItems: 'baseline' }}>
                                <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 16 }}>{factorName(r.factor)}</span>
                                <span style={{ fontSize: 11.5, letterSpacing: '0.1em', textTransform: 'uppercase', color: r.rating === 0 ? 'var(--color-neutral-600)' : 'var(--color-accent-800)' }}>
                                  {ratingLabel(r.rating)}
                                </span>
                              </div>
                              <div style={{ fontSize: 13.5, lineHeight: 1.5, color: 'var(--color-neutral-700)' }}>{r.description}</div>
                              {r.suggestedAction && (
                                <div style={{ fontSize: 13, lineHeight: 1.5, color: 'var(--color-accent-800)', marginTop: 3 }}>
                                  Next: {r.suggestedAction}
                                </div>
                              )}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>

                    <div style={{ marginTop: 18, border: '1px solid var(--color-divider)', padding: '12px 14px', fontSize: 12.5, lineHeight: 1.55, color: 'var(--color-neutral-700)' }}>
                      Based on what you have recorded, this is how a lender may view your business today. It is not an approval or an offer.
                    </div>
                  </>
                )}
              </div>
            </div>
          )}
        </section>
      </div>
    </AppShell>
  )
}

// Parses a numeric-string score and clamps it into 0–100 for use as a percentage.
function clampPct(score: string): number {
  const n = Number(score)
  if (!Number.isFinite(n)) return 0
  return Math.max(0, Math.min(100, Math.round(n)))
}

function verdict(strength: number): string {
  if (strength >= 75) return 'Loan-ready signals'
  if (strength >= 50) return 'Getting there'
  if (strength >= 25) return 'Early days'
  return 'Not enough yet'
}

function MeterCard({ label, pct, note, color }: { label: string; pct: number; note: string; color: string }) {
  return (
    <div style={{ border: '1px solid var(--color-divider)', padding: '14px 16px' }}>
      <div style={{ fontSize: 12, letterSpacing: '0.08em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 10 }}>
        {label}
      </div>
      <div style={{ height: 6, background: 'var(--color-neutral-300)', marginBottom: 10 }}>
        <div style={{ height: 6, background: color, width: `${pct}%` }} />
      </div>
      <div style={{ fontSize: 13.5, color: 'var(--color-neutral-700)' }}>
        <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 15, color: 'var(--color-text)' }}>{pct}%</span> — {note}
      </div>
    </div>
  )
}

function ToggleButton({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '11px 20px',
        fontFamily: 'var(--font-body)',
        fontSize: 14.5,
        cursor: 'pointer',
        border: `1px solid ${active ? 'var(--color-accent-700)' : 'var(--color-neutral-400)'}`,
        background: active ? 'var(--color-accent-700)' : 'transparent',
        color: active ? '#ffffff' : 'var(--color-text)',
      }}
    >
      {label}
    </button>
  )
}

function ErrorLine({ message }: { message: string }) {
  return (
    <p style={{ margin: 0, fontSize: 13.5, color: '#b3261e' }} role="alert">
      {message}
    </p>
  )
}
