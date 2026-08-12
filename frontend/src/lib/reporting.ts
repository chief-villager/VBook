// Reporting endpoints on the Bookkeeping API. Thin typed wrappers over `get` so
// pages don't hand-write paths or response shapes. These require a session (the
// bearer token is attached automatically) and the caller's role must grant
// Reports.Read for the business in the route.

import { get } from './api'

export interface StatementLineItem {
  statementLine: string
  accountName: string
  amount: number
}

// Mirrors Application/Reporting/Statements.cs `ProfitAndLoss` (only the fields the
// UI reads are typed; the API also returns `business` and `period`).
export interface ProfitAndLoss {
  revenue: StatementLineItem[]
  expenses: StatementLineItem[]
  totalRevenue: number
  totalExpenses: number
  netProfit: number
}

// `from` and `to` are ISO calendar dates (yyyy-MM-dd) — the API binds them to
// DateOnly query parameters.
export function getProfitAndLoss(businessId: string, from: string, to: string) {
  return get<ProfitAndLoss>(
    `/api/businesses/${businessId}/reports/profit-and-loss?from=${from}&to=${to}`,
  )
}
