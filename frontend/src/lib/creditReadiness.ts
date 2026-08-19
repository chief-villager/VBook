// Credit-readiness endpoints on the Bookkeeping API. Thin typed wrappers over `get`.
// A session is required (the bearer token is attached automatically) and the caller's
// role must grant CreditReadiness.Read for the business in the route.
//
// Two endpoints back the page:
//   GET .../credit-readiness/dashboard   -> CreditReadinessDashboard (the "journey" stats)
//   GET .../credit-readiness/evaluation  -> FiveCsRating[] (the 5Cs assessment)

import { get } from './api'

// Strongly-typed ids serialize as { value }.
export interface Id {
  value: string
}

// Mirrors Application/CreditReadiness/Report.cs `CreditReadinessDashBoard`.
// NumberOfTransactions is scoped to the window; MonthsOfHistory and NumberOfInvoices
// are all-time (from the earliest recorded transaction / every invoice).
export interface CreditReadinessDashboard {
  business: Id
  numberOfTransactions: number
  monthsOfHistory: number
  numberOfInvoices: number
}

// Mirrors Application/CreditReadiness/Report.cs `CreditFactor` (serialized as its
// ordinal, since the API has no string-enum converter).
export const CreditFactor = {
  Character: 0,
  Capacity: 1,
  Capital: 2,
  Collateral: 3,
  Conditions: 4,
} as const

// Mirrors the `Rating` enum. Its members carry explicit values, so the API serializes
// them as those numbers (25/50/75/100), not ordinals — NotObservable is 0.
export const Rating = {
  NotObservable: 0,
  Weak: 25,
  ModerateSignal: 50,
  StrongSignal: 75,
  VeryStrongSignal: 100,
} as const

// Mirrors `CreditFactorRating`.
export interface CreditFactorRating {
  factor: number
  rating: number
  description: string
  suggestedAction: string
  score: number
}

// Mirrors `FiveCsRating`. Note `obeservableStrengthScore` reproduces the typo in the
// API contract. Both scores arrive as numeric strings (e.g. "56").
export interface FiveCsRating {
  ratings: CreditFactorRating[]
  recordKeepingScore: string
  obeservableStrengthScore: string
}

export function getCreditReadinessDashboard(businessId: string, from: string, to: string) {
  return get<CreditReadinessDashboard>(
    `/api/businesses/${businessId}/credit-readiness/dashboard?from=${from}&to=${to}`,
  )
}

// The service returns a single-element list; the caller reads the first entry.
export function evaluateCreditReadiness(businessId: string, from: string, to: string) {
  return get<FiveCsRating[]>(
    `/api/businesses/${businessId}/credit-readiness/evaluation?from=${from}&to=${to}`,
  )
}

// Human label for a factor ordinal.
export function factorName(factor: number): string {
  switch (factor) {
    case CreditFactor.Character:
      return 'Character'
    case CreditFactor.Capacity:
      return 'Capacity'
    case CreditFactor.Capital:
      return 'Capital'
    case CreditFactor.Collateral:
      return 'Collateral'
    case CreditFactor.Conditions:
      return 'Conditions'
    default:
      return 'Unknown'
  }
}

// Short band label for a rating value.
export function ratingLabel(rating: number): string {
  if (rating >= Rating.VeryStrongSignal) return 'Very strong'
  if (rating >= Rating.StrongSignal) return 'Strong signal'
  if (rating >= Rating.ModerateSignal) return 'Moderate signal'
  if (rating >= Rating.Weak) return 'Weak'
  return 'Not observable'
}
