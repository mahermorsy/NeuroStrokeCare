import { api } from '@/lib/api'
import type { UserSummaryResponse, PendingUserResponse } from '@/types/entities'

// Not a generic CRUD entity — these ride on /api/Auth, admin-only, and there
// is no paged/status/delete endpoint for users (yet), so this stays a small
// hand-written client instead of going through entityApi().

export interface CreateUserPayload {
  firstName: string
  lastName: string
  email: string
  userName: string
  password: string
  confirmPassword: string
  phoneNumber?: string
  role: string
  employeeId?: string
}

interface AuthResponse {
  success: boolean
  message: string
  errors?: string[]
}

export interface ChangePasswordPayload {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

export const usersApi = {
  list: () => api.get<UserSummaryResponse[]>('/Auth/users').then((r) => r.data),
  create: (payload: CreateUserPayload) => api.post('/Auth/register', payload).then((r) => r.data),
  listPending: () => api.get<PendingUserResponse[]>('/Auth/pending-users').then((r) => r.data),
  approve: (id: string, role: string, employeeId?: string) =>
    api.post(`/Auth/approve/${id}`, null, { params: { role, employeeId } }).then((r) => r.data),
  reject: (id: string) => api.post(`/Auth/reject/${id}`).then((r) => r.data),
  updateAdmin: (
    id: string,
    payload: {
      role?: string
      employeeId?: string
      profession?: string
      jobTitle?: string
      academicDegree?: string
      department?: string
    },
  ) => api.put<AuthResponse>(`/Auth/users/${id}`, payload).then((r) => r.data),
  myProfile: () => api.get<UserSummaryResponse>('/Auth/profile').then((r) => r.data),
  // Self-service edit of the user's own non-privileged fields only (FirstName/LastName/
  // Email/PhoneNumber) - rides the existing PUT /api/auth/profile endpoint, whose
  // UpdateProfileRequest DTO deliberately has no Role/EmployeeId/Profession/JobTitle/
  // AcademicDegree/Department fields, so this can never touch privileged employment data
  // (ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS, area 4).
  updateProfile: (payload: { firstName: string; lastName: string; email: string; phoneNumber?: string }) =>
    api.put<AuthResponse>('/Auth/profile', payload).then((r) => r.data),
  // Authenticated change-password, for a signed-in user who knows their
  // current password — distinct from the forgot/reset-password flow, which
  // stays for the "I can't log in at all" case.
  changePassword: (payload: ChangePasswordPayload) =>
    api.post<AuthResponse>('/Auth/change-password', payload).then((r) => r.data),
  uploadPhoto: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api
      .post<AuthResponse & { photoUrl: string }>('/Auth/upload-photo', form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data)
  },
}
