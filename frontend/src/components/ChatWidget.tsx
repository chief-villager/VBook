// Floating AI-assistant panel, mounted by AppShell so it appears on every signed-in
// page (AppShell only renders behind RequireAuth). It asks the API's chat endpoint
// about the user's primary business; the conversation is kept in component state only.

import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { ApiError } from '../lib/api'
import { getBusinessId, isAuthenticated } from '../lib/auth'
import { askAssistant, MAX_QUESTION_LENGTH } from '../lib/chat'

interface Message {
  role: 'user' | 'assistant' | 'error'
  text: string
}

const SUGGESTIONS = [
  'What was my net profit this month?',
  'How much cash do I have today?',
  'What were my biggest expenses last month?',
]

function describeError(err: unknown): string {
  if (err instanceof ApiError) {
    if (err.status === 401) return 'Your session has expired — please sign in again.'
    if (err.status === 403) return "You don't have permission to use the assistant for this business."
    if (err.status === 429) return "You're asking too quickly — wait a minute and try again."
    return err.message
  }
  return 'Could not reach the server. Check your connection and try again.'
}

export default function ChatWidget() {
  const [open, setOpen] = useState(false)
  const [messages, setMessages] = useState<Message[]>([])
  const [draft, setDraft] = useState('')
  const [pending, setPending] = useState(false)
  const scrollRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLTextAreaElement>(null)
  const businessId = getBusinessId()

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight })
  }, [messages, pending])

  useEffect(() => {
    if (open) inputRef.current?.focus()
  }, [open])

  // Logged-in users only; also hidden when the session has no business to ask about.
  if (!isAuthenticated() || !businessId) return null

  async function send(question: string) {
    const q = question.trim()
    if (!q || pending || !businessId) return
    setDraft('')
    setMessages((m) => [...m, { role: 'user', text: q }])
    setPending(true)
    try {
      const { answer } = await askAssistant(businessId, q)
      setMessages((m) => [...m, { role: 'assistant', text: answer?.trim() || "I don't know." }])
    } catch (err) {
      setMessages((m) => [...m, { role: 'error', text: describeError(err) }])
    } finally {
      setPending(false)
    }
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault()
    void send(draft)
  }

  // Enter sends; Shift+Enter inserts a newline.
  function onKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      void send(draft)
    }
  }

  return (
    <>
      {open && (
        <section
          aria-label="Bookkeeping assistant"
          style={{
            position: 'fixed',
            right: 24,
            bottom: 88,
            zIndex: 1000,
            width: 'min(380px, calc(100vw - 32px))',
            height: 'min(540px, calc(100vh - 120px))',
            display: 'flex',
            flexDirection: 'column',
            background: 'var(--color-surface)',
            border: '1px solid var(--color-divider)',
            borderRadius: 'var(--radius-lg)',
            boxShadow: 'var(--shadow-lg)',
            fontFamily: 'var(--font-body)',
            color: 'var(--color-text)',
            overflow: 'hidden',
          }}
        >
          <header
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '14px 16px',
              background: 'var(--color-accent-900)',
              color: '#fff',
            }}
          >
            <div>
              <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 17 }}>Assistant</div>
              <div style={{ fontSize: 11.5, color: 'var(--color-accent-300)' }}>Ask about your books</div>
            </div>
            <div style={{ display: 'flex', gap: 6 }}>
              {messages.length > 0 && (
                <button type="button" onClick={() => setMessages([])} disabled={pending} style={headerButton}>
                  Clear
                </button>
              )}
              <button type="button" onClick={() => setOpen(false)} aria-label="Close assistant" style={headerButton}>
                ✕
              </button>
            </div>
          </header>

          <div ref={scrollRef} style={{ flex: 1, overflowY: 'auto', padding: 16, display: 'flex', flexDirection: 'column', gap: 10 }}>
            {messages.length === 0 && (
              <div style={{ fontSize: 13.5, color: 'var(--color-neutral-700)' }}>
                <p style={{ margin: '0 0 10px' }}>
                  Ask a question about your transactions, profit and loss, balance sheet or cash flow.
                </p>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                  {SUGGESTIONS.map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => void send(s)}
                      style={{
                        textAlign: 'left',
                        padding: '8px 10px',
                        background: 'var(--color-bg)',
                        border: '1px solid var(--color-divider)',
                        borderRadius: 'var(--radius-md)',
                        fontFamily: 'var(--font-body)',
                        fontSize: 13,
                        color: 'var(--color-accent-700)',
                        cursor: 'pointer',
                      }}
                    >
                      {s}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {messages.map((m, i) => (
              <div
                key={i}
                style={{
                  alignSelf: m.role === 'user' ? 'flex-end' : 'flex-start',
                  maxWidth: '85%',
                  padding: '9px 12px',
                  borderRadius: 'var(--radius-md)',
                  fontSize: 14,
                  lineHeight: 1.45,
                  whiteSpace: 'pre-wrap',
                  overflowWrap: 'anywhere',
                  ...(m.role === 'user'
                    ? { background: 'var(--color-accent-700)', color: '#fff' }
                    : m.role === 'error'
                      ? { background: 'var(--color-accent-2-100)', color: 'var(--color-accent-2-800)', border: '1px solid var(--color-accent-2-300)' }
                      : { background: 'var(--color-bg)', border: '1px solid var(--color-divider)' }),
                }}
              >
                {m.text}
              </div>
            ))}

            {pending && (
              <div role="status" style={{ alignSelf: 'flex-start', fontSize: 13, color: 'var(--color-neutral-600)' }}>
                Thinking…
              </div>
            )}
          </div>

          <form onSubmit={onSubmit} style={{ display: 'flex', gap: 8, padding: 12, borderTop: '1px solid var(--color-divider)' }}>
            <textarea
              ref={inputRef}
              value={draft}
              onChange={(e) => setDraft(e.target.value)}
              onKeyDown={onKeyDown}
              maxLength={MAX_QUESTION_LENGTH}
              rows={2}
              placeholder="Ask a question…"
              aria-label="Your question"
              disabled={pending}
              style={{
                flex: 1,
                resize: 'none',
                padding: '8px 10px',
                border: '1px solid var(--color-divider)',
                borderRadius: 'var(--radius-md)',
                fontFamily: 'var(--font-body)',
                fontSize: 14,
                color: 'var(--color-text)',
                background: 'var(--color-surface)',
              }}
            />
            <button
              type="submit"
              disabled={pending || !draft.trim()}
              style={{
                padding: '0 14px',
                border: 0,
                borderRadius: 'var(--radius-md)',
                background: 'var(--color-accent-700)',
                color: '#fff',
                fontFamily: 'var(--font-body)',
                fontSize: 14,
                cursor: pending || !draft.trim() ? 'default' : 'pointer',
                opacity: pending || !draft.trim() ? 0.6 : 1,
              }}
            >
              Send
            </button>
          </form>
        </section>
      )}

      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-label={open ? 'Close assistant' : 'Open assistant'}
        aria-expanded={open}
        style={{
          position: 'fixed',
          right: 24,
          bottom: 24,
          zIndex: 1000,
          width: 52,
          height: 52,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          border: 0,
          borderRadius: '50%',
          background: 'var(--color-accent-700)',
          color: '#fff',
          boxShadow: 'var(--shadow-md)',
          cursor: 'pointer',
        }}
      >
        <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
          {open ? <path d="M18 6 6 18M6 6l12 12" /> : <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />}
        </svg>
      </button>
    </>
  )
}

const headerButton = {
  background: 'none',
  border: '1px solid var(--color-accent-700)',
  borderRadius: 'var(--radius-sm)',
  color: 'var(--color-accent-200)',
  fontFamily: 'var(--font-body)',
  fontSize: 12,
  padding: '4px 8px',
  cursor: 'pointer',
} as const
