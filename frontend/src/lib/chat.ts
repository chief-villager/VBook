// AI assistant endpoint on the Bookkeeping API. A session is required (the bearer
// token is attached automatically) and the caller's role must grant both Reports.Read
// and Transactions.Read on the business. The assistant is stateless server-side: each
// question is answered on its own, so the transcript lives only in the widget.

import { post } from './api'

export interface ChatAnswer {
  answer: string
}

// Mirrors ChatBotService.MaxQuestionLength on the API.
export const MAX_QUESTION_LENGTH = 2000

export function askAssistant(businessId: string, question: string) {
  return post<ChatAnswer>(`/api/businesses/${businessId}/chat`, { question })
}
