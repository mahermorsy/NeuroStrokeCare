import { api } from '@/lib/api'

interface AuthResponse {
  success: boolean
  message: string
  errors?: string[]
}

export interface RegisterRequestPayload {
  firstName: string
  lastName: string
  email: string
  userName: string
  password: string
  confirmPassword: string
  phoneNumber?: string
  requestedRole?: string
}

// Public, unauthenticated self-registration request. Creates the account
// unapproved (no role, cannot log in) — an Admin must approve it from the
// Staff page before it can be used.
export function requestNewAccount(payload: RegisterRequestPayload) {
  return api.post<AuthResponse>('/Auth/register-request', payload).then((r) => r.data)
}
