// Reports — React port of the "reports" section of "Vbook.dc.html", wired to the live
// Reporting endpoints. The three IFRS-for-SMEs statements — Profit and Loss, Balance
// Sheet, Cash Flow — are chosen from the sidebar's Reports submenu (the active one
// comes off the /reports/:tab route). Each is a plain-words summary over a
// single-column statement table, with a date range (or "as at" date) and an
// "Export as PDF" action that streams the server-rendered document.
//
// The period reports (P&L, cash flow) default to this month to date — the first of the
// current month through today, in local time so "today" matches the user's calendar.
// The page owns that default (and the pickers that change it); the API stays explicit.
// The balance sheet defaults its "as at" date to today. Visuals come from
// src/styles/industry.css.

import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import { useParams } from 'react-router-dom'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import {
  downloadReportPdf,
  getBalanceSheet,
  getCashFlow,
  getProfitAndLoss,
  ReportType,
  type BalanceSheet,
  type CashFlowStatement,
  type ProfitAndLoss,
  type ReportTypeValue,
} from '../lib/reports'

const NAIRA = '₦'
const MINUS = '−'

// "₦1,200" / "−₦900".
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(Math.round(n)).toLocaleString('en-NG')

// Local-date yyyy-MM-dd (the API binds these to DateOnly). Local, not UTC, so "today"
// and "first of the month" match the user's calendar.
function toDateParam(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

// "2026-08-15" -> "15 Aug 2026". Parse the parts by hand so the calendar day never
// shifts across a timezone boundary (new Date("yyyy-MM-dd") is parsed as UTC).
function formatDate(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number)
  if (!y || !m || !d) return iso
  return new Date(y, m - 1, d).toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: 'numeric' })
}

// This month to date: first of the current month through today.
function monthToDate() {
  const now = new Date()
  return {
    from: toDateParam(new Date(now.getFullYear(), now.getMonth(), 1)),
    to: toDateParam(now),
  }
}

// Route tab -> report metadata. `key` matches the /reports/:tab segment.
const TABS: Record<string, { key: string; tab: string; term: string; type: ReportTypeValue }> = {
  pl: { key: 'pl', tab: 'Profit and Loss', term: 'Statement of profit or loss — IFRS for SMEs', type: ReportType.ProfitAndLoss },
  bs: { key: 'bs', tab: 'Balance Sheet', term: 'Statement of financial position — IFRS for SMEs', type: ReportType.BalanceSheet },
  cf: { key: 'cf', tab: 'Cash Flow', term: 'Statement of cash flows — IFRS for SMEs', type: ReportType.CashFlow },
}

// The shape the table renders — the same for all three statements.
interface ViewSection {
  head: string
  term: string
  rows: { l: string; v: number }[]
  total: { l: string; v: number }
}
interface ViewModel {
  summary: string
  sub: string
  sections: ViewSection[]
  bottom: { l: string; v: number }
  opening?: { l: string; v: number }
}

function buildProfitAndLoss(r: ProfitAndLoss): ViewModel {
  const margin = r.totalRevenue > 0 ? Math.round((r.netProfit / r.totalRevenue) * 100) : 0
  return {
    summary: r.netProfit >= 0 ? `You made a profit of ${amt(r.netProfit)}.` : `You made a loss of ${amt(-r.netProfit)}.`,
    sub:
      r.totalRevenue > 0
        ? `For every ₦100 that came in, ${amt(margin)} stayed with the business.`
        : 'Nothing has come in over this period yet.',
    sections: [
      {
        head: 'Money you earned',
        term: 'Revenue',
        rows: r.revenue.map((i) => ({ l: i.accountName, v: i.amount })),
        total: { l: 'Total money earned', v: r.totalRevenue },
      },
      {
        head: 'Money you spent',
        term: 'Expenses',
        rows: r.expenses.map((i) => ({ l: i.accountName, v: i.amount })),
        total: { l: 'Total money spent', v: r.totalExpenses },
      },
    ],
    bottom: { l: 'Profit for the period', v: r.netProfit },
  }
}

function buildBalanceSheet(r: BalanceSheet): ViewModel {
  const netWorth = r.totalAssets - r.totalLiabilities
  return {
    summary: `The business is worth ${amt(netWorth)} to you today.`,
    sub: 'That is what would be left if you sold everything you own and paid off everything you owe.',
    sections: [
      {
        head: 'What you own',
        term: 'Assets',
        rows: r.assets.map((i) => ({ l: i.accountName, v: i.amount })),
        total: { l: 'Total you own', v: r.totalAssets },
      },
      {
        head: 'What you owe',
        term: 'Liabilities',
        rows: r.liabilities.map((i) => ({ l: i.accountName, v: i.amount })),
        total: { l: 'Total you owe', v: r.totalLiabilities },
      },
      {
        head: 'What the business is worth to you',
        term: 'Equity',
        rows: r.equity.map((i) => ({ l: i.accountName, v: i.amount })),
        total: { l: 'Total worth to you', v: r.totalEquity },
      },
    ],
    bottom: { l: 'What you own, less what you owe', v: netWorth },
  }
}

function buildCashFlow(r: CashFlowStatement): ViewModel {
  return {
    summary: `You ended the period with ${amt(r.closingCash)} in the bank.`,
    sub:
      r.netChange >= 0
        ? `That is ${amt(r.netChange)} more than you started with.`
        : `That is ${amt(-r.netChange)} less than you started with.`,
    sections: [
      { head: 'Change over the period', term: 'Net change', rows: [], total: { l: 'Net change', v: r.netChange } },
    ],
    bottom: { l: 'Cash in the bank at the end', v: r.closingCash },
    opening: { l: 'Cash in the bank at the start', v: r.openingCash },
  }
}

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

export default function Reports() {
  const [showTerms, setShowTerms] = useState(false)
  const { tab } = useParams<{ tab: string }>()
  const meta = tab && tab in TABS ? TABS[tab] : TABS.pl
  const isBalanceSheet = meta.key === 'bs'

  const businessId = useMemo(() => getBusinessId(), [])

  // The period reports share one from/to; the balance sheet uses asOf. All default to
  // this month / today, owned here rather than by the API.
  const initial = useMemo(monthToDate, [])
  const [from, setFrom] = useState(initial.from)
  const [to, setTo] = useState(initial.to)
  const [asOf, setAsOf] = useState(initial.to)

  const [view, setView] = useState<ViewModel | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)

  useEffect(() => {
    if (!businessId) {
      setError('We could not find a business on your account.')
      setLoading(false)
      return
    }
    let cancelled = false
    setLoading(true)
    setExportError(null)

    const request =
      meta.key === 'pl'
        ? getProfitAndLoss(businessId, from, to).then(buildProfitAndLoss)
        : meta.key === 'bs'
          ? getBalanceSheet(businessId, asOf).then(buildBalanceSheet)
          : getCashFlow(businessId, from, to).then(buildCashFlow)

    request
      .then((vm) => {
        if (!cancelled) {
          setView(vm)
          setError(null)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setView(null)
          setError(err instanceof ApiError ? err.message : 'Could not load this report.')
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [businessId, meta.key, from, to, asOf])

  function exportPdf() {
    if (!businessId || exporting) return
    setExportError(null)
    setExporting(true)
    const range = isBalanceSheet ? { asOf } : { from, to }
    const stamp = isBalanceSheet ? asOf : `${from}-to-${to}`
    downloadReportPdf(businessId, meta.type, range, `${meta.key}-${stamp}`)
      .catch((err) => setExportError(err instanceof ApiError ? err.message : 'Could not export the PDF.'))
      .finally(() => setExporting(false))
  }

  const periodLabel = isBalanceSheet ? `As at ${formatDate(asOf)}` : `${formatDate(from)} – ${formatDate(to)}`

  return (
    <AppShell
      active="reports"
      title="Reports"
      kicker="How the business is doing"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '26px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 20 }}>
        {/* Statement heading + date controls + export */}
        <section style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'center', justifyContent: 'space-between' }}>
          <div>
            <div style={{ fontSize: 12, letterSpacing: '0.1em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 4 }}>Reports</div>
            <h1 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 30, lineHeight: 1.1, margin: 0 }}>{meta.tab}</h1>
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
            {isBalanceSheet ? (
              <DateBox label="As at" value={asOf} onChange={setAsOf} />
            ) : (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                <DateBox label="From" value={from} onChange={setFrom} />
                <DateBox label="To" value={to} onChange={setTo} />
              </div>
            )}
            <button
              onClick={exportPdf}
              disabled={exporting || loading || !view}
              className="btn btn-primary blueprint"
              style={{ position: 'relative', opacity: exporting || loading || !view ? 0.6 : 1 }}
            >
              {cornerMarks}
              {exporting ? 'Preparing…' : 'Export as PDF'}
            </button>
          </div>
        </section>

        {exportError && <ErrorLine message={exportError} />}
        {error && <ErrorLine message={error} />}

        {loading && !view ? (
          <EmptyCard message="Loading this report…" />
        ) : view ? (
          <>
            {/* Plain-words summary */}
            <section className="card blueprint" style={{ position: 'relative', padding: '24px 26px', opacity: loading ? 0.6 : 1 }}>
              {cornerMarks}
              <h2 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 28, lineHeight: 1.15, margin: '0 0 8px 0' }}>{view.summary}</h2>
              <p style={{ margin: 0, fontSize: 15, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '68ch' }}>{view.sub}</p>
              {showTerms && <p style={{ margin: '10px 0 0 0', fontSize: 13, color: 'var(--color-neutral-500)' }}>{meta.term}</p>}
            </section>

            {/* Statement table */}
            <div className="card blueprint" style={{ position: 'relative', padding: 0, opacity: loading ? 0.6 : 1 }}>
              {cornerMarks}
              <div style={{ overflowX: 'auto' }}>
                <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, padding: '12px 24px', borderBottom: '1px solid var(--color-divider)', fontSize: 11.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', minWidth: 560 }}>
                  <span style={{ flex: '1 1 220px', minWidth: 0 }}>Line</span>
                  <span style={{ flex: '0 0 160px', textAlign: 'right' }}>{periodLabel}</span>
                </div>

                {view.sections.map((sec) => (
                  <div key={sec.head} style={{ minWidth: 560 }}>
                    <div style={{ display: 'flex', alignItems: 'baseline', gap: 10, padding: '18px 24px 8px 24px' }}>
                      <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 19 }}>{sec.head}</span>
                      {showTerms && <span style={{ fontSize: 12.5, color: 'var(--color-neutral-500)' }}>{sec.term}</span>}
                    </div>
                    {sec.rows.length === 0 && (
                      <div style={{ padding: '9px 24px', borderBottom: '1px solid var(--color-divider)', fontSize: 14, color: 'var(--color-neutral-500)' }}>
                        Nothing to show for this period.
                      </div>
                    )}
                    {sec.rows.map((r) => (
                      <div key={r.l} style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'baseline', padding: '9px 24px', borderBottom: '1px solid var(--color-divider)' }}>
                        <span style={{ flex: '1 1 220px', minWidth: 0, fontSize: 15 }}>{r.l}</span>
                        <span style={{ flex: '0 0 160px', textAlign: 'right', fontSize: 15.5 }}>{amt(r.v)}</span>
                      </div>
                    ))}
                    <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'baseline', padding: '12px 24px', background: 'var(--color-neutral-100)', borderBottom: '1px solid var(--color-divider)' }}>
                      <span style={{ flex: '1 1 220px', minWidth: 0, fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 17 }}>{sec.total.l}</span>
                      <span style={{ flex: '0 0 160px', textAlign: 'right', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 20 }}>{amt(sec.total.v)}</span>
                    </div>
                  </div>
                ))}

                {view.opening && (
                  <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'baseline', padding: '12px 24px', borderBottom: '1px solid var(--color-divider)', minWidth: 560, color: 'var(--color-neutral-700)' }}>
                    <span style={{ flex: '1 1 220px', minWidth: 0, fontSize: 15 }}>{view.opening.l}</span>
                    <span style={{ flex: '0 0 160px', textAlign: 'right', fontSize: 15.5 }}>{amt(view.opening.v)}</span>
                  </div>
                )}

                <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'baseline', padding: '18px 24px', minWidth: 560, background: 'var(--color-accent-100)' }}>
                  <span style={{ flex: '1 1 220px', minWidth: 0, fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 21, color: 'var(--color-accent-800)' }}>{view.bottom.l}</span>
                  <span style={{ flex: '0 0 160px', textAlign: 'right', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 26, color: 'var(--color-accent-800)' }}>{amt(view.bottom.v)}</span>
                </div>
              </div>
            </div>

            <p style={{ margin: 0, fontSize: 13, color: 'var(--color-neutral-600)', maxWidth: '74ch' }}>
              These statements follow IFRS for SMEs, so a lender or an accountant will recognise them. Export as PDF to
              share or file a copy.
            </p>
          </>
        ) : null}
      </div>
    </AppShell>
  )
}

// A blueprint-framed date field with a small uppercase label above the picker.
function DateBox({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }): ReactNode {
  return (
    <label className="blueprint" style={{ position: 'relative', display: 'flex', flexDirection: 'column', gap: 3, padding: '7px 11px', border: '1px solid var(--color-divider)', background: 'transparent' }}>
      {cornerMarks}
      <span style={{ fontSize: 10.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>{label}</span>
      <input type="date" value={value} onChange={(e) => onChange(e.target.value)} style={dateInput} />
    </label>
  )
}

function EmptyCard({ message }: { message: string }) {
  return (
    <div className="card blueprint" style={{ position: 'relative', padding: 26, textAlign: 'center' }}>
      {cornerMarks}
      <p style={{ margin: 0, fontSize: 15, color: 'var(--color-neutral-700)' }}>{message}</p>
    </div>
  )
}

function ErrorLine({ message }: { message: string }) {
  return (
    <p style={{ margin: 0, fontSize: 13.5, color: '#b3261e' }} role="alert">
      {message}
    </p>
  )
}

const dateInput: CSSProperties = {
  border: 0,
  background: 'transparent',
  fontFamily: 'var(--font-body)',
  fontSize: 14,
  color: 'var(--color-text)',
  padding: 0,
  outline: 'none',
  cursor: 'pointer',
}
