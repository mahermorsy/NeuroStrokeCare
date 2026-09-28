import { useState, type FormEvent } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { LogoMark } from '@/components/icons'

export default function Login() {
  const { login, isLoading, error } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [userNameOrEmail, setUserNameOrEmail] = useState('')
  const [password, setPassword] = useState('')

  const from = (location.state as { from?: string } | null)?.from ?? '/'

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    try {
      await login(userNameOrEmail, password)
      navigate(from, { replace: true })
    } catch {
      // error is already surfaced via auth context state
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-bg px-4">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.45, ease: 'easeOut' }}
        className="w-full max-w-[380px] rounded-2xl border border-border bg-surface p-9 shadow-sm"
      >
        <div className="mb-7 flex flex-col items-center gap-3 text-center">
          <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-accent">
            <LogoMark className="h-7 w-7" />
          </span>
          <div>
            <h1 className="font-display text-[22px] font-semibold text-text">NeuroStrokeCare</h1>
            <p className="mt-1 text-[13px] text-text-secondary">Mansoura University Hospital</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
            Username or email
            <input
              type="text"
              required
              autoComplete="username"
              value={userNameOrEmail}
              onChange={(e) => setUserNameOrEmail(e.target.value)}
              className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
              placeholder="doctor or you@hospital.edu.eg"
            />
          </label>

          <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
            Password
            <input
              type="password"
              required
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
              placeholder="••••••••"
            />
          </label>

          {error && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="mt-2 rounded-lg bg-accent px-4 py-2.5 text-[14px] font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-60"
          >
            {isLoading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </motion.div>
    </div>
  )
}
