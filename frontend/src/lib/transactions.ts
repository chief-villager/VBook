// Transaction and bank-import endpoints on the Bookkeeping API. Thin typed wrappers
// over the shared fetch client so pages don't hand-write paths or response shapes. A
// session is required (the bearer token is attached automatically); the caller's role
// must grant Transactions.Read / BankImports.Read to list and BankImports.Manage to
// categorise or approve.

import { get, post, patch } from './api'

// Mirrors Domain/Transactions/TransactionType.cs. Enums serialize as their ordinal
// (there is no JsonStringEnumConverter on the API).
export const TransactionType = { Income: 0, Expense: 1, Capital: 2, Loan: 3 } as const

// Mirrors Domain/Transactions/StagedBankTransaction.cs `StagedTransactionStatus`.
export const StagedStatus = { Pending: 0, Approved: 1, Discarded: 2 } as const

// Amounts persist as positive magnitudes (Transaction.Record rejects <= 0), so
// money-in vs money-out is decided by the category type, not a stored sign. Only an
// Expense is an outflow; income, capital, and loan inflows are all money in.
export const isOutflow = (type: number) => type === TransactionType.Expense

// Strongly-typed ids serialize as { value } (see lib/bankAccounts.ts).
export interface Id {
  value: string
}

// Mirrors Application/Transactions/Dtos.cs `CategoryDto`.
export interface Category {
  id: Id
  name: string
  type: number
}

// Mirrors Application/Transactions/Dtos.cs `TransactionSummary`. `occurredOn` is an
// ISO calendar date (yyyy-MM-dd); `amount` is a positive magnitude.
export interface TransactionSummary {
  id: Id
  type: number
  amount: number
  category: string
  occurredOn: string
  isVoided: boolean
}

// Mirrors Application/Transactions/Dtos.cs `StagedTransactionDto`.
export interface StagedTransaction {
  id: Id
  amount: number
  occurredOn: string
  narration: string
  providerCategory: string | null
  suggestedType: number
  categoryId: Id | null
  status: number
  recordedTransactionId: Id | null
}

// Mirrors Application/Common/PagedResult.cs.
export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

// Mirrors Application/Transactions/Dtos.cs `BankImportSummary`.
export interface BankImportSummary {
  imported: number
  skipped: number
}

export function getCategories(businessId: string) {
  return get<Category[]>(`/api/businesses/${businessId}/categories`)
}

// Pulls a linked account's feed over a date range and stages the rows for review.
// Requires BankImports.Manage.
export function pullBankImports(
  businessId: string,
  externalAccountId: string,
  from: string,
  to: string,
) {
  return post<BankImportSummary>(`/api/businesses/${businessId}/bank-imports`, {
    externalAccountId,
    from,
    to,
  })
}

// `from`/`to` are ISO calendar dates (yyyy-MM-dd); both are required by the API.
export function listTransactions(
  businessId: string,
  from: string,
  to: string,
  page = 1,
  pageSize = 50,
) {
  return get<Paged<TransactionSummary>>(
    `/api/businesses/${businessId}/transactions?from=${from}&to=${to}&page=${page}&pageSize=${pageSize}`,
  )
}

// The bank-import review queue. Defaults to Pending — the rows awaiting a category.
export function listStagedImports(
  businessId: string,
  page = 1,
  pageSize = 50,
  status: number = StagedStatus.Pending,
) {
  return get<Paged<StagedTransaction>>(
    `/api/businesses/${businessId}/bank-imports?status=${status}&page=${page}&pageSize=${pageSize}`,
  )
}

// Assigns a category to a staged row. The row stays Pending until it is approved.
export function categoriseImport(businessId: string, stagedId: string, categoryId: string) {
  return patch<void>(`/api/businesses/${businessId}/bank-imports/${stagedId}`, { categoryId })
}

// Promotes a categorised row into a real Transaction (and its journal entry). The API
// rejects this until the row has a category, so the caller gates the button on that.
export function approveImport(businessId: string, stagedId: string) {
  return post<{ transactionId: string }>(
    `/api/businesses/${businessId}/bank-imports/${stagedId}/approve`,
  )
}

export function discardImport(businessId: string, stagedId: string) {
  return post<void>(`/api/businesses/${businessId}/bank-imports/${stagedId}/discard`)
}
