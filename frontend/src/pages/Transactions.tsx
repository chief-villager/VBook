// Transactions — React port of the "transactions" section of "Vbook.dc.html", wired
// to the live API. Tabs come first, then:
//   "Categorise" — the bank-feed review queue. When nothing is staged, a "Pull
//     transactions from bank" card fetches a linked account's feed
//     (POST .../bank-imports). Once rows are staged, a pulled bar + four summary
//     cards + the review table appear; each row takes a category
//     (PATCH .../bank-imports/{id}) and is approved into the ledger
//     (POST .../bank-imports/{id}/approve).
//   "Find a transaction" — the recorded-transactions list over a date range
//     (GET .../transactions?from&to), paged, with running money-in/out/net totals.
//
// Visuals come from src/styles/industry.css (blueprint cards, corner marks, tokens).

import { useEffect, useMemo, useState, type CSSProperties, type ReactNode } from 'react'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import { listBankAccounts, type LinkedBankAccount } from '../lib/bankAccounts'
import {
  approveImport,
  categoriseImport,
  getCategories,
  isOutflow,
  listStagedImports,
  listTransactions,
  pullBankImports,
  TransactionType,
  type Category,
  type Paged,
  type StagedTransaction,
  type TransactionSummary,
} from '../lib/transactions'

const NAIRA = '₦'
const MINUS = '−'
const FIND_PAGE_SIZE = 50
// The review queue is usually small; pull a generous first page rather than paging it.
const QUEUE_PAGE_SIZE = 100
// How far back the "Pull transactions from bank" action reaches.
const PULL_WINDOW_DAYS = 90

// "+₦1,200" / "−₦900" — signed, for the amount column. `out` decides direction since
// amounts are stored as positive magnitudes.
const naira = (magnitude: number, out: boolean) =>
  (out ? MINUS : '+') + NAIRA + Math.abs(magnitude).toLocaleString('en-NG')
// "₦1,200" — for the totals cards (magnitude only; the label carries direction).
const money = (n: number) => NAIRA + Math.abs(n).toLocaleString('en-NG')
// "₦1,200" / "−₦900" — for the net figure, which can be negative.
const signed = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(n).toLocaleString('en-NG')

// Local-date yyyy-MM-dd (the API binds these to DateOnly). Local, not UTC, so "today"
// matches the user's calendar day.
function toDateParam(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

// "2026-08-27" -> "27 Aug". Parse the parts by hand so the calendar day never shifts
// across a timezone boundary (new Date("yyyy-MM-dd") is parsed as UTC).
function formatDay(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number)
  if (!y || !m || !d) return iso
  return new Date(y, m - 1, d).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

const TYPE_LABELS: Record<number, string> = {
  [TransactionType.Income]: 'Income',
  [TransactionType.Expense]: 'Expense',
  [TransactionType.Capital]: 'Capital',
  [TransactionType.Loan]: 'Loan',
}

// The current-month range used as the Find tab's default and its Clear target.
function defaultRange() {
  const now = new Date()
  return {
    from: toDateParam(new Date(now.getFullYear(), now.getMonth(), 1)),
    to: toDateParam(now),
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

type Tab = 'categorise' | 'find'

export default function Transactions() {
  const [showTerms, setShowTerms] = useState(false)
  const [tab, setTab] = useState<Tab>('categorise')

  const businessId = useMemo(() => getBusinessId(), [])

  // Shared reference categories for the categorise dropdown.
  const [categories, setCategories] = useState<Category[]>([])

  // Categorise tab — the pending review queue and the bank behind it.
  const [queue, setQueue] = useState<StagedTransaction[] | null>(null)
  const [queueError, setQueueError] = useState<string | null>(null)
  const [selected, setSelected] = useState<Record<string, string>>({})
  const [rowBusy, setRowBusy] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)

  const [banks, setBanks] = useState<LinkedBankAccount[]>([])
  const [pulling, setPulling] = useState(false)
  const [pullError, setPullError] = useState<string | null>(null)

  // Find tab — recorded transactions over a date range.
  const initialRange = useMemo(defaultRange, [])
  const [fFrom, setFFrom] = useState(initialRange.from)
  const [fTo, setFTo] = useState(initialRange.to)
  const [page, setPage] = useState(1)
  const [txns, setTxns] = useState<Paged<TransactionSummary> | null>(null)
  const [txLoading, setTxLoading] = useState(true)
  const [txError, setTxError] = useState<string | null>(null)

  // Categories once, up front (the dropdown needs their ids).
  useEffect(() => {
    if (!businessId) return
    let cancelled = false
    getCategories(businessId)
      .then((data) => !cancelled && setCategories(data))
      .catch(() => !cancelled && setCategories([]))
    return () => {
      cancelled = true
    }
  }, [businessId])

  // Linked bank accounts drive the pull action.
  useEffect(() => {
    if (!businessId) return
    let cancelled = false
    listBankAccounts(businessId)
      .then((data) => !cancelled && setBanks(data))
      .catch(() => !cancelled && setBanks([]))
    return () => {
      cancelled = true
    }
  }, [businessId])

  // The pending review queue.
  function loadQueue(signal?: { cancelled: boolean }) {
    if (!businessId) return
    listStagedImports(businessId, 1, QUEUE_PAGE_SIZE)
      .then((data) => {
        if (signal?.cancelled) return
        setQueue(data.items)
        setSelected((prev) => {
          const next = { ...prev }
          for (const row of data.items) if (row.categoryId) next[row.id.value] ??= row.categoryId.value
          return next
        })
        setQueueError(null)
      })
      .catch((err) => {
        if (!signal?.cancelled)
          setQueueError(err instanceof ApiError ? err.message : 'Could not load the review queue.')
      })
  }

  useEffect(() => {
    const signal = { cancelled: false }
    loadQueue(signal)
    return () => {
      signal.cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [businessId])

  // Recorded transactions for the selected range/page.
  useEffect(() => {
    if (!businessId) {
      setTxError('We could not find a business on your account.')
      setTxLoading(false)
      return
    }
    let cancelled = false
    setTxLoading(true)
    listTransactions(businessId, fFrom, fTo, page, FIND_PAGE_SIZE)
      .then((data) => {
        if (!cancelled) {
          setTxns(data)
          setTxError(null)
        }
      })
      .catch((err) => {
        if (!cancelled) setTxError(err instanceof ApiError ? err.message : 'Could not load your transactions.')
      })
      .finally(() => {
        if (!cancelled) setTxLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [businessId, fFrom, fTo, page])

  // Pull a linked account's feed into the review queue, then reload it.
  function pull() {
    if (!businessId || pulling) return
    setPullError(null)
    const account = banks.find((b) => b.status === 0) ?? banks[0]
    if (!account) {
      setPullError('Connect a bank account on the dashboard before pulling transactions.')
      return
    }
    const now = new Date()
    const from = toDateParam(new Date(now.getTime() - PULL_WINDOW_DAYS * 86_400_000))
    setPulling(true)
    pullBankImports(businessId, account.externalAccountId, from, toDateParam(now))
      .then(() => loadQueue())
      .catch((err) => setPullError(err instanceof ApiError ? err.message : 'Could not pull from your bank.'))
      .finally(() => setPulling(false))
  }

  // Assign a category to a staged row (stays Pending until approved).
  function chooseCategory(stagedId: string, categoryId: string) {
    setSelected((prev) => ({ ...prev, [stagedId]: categoryId }))
    setActionError(null)
    if (!categoryId || !businessId) return
    categoriseImport(businessId, stagedId, categoryId).catch((err) =>
      setActionError(err instanceof ApiError ? err.message : 'Could not save that category.'),
    )
  }

  // Promote a categorised row into the ledger; on success it leaves the queue.
  function approve(stagedId: string) {
    if (!businessId || !selected[stagedId] || rowBusy) return
    setRowBusy(stagedId)
    setActionError(null)
    approveImport(businessId, stagedId)
      .then(() => setQueue((prev) => (prev ?? []).filter((r) => r.id.value !== stagedId)))
      .catch((err) => setActionError(err instanceof ApiError ? err.message : 'Could not approve that transaction.'))
      .finally(() => setRowBusy(null))
  }

  const bankLabel = (banks.find((b) => b.status === 0) ?? banks[0])?.institutionName ?? 'your bank'
  const reviewCount = queue?.length ?? 0
  const hasQueue = (queue?.length ?? 0) > 0

  // Summary cards reflect the pulled feed (the pending queue).
  const queueIn = (queue ?? []).filter((r) => !isOutflow(r.suggestedType)).reduce((a, r) => a + r.amount, 0)
  const queueOut = (queue ?? []).filter((r) => isOutflow(r.suggestedType)).reduce((a, r) => a + r.amount, 0)

  // Find tab totals reflect the loaded page for the current range.
  const items = txns?.items ?? []
  const findIn = items.filter((t) => !isOutflow(t.type)).reduce((a, t) => a + t.amount, 0)
  const findOut = items.filter((t) => isOutflow(t.type)).reduce((a, t) => a + t.amount, 0)
  const totalPages = txns ? Math.max(1, Math.ceil(txns.totalCount / txns.pageSize)) : 1

  return (
    <AppShell
      active="transactions"
      title="Transactions"
      kicker="Money in and out"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '26px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 22 }}>
        {/* Tabs */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          <TabButton label="Categorise" active={tab === 'categorise'} onClick={() => setTab('categorise')} />
          <TabButton label="Find a transaction" active={tab === 'find'} onClick={() => setTab('find')} />
        </div>

        {tab === 'categorise' && (
          <>
            {pullError && <ErrorLine message={pullError} />}
            {queueError && <ErrorLine message={queueError} />}

            {queue === null ? (
              <EmptyCard message="Loading the review queue…" />
            ) : hasQueue ? (
              <>
                {/* Pulled bar */}
                <div
                  style={{
                    display: 'flex',
                    flexWrap: 'wrap',
                    gap: 12,
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    border: '1px solid var(--color-divider)',
                    padding: '12px 18px',
                  }}
                >
                  <span style={{ fontSize: 13.5, color: 'var(--color-neutral-700)' }}>
                    Transactions pulled from {bankLabel} &middot; up to date as of today
                  </span>
                  <button onClick={pull} disabled={pulling} className="btn btn-ghost" style={{ fontSize: 13.5 }}>
                    {pulling ? 'Pulling…' : 'Pull again'}
                  </button>
                </div>

                {/* Summary cards */}
                <section style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 18 }}>
                  <SummaryCard label="Money in" value={money(queueIn)} accent />
                  <SummaryCard label="Money out" value={money(queueOut)} />
                  <SummaryCard
                    label={<>Left over{showTerms && <span style={termHint}>&nbsp;(net)</span>}</>}
                    value={signed(queueIn - queueOut)}
                    accent
                  />
                  <SummaryCard label="Needs review" value={String(reviewCount)} />
                </section>

                {/* Categorise table */}
                <section style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                  <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'baseline', justifyContent: 'space-between' }}>
                    <h2 style={sectionHeading}>Categorise these</h2>
                    <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>{reviewCount} waiting</span>
                  </div>
                  <p style={{ margin: 0, fontSize: 14.5, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '66ch' }}>
                    Your bank feed brought these in automatically. Tell vbook what each one was for, then approve it into
                    your books.
                  </p>

                  {actionError && <ErrorLine message={actionError} />}

                  <div className="card blueprint" style={{ position: 'relative', padding: 0 }}>
                    {cornerMarks}
                    <div style={{ overflowX: 'auto' }}>
                      <TableHead columns={['Date', 'Description', 'Category', 'Amount', 'Approve']} />
                      {queue.map((t) => {
                        const chosen = selected[t.id.value] ?? ''
                        const out = isOutflow(t.suggestedType)
                        return (
                          <div key={t.id.value} style={{ ...rowStyle, background: chosen ? 'transparent' : 'var(--color-neutral-100)' }}>
                            <span style={dateCell}>{formatDay(t.occurredOn)}</span>
                            <DescriptionCell who={t.narration} note={t.providerCategory ?? 'Bank feed'} needsReview={!chosen} />
                            <select
                              className="input"
                              value={chosen}
                              onChange={(e) => chooseCategory(t.id.value, e.target.value)}
                              style={{ flex: '0 0 180px', minWidth: 0, fontSize: 13.5 }}
                            >
                              <option value="">Choose a category</option>
                              {categories.map((c) => (
                                <option key={c.id.value} value={c.id.value}>
                                  {c.name}
                                </option>
                              ))}
                            </select>
                            <span style={{ ...amountCell, color: out ? 'var(--color-text)' : 'var(--color-accent-800)' }}>
                              {naira(t.amount, out)}
                            </span>
                            <span style={{ flex: '0 0 110px', minWidth: 0, textAlign: 'right' }}>
                              <button
                                onClick={() => approve(t.id.value)}
                                disabled={!chosen || rowBusy === t.id.value}
                                style={{ ...approveBtn, opacity: !chosen || rowBusy === t.id.value ? 0.5 : 1 }}
                              >
                                {rowBusy === t.id.value ? 'Approving…' : 'Approve'}
                              </button>
                            </span>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                </section>
              </>
            ) : (
              /* Nothing staged — pull from the bank. */
              <section className="card blueprint" style={{ position: 'relative', padding: '40px 34px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16, textAlign: 'center' }}>
                {cornerMarks}
                <svg width="34" height="34" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent)" strokeWidth={1.5} strokeLinecap="round" strokeLinejoin="round">
                  <path d="M3 21h18" />
                  <path d="M5 21V10l7-5 7 5v11" />
                  <path d="M9 21v-6h6v6" />
                </svg>
                <h2 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 26, margin: 0 }}>Nothing to categorise</h2>
                <p style={{ margin: 0, maxWidth: '52ch', fontSize: 14.5, lineHeight: 1.55, color: 'var(--color-neutral-700)' }}>
                  Bring in everything that went through your bank account. vbook reads the entries and lists the ones that
                  still need a category.
                </p>
                <button onClick={pull} disabled={pulling} className="btn btn-primary blueprint" style={{ position: 'relative', marginTop: 6, padding: '14px 26px', fontSize: 16, opacity: pulling ? 0.6 : 1 }}>
                  {cornerMarks}
                  {pulling ? 'Pulling transactions…' : 'Pull transactions from bank'}
                </button>
                <span style={{ fontSize: 12.5, color: 'var(--color-neutral-600)' }}>
                  {banks.length > 0 ? `${bankLabel} · ${banks.length} account${banks.length === 1 ? '' : 's'} connected` : 'No bank connected yet'}
                </span>
              </section>
            )}
          </>
        )}

        {tab === 'find' && (
          <section style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'baseline', justifyContent: 'space-between' }}>
              <h2 style={sectionHeading}>Find a transaction</h2>
              <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>
                {txns ? (txns.totalCount === 1 ? '1 transaction' : `${txns.totalCount} transactions`) : '—'} found
              </span>
            </div>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14, alignItems: 'flex-end' }}>
              <div className="field" style={{ flex: '0 1 200px' }}>
                <label htmlFor="tx-from">From</label>
                <input
                  className="input"
                  id="tx-from"
                  type="date"
                  value={fFrom}
                  onChange={(e) => {
                    setPage(1)
                    setFFrom(e.target.value)
                  }}
                />
              </div>
              <div className="field" style={{ flex: '0 1 200px' }}>
                <label htmlFor="tx-to">To</label>
                <input
                  className="input"
                  id="tx-to"
                  type="date"
                  value={fTo}
                  onChange={(e) => {
                    setPage(1)
                    setFTo(e.target.value)
                  }}
                />
              </div>
              <button
                onClick={() => {
                  const r = defaultRange()
                  setPage(1)
                  setFFrom(r.from)
                  setFTo(r.to)
                }}
                className="btn btn-ghost"
                style={{ fontSize: 13, marginBottom: 2 }}
              >
                Clear
              </button>
            </div>

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 26, padding: '14px 20px', border: '1px solid var(--color-divider)' }}>
              <MiniTotal label="Money in" value={money(findIn)} accent />
              <MiniTotal label="Money out" value={money(findOut)} />
              <MiniTotal label="Left over" value={signed(findIn - findOut)} accent />
            </div>

            {txError && <ErrorLine message={txError} />}

            {txLoading && !txns ? (
              <EmptyCard message="Loading your transactions…" />
            ) : items.length > 0 ? (
              <>
                <div className="card blueprint" style={{ position: 'relative', padding: 0, opacity: txLoading ? 0.6 : 1 }}>
                  {cornerMarks}
                  <div style={{ overflowX: 'auto' }}>
                    <TableHead columns={['Date', 'Category', 'Type', 'Amount']} />
                    {items.map((t) => {
                      const out = isOutflow(t.type)
                      return (
                        <div key={t.id.value} style={{ ...rowStyle, opacity: t.isVoided ? 0.5 : 1 }}>
                          <span style={dateCell}>{formatDay(t.occurredOn)}</span>
                          <span style={{ flex: '1 1 160px', minWidth: 0, fontSize: 14.5, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {t.category}
                            {t.isVoided && (
                              <span className="tag tag-outline" style={{ fontSize: 11, marginLeft: 8 }}>
                                Voided
                              </span>
                            )}
                          </span>
                          <span style={{ flex: '0 0 180px', minWidth: 0, fontSize: 14, color: 'var(--color-neutral-700)' }}>
                            {TYPE_LABELS[t.type] ?? '—'}
                          </span>
                          <span style={{ ...amountCell, color: out ? 'var(--color-text)' : 'var(--color-accent-800)' }}>
                            {naira(t.amount, out)}
                          </span>
                        </div>
                      )
                    })}
                  </div>
                </div>

                {totalPages > 1 && (
                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12 }}>
                    <button className="btn btn-ghost" style={{ fontSize: 13 }} disabled={page <= 1 || txLoading} onClick={() => setPage((p) => Math.max(1, p - 1))}>
                      ← Previous
                    </button>
                    <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>
                      Page {page} of {totalPages}
                    </span>
                    <button className="btn btn-ghost" style={{ fontSize: 13 }} disabled={page >= totalPages || txLoading} onClick={() => setPage((p) => p + 1)}>
                      Next →
                    </button>
                  </div>
                )}
              </>
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

function ErrorLine({ message }: { message: string }) {
  return (
    <p style={{ margin: 0, fontSize: 13.5, color: '#b3261e' }} role="alert">
      {message}
    </p>
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
