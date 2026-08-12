// Transactions — React port of the "transactions" section of "Vbook.dc.html".
// Two tabs: "Categorise" (the bank-feed review queue — pick a category per row and
// approve it into the ledger) and "Find a transaction" (a date-filtered list of
// everything, with running money-in/out/net totals).
//
// Visuals come from src/styles/industry.css (blueprint cards, corner marks, tokens).
// Data is the prototype's own mock set so the page renders standalone; the API
// wiring is marked with TODOs against the real bank-import + transactions endpoints:
//   GET   /api/businesses/{businessId}/bank-imports?status=Pending   (list to categorise)
//   PATCH /api/businesses/{businessId}/bank-imports/{stagedId}       (set category)
//   POST  /api/businesses/{businessId}/bank-imports/{stagedId}/approve
//   (find tab) the recorded-transactions list endpoint

import { useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import AppShell from '../components/AppShell.tsx'

// Reference categories — mirrors transactions.categories seed data.
const CATEGORIES = [
  'Sales',
  'Other income',
  'Purchases',
  'Rent',
  'Transport',
  'Utilities',
  'Wages',
  'Owner investment',
  'Bank loan',
  'Other Expenses',
]

interface Tx {
  id: string
  date: string
  who: string
  note: string
  amount: number
  category: string | null
}

// Mock feed — positive = money in, negative = money out.
const TX: Tx[] = [
  { id: 't1', date: '27 Aug', who: 'Zenith Bank ••4471', note: 'Transfer received', amount: 180000, category: null },
  { id: 't2', date: '26 Aug', who: 'Total Filling Station', note: 'Card payment, Lekki', amount: -42500, category: null },
  { id: 't3', date: '24 Aug', who: 'Lekki Retail Ltd', note: 'GTBank transfer', amount: 1250000, category: null },
  { id: 't4', date: '23 Aug', who: 'Sahara Foods Ltd', note: 'Transfer received', amount: 640000, category: 'Sales' },
  { id: 't5', date: '22 Aug', who: 'Chidi Nwosu', note: 'August salary', amount: -320000, category: 'Wages' },
  { id: 't6', date: '21 Aug', who: 'Ikeja Auto Spares', note: 'Card payment', amount: -87400, category: null },
  { id: 't7', date: '20 Aug', who: 'Bright Star Ventures', note: 'Transfer received', amount: 415000, category: 'Sales' },
  { id: 't8', date: '19 Aug', who: 'GTBank', note: 'Monthly maintenance fee', amount: -1075, category: 'Other Expenses' },
  { id: 't9', date: '18 Aug', who: 'Ojota Warehouse Ltd', note: 'Standing order', amount: -450000, category: 'Rent' },
  { id: 't10', date: '17 Aug', who: 'MTN Data', note: 'Card payment', amount: -15000, category: null },
  { id: 't11', date: '15 Aug', who: 'Kano Traders Co', note: 'Transfer received', amount: 890000, category: 'Sales' },
  { id: 't12', date: '14 Aug', who: 'Dangote Cement Depot', note: 'Card payment', amount: -268000, category: 'Purchases' },
  { id: 't13', date: '13 Aug', who: 'Uche Enterprises', note: 'Transfer received', amount: 505000, category: null },
  { id: 't14', date: '12 Aug', who: 'Palm Grove Salon', note: 'Transfer received', amount: 300000, category: 'Sales' },
  { id: 't15', date: '12 Aug', who: 'Mobil Filling Station', note: 'Card payment, Ojota', amount: -96000, category: null },
  { id: 't16', date: '11 Aug', who: 'Dangote Cement Depot', note: 'Transfer sent', amount: -620000, category: null },
  { id: 't17', date: '10 Aug', who: 'Ngozi Adeyemi', note: 'August salary', amount: -285000, category: 'Wages' },
  { id: 't18', date: '9 Aug', who: 'Ojota Fuel Depot', note: 'Generator diesel', amount: -140000, category: null },
  { id: 't19', date: '8 Aug', who: 'AXA Mansard', note: 'Vehicle cover, quarterly', amount: -210000, category: null },
  { id: 't20', date: '7 Aug', who: 'GTBank', note: 'POS terminal charges', amount: -25025, category: 'Other Expenses' },
  { id: 't21', date: '6 Aug', who: 'Swift Haulage', note: 'Transfer sent', amount: -175000, category: null },
  { id: 't22', date: '5 Aug', who: 'Airtel', note: 'Airtime and data', amount: -40000, category: null },
  { id: 't23', date: '4 Aug', who: 'Sterling Bank', note: 'Monthly loan repayment', amount: -160000, category: 'Bank loan' },
]

const NAIRA = '₦'
const MINUS = '−'

// "+₦1,200" / "−₦900" — signed, for the amount column.
const naira = (n: number) => (n > 0 ? '+' : MINUS) + NAIRA + Math.abs(n).toLocaleString('en-NG')
// "₦1,200" / "−₦900" — for totals (no leading + on positives).
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(n).toLocaleString('en-NG')

// "27 Aug" (year assumed 2026, matching the prototype) -> Date, for range filtering.
const MONTHS: Record<string, number> = {
  Jan: 0, Feb: 1, Mar: 2, Apr: 3, May: 4, Jun: 5, Jul: 6, Aug: 7, Sep: 8, Oct: 9, Nov: 10, Dec: 11,
}
const dayOf = (label: string) => {
  const [d, mon] = label.split(' ')
  return new Date(2026, MONTHS[mon] ?? 0, Number(d))
}

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

type Tab = 'categorise' | 'find'

export default function Transactions() {
  const [showTerms, setShowTerms] = useState(false)
  const [tab, setTab] = useState<Tab>('categorise')

  // Per-row category overrides, keyed by transaction id. A row leaves the
  // "categorise" list once it has a category.
  const [cats, setCats] = useState<Record<string, string | null>>({})
  const [fFrom, setFFrom] = useState('')
  const [fTo, setFTo] = useState('')

  const withCats = useMemo(
    () => TX.map((t) => ({ ...t, category: t.id in cats ? cats[t.id] : t.category })),
    [cats],
  )

  const reviewCount = withCats.filter((t) => !t.category).length
  const moneyIn = withCats.filter((t) => t.amount > 0).reduce((a, t) => a + t.amount, 0)
  const moneyOut = withCats.filter((t) => t.amount < 0).reduce((a, t) => a + t.amount, 0)

  const uncategorised = withCats.filter((t) => !t.category)

  const filteredSource = withCats.filter((t) => {
    const d = dayOf(t.date)
    if (fFrom && d < new Date(fFrom)) return false
    if (fTo && d > new Date(fTo)) return false
    return true
  })
  const filteredIn = filteredSource.filter((t) => t.amount > 0).reduce((a, t) => a + t.amount, 0)
  const filteredOut = filteredSource.filter((t) => t.amount < 0).reduce((a, t) => a + t.amount, 0)

  const setCat = (id: string, value: string) =>
    // TODO: PATCH .../bank-imports/{id} with the chosen CategoryId.
    setCats((prev) => ({ ...prev, [id]: value || null }))

  const approve = (id: string) =>
    // TODO: POST .../bank-imports/{id}/approve — on success the row leaves the queue.
    // For now, treat approval as "categorised" so it drops off the review list only
    // if it already has a category (the button mirrors the design's red CTA).
    setCats((prev) => ({ ...prev, [id]: prev[id] ?? TX.find((t) => t.id === id)?.category ?? null }))

  return (
    <AppShell
      active="transactions"
      title="Transactions"
      kicker="Money in and out"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '26px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 22 }}>
        {/* Summary cards */}
        <section style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 18 }}>
          <SummaryCard label="Money in" value={NAIRA + moneyIn.toLocaleString('en-NG')} accent />
          <SummaryCard label="Money out" value={NAIRA + Math.abs(moneyOut).toLocaleString('en-NG')} />
          <SummaryCard
            label={<>Left over{showTerms && <span style={termHint}>&nbsp;(net)</span>}</>}
            value={NAIRA + (moneyIn + moneyOut).toLocaleString('en-NG')}
            accent
          />
          <SummaryCard label="Needs review" value={String(reviewCount)} />
        </section>

        {/* Tabs */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <TabButton label="Categorise" active={tab === 'categorise'} onClick={() => setTab('categorise')} />
          <TabButton label="Find a transaction" active={tab === 'find'} onClick={() => setTab('find')} />
        </div>

        {tab === 'categorise' && (
          <section style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'baseline', justifyContent: 'space-between' }}>
              <h2 style={sectionHeading}>Categorise these</h2>
              <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>
                {reviewCount} waiting &middot; about 6 minutes of work
              </span>
            </div>
            <p style={{ margin: 0, fontSize: 14.5, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '66ch' }}>
              Your bank feed brought these in automatically. Tell vbook what each one was for and it disappears from this list.
            </p>

            {uncategorised.length > 0 ? (
              <div className="card blueprint" style={{ position: 'relative', padding: 0 }}>
                {cornerMarks}
                <div style={{ overflowX: 'auto' }}>
                  <TableHead columns={['Date', 'Description', 'Category', 'Amount', 'Approve']} />
                  {uncategorised.map((t) => (
                    <div key={t.id} style={{ ...rowStyle, background: 'var(--color-neutral-100)' }}>
                      <span style={dateCell}>{t.date}</span>
                      <DescriptionCell who={t.who} note={t.note} needsReview />
                      <select
                        className="input"
                        value={(t.id in cats ? cats[t.id] : t.category) ?? ''}
                        onChange={(e) => setCat(t.id, e.target.value)}
                        style={{ flex: '0 0 180px', minWidth: 0, fontSize: 13.5 }}
                      >
                        <option value="">Choose a category</option>
                        {CATEGORIES.map((c) => (
                          <option key={c} value={c}>
                            {c}
                          </option>
                        ))}
                      </select>
                      <span style={{ ...amountCell, color: t.amount > 0 ? 'var(--color-accent-800)' : 'var(--color-text)' }}>
                        {naira(t.amount)}
                      </span>
                      <span style={{ flex: '0 0 110px', minWidth: 0, textAlign: 'right' }}>
                        <button onClick={() => approve(t.id)} style={approveBtn}>
                          Approve
                        </button>
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <EmptyCard message="Nothing left to categorise. Every transaction is explained." />
            )}
          </section>
        )}

        {tab === 'find' && (
          <section style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'baseline', justifyContent: 'space-between' }}>
              <h2 style={sectionHeading}>Find a transaction</h2>
              <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>
                {filteredSource.length === 1 ? '1 transaction' : `${filteredSource.length} transactions`} found
              </span>
            </div>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14, alignItems: 'flex-end' }}>
              <div className="field" style={{ flex: '0 1 200px' }}>
                <label htmlFor="tx-from">From</label>
                <input className="input" id="tx-from" type="date" value={fFrom} onChange={(e) => setFFrom(e.target.value)} />
              </div>
              <div className="field" style={{ flex: '0 1 200px' }}>
                <label htmlFor="tx-to">To</label>
                <input className="input" id="tx-to" type="date" value={fTo} onChange={(e) => setFTo(e.target.value)} />
              </div>
              <button
                onClick={() => {
                  setFFrom('')
                  setFTo('')
                }}
                className="btn btn-ghost"
                style={{ fontSize: 13, marginBottom: 2 }}
              >
                Clear
              </button>
            </div>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 26, padding: '14px 20px', border: '1px solid var(--color-divider)' }}>
              <MiniTotal label="Money in" value={amt(filteredIn)} accent />
              <MiniTotal label="Money out" value={amt(Math.abs(filteredOut))} />
              <MiniTotal label="Left over" value={amt(filteredIn + filteredOut)} accent />
            </div>

            {filteredSource.length > 0 ? (
              <div className="card blueprint" style={{ position: 'relative', padding: 0 }}>
                {cornerMarks}
                <div style={{ overflowX: 'auto' }}>
                  <TableHead columns={['Date', 'Description', 'Category', 'Amount']} />
                  {filteredSource.map((t) => (
                    <div key={t.id} style={{ ...rowStyle, background: !t.category ? 'var(--color-neutral-100)' : 'transparent' }}>
                      <span style={dateCell}>{t.date}</span>
                      <DescriptionCell who={t.who} note={t.note} needsReview={!t.category} />
                      <span
                        style={{
                          flex: '0 0 180px',
                          minWidth: 0,
                          fontSize: 14,
                          color: 'var(--color-neutral-700)',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                        }}
                      >
                        {t.category ?? ''}
                      </span>
                      <span style={{ ...amountCell, color: t.amount > 0 ? 'var(--color-accent-800)' : 'var(--color-text)' }}>
                        {naira(t.amount)}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <EmptyCard message="Nothing matches those filters. Try widening them." />
            )}
          </section>
        )}
      </div>
    </AppShell>
  )
}

// ---- small presentational helpers ----

function SummaryCard({ label, value, accent }: { label: ReactNode; value: string; accent?: boolean }) {
  return (
    <div className="card blueprint" style={{ position: 'relative', padding: '18px 20px' }}>
      {cornerMarks}
      <div style={{ fontSize: 11.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 7 }}>
        {label}
      </div>
      <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 29, color: accent ? 'var(--color-accent-800)' : undefined }}>
        {value}
      </div>
    </div>
  )
}

function TabButton({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '9px 16px',
        fontFamily: 'var(--font-body)',
        fontSize: 14,
        cursor: 'pointer',
        border: `1px solid ${active ? 'var(--color-accent)' : 'var(--color-divider)'}`,
        background: active ? 'var(--color-accent-100)' : 'transparent',
        color: active ? 'var(--color-accent-800)' : 'var(--color-neutral-700)',
      }}
    >
      {label}
    </button>
  )
}

function TableHead({ columns }: { columns: string[] }) {
  // Column widths mirror the row cells below; the last column is right-aligned.
  const widths: CSSProperties[] = [
    { width: 62, flex: '0 0 62px' },
    { flex: '1 1 160px', minWidth: 0 },
    { flex: '0 0 180px' },
    { flex: '0 0 120px', textAlign: 'right' },
    { flex: '0 0 110px', textAlign: 'right' },
  ]
  return (
    <div
      style={{
        display: 'flex',
        flexWrap: 'nowrap',
        gap: 16,
        alignItems: 'center',
        padding: '12px 20px',
        borderBottom: '1px solid var(--color-divider)',
        fontSize: 11.5,
        letterSpacing: '0.12em',
        textTransform: 'uppercase',
        color: 'var(--color-neutral-600)',
        minWidth: 800,
      }}
    >
      {columns.map((c, i) => (
        <span key={c} style={widths[i]}>
          {c}
        </span>
      ))}
    </div>
  )
}

function DescriptionCell({ who, note, needsReview }: { who: string; note: string; needsReview: boolean }) {
  return (
    <span style={{ flex: '1 1 160px', minWidth: 0, display: 'flex', flexDirection: 'column', gap: 2 }}>
      <span style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
        <span style={{ fontSize: 14.5 }}>{who}</span>
        {needsReview && (
          <span className="tag tag-outline" style={{ fontSize: 11 }}>
            Needs review
          </span>
        )}
      </span>
      <span style={{ fontSize: 12.5, color: 'var(--color-neutral-600)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {note}
      </span>
    </span>
  )
}

function MiniTotal({ label, value, accent }: { label: string; value: string; accent?: boolean }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      <span style={{ fontSize: 11.5, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>{label}</span>
      <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 21, color: accent ? 'var(--color-accent-800)' : undefined }}>
        {value}
      </span>
    </div>
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

// ---- shared style constants ----

const sectionHeading: CSSProperties = {
  fontFamily: 'var(--font-heading)',
  fontWeight: 600,
  fontSize: 23,
  margin: 0,
}

const termHint: CSSProperties = {
  textTransform: 'none',
  letterSpacing: 0,
  color: 'var(--color-neutral-500)',
}

const rowStyle: CSSProperties = {
  display: 'flex',
  flexWrap: 'nowrap',
  gap: 16,
  alignItems: 'center',
  padding: '13px 20px',
  borderBottom: '1px solid var(--color-divider)',
  minWidth: 800,
}

const dateCell: CSSProperties = {
  width: 62,
  flex: '0 0 62px',
  fontSize: 13,
  color: 'var(--color-neutral-600)',
}

const amountCell: CSSProperties = {
  flex: '0 0 120px',
  textAlign: 'right',
  whiteSpace: 'nowrap',
  fontFamily: 'var(--font-heading)',
  fontWeight: 600,
  fontSize: 19,
}

const approveBtn: CSSProperties = {
  padding: '7px 16px',
  fontFamily: 'var(--font-body)',
  fontSize: 13,
  color: '#ffffff',
  background: '#b3261e',
  border: '1px solid #b3261e',
  cursor: 'pointer',
  whiteSpace: 'nowrap',
}
