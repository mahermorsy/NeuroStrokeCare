import { useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { useEntityList } from '@/hooks/useEntityList'
import { usersApi } from '@/lib/usersApi'
import type { UserSummaryResponse, PendingUserResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { ALL_ROLES, ROLE_LABELS, roleLabel } from '@/lib/roles'
import { describeApiError } from '@/lib/apiError'

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
  employeeId: '',
}

export default function Users() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useEntityList(() => usersApi.list())
  const pending = useEntityList(() => usersApi.listPending())

  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const [approveTarget, setApproveTarget] = useState<PendingUserResponse | null>(null)
  const [approveRole, setApproveRole] = useState('Resident')
  const [approveEmployeeId, setApproveEmployeeId] = useState('')
  const [approveBusy, setApproveBusy] = useState(false)
  const [approveError, setApproveError] = useState<string | null>(null)

  const [editTarget, setEditTarget] = useState<UserSummaryResponse | null>(null)
  const [editRole, setEditRole] = useState('Resident')
  const [editEmployeeId, setEditEmployeeId] = useState('')
  // ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 2/4): Profession/JobTitle/
  // AcademicDegree/Department are privileged employment fields - like EmployeeId and Role
  // above, they are only editable here (Admin-only page, see the role check below), never
  // through the self-service "My Profile" editor.
  const [editProfession, setEditProfession] = useState('')
  const [editJobTitle, setEditJobTitle] = useState('')
  const [editAcademicDegree, setEditAcademicDegree] = useState('')
  const [editDepartment, setEditDepartment] = useState('')
  const [editBusy, setEditBusy] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  // Phase F1 - Frontend Hardening (Section 16/17): Reject previously had no confirmation step,
  // no busy state, and no error handling at all - a double-click could fire two concurrent
  // reject calls, and any failure left the row silently stuck in the pending list with zero
  // feedback. Routed through a confirmation modal mirroring the existing Approve/Edit pattern.
  const [rejectTarget, setRejectTarget] = useState<PendingUserResponse | null>(null)
  const [rejectBusy, setRejectBusy] = useState(false)
  const [rejectError, setRejectError] = useState<string | null>(null)

  async function handleApprove(e: FormEvent) {
    e.preventDefault()
    if (!approveTarget) return
    setApproveBusy(true)
    setApproveError(null)
    try {
      await usersApi.approve(approveTarget.id, approveRole, approveEmployeeId || undefined)
      setApproveTarget(null)
      pending.reload()
      reload()
    } catch (err) {
      setApproveError(describeApiError(err, { 400: 'Could not approve this account — try again.' }))
    } finally {
      setApproveBusy(false)
    }
  }

  async function handleReject() {
    if (!rejectTarget) return
    setRejectBusy(true)
    setRejectError(null)
    try {
      await usersApi.reject(rejectTarget.id)
      setRejectTarget(null)
      pending.reload()
    } catch (err) {
      setRejectError(describeApiError(err, { 400: 'Could not reject this request — try again.' }))
    } finally {
      setRejectBusy(false)
    }
  }

  async function handleEdit(e: FormEvent) {
    e.preventDefault()
    if (!editTarget) return
    setEditBusy(true)
    setEditError(null)
    try {
      await usersApi.updateAdmin(editTarget.id, {
        role: editRole,
        employeeId: editEmployeeId,
        profession: editProfession,
        jobTitle: editJobTitle,
        academicDegree: editAcademicDegree,
        department: editDepartment,
      })
      setEditTarget(null)
      reload()
    } catch (err) {
      setEditError(describeApiError(err, { 400: 'Could not update this account — try again.' }))
    } finally {
      setEditBusy(false)
    }
  }

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
      setFormError(
        describeApiError(err, {
          400: 'Could not create the account — check the fields and try again.',
          409: 'An account with this username or email already exists.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const columns: Column<UserSummaryResponse>[] = [
    {
      header: 'Name',
      render: (u) => (
        <div className="flex items-center gap-2.5">
          <span className="flex h-7 w-7 flex-shrink-0 items-center justify-center overflow-hidden rounded-full bg-accent/10 text-[11px] font-semibold text-accent">
            {u.profilePhotoUrl ? (
              <img src={u.profilePhotoUrl} alt="" className="h-full w-full object-cover" />
            ) : (
              `${u.firstName[0] ?? ''}${u.lastName[0] ?? ''}`
            )}
          </span>
          {u.firstName} {u.lastName}
        </div>
      ),
    },
    { header: 'Username', render: (u) => u.userName },
    { header: 'Email', render: (u) => <span className="text-text-secondary">{u.email}</span> },
    { header: 'Employee ID', render: (u) => u.employeeId ?? <span className="text-text-muted">—</span> },
    { header: 'Role', render: (u) => <StatusPill label={roleLabel(u.role)} tone={ROLE_TONE[u.role] ?? 'neutral'} /> },
    {
      header: '',
      render: (u) => (
        <button
          type="button"
          onClick={() => {
            setEditRole(u.role)
            setEditEmployeeId(u.employeeId ?? '')
            setEditProfession(u.profession ?? '')
            setEditJobTitle(u.jobTitle ?? '')
            setEditAcademicDegree(u.academicDegree ?? '')
            setEditDepartment(u.department ?? '')
            setEditError(null)
            setEditTarget(u)
          }}
          className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent hover:bg-accent/10"
        >
          Edit
        </button>
      ),
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Staff"
        subtitle={`${data.length} account${data.length === 1 ? '' : 's'} can sign in`}
        action={<PrimaryButton onClick={() => setModalOpen(true)}>+ New account</PrimaryButton>}
      />

      {pending.error && (
        <Card className="flex flex-col gap-2 border-critical/40">
          <p className="text-[13px] font-medium text-critical">
            Could not load pending registration requests — {pending.error}
          </p>
          <button
            type="button"
            onClick={pending.reload}
            className="self-start rounded-lg border border-border-subtle px-3 py-1.5 text-[12.5px] font-medium text-accent hover:bg-accent/10"
          >
            Retry
          </button>
        </Card>
      )}

      {pending.data.length > 0 && (
        <Card className="flex flex-col gap-3 border-warning/40 bg-warning-bg/40">
          <div>
            <h2 className="text-[14px] font-semibold text-text">
              Pending approval ({pending.data.length})
            </h2>
            <p className="text-[12.5px] text-text-secondary">
              Self-registration requests — no one on this list can sign in yet.
            </p>
          </div>
          <div className="flex flex-col gap-2">
            {pending.data.map((p) => (
              <div
                key={p.id}
                className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border-subtle bg-surface px-3 py-2.5"
              >
                <div>
                  <div className="text-[13.5px] font-semibold text-text">
                    {p.firstName} {p.lastName} <span className="font-normal text-text-muted">({p.userName})</span>
                  </div>
                  <div className="text-[12px] text-text-secondary">
                    {p.email} — requested {p.requestedRole ? roleLabel(p.requestedRole) : 'no role specified'}
                  </div>
                </div>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => {
                      setApproveRole(p.requestedRole ?? 'Resident')
                      setApproveEmployeeId('')
                      setApproveError(null)
                      setApproveTarget(p)
                    }}
                    className="rounded-lg bg-accent px-3 py-1.5 text-[12.5px] font-semibold text-white hover:opacity-90"
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      setRejectError(null)
                      setRejectTarget(p)
                    }}
                    className="rounded-lg border border-border-subtle px-3 py-1.5 text-[12.5px] font-medium text-critical hover:bg-critical-bg"
                  >
                    Reject
                  </button>
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}

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

      <Modal
        open={approveTarget !== null}
        title={`Approve ${approveTarget ? `${approveTarget.firstName} ${approveTarget.lastName}` : ''}`}
        onClose={() => setApproveTarget(null)}
      >
        {approveTarget && (
          <form onSubmit={handleApprove} className="flex flex-col gap-3.5">
            <Field label="Assign role">
              <Select value={approveRole} onChange={(e) => setApproveRole(e.target.value)}>
                {ALL_ROLES.map((r) => (
                  <option key={r} value={r}>
                    {ROLE_LABELS[r]}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Employee ID (optional, can set later)">
              <TextInput value={approveEmployeeId} onChange={(e) => setApproveEmployeeId(e.target.value)} />
            </Field>
            {approveError && (
              <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">
                {approveError}
              </p>
            )}
            <PrimaryButton type="submit" disabled={approveBusy}>
              {approveBusy ? 'Approving…' : 'Approve & activate account'}
            </PrimaryButton>
          </form>
        )}
      </Modal>

      <Modal
        open={rejectTarget !== null}
        title={`Reject ${rejectTarget ? `${rejectTarget.firstName} ${rejectTarget.lastName}` : ''}`}
        onClose={() => setRejectTarget(null)}
      >
        {rejectTarget && (
          <div className="flex flex-col gap-3.5">
            <p className="text-[13.5px] text-text-secondary">
              This permanently rejects the registration request from{' '}
              <span className="font-semibold text-text">
                {rejectTarget.firstName} {rejectTarget.lastName}
              </span>{' '}
              ({rejectTarget.userName}). They will need to submit a new request to be considered again.
            </p>
            {rejectError && (
              <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">
                {rejectError}
              </p>
            )}
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => setRejectTarget(null)}
                disabled={rejectBusy}
                className="rounded-lg border border-border-subtle px-3.5 py-2 text-[13px] font-medium text-text-secondary hover:bg-border-soft disabled:opacity-60"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={handleReject}
                disabled={rejectBusy}
                className="rounded-lg bg-critical px-3.5 py-2 text-[13px] font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-60"
              >
                {rejectBusy ? 'Rejecting…' : 'Reject request'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        open={editTarget !== null}
        title={`Edit ${editTarget ? `${editTarget.firstName} ${editTarget.lastName}` : ''}`}
        onClose={() => setEditTarget(null)}
      >
        {editTarget && (
          <form onSubmit={handleEdit} className="flex flex-col gap-3.5">
            <Field label="Role">
              <Select value={editRole} onChange={(e) => setEditRole(e.target.value)}>
                {ALL_ROLES.map((r) => (
                  <option key={r} value={r}>
                    {ROLE_LABELS[r]}
                  </option>
                ))}
              </Select>
            </Field>
            <Field label="Employee ID">
              <TextInput value={editEmployeeId} onChange={(e) => setEditEmployeeId(e.target.value)} />
            </Field>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Field label="Profession (optional)">
                <TextInput
                  placeholder="Doctor, Nurse, Pharmacist…"
                  value={editProfession}
                  onChange={(e) => setEditProfession(e.target.value)}
                />
              </Field>
              <Field label="Job title (optional)">
                <TextInput
                  placeholder="Resident, Consultant, Head Nurse…"
                  value={editJobTitle}
                  onChange={(e) => setEditJobTitle(e.target.value)}
                />
              </Field>
            </div>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <Field label="Academic degree (optional)">
                <TextInput
                  placeholder="MBBS, MSc, MD, PhD…"
                  value={editAcademicDegree}
                  onChange={(e) => setEditAcademicDegree(e.target.value)}
                />
              </Field>
              <Field label="Department (optional)">
                <TextInput value={editDepartment} onChange={(e) => setEditDepartment(e.target.value)} />
              </Field>
            </div>
            {editError && (
              <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{editError}</p>
            )}
            <PrimaryButton type="submit" disabled={editBusy}>
              {editBusy ? 'Saving…' : 'Save changes'}
            </PrimaryButton>
          </form>
        )}
      </Modal>

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
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Phone (optional)">
              <TextInput
                value={form.phoneNumber}
                onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })}
              />
            </Field>
            <Field label="Employee ID (optional)">
              <TextInput
                value={form.employeeId}
                onChange={(e) => setForm({ ...form, employeeId: e.target.value })}
              />
            </Field>
          </div>

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
