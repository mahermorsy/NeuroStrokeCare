import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { api, getToken, setToken, clearToken } from '@/lib/api'

export interface AuthUser {
  userId?: string
  userName?: string
  email?: string
  role?: string
}

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
  login: (userNameOrEmail: string, password: string) => Promise<void>
  logout: () => void
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
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

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
    isAuthenticated: Boolean(getToken()),
    isLoading,
    error,
    login,
    logout,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside an AuthProvider')
  return ctx
}
