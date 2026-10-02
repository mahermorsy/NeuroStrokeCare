import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { requestPasswordReset } from '@/lib/passwordResetApi'

export default function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    setMessage(null)
    try {
      const result = await requestPasswordReset(email)
      setMessage(result.message)
    } catch {
      setError('Something went wrong — please try again in a moment.')
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
          <img
            src="/brand/NeuroStrokeCare_Logo.svg"
            alt="NeuroStrokeCare — Mansoura University Hospital"
            className="h-auto w-full max-w-[280px] rounded-xl object-contain"
          />
          <div>
            <h1 className="font-display text-[20px] font-semibold text-text">Reset your password</h1>
            <p className="mt-1 text-[13px] text-text-secondary">
              We'll email you a link to set a new password.
            </p>
          </div>
        </div>

        {message ? (
          <p className="rounded-lg bg-success-bg px-3 py-2.5 text-[13.5px] font-medium text-success">{message}</p>
        ) : (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
              Email
              <input
                type="email"
                required
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                placeholder="you@hospital.edu.eg"
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
              {submitting ? 'Sending…' : 'Send reset link'}
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
