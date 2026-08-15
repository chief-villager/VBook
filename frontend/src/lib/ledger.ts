// Ledger endpoints on the Bookkeeping API. Thin typed wrappers over the shared fetch
// client. A session is required (the bearer token is attached automatically); the
// caller's role must grant Ledger.Read.

import { get } from './api'

// Mirrors Domain/Ledger/AccountType.cs. Enums serialize as their ordinal (there is no
// JsonStringEnumConverter on the API).
export const AccountType = { Asset: 0, Liability: 1, Equity: 2, Income: 3, Expense: 4 } as const

// Asset and Expense accounts are debit-normal (a positive natural balance shows as a
// debit); Liability, Equity, and Income are credit-normal. The API returns `balance`
// signed as debits-minus-credits, so a debit-normal account's natural balance is the
// signed value and a credit-normal account's is its negation.
const CREDIT_NORMAL = new Set<number>([AccountType.Liability, AccountType.Equity, AccountType.Income])

// The balance a reader expects to see: positive when the account sits on its natural
// side. Folds the signed debits-minus-credits value onto the account's normal side.
export const naturalBalance = (type: number, signedBalance: number) =>
  CREDIT_NORMAL.has(type) ? -signedBalance : signedBalance

// Strongly-typed ids serialize as { value } (see lib/bankAccounts.ts).
export interface Id {
  value: string
}

// Mirrors Application/Ledger/Dtos.cs `AccountBalance`. `balance` is signed:
// debits minus credits (see naturalBalance to read it on the account's normal side).
export interface AccountBalance {
  id: Id
  code: string
  name: string
  type: number
  statementLine: string
  balance: number
}

// Mirrors Application/Ledger/Dtos.cs `TrialBalance`. `asOf` is an ISO calendar date
// (yyyy-MM-dd); `isBalanced` is the computed IsBalanced property.
export interface TrialBalance {
  business: Id
  asOf: string
  accounts: AccountBalance[]
  totalDebits: number
  totalCredits: number
  isBalanced: boolean
}

// The trial balance as of a date (defaults server-side to today when `asOf` is
// omitted). Returns every account, including those standing at zero.
export function getTrialBalance(businessId: string, asOf?: string) {
  const query = asOf ? `?asOf=${asOf}` : ''
  return get<TrialBalance>(`/api/businesses/${businessId}/trial-balance${query}`)
}
