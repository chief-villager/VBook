import { Navigate, Route, Routes } from 'react-router-dom'
import Onboarding from './pages/Onboarding.tsx'

export default function App() {
  return (
    <Routes>
      <Route path="/onboarding" element={<Onboarding />} />
      <Route path="*" element={<Navigate to="/onboarding" replace />} />
    </Routes>
  )
}
