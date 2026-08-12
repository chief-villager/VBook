// Identity endpoints on the Bookkeeping API: combined sign-up and sign-in. Thin
// typed wrappers over `post` so pages don't hand-write paths, request shapes, or
// response shapes. Both are anonymous — the caller has no token yet.

import { post } from './api'

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

interface LoginResponse {
  token: string
}

export function login(email: string, password: string) {
  return post<LoginResponse>('/api/auth/login', { email, password }, { auth: false })
}
