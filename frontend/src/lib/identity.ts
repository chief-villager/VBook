// Identity endpoints on the Bookkeeping API: combined sign-up and sign-in. Thin
// typed wrappers over `post` so pages don't hand-write paths, request shapes, or
// response shapes. Both are anonymous — the caller has no token yet.

import { ApiError, get, post, put } from './api'
import { clearSession, getRefreshToken, storeSession } from './auth'

// Order matches the API's BusinessSector enum (Domain/Identity/BusinessSector.cs).
// The API has no JsonStringEnumConverter configured, so it deserializes enums by
// their integer value — we send the array index, never the name.
export const BUSINESS_SECTORS = [
  'Retail',
  'Wholesale',
  'Manufacturing',
  'Agriculture',
  'Services',
  'Hospitality',
  'Transport',
  'Other',
] as const

export type BusinessSector = (typeof BUSINESS_SECTORS)[number]

export interface RegisterBusinessWithOwnerInput {
  email: string
  displayName: string
  password: string
  businessName: string
  sector: BusinessSector
}

interface RegisterBusinessWithOwnerResponse {
  ownerId: string
  businessId: string
}

// Creates the owner account and their business in one API transaction.
export function registerBusinessWithOwner(input: RegisterBusinessWithOwnerInput) {
  return post<RegisterBusinessWithOwnerResponse>(
    '/api/businesses/register',
    {
      email: input.email,
      displayName: input.displayName,
      password: input.password,
      businessName: input.businessName,
      sector: BUSINESS_SECTORS.indexOf(input.sector),
    },
    { auth: false },
  )
}

// Mirrors the API's AuthTokens payload (Application/Identity/AuthTokens.cs). The
// access token authorises calls until it expires; the refresh token is exchanged for
// a new pair after that (see api.ts `attemptRefresh`).
export interface AuthTokens {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
}

// Signs in and stores the session (access token in memory, refresh token persisted),
// so callers just await it and navigate.
export async function login(email: string, password: string): Promise<AuthTokens> {
  const tokens = await post<AuthTokens>('/api/auth/login', { email, password }, { auth: false })
  storeSession({ accessToken: tokens.accessToken, refreshToken: tokens.refreshToken })
  return tokens
}

// Revokes the session server-side (best-effort) and clears it locally.
export async function logout(): Promise<void> {
  const refreshToken = getRefreshToken()
  try {
    if (refreshToken) await post('/api/auth/logout', { refreshToken }, { auth: false })
  } finally {
    clearSession()
  }
}

// The per-business roles, index-aligned with the API's BusinessRole enum
// (Domain/Identity/BusinessMembership.cs). The API has no JsonStringEnumConverter,
// so it serializes the role as its integer value — we index this to get the name.
export const BUSINESS_ROLES = ['Owner', 'Admin', 'Accountant'] as const
export type BusinessRoleName = (typeof BUSINESS_ROLES)[number]

export function roleName(role: number): BusinessRoleName | 'Member' {
  return BUSINESS_ROLES[role] ?? 'Member'
}

// Strongly-typed ids serialize as { value } (see lib/invoices.ts).
interface Id {
  value: string
}

// Mirrors Application/Identity/BusinessContext.cs `UserMembershipDto`. `role` is the
// BusinessRole integer (see BUSINESS_ROLES).
export interface UserMembership {
  businessId: Id
  businessName: string
  role: number
  joinedAt: string
}

// Mirrors Application/Identity/BusinessContext.cs `CurrentUserDto` — the payload of
// GET /api/users/me for the authenticated caller.
export interface CurrentUser {
  userId: Id
  email: string
  displayName: string
  memberships: UserMembership[]
}

// The authenticated caller's own profile and memberships, keyed off the JWT subject.
export function getCurrentUser() {
  return get<CurrentUser>('/api/users/me')
}

// Mirrors Application/Identity/BusinessContext.cs `InvoiceTemplateDto`. `logoUrl` is
// the stored object-storage URL of the uploaded logo.
export interface InvoiceTemplate {
  business: Id
  logoUrl: string
  businessName: string
  accountNumber: string
  bankName: string
  terms: string
}

// The business's saved invoice template, or null if none has been set yet (the API
// 404s in that case — a first-time set is expected, not an error).
export async function getInvoiceTemplate(businessId: string): Promise<InvoiceTemplate | null> {
  try {
    return await get<InvoiceTemplate>(`/api/businesses/${businessId}/invoice-template`)
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null
    throw err
  }
}

export interface SetInvoiceTemplateInput {
  logo: File
  businessName: string
  accountNumber: string
  bankName: string
  terms: string
}

// Saves the business's default invoice template. Multipart: the logo rides as a file
// (the API validates the image type server-side and never trusts a client filename)
// alongside the text fields. The API requires a logo on every save.
export function setInvoiceTemplate(businessId: string, input: SetInvoiceTemplateInput) {
  const form = new FormData()
  form.append('Logo', input.logo)
  form.append('BusinessName', input.businessName)
  form.append('AccountNumber', input.accountNumber)
  form.append('BankName', input.bankName)
  form.append('Terms', input.terms)
  return put<void>(`/api/businesses/${businessId}/invoice-template`, form)
}
