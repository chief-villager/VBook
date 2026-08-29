// Bank-account endpoints on the Bookkeeping API. Linking exchanges the Mono widget's
// authorisation code (see lib/mono.ts) for a durable account the bank-imports feed
// pulls against. A session is required; the caller's role must grant
// BankAccounts.Manage to link and BankAccounts.Read to list.

import { get, post, del } from './api'

// Mirrors Application/Transactions/Dtos.cs `LinkedBankAccountDto`. `id` is the API's
// strongly-typed id, serialized as { value }; `status` is the enum's ordinal
// (0 = Active, 1 = Unlinked).
export interface LinkedBankAccount {
  id: { value: string }
  externalAccountId: string
  institutionName: string
  accountNumberMasked: string
  currency: string
  status: number
}

export function linkBankAccount(businessId: string, authorisationCode: string) {
  return post<LinkedBankAccount>(`/api/businesses/${businessId}/bank-accounts`, {
    authorisationCode,
  })
}

export function listBankAccounts(businessId: string) {
  return get<LinkedBankAccount[]>(`/api/businesses/${businessId}/bank-accounts`)
}

// Disconnects a linked account from the feed. Returns 204 No Content on success.
// Requires BankAccounts.Manage.
export function unlinkBankAccount(businessId: string, accountId: string) {
  return del<void>(`/api/businesses/${businessId}/bank-accounts/${accountId}`)
}
