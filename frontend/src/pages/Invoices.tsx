// Invoices — React port of the "invoices" section of "Vbook.dc.html". Two tabs:
// "New invoice" (a live invoice builder — customer, editable line items, a 7.5% VAT
// toggle, running totals) and "All invoices" (a status-coloured list of past ones).
//
// Visuals come from src/styles/industry.css. Data is the prototype's own mock set so
// the page renders standalone; TODOs mark the real Invoices endpoints:
//   Send / Download PDF / Save as draft -> POST create invoice (raises InvoiceCreated
//     -> PDF outbox), then the returned PdfUrl for download
//   All invoices list -> GET the business's invoices

import { useState, type CSSProperties } from 'react'
import AppShell from '../components/AppShell.tsx'

const BUSINESS_NAME = 'Okafor Logistics Ltd'
const NAIRA = '₦'
const MINUS = '−'
const VAT_RATE = 0.075

// "₦1,200" / "−₦900".
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(n).toLocaleString('en-NG')

interface LineItem {
  desc: string
  qty: string
  price: string
}

const DEFAULT_ITEMS: LineItem[] = [
  { desc: 'Haulage — Lagos to Ibadan, 2 trips', qty: '2', price: '185000' },
  { desc: 'Loading and offloading', qty: '1', price: '45000' },
]

type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Overdue'

interface PastInvoice {
  no: string
  who: string
  issued: string
  due: string
  amount: number
  status: InvoiceStatus
}

const PAST_INVOICES: PastInvoice[] = [
  { no: 'INV-0146', who: 'Havilah Interiors', issued: '28 Aug', due: '11 Sep', amount: 340000, status: 'Draft' },
  { no: 'INV-0145', who: 'Lagos Freight Co', issued: '25 Aug', due: '8 Sep', amount: 780000, status: 'Sent' },
  { no: 'INV-0144', who: 'Uche Enterprises', issued: '16 Aug', due: '30 Aug', amount: 505000, status: 'Sent' },
  { no: 'INV-0142', who: 'Sahara Foods Ltd', issued: '9 Aug', due: '23 Aug', amount: 640000, status: 'Paid' },
  { no: 'INV-0139', who: 'Bright Star Ventures', issued: '6 Aug', due: '20 Aug', amount: 415000, status: 'Paid' },
  { no: 'INV-0136', who: 'Kano Traders Co', issued: '1 Aug', due: '15 Aug', amount: 890000, status: 'Paid' },
  { no: 'INV-0131', who: 'Palm Grove Salon', issued: '28 Jul', due: '11 Aug', amount: 300000, status: 'Paid' },
  { no: 'INV-0128', who: 'Delta Motors', issued: '12 Jul', due: '26 Jul', amount: 220000, status: 'Overdue' },
]

// Border / background / text colour per status pill.
const statusStyle = (st: InvoiceStatus): { bg: string; color: string; border: string } => {
  switch (st) {
    case 'Paid':
      return { bg: 'var(--color-accent-100)', color: 'var(--color-accent-800)', border: 'var(--color-accent)' }
    case 'Overdue':
      return { bg: 'transparent', color: 'var(--color-text)', border: 'var(--color-text)' }
    case 'Sent':
      return { bg: 'transparent', color: 'var(--color-accent-700)', border: 'var(--color-accent-400)' }
    default:
      return { bg: 'transparent', color: 'var(--color-neutral-600)', border: 'var(--color-neutral-400)' }
  }
}

const num = (s: string) => Number(s) || 0
const digitsOnly = (s: string) => s.replace(/[^0-9]/g, '')

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

  const [customer, setCustomer] = useState('')
  const [email, setEmail] = useState('')
  const [due, setDue] = useState('11 September 2026')
  const [note, setNote] = useState('')
  const [vat, setVat] = useState(false)
  const [items, setItems] = useState<LineItem[]>(DEFAULT_ITEMS)

  const subtotal = items.reduce((s, it) => s + num(it.qty) * num(it.price), 0)
  const vatAmount = vat ? Math.round(subtotal * VAT_RATE) : 0
  const total = subtotal + vatAmount

  const setItem = (i: number, key: keyof LineItem, value: string) =>
    setItems((prev) => prev.map((it, n) => (n === i ? { ...it, [key]: key === 'desc' ? value : digitsOnly(value) } : it)))
  const addItem = () => setItems((prev) => [...prev, { desc: '', qty: '1', price: '0' }])
  const removeItem = (i: number) => setItems((prev) => prev.filter((_, n) => n !== i))

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
          <TabButton label="All invoices" active={view === 'list'} onClick={() => setView('list')} />
        </div>

        {view === 'create' && (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 28, alignItems: 'flex-start' }}>
            <div style={{ flex: '1 1 100%', maxWidth: 780, display: 'flex', flexDirection: 'column', gap: 22 }}>
              {/* Your details */}
              <div>
                <div style={fieldGroupLabel}>Your details</div>
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 14, alignItems: 'flex-start' }}>
                  {/* TODO: upload logo -> logos/{businessId}/... via the Identity endpoint. */}
                  <button
                    style={{
                      width: 88,
                      height: 88,
                      flex: '0 0 88px',
                      border: '1px dashed var(--color-neutral-400)',
                      background: 'transparent',
                      cursor: 'pointer',
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'center',
                      justifyContent: 'center',
                      gap: 5,
                      fontFamily: 'var(--font-body)',
                      fontSize: 11.5,
                      color: 'var(--color-neutral-600)',
                    }}
                  >
                    <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M12 5v14" />
                      <path d="M5 12h14" />
                    </svg>
                    Add logo
                  </button>
                  <div style={{ flex: '1 1 240px', minWidth: 200, display: 'flex', flexDirection: 'column', gap: 4, fontSize: 14, lineHeight: 1.5 }}>
                    <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 18 }}>{BUSINESS_NAME}</span>
                    <span style={{ color: 'var(--color-neutral-700)' }}>RC 1234567 &middot; 14 Ojota Industrial Road, Lagos</span>
                    <span style={{ color: 'var(--color-neutral-700)' }}>hello@okaforlogistics.ng &middot; 0803 000 0000</span>
                    <button className="btn btn-ghost" style={{ alignSelf: 'flex-start', fontSize: 13, paddingLeft: 0 }}>
                      Edit business details
                    </button>
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

              {/* Due + note */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12 }}>
                <div className="field" style={{ flex: '1 1 200px' }}>
                  <label htmlFor="in-due">Payment due by</label>
                  <input className="input" id="in-due" type="text" value={due} onChange={(e) => setDue(e.target.value)} />
                </div>
                <div className="field" style={{ flex: '1 1 260px' }}>
                  <label htmlFor="in-note">Note at the bottom</label>
                  <input className="input" id="in-note" type="text" value={note} onChange={(e) => setNote(e.target.value)} placeholder="Thank you for your business." />
                </div>
              </div>

              {/* Actions — TODO: wire to the create-invoice endpoint. */}
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10, alignItems: 'center', borderTop: '1px solid var(--color-divider)', paddingTop: 18 }}>
                <button className="btn btn-primary blueprint" style={{ position: 'relative' }}>
                  {cornerMarks}
                  Send
                </button>
                <button className="btn btn-secondary">Download PDF</button>
                <button className="btn btn-ghost" style={{ fontSize: 13.5 }}>
                  Save as draft
                </button>
              </div>
            </div>
          </div>
        )}

        {view === 'list' && (
          <div className="card blueprint" style={{ position: 'relative', padding: 0 }}>
            {cornerMarks}
            <div style={{ overflowX: 'auto' }}>
              <div
                style={{
                  display: 'flex',
                  flexWrap: 'nowrap',
                  gap: 16,
                  alignItems: 'center',
                  padding: '12px 22px',
                  borderBottom: '1px solid var(--color-divider)',
                  fontSize: 11.5,
                  letterSpacing: '0.12em',
                  textTransform: 'uppercase',
                  color: 'var(--color-neutral-600)',
                  minWidth: 720,
                }}
              >
                <span style={{ flex: '0 0 90px' }}>Number</span>
                <span style={{ flex: '1 1 180px', minWidth: 0 }}>Customer</span>
                <span style={{ flex: '0 0 90px' }}>Issued</span>
                <span style={{ flex: '0 0 90px' }}>Due</span>
                <span style={{ flex: '0 0 100px' }}>Status</span>
                <span style={{ flex: '0 0 130px', textAlign: 'right' }}>Amount</span>
              </div>
              {PAST_INVOICES.map((p) => {
                const s = statusStyle(p.status)
                return (
                  <div key={p.no} style={{ display: 'flex', flexWrap: 'nowrap', gap: 16, alignItems: 'center', padding: '13px 22px', borderBottom: '1px solid var(--color-divider)', minWidth: 720 }}>
                    <span style={{ flex: '0 0 90px', fontSize: 13.5, color: 'var(--color-neutral-600)' }}>{p.no}</span>
                    <span style={{ flex: '1 1 180px', minWidth: 0, fontSize: 15, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{p.who}</span>
                    <span style={{ flex: '0 0 90px', fontSize: 13.5, color: 'var(--color-neutral-600)' }}>{p.issued}</span>
                    <span style={{ flex: '0 0 90px', fontSize: 13.5, color: 'var(--color-neutral-600)' }}>{p.due}</span>
                    <span style={{ flex: '0 0 100px' }}>
                      <span style={{ display: 'inline-block', padding: '3px 9px', fontSize: 12, border: `1px solid ${s.border}`, background: s.bg, color: s.color }}>{p.status}</span>
                    </span>
                    <span style={{ flex: '0 0 130px', textAlign: 'right', fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 18 }}>{amt(p.amount)}</span>
                  </div>
                )
              })}
              <div style={{ padding: '14px 22px', fontSize: 13, color: 'var(--color-neutral-600)' }}>
                When a payment lands in your bank, vbook matches it to the invoice and marks it Paid on its own.
              </div>
            </div>
          </div>
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

const fieldGroupLabel: CSSProperties = {
  fontSize: 11.5,
  letterSpacing: '0.14em',
  textTransform: 'uppercase',
  color: 'var(--color-neutral-600)',
  marginBottom: 10,
}
