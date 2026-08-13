// Ledger — React port of the "ledger" section of "Vbook.dc.html". A full-width,
// grouped overview of every account group the business keeps (Income / Expenses /
// Assets / Liabilities / Equity), each with a plain-words note and the balance the
// whole group stands at today. The design deliberately keeps this a summary — no
// per-account drill-down.
//
// Visuals come from src/styles/industry.css. Data is the prototype's own mock set so
// the page renders standalone; TODO: source group balances from the business's
// chart-of-accounts balances.

import { useState } from 'react'
import AppShell from '../components/AppShell.tsx'

const NAIRA = '₦'
const MINUS = '−'

// "₦1,200" / "−₦900".
const amt = (n: number) => (n < 0 ? MINUS : '') + NAIRA + Math.abs(n).toLocaleString('en-NG')

interface Entry {
  d: string
  t: string
  dr: number
  cr: number
}

interface Account {
  id: string
  name: string
  term: string
  opening: number
  entries: Entry[]
  // Owner's drawings reads more naturally shown as a positive figure.
  showPositive?: boolean
}

interface Group {
  group: string
  note: string
  // Which side increases the balance: debit-normal or credit-normal.
  side: 'dr' | 'cr'
  accounts: Account[]
}

const LEDGER: Group[] = [
  {
    group: 'Income',
    note: 'Money you earn',
    side: 'cr',
    accounts: [
      {
        id: 'a1', name: 'Sales to customers', term: 'Sales revenue', opening: 1932600, entries: [
          { d: '23 Aug', t: 'Sahara Foods Ltd — INV-0142', dr: 0, cr: 640000 },
          { d: '20 Aug', t: 'Bright Star Ventures — INV-0139', dr: 0, cr: 415000 },
          { d: '15 Aug', t: 'Kano Traders Co — INV-0136', dr: 0, cr: 890000 },
          { d: '12 Aug', t: 'Palm Grove Salon — INV-0131', dr: 0, cr: 300000 },
          { d: '9 Aug', t: 'Credit note issued to Kano Traders Co', dr: 45000, cr: 0 },
        ],
      },
      {
        id: 'a2', name: 'Other income', term: 'Other income', opening: 0, entries: [
          { d: '18 Aug', t: 'Interest paid by GTBank', dr: 0, cr: 12400 },
          { d: '4 Aug', t: 'Scrap sale — old pallets', dr: 0, cr: 35000 },
        ],
      },
    ],
  },
  {
    group: 'Expenses',
    note: 'Money you spend to run the business',
    side: 'dr',
    accounts: [
      {
        id: 'b1', name: 'Staff pay', term: 'Salaries and wages', opening: 0, entries: [
          { d: '22 Aug', t: 'Chidi Nwosu — August salary', dr: 320000, cr: 0 },
          { d: '10 Aug', t: 'Ngozi Adeyemi — August salary', dr: 285000, cr: 0 },
          { d: '2 Aug', t: 'Casual loaders — weekend shift', dr: 60000, cr: 0 },
        ],
      },
      {
        id: 'b2', name: 'Fuel and transport', term: 'Motor and travel expenses', opening: 0, entries: [
          { d: '26 Aug', t: 'Total Filling Station, Lekki', dr: 42500, cr: 0 },
          { d: '12 Aug', t: 'Mobil Filling Station, Ojota', dr: 96000, cr: 0 },
          { d: '9 Aug', t: 'Ojota Fuel Depot — generator diesel', dr: 140000, cr: 0 },
          { d: '6 Aug', t: 'Swift Haulage — delivery run', dr: 175000, cr: 0 },
        ],
      },
      {
        id: 'b3', name: 'Stock and supplies', term: 'Cost of goods sold', opening: 0, entries: [
          { d: '14 Aug', t: 'Dangote Cement Depot', dr: 268000, cr: 0 },
          { d: '11 Aug', t: 'Dangote Cement Depot', dr: 620000, cr: 0 },
          { d: '21 Aug', t: 'Ikeja Auto Spares', dr: 87400, cr: 0 },
        ],
      },
      {
        id: 'b4', name: 'Rent', term: 'Rent and rates', opening: 0, entries: [
          { d: '18 Aug', t: 'Ojota Warehouse Ltd — August', dr: 450000, cr: 0 },
        ],
      },
      {
        id: 'b5', name: 'Bank charges', term: 'Bank charges', opening: 0, entries: [
          { d: '19 Aug', t: 'GTBank monthly maintenance', dr: 1075, cr: 0 },
          { d: '7 Aug', t: 'POS terminal charges', dr: 25025, cr: 0 },
        ],
      },
      {
        id: 'b6', name: 'Insurance, phone and other', term: 'General administrative expenses', opening: 100000, entries: [
          { d: '8 Aug', t: 'AXA Mansard — vehicle cover', dr: 210000, cr: 0 },
          { d: '5 Aug', t: 'Airtel — airtime and data', dr: 40000, cr: 0 },
          { d: '17 Aug', t: 'MTN — data', dr: 15000, cr: 0 },
        ],
      },
    ],
  },
  {
    group: 'Assets',
    note: 'What the business owns',
    side: 'dr',
    accounts: [
      {
        id: 'c1', name: 'Money in the bank', term: 'Cash at bank — GTBank ••4471', opening: 1840000, entries: [
          { d: '27 Aug', t: 'Transfer received — Zenith Bank ••4471', dr: 180000, cr: 0 },
          { d: '26 Aug', t: 'Total Filling Station', dr: 0, cr: 42500 },
          { d: '24 Aug', t: 'Lekki Retail Ltd — transfer received', dr: 1250000, cr: 0 },
          { d: '23 Aug', t: 'Sahara Foods Ltd — INV-0142 paid', dr: 640000, cr: 0 },
          { d: '22 Aug', t: 'Chidi Nwosu — August salary', dr: 0, cr: 320000 },
          { d: '18 Aug', t: 'Ojota Warehouse Ltd — rent', dr: 0, cr: 450000 },
          { d: '1–27 Aug', t: 'Everything else that came in (5 items)', dr: 2110000, cr: 0 },
          { d: '1–27 Aug', t: 'Everything else that went out (12 items)', dr: 0, cr: 2122500 },
        ],
      },
      {
        id: 'c2', name: 'Money customers still owe you', term: 'Accounts receivable', opening: 0, entries: [
          { d: '25 Aug', t: 'INV-0145 raised — Lagos Freight Co', dr: 780000, cr: 0 },
          { d: '23 Aug', t: 'INV-0142 settled — Sahara Foods Ltd', dr: 0, cr: 640000 },
          { d: '16 Aug', t: 'INV-0144 raised — Uche Enterprises', dr: 505000, cr: 0 },
        ],
      },
      {
        id: 'c3', name: 'Vehicles and equipment', term: 'Fixed assets', opening: 6400000, entries: [
          { d: '3 Aug', t: 'Wear and tear for the month', dr: 0, cr: 106000 },
        ],
      },
    ],
  },
  {
    group: 'Liabilities',
    note: 'What the business owes',
    side: 'cr',
    accounts: [
      {
        id: 'd1', name: 'Money you owe suppliers', term: 'Accounts payable', opening: 1200000, entries: [
          { d: '11 Aug', t: 'Dangote Cement Depot — settled', dr: 620000, cr: 0 },
          { d: '13 Aug', t: 'Ikeja Auto Spares — invoice received', dr: 0, cr: 87400 },
        ],
      },
      {
        id: 'd2', name: 'Bank loan', term: 'Long-term borrowings', opening: 3200000, entries: [
          { d: '4 Aug', t: 'Sterling Bank — monthly repayment', dr: 160000, cr: 0 },
        ],
      },
    ],
  },
  {
    group: 'Equity',
    note: 'What the business is worth to you',
    side: 'cr',
    accounts: [
      {
        id: 'e1', name: 'What you put in', term: "Owner's capital", opening: 2500000, entries: [
          { d: '1 Aug', t: 'Opening balance carried forward', dr: 0, cr: 0 },
        ],
      },
      {
        id: 'e2', name: 'Money you took out', term: "Owner's drawings", opening: 0, showPositive: true, entries: [
          { d: '16 Aug', t: 'Transfer to personal account', dr: 250000, cr: 0 },
        ],
      },
      {
        id: 'e3', name: 'Profits kept in the business', term: 'Retained earnings', opening: 2821600, entries: [
          { d: '1 Aug', t: 'Brought forward from last year', dr: 0, cr: 0 },
        ],
      },
    ],
  },
]

// A debit-normal account rises with debits; a credit-normal one with credits.
const balOf = (g: Group, a: Account) => {
  const net = a.entries.reduce((s, e) => s + e.dr - e.cr, 0)
  return g.side === 'dr' ? a.opening + net : a.opening - net
}

// The balance the whole group stands at today.
const groupBalance = (g: Group) => g.accounts.reduce((s, a) => s + balOf(g, a), 0)

export default function Ledger() {
  const [showTerms, setShowTerms] = useState(false)

  return (
    <AppShell
      active="ledger"
      title="Ledger"
      kicker="Every entry, in order"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((s) => !s)}
    >
      <div style={{ padding: '26px 40px 48px 40px', display: 'flex', flexWrap: 'wrap', gap: 26, alignItems: 'flex-start' }}>
        <aside style={{ flex: '1 1 100%', width: '100%', display: 'flex', flexDirection: 'column', gap: 22 }}>
          <p style={{ margin: 0, fontSize: 14, lineHeight: 1.5, color: 'var(--color-neutral-700)' }}>
            Every account your business keeps, and what each one stands at today.
          </p>

          {LEDGER.map((g) => (
            <div key={g.group}>
              <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 12, paddingBottom: 7, borderBottom: '1px solid var(--color-divider)' }}>
                <span style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 17 }}>{g.group}</span>
                <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>{amt(groupBalance(g))}</span>
              </div>
              <div style={{ fontSize: 12.5, color: 'var(--color-neutral-600)', padding: '6px 0 4px 0' }}>{g.note}</div>
            </div>
          ))}
        </aside>
      </div>
    </AppShell>
  )
}
