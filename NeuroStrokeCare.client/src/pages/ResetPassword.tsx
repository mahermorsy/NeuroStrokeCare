import { useState, type FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogoMark } from '@/components/icons'
import { resetPassword } from '@/lib/passwordResetApi'

export default function ResetPassword() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmNewPassword, setConfirmNewPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState(false)

  const linkInvalid = !email || !token

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const result = await resetPassword({ email, token, newPassword, confirmNewPassword })
      if (!result.success) {
        setError(result.errors?.join(', ') ?? result.message)
        return
      }
      setDone(true)
      setTimeout(() => navigate('/login', { replace: true }), 1800)
    } catch {
      setError('Could not reset the password — the link may have expired, request a new one.')
    } finally {
      setSubmitting(false)
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
          <h1 className="font-display text-[20px] font-semibold text-text">Set a new password</h1>
        </div>

        {linkInvalid ? (
          <p className="rounded-lg bg-critical-bg px-3 py-2.5 text-[13.5px] font-medium text-critical">
            This reset link is missing its email or token — open it directly from the email you received, or request
            a new one.
          </p>
        ) : done ? (
          <p className="rounded-lg bg-success-bg px-3 py-2.5 text-[13.5px] font-medium text-success">
            Password reset — redirecting to sign in…
          </p>
        ) : (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
              New password
              <input
                type="password"
                required
                minLength={6}
                autoComplete="new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
              />
            </label>

            <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
              Confirm new password
              <input
                type="password"
                required
                minLength={6}
                autoComplete="new-password"
                value={confirmNewPassword}
                onChange={(e) => setConfirmNewPassword(e.target.value)}
                className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
              />
            </label>

            {error && (
              <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{error}</p>
            )}

            <button
              type="submit"
              disabled={submitting}
              className="mt-2 rounded-lg bg-accent px-4 py-2.5 text-[14px] font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-60"
            >
              {submitting ? 'Saving…' : 'Reset password'}
            </button>
          </form>
        )}

        <Link to="/login" className="mt-5 block text-center text-[13px] font-medium text-accent hover:underline">
          Back to sign in
        </Link>
      </motion.div>
    </div>
  )
}
