// Placeholder for sidebar sections that aren't built yet, so navigation from the
// AppShell never dead-ends. Each renders inside the shell with its own title.

import { useState } from 'react'
import AppShell, { type Section } from '../components/AppShell.tsx'

type Placeholder = Exclude<Section, 'transactions' | 'dashboard' | 'invoices' | 'ledger' | 'reports' | 'settings'>

const META: Record<Placeholder, { title: string; kicker: string }> = {
  credit: { title: 'Credit Readiness', kicker: 'Getting loan-ready' },
}

export default function ComingSoon({ section }: { section: Placeholder }) {
  const [showTerms, setShowTerms] = useState(false)
  const { title, kicker } = META[section]

  return (
    <AppShell active={section} title={title} kicker={kicker} showTerms={showTerms} onToggleTerms={() => setShowTerms((s) => !s)}>
      <div style={{ padding: '40px', display: 'flex' }}>
        <div className="card blueprint" style={{ position: 'relative', padding: 26, textAlign: 'center', flex: 1 }}>
          <i className="corner tl" />
          <i className="corner tr" />
          <i className="corner bl" />
          <i className="corner br" />
          <p style={{ margin: 0, fontSize: 15, color: 'var(--color-neutral-700)' }}>This section is coming soon.</p>
        </div>
      </div>
    </AppShell>
  )
}
