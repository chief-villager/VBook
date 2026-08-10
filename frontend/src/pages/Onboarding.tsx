// Vbook Onboarding — React port of "Vbook Onboarding.dc.html" (Claude Design).
// A two-step flow: setup -> done. The single "setup" step collects the account
// (name, email, phone, password) and the business (name, type, sector, RC) on one
// form. Visuals come from the design system in src/styles/industry.css (blueprint
// cards, corner marks, tokens).
//
// Inputs are controlled so the values are ready to POST to the Identity endpoints;
// wiring to the API is left as a TODO — "Create my account" just advances the flow,
// matching the original prototype.

import { useState, type CSSProperties } from 'react'

type Step = 'setup' | 'done'

const ORDER: Step[] = ['setup', 'done']
const LABELS: { id: Step; label: string }[] = [
  { id: 'setup', label: 'Your details' },
  { id: 'done', label: 'Finished' },
]
const BIZ_TYPES = ['Sole trader', 'Limited company', 'Partnership', 'Not registered yet']
const SECTORS = ['Retail', 'Wholesale', 'Manufacturing', 'Agriculture', 'Services', 'Hospitality', 'Transport', 'Other']

const DONE_ITEMS = [
  'Connect a bank account from the dashboard to start your records automatically',
  'Your business details are saved and will appear on invoices',
  'Every month you keep clean books moves you closer to loan-ready',
]

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

export default function Onboarding() {
  const [step, setStep] = useState<Step>('setup')

  // Account fields
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [password, setPassword] = useState('')

  // Business fields
  const [bizName, setBizName] = useState('')
  const [bizType, setBizType] = useState('Limited company')
  const [sector, setSector] = useState(SECTORS[0])
  const [rcNumber, setRcNumber] = useState('')

  const currentIndex = ORDER.indexOf(step)
  const next = () => setStep(ORDER[Math.min(currentIndex + 1, ORDER.length - 1)])
  const restart = () => setStep('setup')

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
            setup
          </span>
        </div>
      </header>

      {/* Step progress */}
      <div style={{ padding: '26px 32px 0 32px' }}>
        <div
          style={{
            maxWidth: 780,
            margin: '0 auto',
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))',
            gap: 14,
          }}
        >
          {LABELS.map((l, n) => {
            const done = n < currentIndex
            const active = n === currentIndex
            const accentIfReached = done || active ? 'var(--color-accent)' : 'var(--color-neutral-300)'
            return (
              <div key={l.id} style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                <div style={{ height: 3, background: accentIfReached }} />
                <div style={{ display: 'flex', alignItems: 'center', gap: 7 }}>
                  <span
                    style={{
                      fontFamily: 'var(--font-heading)',
                      fontSize: 12,
                      width: 16,
                      height: 16,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      border: `1px solid ${done || active ? 'var(--color-accent)' : 'var(--color-neutral-400)'}`,
                      color: done || active ? 'var(--color-accent-800)' : 'var(--color-neutral-600)',
                    }}
                  >
                    {done ? '✓' : n + 1}
                  </span>
                  <span style={{ fontSize: 12.5, color: active ? 'var(--color-text)' : 'var(--color-neutral-600)' }}>
                    {l.label}
                  </span>
                </div>
              </div>
            )
          })}
        </div>
      </div>

      <main
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'center',
          padding: '34px 32px 56px 32px',
        }}
      >
        <div
          className="card blueprint"
          style={{ position: 'relative', width: '100%', maxWidth: 620, padding: '38px 40px 34px 40px' }}
        >
          {cornerMarks}

          {step === 'setup' && (
            <div>
              <div style={kickerStyle}>Step 1 of 2</div>
              <h1 style={headingStyle}>Let&rsquo;s open your books</h1>
              <p style={{ ...leadStyle, marginBottom: 28 }}>
                One short form, about two minutes. You can change any of it later.
              </p>

              {/* About you */}
              <div style={{ ...sectionKickerStyle, marginBottom: 14 }}>About you</div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16, marginBottom: 30 }}>
                <Field id="ob-name" label="Your name" type="text" placeholder="Adaeze Okafor" value={name} onChange={setName} />
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 16 }}>
                  <Field id="ob-email" label="Email" type="email" placeholder="you@yourbusiness.com" value={email} onChange={setEmail} flex="1 1 220px" />
                  <Field id="ob-phone" label="Phone number" type="tel" placeholder="0803 000 0000" value={phone} onChange={setPhone} flex="1 1 180px" />
                </div>
                <Field id="ob-pass" label="Create a password" type="password" placeholder="At least 8 characters" value={password} onChange={setPassword} />
              </div>

              {/* About the business */}
              <div style={{ ...sectionKickerStyle, marginBottom: 6 }}>About the business</div>
              <p style={{ margin: '0 0 14px 0', fontSize: 13.5, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '50ch' }}>
                This is what shows on your invoices and on anything you send to a lender.
              </p>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 16, marginBottom: 28 }}>
                <Field id="ob-biz" label="Business name" type="text" placeholder="Okafor Logistics Ltd" value={bizName} onChange={setBizName} />

                <div>
                  <label style={{ display: 'block', fontSize: 13, marginBottom: 8, color: 'var(--color-neutral-700)' }}>
                    What kind of business is it?
                  </label>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 8 }}>
                    {BIZ_TYPES.map((label) => {
                      const on = bizType === label
                      return (
                        <button
                          key={label}
                          className="biz-type"
                          onClick={() => setBizType(label)}
                          style={{
                            padding: '11px 13px',
                            textAlign: 'left',
                            fontFamily: 'var(--font-body)',
                            fontSize: 14,
                            cursor: 'pointer',
                            border: `1px solid ${on ? 'var(--color-accent)' : 'var(--color-divider)'}`,
                            background: on ? 'var(--color-accent-100)' : 'transparent',
                            color: on ? 'var(--color-accent-800)' : 'var(--color-text)',
                          }}
                        >
                          {label}
                        </button>
                      )
                    })}
                  </div>
                </div>

                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 16 }}>
                  <div className="field" style={{ flex: '1 1 220px' }}>
                    <label htmlFor="ob-ind">Business sector</label>
                    <select className="input" id="ob-ind" value={sector} onChange={(e) => setSector(e.target.value)}>
                      {SECTORS.map((s) => (
                        <option key={s}>{s}</option>
                      ))}
                    </select>
                  </div>
                  <div className="field" style={{ flex: '1 1 180px' }}>
                    <label htmlFor="ob-rc">
                      RC number <span style={{ color: 'var(--color-neutral-500)' }}>&mdash; optional</span>
                    </label>
                    <input
                      className="input"
                      id="ob-rc"
                      type="text"
                      placeholder="RC 1234567"
                      value={rcNumber}
                      onChange={(e) => setRcNumber(e.target.value)}
                    />
                  </div>
                </div>
                <p style={{ margin: 0, fontSize: 12.5, color: 'var(--color-neutral-600)' }}>
                  RC is your CAC registration number. Lenders usually ask for it, but you can add it later.
                </p>
              </div>

              {/* TODO: register account + create business before advancing. */}
              <button onClick={next} className="btn btn-primary btn-block blueprint" style={{ position: 'relative' }}>
                {cornerMarks}
                Create my account
              </button>
              <p style={{ margin: '14px 0 0 0', fontSize: 12.5, color: 'var(--color-neutral-600)', textAlign: 'center' }}>
                Already have an account? <a href="#">Sign in</a>
              </p>
            </div>
          )}

          {step === 'done' && (
            <div style={{ textAlign: 'center', padding: '10px 0 6px 0' }}>
              <div
                style={{
                  width: 62,
                  height: 62,
                  margin: '0 auto 22px auto',
                  border: '1px solid var(--color-accent)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <svg width="30" height="30" viewBox="0 0 24 24" fill="none" stroke="var(--color-accent-700)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
                  <path d="m4 12 5 5L20 6" />
                </svg>
              </div>
              <h1 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 38, lineHeight: 1.05, margin: '0 0 12px 0' }}>
                You&rsquo;re set up
              </h1>
              <p style={{ margin: '0 auto 28px auto', fontSize: 16, lineHeight: 1.55, color: 'var(--color-neutral-700)', maxWidth: '42ch' }}>
                Your books are open. Connect your bank from the dashboard and vbook will start recording what comes in and goes
                out &mdash; you only confirm what things were for.
              </p>

              <div style={{ display: 'flex', flexDirection: 'column', gap: 11, textAlign: 'left', marginBottom: 30 }}>
                {DONE_ITEMS.map((d) => (
                  <div key={d} style={{ display: 'flex', alignItems: 'flex-start', gap: 10, borderTop: '1px solid var(--color-divider)', paddingTop: 11 }}>
                    <span style={{ fontFamily: 'var(--font-heading)', fontSize: 13, color: 'var(--color-accent-700)', lineHeight: 1.5 }}>
                      &#10003;
                    </span>
                    <span style={{ fontSize: 14.5, lineHeight: 1.5 }}>{d}</span>
                  </div>
                ))}
              </div>

              {/* TODO: route to the dashboard once it exists. */}
              <button className="btn btn-primary btn-block blueprint" style={{ position: 'relative' }}>
                {cornerMarks}
                Go to my dashboard
              </button>
              <div style={{ display: 'flex', justifyContent: 'center', marginTop: 12 }}>
                <button onClick={restart} className="btn btn-ghost" style={{ fontSize: 13 }}>
                  Start the setup again
                </button>
              </div>
            </div>
          )}
        </div>
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

const sectionKickerStyle: CSSProperties = {
  fontSize: 11.5,
  letterSpacing: '0.16em',
  textTransform: 'uppercase',
  color: 'var(--color-neutral-600)',
}

const headingStyle: CSSProperties = {
  fontFamily: 'var(--font-heading)',
  fontWeight: 600,
  fontSize: 34,
  lineHeight: 1.08,
  margin: '0 0 10px 0',
}

const leadStyle: CSSProperties = {
  margin: '0 0 26px 0',
  fontSize: 15.5,
  lineHeight: 1.5,
  color: 'var(--color-neutral-700)',
  maxWidth: '46ch',
}

interface FieldProps {
  id: string
  label: string
  type: string
  placeholder: string
  value: string
  onChange: (value: string) => void
  flex?: string
}

function Field({ id, label, type, placeholder, value, onChange, flex }: FieldProps) {
  return (
    <div className="field" style={flex ? { flex } : undefined}>
      <label htmlFor={id}>{label}</label>
      <input
        className="input"
        id={id}
        type={type}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
    </div>
  )
}
