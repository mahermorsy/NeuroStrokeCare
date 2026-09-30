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
}

export const usersApi = {
  list: () => api.get<UserSummaryResponse[]>('/Auth/users').then((r) => r.data),
  create: (payload: CreateUserPayload) => api.post('/Auth/register', payload).then((r) => r.data),
  listPending: () => api.get<PendingUserResponse[]>('/Auth/pending-users').then((r) => r.data),
  approve: (id: string, role: string) =>
    api.post(`/Auth/approve/${id}`, null, { params: { role } }).then((r) => r.data),
  reject: (id: string) => api.post(`/Auth/reject/${id}`).then((r) => r.data),
}
