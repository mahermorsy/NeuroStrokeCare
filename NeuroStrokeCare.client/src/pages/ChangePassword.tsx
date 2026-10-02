import { useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { usersApi } from '@/lib/usersApi'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import { Field, TextInput } from '@/components/FormField'
import { describeApiError } from '@/lib/apiError'

export default function ChangePassword() {
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmNewPassword, setConfirmNewPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  // Mirrors the required/confirm-match checks the backend DTO already
  // enforces (ChangePasswordRequest) — this just gives faster feedback
  // before the request round-trip. The actual password policy (length,
  // complexity, etc.) is still enforced by Identity server-side; we don't
  // duplicate those rules here so the two can never drift apart.
  function validate(): string | null {
    if (!currentPassword.trim() || !newPassword.trim() || !confirmNewPassword.trim()) {
      return 'All fields are required.'
    }
    if (newPassword.length < 6) {
      return 'New password must be at least 6 characters.'
    }
    if (newPassword !== confirmNewPassword) {
      return 'New password and confirmation do not match.'
    }
    return null
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (submitting) return
    setError(null)
    setSuccess(false)

    const validationError = validate()
    if (validationError) {
      setError(validationError)
      return
    }

    setSubmitting(true)
    try {
      const result = await usersApi.changePassword({ currentPassword, newPassword, confirmNewPassword })
      if (!result.success) {
        setError(result.errors?.join(' ') || result.message || 'Could not change the password — try again.')
        return
      }
      setSuccess(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmNewPassword('')
    } catch (err) {
      // The backend returns a generic failure message on a wrong current
      // password (it never confirms/denies the password itself beyond
      // that), plus any Identity policy errors (e.g. "needs a digit").
      setError(
        describeApiError(err, {
          400: 'Could not change the password — check your current password and try again.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="Change Password" subtitle="Update the password for your own account" />

      <Card className="flex max-w-[460px] flex-col gap-4">
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <Field label="Current password">
            <TextInput
              type="password"
              required
              autoComplete="current-password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
            />
          </Field>
          <Field label="New password">
            <TextInput
              type="password"
              required
              minLength={6}
              autoComplete="new-password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
            />
          </Field>
          <Field label="Confirm new password">
            <TextInput
              type="password"
              required
              autoComplete="new-password"
              value={confirmNewPassword}
              onChange={(e) => setConfirmNewPassword(e.target.value)}
            />
          </Field>

          {error && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{error}</p>
          )}
          {success && (
            <p className="rounded-lg bg-success-bg px-3 py-2 text-[13px] font-medium text-success">
              Password changed successfully.
            </p>
          )}

          <PrimaryButton type="submit" disabled={submitting} className="mt-1">
            {submitting ? 'Saving…' : 'Change password'}
          </PrimaryButton>
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="text-left text-[12.5px] font-medium text-text-muted hover:text-text"
          >
            Cancel
          </button>
        </form>
      </Card>
    </motion.div>
  )
}
