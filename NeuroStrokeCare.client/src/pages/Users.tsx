import { useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { useEntityList } from '@/hooks/useEntityList'
import { usersApi } from '@/lib/usersApi'
import type { UserSummaryResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { ALL_ROLES, ROLE_LABELS, roleLabel } from '@/lib/roles'

const ROLE_TONE: Record<string, 'critical' | 'info' | 'success' | 'neutral'> = {
  Admin: 'critical',
  Consultant: 'info',
  Registrar: 'info',
  Resident: 'neutral',
  Nurse: 'success',
  NursingSupervisor: 'success',
}

const emptyForm = {
  firstName: '',
  lastName: '',
  email: '',
  userName: '',
  password: '',
  confirmPassword: '',
  phoneNumber: '',
  role: 'Staff',
}

export default function Users() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useEntityList(() => usersApi.list())

  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  if (user?.role !== 'Admin') {
    return (
      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
        <PageHeader title="Staff" subtitle="Manage the clinicians and staff who can sign in" />
        <Card>
          <p className="text-[13.5px] text-text-secondary">
            This section is limited to Admin accounts. Ask an administrator if you need a new account created.
          </p>
        </Card>
      </motion.div>
    )
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setFormError(null)
    try {
      await usersApi.create(form)
      setModalOpen(false)
      setForm(emptyForm)
      reload()
    } catch (err) {
      const message = (err as { response?: { data?: { errors?: string[]; message?: string } } })?.response?.data
      setFormError(
        (Array.isArray(message?.errors) && message.errors.join(', ')) ||
          message?.message ||
          'Could not create the account — check the fields and try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  const columns: Column<UserSummaryResponse>[] = [
    { header: 'Name', render: (u) => `${u.firstName} ${u.lastName}` },
    { header: 'Username', render: (u) => u.userName },
    { header: 'Email', render: (u) => <span className="text-text-secondary">{u.email}</span> },
    { header: 'Phone', render: (u) => u.phoneNumber ?? '—' },
    { header: 'Role', render: (u) => <StatusPill label={roleLabel(u.role)} tone={ROLE_TONE[u.role] ?? 'neutral'} /> },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Staff"
        subtitle={`${data.length} account${data.length === 1 ? '' : 's'} can sign in`}
        action={<PrimaryButton onClick={() => setModalOpen(true)}>+ New account</PrimaryButton>}
      />

      <Card>
        <DataTable
          columns={columns}
          rows={data}
          rowKey={(u) => u.id}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyMessage="No staff accounts yet."
        />
      </Card>

      <Modal open={modalOpen} title="New staff account" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="First name">
              <TextInput
                required
                value={form.firstName}
                onChange={(e) => setForm({ ...form, firstName: e.target.value })}
              />
            </Field>
            <Field label="Last name">
              <TextInput
                required
                value={form.lastName}
                onChange={(e) => setForm({ ...form, lastName: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Email">
            <TextInput
              type="email"
              required
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
            />
          </Field>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Username">
              <TextInput
                required
                minLength={3}
                value={form.userName}
                onChange={(e) => setForm({ ...form, userName: e.target.value })}
              />
            </Field>
            <Field label="Role">
              <Select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value })}>
                {ALL_ROLES.map((r) => (
                  <option key={r} value={r}>
                    {ROLE_LABELS[r]}
                  </option>
                ))}
              </Select>
            </Field>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Password">
              <TextInput
                type="password"
                required
                minLength={6}
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
              />
            </Field>
            <Field label="Confirm password">
              <TextInput
                type="password"
                required
                minLength={6}
                value={form.confirmPassword}
                onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Phone (optional)">
            <TextInput
              value={form.phoneNumber}
              onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
            />
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting} className="mt-1">
            {submitting ? 'Creating…' : 'Create account'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
