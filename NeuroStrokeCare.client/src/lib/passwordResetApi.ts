import { api } from '@/lib/api'

interface AuthResponse {
  success: boolean
  message: string
  errors?: string[]
}

export function requestPasswordReset(email: string) {
  return api.post<AuthResponse>('/Auth/forgot-password', { email }).then((r) => r.data)
}

export function resetPassword(params: {
  email: string
  token: string
  newPassword: string
  confirmNewPassword: string
}) {
  return api.post<AuthResponse>('/Auth/reset-password', params).then((r) => r.data)
}
