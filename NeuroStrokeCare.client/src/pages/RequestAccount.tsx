import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { motion } from 'framer-motion'
import { LogoMark } from '@/components/icons'
import { requestNewAccount } from '@/lib/registerRequestApi'
import { ALL_ROLES, ROLE_LABELS } from '@/lib/roles'
import { describeApiError } from '@/lib/apiError'

const emptyForm = {
  firstName: '',
  lastName: '',
  email: '',
  userName: '',
  password: '',
  confirmPassword: '',
  phoneNumber: '',
  requestedRole: 'Resident',
}

export default function RequestAccount() {
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (submitting) return
    setSubmitting(true)
    setError(null)
    setMessage(null)
    try {
      const result = await requestNewAccount(form)
      setMessage(result.message)
    } catch (err) {
      setError(
        describeApiError(err, {
          400: 'Could not submit the request — check the fields and try again.',
          409: 'A request or account with this username or email already exists.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-bg px-4 py-10">
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.45, ease: 'easeOut' }}
        className="w-full max-w-[420px] rounded-2xl border border-border bg-surface p-9 shadow-sm"
      >
        <div className="mb-7 flex flex-col items-center gap-3 text-center">
          <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-accent">
            <LogoMark className="h-7 w-7" />
          </span>
          <div>
            <h1 className="font-display text-[20px] font-semibold text-text">Request an account</h1>
            <p className="mt-1 text-[13px] text-text-secondary">
              An administrator reviews every request and assigns your role before you can sign in.
            </p>
          </div>
        </div>

        {message ? (
          <p className="rounded-lg bg-success-bg px-3 py-2.5 text-[13.5px] font-medium text-success">{message}</p>
        ) : (
          <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
            <div className="grid grid-cols-2 gap-3">
              <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
                First name
                <input
                  required
                  value={form.firstName}
                  onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                  className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                />
              </label>
              <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
                Last name
                <input
                  required
                  value={form.lastName}
                  onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                  className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                />
              </label>
            </div>
            <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
              Email
              <input
                type="email"
                required
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
              />
            </label>
            <div className="grid grid-cols-2 gap-3">
              <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
                Username
                <input
                  required
                  minLength={3}
                  value={form.userName}
                  onChange={(e) => setForm({ ...form, userName: e.target.value })}
                  className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                />
              </label>
              <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
                Role you're requesting
                <select
                  value={form.requestedRole}
                  onChange={(e) => setForm({ ...form, requestedRole: e.target.value })}
                  className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                >
                  {ALL_ROLES.filter((r) => r !== 'Admin').map((r) => (
                    <option key={r} value={r}>
                      {ROLE_LABELS[r]}
                    </option>
                  ))}
                </select>
              </label>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
                Password
                <input
                  type="password"
                  required
                  minLength={6}
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                  className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                />
              </label>
              <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
                Confirm password
                <input
                  type="password"
                  required
                  minLength={6}
                  value={form.confirmPassword}
                  onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
                  className="rounded-lg border border-border px-3.5 py-2.5 text-[14px] text-text outline-none focus:border-accent"
                />
              </label>
            </div>
            <label className="flex flex-col gap-1.5 text-[13px] font-medium text-text-secondary">
              Phone (optional)
              <input
                value={form.phoneNumber}
                onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
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
              {submitting ? 'Submitting…' : 'Submit request'}
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
