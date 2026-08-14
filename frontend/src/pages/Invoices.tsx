// Invoices — React port of the "invoices" section of "Vbook.dc.html", wired to the
// live API. Two tabs:
//   "New invoice" — a live builder (customer, editable line items, a 7.5% VAT toggle,
//     running totals) that POSTs to create the invoice.
//   "All invoices" — the business's invoices (GET, paged), each with a PDF download
//     and, while unpaid, a "Mark paid" action.
//
// Visuals come from src/styles/industry.css. The backend has no per-invoice email or
// note and no draft/sent states (an invoice is Pending until Paid), so those form
// fields are presentational and the actions map to create / mark-paid / download.

import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import {
  createInvoice,
  downloadInvoicePdf,
  InvoiceStatus,
  listInvoices,
  markInvoicePaid,
  type InvoiceSummary,
  type Paged,
} from '../lib/invoices'

const BUSINESS_NAME = 'Your business'
const NAIRA = '₦'
const MINUS = '−'
const VAT_RATE = 0.075
const LIST_PAGE_SIZE = 100

// "₦1,200" / "−₦900".
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(n).toLocaleString('en-NG')
const num = (s: string) => Number(s) || 0
const digitsOnly = (s: string) => s.replace(/[^0-9]/g, '')

// Local-date yyyy-MM-dd for the DateOnly API params.
function toDateParam(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

// "2026-08-28" -> "28 Aug", parsed locally so the day never shifts across a timezone.
function formatDay(iso: string): string {
  const [y, m, d] = iso.split('-').map(Number)
  if (!y || !m || !d) return iso
  return new Date(y, m - 1, d).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}

const startOfToday = () => {
  const n = new Date()
  return new Date(n.getFullYear(), n.getMonth(), n.getDate())
}

interface LineItem {
  desc: string
  qty: string
  price: string
}

// The backend has only Pending/Paid; "Overdue" is derived client-side from the due date.
type Pill = 'Paid' | 'Overdue' | 'Pending'

function pillFor(inv: InvoiceSummary): Pill {
  if (inv.status === InvoiceStatus.Paid) return 'Paid'
  const [y, m, d] = inv.dueDate.split('-').map(Number)
  if (y && new Date(y, m - 1, d) < startOfToday()) return 'Overdue'
  return 'Pending'
}

const statusStyle = (st: Pill): { bg: string; color: string; border: string } => {
  switch (st) {
    case 'Paid':
      return { bg: 'var(--color-accent-100)', color: 'var(--color-accent-800)', border: 'var(--color-accent)' }
    case 'Overdue':
      return { bg: 'transparent', color: 'var(--color-text)', border: 'var(--color-text)' }
    default: // Pending
      return { bg: 'transparent', color: 'var(--color-accent-700)', border: 'var(--color-accent-400)' }
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

type View = 'create' | 'list'

export default function Invoices() {
  const [showTerms, setShowTerms] = useState(false)
  const [view, setView] = useState<View>('create')

  const businessId = useMemo(() => getBusinessId(), [])
  const today = useMemo(() => toDateParam(new Date()), [])
  const defaultDue = useMemo(() => toDateParam(new Date(Date.now() + 14 * 86_400_000)), [])

  // New-invoice form.
  const [invoiceNumber, setInvoiceNumber] = useState('')
  const [customer, setCustomer] = useState('')
  const [email, setEmail] = useState('')
  const [due, setDue] = useState(defaultDue)
  const [note, setNote] = useState('')
  const [vat, setVat] = useState(false)
  const [items, setItems] = useState<LineItem[]>([{ desc: '', qty: '1', price: '' }])
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [formSuccess, setFormSuccess] = useState<string | null>(null)

  // All-invoices list.
  const [list, setList] = useState<Paged<InvoiceSummary> | null>(null)
  const [listLoading, setListLoading] = useState(true)
  const [listError, setListError] = useState<string | null>(null)
  const [rowBusy, setRowBusy] = useState<string | null>(null)

  const subtotal = items.reduce((s, it) => s + num(it.qty) * num(it.price), 0)
  const vatAmount = vat ? Math.round(subtotal * VAT_RATE) : 0
  const total = subtotal + vatAmount

  const setItem = (i: number, key: keyof LineItem, value: string) =>
    setItems((prev) => prev.map((it, n) => (n === i ? { ...it, [key]: key === 'desc' ? value : digitsOnly(value) } : it)))
  const addItem = () => setItems((prev) => [...prev, { desc: '', qty: '1', price: '' }])
  const removeItem = (i: number) => setItems((prev) => (prev.length > 1 ? prev.filter((_, n) => n !== i) : prev))

  function loadInvoices() {
    if (!businessId) {
      setListError('We could not find a business on your account.')
      setListLoading(false)
      return
    }
    setListLoading(true)
    listInvoices(businessId, 1, LIST_PAGE_SIZE)
      .then((data) => {
        setList(data)
        setListError(null)
      })
      .catch((err) => setListError(err instanceof ApiError ? err.message : 'Could not load your invoices.'))
      .finally(() => setListLoading(false))
  }

  useEffect(() => {
    loadInvoices()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [businessId])

  async function submitInvoice() {
    setFormError(null)
    setFormSuccess(null)
    if (!businessId) {
      setFormError('We could not find a business on your account.')
      return
    }
    const lineItems = items
      .map((it) => ({ description: it.desc.trim(), quantity: num(it.qty), unitPrice: num(it.price) }))
      .filter((li) => li.description && li.quantity > 0)

    if (!invoiceNumber.trim()) return setFormError('Give the invoice a number.')
    if (!customer.trim()) return setFormError('Add who the invoice is for.')
    if (lineItems.length === 0) return setFormError('Add at least one line with a description and quantity.')

    setSubmitting(true)
    try {
      await createInvoice(businessId, {
        invoiceNumber: invoiceNumber.trim(),
        issueDate: today,
        dueDate: due,
        billTo: customer.trim(),
        vatRate: vat ? VAT_RATE : 0,
        lineItems,
      })
      setFormSuccess(`Invoice ${invoiceNumber.trim()} created.`)
      setInvoiceNumber('')
      setCustomer('')
      setEmail('')
      setNote('')
      setVat(false)
      setItems([{ desc: '', qty: '1', price: '' }])
      loadInvoices()
      setView('list')
    } catch (err) {
      setFormError(err instanceof ApiError ? err.message : 'Could not create the invoice.')
    } finally {
      setSubmitting(false)
    }
  }

  async function markPaid(inv: InvoiceSummary) {
    if (!businessId || rowBusy) return
    setRowBusy(inv.id.value)
    setListError(null)
    try {
      await markInvoicePaid(businessId, inv.id.value)
      loadInvoices()
    } catch (err) {
      setListError(err instanceof ApiError ? err.message : 'Could not mark that invoice paid.')
    } finally {
      setRowBusy(null)
    }
  }

  async function download(inv: InvoiceSummary) {
    if (!businessId) return
    setListError(null)
    try {
      await downloadInvoicePdf(businessId, inv.id.value, inv.invoiceNumber)
    } catch (err) {
      setListError(err instanceof ApiError ? err.message : 'Could not download that PDF.')
    }
  }

  const invoices = list?.items ?? []

  return (
    <AppShell
      active="invoices"
      title="Invoices"
      kicker="What people owe you"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '26px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 22 }}>
        {/* Tabs */}
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 7 }}>
          <TabButton label="New invoice" active={view === 'create'} onClick={() => setView('create')} />
          <TabButton
            label={`All invoices${list ? ` (${list.totalCount})` : ''}`}
            active={view === 'list'}
            onClick={() => setView('list')}
          />
        </div>

        {view === 'create' && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 28, alignItems: 'flex-start' }}>
            <div style={{ flex: '1 1 100%', maxWidth: 780, display: 'flex', flexDirection: 'column', gap: 22 }}>
              {/* Your details */}
              <div>
                <div style={fieldGroupLabel}>Your details</div>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14, alignItems: 'flex-start' }}>
                  <div style={{ flex: '1 1 240px', minWidth: 200, display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, lineHeight: 1.5 }}>
                    <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 18 }}>{BUSINESS_NAME}</span>
                    <span style={{ color: 'var(--color-neutral-700)' }}>
                      Your logo and bank details come from your invoice template — set them once and every invoice uses them.
                    </span>
                  </div>
                </div>
              </div>

              {/* Who is it for */}
              <div>
                <div style={fieldGroupLabel}>Who is it for?</div>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
                  <div className="field" style={{ flex: '1 1 220px' }}>
                    <label htmlFor="in-cust">Customer name</label>
                    <input className="input" id="in-cust" type="text" value={customer} onChange={(e) => setCustomer(e.target.value)} placeholder="Lagos Freight Co" />
                  </div>
                  <div className="field" style={{ flex: '1 1 220px' }}>
                    <label htmlFor="in-mail">Their email</label>
                    <input className="input" id="in-mail" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="accounts@lagosfreight.ng" />
                  </div>
                </div>
              </div>

              {/* Line items */}
              <div>
                <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 12, marginBottom: 10 }}>
                  <span style={{ ...fieldGroupLabel, marginBottom: 0 }}>What are you charging for?</span>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {items.map((it, i) => (
                    <div key={i} style={{ display: 'flex', flexWrap: 'wrap', gap: 8, alignItems: 'center' }}>
                      <input className="input" type="text" value={it.desc} onChange={(e) => setItem(i, 'desc', e.target.value)} placeholder="What you did" style={{ flex: '1 1 200px', minWidth: 0 }} />
                      <input className="input" type="text" value={it.qty} onChange={(e) => setItem(i, 'qty', e.target.value)} placeholder="1" style={{ flex: '0 0 62px', textAlign: 'right' }} />
                      <input className="input" type="text" value={it.price} onChange={(e) => setItem(i, 'price', e.target.value)} placeholder="0" style={{ flex: '0 0 120px', textAlign: 'right' }} />
                      <span style={{ flex: '0 0 130px', textAlign: 'right', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 17 }}>
                        {amt(num(it.qty) * num(it.price))}
                      </span>
                      <button
                        onClick={() => removeItem(i)}
                        style={{ flex: '0 0 auto', background: 'none', border: 0, cursor: 'pointer', color: 'var(--color-neutral-600)', padding: 4 }}
                        aria-label="Remove line"
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                          <path d="M18 6 6 18" />
                          <path d="m6 6 12 12" />
                        </svg>
                      </button>
                    </div>
                  ))}
                </div>

                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, alignItems: 'center', justifyContent: 'space-between', marginTop: 12 }}>
                  <button onClick={addItem} className="btn btn-ghost" style={{ fontSize: 13.5, paddingLeft: 0 }}>
                    Add another line
                  </button>
                  <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13.5, color: 'var(--color-neutral-700)', cursor: 'pointer' }}>
                    <input type="checkbox" checked={vat} onChange={(e) => setVat(e.target.checked)} style={{ width: 15, height: 15, accentColor: 'var(--color-accent)' }} />
                    Add 7.5% VAT
                  </label>
                </div>

                {/* Totals */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8, margin: '18px 0 0 auto', maxWidth: 320, borderTop: '1px solid var(--color-divider)', paddingTop: 14, fontSize: 14.5 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', gap: 20 }}>
                    <span style={{ color: 'var(--color-neutral-700)' }}>Subtotal</span>
                    <span>{amt(subtotal)}</span>
                  </div>
                  {vat && (
                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 20 }}>
                      <span style={{ color: 'var(--color-neutral-700)' }}>VAT at 7.5%</span>
                      <span>{amt(vatAmount)}</span>
                    </div>
                  )}
                  <div style={{ display: 'flex', justifyContent: 'space-between', gap: 20, alignItems: 'baseline', borderTop: '1px solid var(--color-divider)', paddingTop: 10 }}>
                    <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 18 }}>Total due</span>
                    <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 28, color: 'var(--color-accent-800)' }}>{amt(total)}</span>
                  </div>
                </div>
              </div>

              {/* Number, due, note */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
                <div className="field" style={{ flex: '1 1 160px' }}>
                  <label htmlFor="in-no">Invoice number</label>
                  <input className="input" id="in-no" type="text" value={invoiceNumber} onChange={(e) => setInvoiceNumber(e.target.value)} placeholder="INV-0001" />
                </div>
                <div className="field" style={{ flex: '1 1 160px' }}>
                  <label htmlFor="in-due">Payment due by</label>
                  <input className="input" id="in-due" type="date" value={due} onChange={(e) => setDue(e.target.value)} />
                </div>
                <div className="field" style={{ flex: '1 1 240px' }}>
                  <label htmlFor="in-note">Note at the bottom</label>
                  <input className="input" id="in-note" type="text" value={note} onChange={(e) => setNote(e.target.value)} placeholder="Thank you for your business." />
                </div>
              </div>

              {formError && <ErrorLine message={formError} />}
              {formSuccess && (
                <p style={{ margin: 0, fontSize: 13.5, color: 'var(--color-accent-800)' }} role="status">
                  {formSuccess}
                </p>
              )}

              {/* Actions */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center', borderTop: '1px solid var(--color-divider)', paddingTop: 18 }}>
                <button onClick={submitInvoice} disabled={submitting} className="btn btn-primary blueprint" style={{ position: 'relative', opacity: submitting ? 0.6 : 1 }}>
                  {cornerMarks}
                  {submitting ? 'Creating…' : 'Create invoice'}
                </button>
                <span style={{ fontSize: 12.5, color: 'var(--color-neutral-600)' }}>
                  Once created, download its PDF from “All invoices”.
                </span>
              </div>
            </div>
          </div>
        )}

        {view === 'list' && (
          <>
            {listError && <ErrorLine message={listError} />}
            {listLoading && !list ? (
              <EmptyCard message="Loading your invoices…" />
            ) : invoices.length > 0 ? (
              <div className="card blueprint" style={{ position: 'relative', padding: 0, opacity: listLoading ? 0.6 : 1 }}>
                {cornerMarks}
                <div style={{ overflowX: 'auto' }}>
                  <div style={{ ...listRow, ...listHead, minWidth: 900 }}>
                    <span style={{ flex: '0 0 90px' }}>Number</span>
                    <span style={{ flex: '1 1 180px', minWidth: 0 }}>Customer</span>
                    <span style={{ flex: '0 0 90px' }}>Issued</span>
                    <span style={{ flex: '0 0 90px' }}>Due</span>
                    <span style={{ flex: '0 0 100px' }}>Status</span>
                    <span style={{ flex: '0 0 120px', textAlign: 'right' }}>Amount</span>
                    <span style={{ flex: '0 0 190px', textAlign: 'right' }}>Actions</span>
                  </div>
                  {invoices.map((inv) => {
                    const pill = pillFor(inv)
                    const s = statusStyle(pill)
                    return (
                      <div key={inv.id.value} style={{ ...listRow, minWidth: 900 }}>
                        <span style={{ flex: '0 0 90px', fontSize: 13.5, color: 'var(--color-neutral-600)' }}>{inv.invoiceNumber}</span>
                        <span style={{ flex: '1 1 180px', minWidth: 0, fontSize: 15, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{inv.billTo}</span>
                        <span style={{ flex: '0 0 90px', fontSize: 13.5, color: 'var(--color-neutral-600)' }}>{formatDay(inv.issueDate)}</span>
                        <span style={{ flex: '0 0 90px', fontSize: 13.5, color: 'var(--color-neutral-600)' }}>{formatDay(inv.dueDate)}</span>
                        <span style={{ flex: '0 0 100px' }}>
                          <span style={{ display: 'inline-block', padding: '3px 9px', fontSize: 12, border: `1px solid ${s.border}`, background: s.bg, color: s.color }}>{pill}</span>
                        </span>
                        <span style={{ flex: '0 0 120px', textAlign: 'right', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 18 }}>{amt(inv.totalAmount)}</span>
                        <span style={{ flex: '0 0 190px', display: 'flex', gap: 10, justifyContent: 'flex-end', alignItems: 'center' }}>
                          <button onClick={() => download(inv)} className="btn btn-ghost" style={{ fontSize: 13 }}>
                            PDF
                          </button>
                          {inv.status !== InvoiceStatus.Paid && (
                            <button onClick={() => markPaid(inv)} disabled={rowBusy === inv.id.value} className="btn btn-secondary" style={{ fontSize: 13 }}>
                              {rowBusy === inv.id.value ? '…' : 'Mark paid'}
                            </button>
                          )}
                        </span>
                      </div>
                    )
                  })}
                  <div style={{ padding: '14px 22px', fontSize: 13, color: 'var(--color-neutral-600)' }}>
                    When a payment lands in your bank, match it to the invoice and mark it paid here.
                  </div>
                </div>
              </div>
            ) : (
              <EmptyCard message="No invoices yet. Create your first one from the “New invoice” tab." />
            )}
          </>
        )}
      </div>
    </AppShell>
  )
}

function TabButton({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '9px 15px',
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

const fieldGroupLabel: CSSProperties = {
  fontSize: 11.5,
  letterSpacing: '0.14em',
  textTransform: 'uppercase',
  color: 'var(--color-neutral-600)',
  marginBottom: 10,
}

const listRow: CSSProperties = {
  display: 'flex',
  flexWrap: 'nowrap',
  gap: 16,
  alignItems: 'center',
  padding: '13px 22px',
  borderBottom: '1px solid var(--color-divider)',
}

const listHead: CSSProperties = {
  fontSize: 11.5,
  letterSpacing: '0.12em',
  textTransform: 'uppercase',
  color: 'var(--color-neutral-600)',
  padding: '12px 22px',
}
