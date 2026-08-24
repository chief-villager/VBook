import { Navigate, Route, Routes } from 'react-router-dom'
import RequireAuth from './components/RequireAuth.tsx'
import SessionBootstrap from './components/SessionBootstrap.tsx'
import Onboarding from './pages/Onboarding.tsx'
import SignIn from './pages/SignIn.tsx'
import Dashboard from './pages/Dashboard.tsx'
import Transactions from './pages/Transactions.tsx'
import Invoices from './pages/Invoices.tsx'
import Ledger from './pages/Ledger.tsx'
import Reports from './pages/Reports.tsx'
import CreditReadiness from './pages/CreditReadiness.tsx'
import Settings from './pages/Settings.tsx'

export default function App() {
  return (
    <SessionBootstrap>
    <Routes>
      {/* Public: no session required. */}
      <Route path="/onboarding" element={<Onboarding />} />
      <Route path="/signin" element={<SignIn />} />
      {/* Protected: RequireAuth redirects to /signin when there's no session. */}
      <Route path="/dashboard" element={<RequireAuth><Dashboard /></RequireAuth>} />
      <Route path="/transactions" element={<RequireAuth><Transactions /></RequireAuth>} />
      <Route path="/invoices" element={<RequireAuth><Invoices /></RequireAuth>} />
      <Route path="/ledger" element={<RequireAuth><Ledger /></RequireAuth>} />
      {/* Reports defaults to Profit and Loss; the sidebar submenu switches :tab. */}
      <Route path="/reports" element={<Navigate to="/reports/pl" replace />} />
      <Route path="/reports/:tab" element={<RequireAuth><Reports /></RequireAuth>} />
      <Route path="/credit" element={<RequireAuth><CreditReadiness /></RequireAuth>} />
      <Route path="/settings" element={<RequireAuth><Settings /></RequireAuth>} />
      <Route path="*" element={<Navigate to="/onboarding" replace />} />
    </Routes>
    </SessionBootstrap>
  )
}
