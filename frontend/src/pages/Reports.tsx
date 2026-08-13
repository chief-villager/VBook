// Reports — React port of the "reports" section of "Vbook.dc.html". The three IFRS
// for SMEs statements — Profit and Loss, Balance Sheet, Cash Flow — chosen from the
// sidebar's Reports submenu (the active one comes off the /reports/:tab route). Each
// is a plain-words summary card over a single-column statement table, with a date
// range (or "as at" date) and an Export as PDF action.
//
// Visuals come from src/styles/industry.css. Figures are the prototype's own mock
// values so the page renders standalone; TODO: source each statement from the
// Reporting endpoints (P&L / balance sheet / cash flow) and wire Export to the PDF.

import { useState, type CSSProperties, type ReactNode } from 'react'
import { useParams } from 'react-router-dom'
import AppShell from '../components/AppShell.tsx'

const NAIRA = '₦'
const MINUS = '−'

// "₦1,200" / "−₦900".
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(n).toLocaleString('en-NG')

interface Row {
  l: string
  v: number
}

interface Section {
  head: string
  term: string
  rows: Row[]
  total: { l: string; v: number }
}

interface Report {
  tab: string
  term: string
  summary: string
  sub: string
  sections: Section[]
  bottom: { l: string; v: number }
  opening?: { l: string; v: number }
}

const REPORTS: Record<string, Report> = {
  pl: {
    tab: 'Profit and Loss',
    term: 'Statement of profit or loss — IFRS for SMEs',
    summary: 'You made a profit of ₦1,245,000 in August.',
    sub: 'That is ₦265,000 more than July. For every ₦100 that came in, ₦30 stayed with the business.',
    sections: [
      { head: 'Money you earned', term: 'Revenue', rows: [], total: { l: 'Total money earned', v: 4180000 } },
      { head: 'Money you spent', term: 'Expenses', rows: [], total: { l: 'Total money spent', v: 2935000 } },
    ],
    bottom: { l: 'Profit for August', v: 1245000 },
  },
  bs: {
    tab: 'Balance Sheet',
    term: 'Statement of financial position — IFRS for SMEs',
    summary: 'The business is worth ₦6,316,600 to you today.',
    sub: 'That is what would be left if you sold everything you own and paid off everything you owe.',
    sections: [
      { head: 'What you own', term: 'Assets', rows: [], total: { l: 'Total you own', v: 10024000 } },
      { head: 'What you owe', term: 'Liabilities', rows: [], total: { l: 'Total you owe', v: 3707400 } },
      { head: 'What the business is worth to you', term: 'Equity', rows: [], total: { l: 'Total worth to you', v: 6316600 } },
    ],
    bottom: { l: 'What you own, less what you owe', v: 6316600 },
  },
  cf: {
    tab: 'Cash Flow',
    term: 'Statement of cash flows — IFRS for SMEs',
    summary: 'You ended August with ₦3,085,000 in the bank.',
    sub: '₦1,245,000 more than you started the month with. Running the business brought in cash every week.',
    sections: [
      { head: 'Cash you started with', term: 'Opening cash', rows: [], total: { l: 'Opening cash', v: 1840000 } },
      { head: 'Change over the month', term: 'Net change', rows: [], total: { l: 'Net change', v: 1245000 } },
      { head: 'Cash you ended with', term: 'Closing cash', rows: [], total: { l: 'Closing cash', v: 3085000 } },
    ],
    bottom: { l: 'Cash in the bank at 31 August', v: 3085000 },
    opening: { l: 'Cash in the bank at 1 August', v: 1840000 },
  },
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
  const key = tab && tab in REPORTS ? tab : 'pl'
  const rep = REPORTS[key]
  const isBalanceSheet = key === 'bs'

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
            <h1 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 30, lineHeight: 1.1, margin: 0 }}>{rep.tab}</h1>
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
            {isBalanceSheet ? (
              <DateBox label="As at" defaultValue="2026-08-31" />
            ) : (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                <DateBox label="From" defaultValue="2026-08-01" />
                <DateBox label="To" defaultValue="2026-08-31" />
              </div>
            )}
            {/* TODO: wire to the Reporting PDF export. */}
            <button className="btn btn-primary blueprint" style={{ position: 'relative' }}>
              {cornerMarks}
              Export as PDF
            </button>
          </div>
        </section>

        {/* Plain-words summary */}
        <section className="card blueprint" style={{ position: 'relative', padding: '24px 26px' }}>
          {cornerMarks}
          <h2 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 28, lineHeight: 1.15, margin: '0 0 8px 0' }}>{rep.summary}</h2>
          <p style={{ margin: 0, fontSize: 15, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '68ch' }}>{rep.sub}</p>
          {showTerms && <p style={{ margin: '10px 0 0 0', fontSize: 13, color: 'var(--color-neutral-500)' }}>{rep.term}</p>}
        </section>

        {/* Statement table */}
        <div className="card blueprint" style={{ position: 'relative', padding: 0 }}>
          {cornerMarks}
          <div style={{ overflowX: 'auto' }}>
            <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, padding: '12px 24px', borderBottom: '1px solid var(--color-divider)', fontSize: 11.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', minWidth: 560 }}>
              <span style={{ flex: '1 1 220px', minWidth: 0 }}>Line</span>
              <span style={{ flex: '0 0 160px', textAlign: 'right' }}>August 2026</span>
            </div>

            {rep.sections.map((sec) => (
              <div key={sec.head} style={{ minWidth: 560 }}>
                <div style={{ display: 'flex', alignItems: 'baseline', gap: 10, padding: '18px 24px 8px 24px' }}>
                  <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 19 }}>{sec.head}</span>
                  {showTerms && <span style={{ fontSize: 12.5, color: 'var(--color-neutral-500)' }}>{sec.term}</span>}
                </div>
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

            {rep.opening && (
              <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'baseline', padding: '12px 24px', borderBottom: '1px solid var(--color-divider)', minWidth: 560, color: 'var(--color-neutral-700)' }}>
                <span style={{ flex: '1 1 220px', minWidth: 0, fontSize: 15 }}>{rep.opening.l}</span>
                <span style={{ flex: '0 0 160px', textAlign: 'right', fontSize: 15.5 }}>{amt(rep.opening.v)}</span>
              </div>
            )}

            <div style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'baseline', padding: '18px 24px', minWidth: 560, background: 'var(--color-accent-100)' }}>
              <span style={{ flex: '1 1 220px', minWidth: 0, fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 21, color: 'var(--color-accent-800)' }}>{rep.bottom.l}</span>
              <span style={{ flex: '0 0 160px', textAlign: 'right', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 26, color: 'var(--color-accent-800)' }}>{amt(rep.bottom.v)}</span>
            </div>
          </div>
        </div>

        <p style={{ margin: 0, fontSize: 13, color: 'var(--color-neutral-600)', maxWidth: '74ch' }}>
          These statements follow IFRS for SMEs, so a lender or an accountant will recognise them. The percentages compare August with July.
        </p>
      </div>
    </AppShell>
  )
}

// A blueprint-framed date field with a small uppercase label above the picker.
function DateBox({ label, defaultValue }: { label: string; defaultValue: string }): ReactNode {
  return (
    <label className="blueprint" style={{ position: 'relative', display: 'flex', flexDirection: 'column', gap: 3, padding: '7px 11px', border: '1px solid var(--color-divider)', background: 'transparent' }}>
      {cornerMarks}
      <span style={{ fontSize: 10.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>{label}</span>
      <input type="date" defaultValue={defaultValue} style={dateInput} />
    </label>
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
