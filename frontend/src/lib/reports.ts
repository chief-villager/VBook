// Reporting endpoints on the Bookkeeping API — the three IFRS-for-SMEs statements and
// their PDF export. Thin typed wrappers over the shared fetch client. A session is
// required (the bearer token is attached automatically); the caller's role must grant
// Reports.Read.
//
// The period reports (P&L, cash flow) take an explicit from/to range; the page owns
// the "this month to date" default rather than the API, so the endpoint stays
// unambiguous and reusable. The balance sheet takes an optional asOf that defaults to
// today server-side.

import { get, getBlob } from './api'

// Report identifiers for the PDF download endpoint. These are the C# `ReportType`
// enum member names, which ASP.NET binds case-insensitively from the query string.
export const ReportType = {
  ProfitAndLoss: 'ProfitAndLoss',
  BalanceSheet: 'BalanceSheet',
  CashFlow: 'CashFlow',
} as const
export type ReportTypeValue = (typeof ReportType)[keyof typeof ReportType]

// Strongly-typed ids serialize as { value } (see lib/bankAccounts.ts).
export interface Id {
  value: string
}

// Mirrors Domain/Common/DateRange.cs. Dates are ISO calendar dates (yyyy-MM-dd).
export interface Period {
  start: string
  end: string
}

// Mirrors Application/Reporting/Statements.cs `StatementLineItem`.
export interface StatementLineItem {
  statementLine: string
  accountName: string
  amount: number
}

// Mirrors Application/Reporting/Statements.cs `ProfitAndLoss`.
export interface ProfitAndLoss {
  business: Id
  period: Period
  revenue: StatementLineItem[]
  expenses: StatementLineItem[]
  totalRevenue: number
  totalExpenses: number
  netProfit: number
}

// Mirrors Application/Reporting/Statements.cs `BalanceSheet`.
export interface BalanceSheet {
  business: Id
  asOf: string
  assets: StatementLineItem[]
  liabilities: StatementLineItem[]
  equity: StatementLineItem[]
  totalAssets: number
  totalLiabilities: number
  totalEquity: number
}

// Mirrors Application/Reporting/Statements.cs `CashFlowStatement`.
export interface CashFlowStatement {
  business: Id
  period: Period
  openingCash: number
  closingCash: number
  netChange: number
}

export function getProfitAndLoss(businessId: string, from: string, to: string) {
  return get<ProfitAndLoss>(`/api/businesses/${businessId}/reports/profit-and-loss?from=${from}&to=${to}`)
}

export function getBalanceSheet(businessId: string, asOf?: string) {
  const query = asOf ? `?asOf=${asOf}` : ''
  return get<BalanceSheet>(`/api/businesses/${businessId}/reports/balance-sheet${query}`)
}

export function getCashFlow(businessId: string, from: string, to: string) {
  return get<CashFlowStatement>(`/api/businesses/${businessId}/reports/cash-flow?from=${from}&to=${to}`)
}

// Downloads the server-rendered statement PDF and prompts the browser to save it.
// `range` carries from/to for the period reports and asOf for the balance sheet; the
// endpoint ignores the ones a given report doesn't use.
export async function downloadReportPdf(
  businessId: string,
  type: ReportTypeValue,
  range: { from?: string; to?: string; asOf?: string },
  fileName: string,
) {
  const params = new URLSearchParams({ type })
  if (range.from) params.set('from', range.from)
  if (range.to) params.set('to', range.to)
  if (range.asOf) params.set('asOf', range.asOf)

  const blob = await getBlob(`/api/businesses/${businessId}/reports/download?${params.toString()}`)
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `${fileName}.pdf`
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}
