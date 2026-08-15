// Invoice endpoints on the Bookkeeping API. Thin typed wrappers over the shared
// fetch client. A session is required; the caller's role must grant Invoices.Read to
// list/view/download and Invoices.Create / Invoices.Update to create / mark paid.

import { get, post, getBlob } from './api'

// Mirrors Domain/Invoices/InvoiceStatus.cs. Enums serialize as their ordinal.
export const InvoiceStatus = { Pending: 0, Paid: 1 } as const

// Strongly-typed ids serialize as { value } (see lib/bankAccounts.ts).
export interface Id {
  value: string
}

// Mirrors Application/Invoices/InvoiceDtos.cs `InvoiceLineItemDto`.
export interface InvoiceLineItem {
  description: string
  quantity: number
  unitPrice: number
}

// Mirrors Application/Invoices/InvoiceDtos.cs `InvoiceSummary`. Dates are ISO
// calendar dates (yyyy-MM-dd).
export interface InvoiceSummary {
  id: Id
  invoiceNumber: string
  billTo: string
  issueDate: string
  dueDate: string
  status: number
  totalAmount: number
}

// Mirrors Application/Common/PagedResult.cs.
export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface CreateInvoiceInput {
  invoiceNumber: string
  issueDate: string
  dueDate: string
  billTo: string
  // A fractional rate, e.g. 0.075 for 7.5% VAT (the API derives the amounts).
  vatRate: number
  lineItems: InvoiceLineItem[]
}

export function listInvoices(businessId: string, page = 1, pageSize = 50) {
  return get<Paged<InvoiceSummary>>(
    `/api/businesses/${businessId}/invoices?page=${page}&pageSize=${pageSize}`,
  )
}

export function createInvoice(businessId: string, input: CreateInvoiceInput) {
  return post<{ invoiceId: string }>(`/api/businesses/${businessId}/invoices`, input)
}

export function markInvoicePaid(businessId: string, invoiceId: string) {
  return post<void>(`/api/businesses/${businessId}/invoices/${invoiceId}/paid`)
}

// Downloads the server-rendered PDF and prompts the browser to save it.
export async function downloadInvoicePdf(businessId: string, invoiceId: string, invoiceNumber: string) {
  const blob = await getBlob(`/api/businesses/${businessId}/invoices/${invoiceId}/pdf`)
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `invoice-${invoiceNumber}.pdf`
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}
