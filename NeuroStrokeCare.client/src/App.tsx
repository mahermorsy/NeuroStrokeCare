import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from '@/context/AuthContext'
import ProtectedRoute from '@/components/ProtectedRoute'
import Layout from '@/components/Layout'
import Login from '@/pages/Login'
import Dashboard from '@/pages/Dashboard'
import Patients from '@/pages/Patients'
import Admissions from '@/pages/Admissions'
import WardsBeds from '@/pages/WardsBeds'
import Assessments from '@/pages/Assessments'
import LabResults from '@/pages/LabResults'
import DoorTiming from '@/pages/DoorTiming'
import Users from '@/pages/Users'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            element={
              <ProtectedRoute>
                <Layout />
              </ProtectedRoute>
            }
          >
            <Route index element={<Dashboard />} />
            <Route path="/patients" element={<Patients />} />
            <Route path="/admissions" element={<Admissions />} />
            <Route path="/wards" element={<WardsBeds />} />
            <Route path="/assessments" element={<Assessments />} />
            <Route path="/lab-results" element={<LabResults />} />
            <Route path="/door-timing" element={<DoorTiming />} />
            <Route path="/users" element={<Users />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
