import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { AuthProvider } from '@/context/AuthContext'
import { PatientSelectionProvider } from '@/context/PatientSelectionContext'
import ProtectedRoute from '@/components/ProtectedRoute'
import RequireRole from '@/components/RequireRole'
import Layout from '@/components/Layout'
import Login from '@/pages/Login'
import ForgotPassword from '@/pages/ForgotPassword'
import ResetPassword from '@/pages/ResetPassword'
import RequestAccount from '@/pages/RequestAccount'
import IdCard from '@/pages/IdCard'
import ChangePassword from '@/pages/ChangePassword'
import Dashboard from '@/pages/Dashboard'
import Alerts from '@/pages/Alerts'
import Patients from '@/pages/Patients'
import PatientSummary from '@/pages/PatientSummary'
import Admissions from '@/pages/Admissions'
import WardsBeds from '@/pages/WardsBeds'
import Assessments from '@/pages/Assessments'
import LabResults from '@/pages/LabResults'
import DoorTiming from '@/pages/DoorTiming'
import FollowUp from '@/pages/FollowUp'
import Report from '@/pages/Report'
import Users from '@/pages/Users'
import NotFound from '@/pages/NotFound'

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <PatientSelectionProvider>
          <Routes>
            <Route path="/login" element={<Login />} />
            <Route path="/forgot-password" element={<ForgotPassword />} />
            <Route path="/reset-password" element={<ResetPassword />} />
            <Route path="/request-account" element={<RequestAccount />} />
            <Route
              element={
                <ProtectedRoute>
                  <Layout />
                </ProtectedRoute>
              }
            >
              <Route index element={<Dashboard />} />
              <Route path="/alerts" element={<Alerts />} />
              <Route path="/patients" element={<Patients />} />
              {/* PHASE 11 (area 6): compact clinical overview. :admissionId is optional — when
                  omitted the page falls back to the shared Patient Selection Context. */}
              <Route path="/patient-summary" element={<PatientSummary />} />
              <Route path="/patient-summary/:admissionId" element={<PatientSummary />} />
              <Route path="/admissions" element={<Admissions />} />
              <Route path="/wards" element={<WardsBeds />} />
              <Route path="/assessments" element={<Assessments />} />
              <Route path="/lab-results" element={<LabResults />} />
              <Route path="/door-timing" element={<DoorTiming />} />
              <Route path="/follow-up" element={<FollowUp />} />
              <Route path="/id-card" element={<IdCard />} />
              <Route path="/account/change-password" element={<ChangePassword />} />
              <Route path="/report" element={<Report />} />
              <Route path="/report/:admissionId" element={<Report />} />
              <Route
                path="/users"
                element={
                  <RequireRole roles={['Admin']}>
                    <Users />
                  </RequireRole>
                }
              />
              <Route path="*" element={<NotFound />} />
            </Route>
          </Routes>
        </PatientSelectionProvider>
      </AuthProvider>
    </BrowserRouter>
  )
}
