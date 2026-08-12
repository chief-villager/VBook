// Sign-in — the returning-user counterpart to Onboarding. Collects email and
// password, calls the Identity login endpoint, stores the returned JWT in the
// in-memory token store, and routes to the dashboard. Visuals reuse the blueprint
// card + corner-mark language from src/styles/industry.css so it sits alongside
// the onboarding flow.

import { useState, type CSSProperties } from 'react'
import { useNavigate } from 'react-router-dom'
import { ApiError } from '../lib/api'
import { setAccessToken } from '../lib/auth'
import { login } from '../lib/identity'

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

export default function SignIn() {
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSignIn() {
    setError(null)
    if (!email.trim() || !password) {
      setError('Please enter your email and password.')
      return
    }

    setSubmitting(true)
    try {
      const { token } = await login(email.trim(), password)
      setAccessToken(token)
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        background: 'var(--color-bg)',
        color: 'var(--color-text)',
        fontFamily: 'var(--font-body)',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <header style={{ padding: '22px 32px 0 32px' }}>
        <div style={{ maxWidth: 780, margin: '0 auto', display: 'flex', alignItems: 'baseline', gap: 8 }}>
          <span
            style={{
              fontFamily: 'var(--font-heading)',
              fontWeight: 600,
              fontSize: 25,
              letterSpacing: '0.02em',
              color: 'var(--color-accent-900)',
            }}
          >
            vbook
          </span>
          <span
            style={{
              fontSize: 10,
              letterSpacing: '0.16em',
              textTransform: 'uppercase',
              color: 'var(--color-neutral-600)',
            }}
          >
            sign in
          </span>
        </div>
      </header>

      <main
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '34px 32px 56px 32px',
        }}
      >
        <form
          className="card blueprint"
          onSubmit={(e) => {
            e.preventDefault()
            if (!submitting) handleSignIn()
          }}
          style={{ position: 'relative', width: '100%', maxWidth: 460, padding: '38px 40px 34px 40px' }}
        >
          {cornerMarks}

          <div style={kickerStyle}>Welcome back</div>
          <h1 style={headingStyle}>Sign in to your books</h1>
          <p style={{ ...leadStyle, marginBottom: 28 }}>
            Enter your details to pick up where you left off.
          </p>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 16, marginBottom: 26 }}>
            <Field
              id="si-email"
              label="Email"
              type="email"
              placeholder="you@yourbusiness.com"
              value={email}
              onChange={setEmail}
              autoComplete="email"
            />
            <Field
              id="si-pass"
              label="Password"
              type="password"
              placeholder="Your password"
              value={password}
              onChange={setPassword}
              autoComplete="current-password"
            />
          </div>

          {error && (
            <p
              role="alert"
              style={{ margin: '0 0 14px 0', fontSize: 13, lineHeight: 1.5, color: 'var(--color-danger, #b42318)' }}
            >
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={submitting}
            className="btn btn-primary btn-block blueprint"
            style={{ position: 'relative', opacity: submitting ? 0.7 : 1 }}
          >
            {cornerMarks}
            {submitting ? 'Signing you in…' : 'Sign in'}
          </button>

          <p style={{ margin: '14px 0 0 0', fontSize: 12.5, color: 'var(--color-neutral-600)', textAlign: 'center' }}>
            New to vbook?{' '}
            <a
              href="/onboarding"
              onClick={(e) => {
                e.preventDefault()
                navigate('/onboarding')
              }}
            >
              Create an account
            </a>
          </p>
        </form>
      </main>
    </div>
  )
}

const kickerStyle: CSSProperties = {
  fontSize: 11.5,
  letterSpacing: '0.16em',
  textTransform: 'uppercase',
  color: 'var(--color-neutral-600)',
  marginBottom: 10,
}

const headingStyle: CSSProperties = {
  fontFamily: 'var(--font-heading)',
  fontWeight: 600,
  fontSize: 30,
  lineHeight: 1.08,
  margin: '0 0 10px 0',
}

const leadStyle: CSSProperties = {
  margin: '0 0 26px 0',
  fontSize: 15,
  lineHeight: 1.5,
  color: 'var(--color-neutral-700)',
  maxWidth: '42ch',
}

interface FieldProps {
  id: string
  label: string
  type: string
  placeholder: string
  value: string
  onChange: (value: string) => void
  autoComplete?: string
}

function Field({ id, label, type, placeholder, value, onChange, autoComplete }: FieldProps) {
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      <input
        className="input"
        id={id}
        type={type}
        placeholder={placeholder}
        value={value}
        autoComplete={autoComplete}
        onChange={(e) => onChange(e.target.value)}
      />
    </div>
  )
}
