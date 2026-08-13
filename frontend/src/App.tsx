import { Navigate, Route, Routes } from 'react-router-dom'
import RequireAuth from './components/RequireAuth.tsx'
import Onboarding from './pages/Onboarding.tsx'
import SignIn from './pages/SignIn.tsx'
import Dashboard from './pages/Dashboard.tsx'
import Transactions from './pages/Transactions.tsx'
import Invoices from './pages/Invoices.tsx'
import Ledger from './pages/Ledger.tsx'
import Reports from './pages/Reports.tsx'
import ComingSoon from './pages/ComingSoon.tsx'

export default function App() {
  return (
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
      {/* Sidebar destinations that aren't built yet — placeholders so nav doesn't dead-end. */}
      <Route path="/credit" element={<RequireAuth><ComingSoon section="credit" /></RequireAuth>} />
      <Route path="*" element={<Navigate to="/onboarding" replace />} />
    </Routes>
  )
}
