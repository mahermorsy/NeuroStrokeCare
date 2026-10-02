import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react'
import { api, getToken, setToken, clearToken } from '@/lib/api'
import { usersApi } from '@/lib/usersApi'
import type { UserSummaryResponse } from '@/types/entities'

export interface AuthUser {
  userId?: string
  userName?: string
  email?: string
  role?: string
}

interface AuthContextValue {
  user: AuthUser | null
  /** The richer `/Auth/profile` payload (photo, employee ID, full name) —
   *  null until it's loaded, which happens once per sign-in, not per route. */
  profile: UserSummaryResponse | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
  login: (userNameOrEmail: string, password: string) => Promise<void>
  logout: () => void
  /** Re-fetches `profile` on demand — call this after anything that changes
   *  it elsewhere (a photo upload, an Admin edit to this user) so every
   *  consumer of the context picks up the change without its own polling. */
  refreshUser: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

const USER_KEY = 'nsc_user'

// Matches NeuroStrokeCare.Service.Auth.Dtos.AuthResponse exactly (camelCase,
// as ASP.NET Core's default JSON serialization outputs it).
interface AuthResponse {
  success: boolean
  message: string
  errors?: string[]
  userId?: string
  userName?: string
  email?: string
  role?: string
  token?: string
  expiresOn?: string
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? (JSON.parse(raw) as AuthUser) : null
  })
  const [profile, setProfile] = useState<UserSummaryResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const refreshUser = useCallback(async () => {
    if (!getToken()) {
      setProfile(null)
      return
    }
    try {
      const p = await usersApi.myProfile()
      setProfile(p)
    } catch {
      // Silent — consumers fall back to the lighter `user` payload from login.
    }
  }, [])

  // Loads once per sign-in, not per route: fires when `user` first appears
  // (a fresh login, or restoring a session from localStorage on app start)
  // and clears the profile again on logout.
  useEffect(() => {
    if (user) {
      refreshUser()
    } else {
      setProfile(null)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [user?.userId])

  const login = useCallback(async (userNameOrEmail: string, password: string) => {
    setIsLoading(true)
    setError(null)
    try {
      const { data } = await api.post<AuthResponse>('/Auth/login', {
        userNameOrEmail,
        password,
      })

      if (!data.success || !data.token) {
        throw new Error(data.message || 'Invalid username/email or password.')
      }

      setToken(data.token)
      const nextUser: AuthUser = {
        userId: data.userId,
        userName: data.userName,
        email: data.email,
        role: data.role,
      }
      localStorage.setItem(USER_KEY, JSON.stringify(nextUser))
      setUser(nextUser)
    } catch (err) {
      const message =
        (err as { response?: { data?: AuthResponse } })?.response?.data?.message ??
        (err instanceof Error ? err.message : 'Invalid username/email or password.')
      setError(message)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [])

  const logout = useCallback(() => {
    clearToken()
    localStorage.removeItem(USER_KEY)
    setUser(null)
  }, [])

  const value: AuthContextValue = {
    user,
    profile,
    isAuthenticated: Boolean(getToken()),
    isLoading,
    error,
    login,
    logout,
    refreshUser,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside an AuthProvider')
  return ctx
}
