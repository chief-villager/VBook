// Settings — React port of the "settings" section of "Vbook.dc.html", wired to the
// live API. Two cards:
//   Account — the signed-in user's name, email and their role in this business, read
//     from GET /api/users/me (the "who am I" endpoint).
//   Invoice template — the details printed on every invoice (logo + business name +
//     bank details + terms), saved via PUT /api/businesses/{id}/invoice-template.
//
// Visuals come from src/styles/industry.css. The API requires a logo file on every
// template save, so an existing logo is shown as a preview but the user must pick a
// file before "Save as default" is enabled.

import { useEffect, useMemo, useState } from 'react'
import AppShell from '../components/AppShell.tsx'
import { ApiError } from '../lib/api'
import { getBusinessId } from '../lib/auth'
import {
  getCurrentUser,
  getInvoiceTemplate,
  roleName,
  setInvoiceTemplate,
  type CurrentUser,
  type UserMembership,
} from '../lib/identity'

const cornerMarks = (
  <>
    <i className="corner tl" />
    <i className="corner tr" />
    <i className="corner bl" />
    <i className="corner br" />
  </>
)

// What each role can do, shown under the role row so the user knows their access.
function roleNote(role: string): string {
  switch (role) {
    case 'Owner':
      return 'Full access. Only you can add people and change roles.'
    case 'Admin':
      return 'Can manage the books and invoicing for this business.'
    case 'Accountant':
      return 'Can view and record entries, and prepare reports.'
    default:
      return 'Access is set by the account owner.'
  }
}

const LOGO_MAX_BYTES = 2 * 1024 * 1024

export default function Settings() {
  const [showTerms, setShowTerms] = useState(false)
  const businessId = useMemo(() => getBusinessId(), [])

  // Account card.
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  // Invoice-template form.
  const [invBiz, setInvBiz] = useState('')
  const [invBank, setInvBank] = useState('')
  const [invAcct, setInvAcct] = useState('')
  const [invTerms, setInvTerms] = useState('')
  const [logoFile, setLogoFile] = useState<File | null>(null)
  const [existingLogoUrl, setExistingLogoUrl] = useState<string | null>(null)

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [tplStatus, setTplStatus] = useState('')

  // The membership for the business the app is currently reporting on (the primary
  // one); its role drives the "Role" row.
  const membership: UserMembership | undefined = useMemo(() => {
    if (!user) return undefined
    return user.memberships.find((m) => m.businessId.value === businessId) ?? user.memberships[0]
  }, [user, businessId])

  const role = membership ? roleName(membership.role) : 'Member'
  const businessName = membership?.businessName ?? ''

  // Load the profile and any existing invoice template in parallel.
  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setLoadError(null)

    Promise.all([getCurrentUser(), businessId ? getInvoiceTemplate(businessId) : Promise.resolve(null)])
      .then(([me, template]) => {
        if (cancelled) return
        setUser(me)
        if (template) {
          setInvBiz(template.businessName)
          setInvBank(template.bankName)
          setInvAcct(template.accountNumber)
          setInvTerms(template.terms)
          setExistingLogoUrl(template.logoUrl || null)
        } else if (me) {
          // No template yet: seed the business name from the profile so the first
          // save starts from something sensible.
          const primary = me.memberships.find((m) => m.businessId.value === businessId) ?? me.memberships[0]
          setInvBiz(primary?.businessName ?? '')
        }
      })
      .catch((err) => {
        if (!cancelled) setLoadError(err instanceof ApiError ? err.message : 'Could not load your settings.')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [businessId])

  // A blob URL for the freshly picked file, revoked when it changes / unmounts.
  const logoPreview = useMemo(() => (logoFile ? URL.createObjectURL(logoFile) : null), [logoFile])
  useEffect(() => {
    return () => {
      if (logoPreview) URL.revokeObjectURL(logoPreview)
    }
  }, [logoPreview])

  function onPickLogo(e: React.ChangeEvent<HTMLInputElement>) {
    setSaveError(null)
    setTplStatus('')
    const file = e.target.files?.[0] ?? null
    if (file && file.size > LOGO_MAX_BYTES) {
      setSaveError('Logo exceeds the 2 MB limit.')
      setLogoFile(null)
      return
    }
    setLogoFile(file)
  }

  async function saveTemplate() {
    if (!businessId || saving) return
    setSaveError(null)
    setTplStatus('')

    if (!logoFile) {
      setSaveError('Choose a logo to save the template.')
      return
    }
    if (!invBiz.trim()) {
      setSaveError('Business name is required.')
      return
    }

    setSaving(true)
    try {
      await setInvoiceTemplate(businessId, {
        logo: logoFile,
        businessName: invBiz.trim(),
        accountNumber: invAcct.trim(),
        bankName: invBank.trim(),
        terms: invTerms.trim(),
      })
      // Reflect the just-saved logo as the new "existing" preview and clear the picker.
      setExistingLogoUrl(logoPreview)
      setLogoFile(null)
      setTplStatus('Saved. This is now the default on every invoice.')
    } catch (err) {
      setSaveError(err instanceof ApiError ? err.message : 'Could not save the template.')
    } finally {
      setSaving(false)
    }
  }

  const previewUrl = logoPreview ?? existingLogoUrl

  return (
    <AppShell
      active="settings"
      title="Settings"
      kicker="Your account"
      showTerms={showTerms}
      onToggleTerms={() => setShowTerms((v) => !v)}
      businessName={businessName || undefined}
      userName={user?.displayName || undefined}
    >
      <div style={{ padding: '30px 40px 48px 40px', display: 'flex', flexDirection: 'column', gap: 26, maxWidth: 940 }}>
        {loading ? (
          <p style={{ margin: 0, fontSize: 14.5, color: 'var(--color-neutral-700)' }}>Loading your settings…</p>
        ) : loadError ? (
          <p style={{ margin: 0, fontSize: 14.5, color: '#b3261e' }}>{loadError}</p>
        ) : (
          <>
            {/* Account card. */}
            <section className="card blueprint" style={{ position: 'relative', padding: '28px 28px 26px 28px' }}>
              {cornerMarks}
              <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '16px 24px', marginBottom: 26 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 16, minWidth: 0 }}>
                  <div
                    style={{
                      width: 60,
                      height: 60,
                      flex: '0 0 60px',
                      background: 'var(--color-accent)',
                      color: '#fff',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontFamily: 'var(--font-heading)',
                      fontWeight: 600,
                      fontSize: 24,
                    }}
                  >
                    {avatarInitials(user?.displayName ?? '')}
                  </div>
                  <div style={{ minWidth: 0 }}>
                    <h2 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 26, margin: '0 0 4px 0' }}>
                      {user?.displayName}
                    </h2>
                    <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 10, fontSize: 13.5, color: 'var(--color-neutral-700)' }}>
                      <span>{user?.email}</span>
                      <span className="tag tag-accent">{role}</span>
                    </div>
                  </div>
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '190px 1fr', gap: 0 }}>
                <Divider />
                <RowLabel>Display name</RowLabel>
                <RowValue>
                  <span style={{ fontSize: 15.5 }}>{user?.displayName}</span>
                </RowValue>

                <Divider />
                <RowLabel>Email</RowLabel>
                <RowValue>
                  <span style={{ fontSize: 15.5 }}>{user?.email}</span>
                </RowValue>

                <Divider />
                <RowLabel>Role</RowLabel>
                <div style={{ padding: '16px 0', display: 'flex', flexDirection: 'column', gap: 4 }}>
                  <span style={{ fontSize: 15.5 }}>{role}</span>
                  <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>{roleNote(role)}</span>
                </div>
                <Divider />
              </div>
            </section>

            <p style={{ margin: 0, fontSize: 13.5, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '62ch' }}>
              Only the account owner can change roles. Ask them if you need different access.
            </p>

            {/* Invoice template card. */}
            <section className="card blueprint" style={{ position: 'relative', padding: 28 }}>
              {cornerMarks}
              <div style={{ fontSize: 12, letterSpacing: '0.14em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 10 }}>
                Invoices
              </div>
              <h2 style={{ fontFamily: 'var(--font-heading)', fontWeight: 600, fontSize: 24, margin: '0 0 6px 0' }}>Set invoice template</h2>
              <p style={{ margin: '0 0 24px 0', fontSize: 14.5, lineHeight: 1.5, color: 'var(--color-neutral-700)', maxWidth: '62ch' }}>
                These details appear on every invoice you send. You can still change any single invoice before you send it.
              </p>

              <div style={{ display: 'grid', gridTemplateColumns: '240px 1fr', gap: '24px 32px' }}>
                <div>
                  <div style={{ fontSize: 12, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)', marginBottom: 10 }}>
                    Logo
                  </div>
                  <label
                    className="blueprint"
                    style={{ position: 'relative', display: 'block', border: '1px solid var(--color-divider)', padding: 10, cursor: 'pointer' }}
                  >
                    {cornerMarks}
                    <div
                      style={{
                        width: '100%',
                        height: 150,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        background: 'var(--color-neutral-100)',
                        overflow: 'hidden',
                      }}
                    >
                      {previewUrl ? (
                        <img src={previewUrl} alt="Invoice logo" style={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
                      ) : (
                        <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>Choose your logo</span>
                      )}
                    </div>
                    <input type="file" accept="image/png,image/jpeg" onChange={onPickLogo} style={{ display: 'none' }} />
                  </label>
                  <p style={{ margin: '10px 0 0 0', fontSize: 12.5, lineHeight: 1.5, color: 'var(--color-neutral-600)' }}>
                    PNG or JPG, at least 400px wide. Prints at the top of every invoice.
                  </p>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: 20, alignContent: 'start' }}>
                  <div className="field">
                    <label htmlFor="inv-biz">Business name</label>
                    <input className="input" id="inv-biz" value={invBiz} onChange={(e) => setInvBiz(e.target.value)} />
                  </div>
                  <div className="field">
                    <label htmlFor="inv-bank">Bank name</label>
                    <input className="input" id="inv-bank" value={invBank} onChange={(e) => setInvBank(e.target.value)} />
                  </div>
                  <div className="field">
                    <label htmlFor="inv-acct">Account number</label>
                    <input
                      className="input"
                      id="inv-acct"
                      value={invAcct}
                      onChange={(e) => setInvAcct(e.target.value.replace(/[^0-9]/g, ''))}
                      inputMode="numeric"
                    />
                  </div>
                  <div className="field" style={{ gridColumn: '1 / -1' }}>
                    <label htmlFor="inv-terms">Terms</label>
                    <textarea
                      className="input"
                      id="inv-terms"
                      rows={3}
                      value={invTerms}
                      onChange={(e) => setInvTerms(e.target.value)}
                      style={{ resize: 'vertical', fontFamily: 'var(--font-body)' }}
                    />
                  </div>
                </div>
              </div>

              <div style={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 14, paddingTop: 24 }}>
                <button onClick={saveTemplate} disabled={saving} className="btn btn-primary blueprint" style={{ position: 'relative', opacity: saving ? 0.6 : 1 }}>
                  {cornerMarks}
                  {saving ? 'Saving…' : 'Save as default'}
                </button>
                {saveError ? (
                  <span style={{ fontSize: 13, color: '#b3261e' }}>{saveError}</span>
                ) : (
                  <span style={{ fontSize: 13, color: 'var(--color-neutral-600)' }}>{tplStatus}</span>
                )}
              </div>
            </section>
          </>
        )}
      </div>
    </AppShell>
  )
}

// "Adaeze Okafor" -> "AO"; a single word yields its first two letters.
function avatarInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '—'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

// The account-card rows share a label/value grid split by full-width dividers.
function Divider() {
  return <div style={{ gridColumn: '1 / -1', height: 1, background: 'var(--color-divider)' }} />
}

function RowLabel({ children }: { children: React.ReactNode }) {
  return (
    <div style={{ padding: '16px 0', fontSize: 12, letterSpacing: '0.12em', textTransform: 'uppercase', color: 'var(--color-neutral-600)' }}>
      {children}
    </div>
  )
}

function RowValue({ children }: { children: React.ReactNode }) {
  return <div style={{ padding: '16px 0', display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: '10px 14px' }}>{children}</div>
}
