import { useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import type { AdmissionResponse, PatientResponse, BedResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { BED_STATUS, PATIENT_STATUS, PATIENT_STATUS_OPTIONS, STROKE_TYPE } from '@/lib/enums'
import { isDoctorRole } from '@/lib/roles'
import { transferAdmission } from '@/lib/admissionTransferApi'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')
const bedsApi = entityApi<BedResponse>('Bed')

const emptyForm = {
  patientId: '',
  admissionTime: '',
  status: 1,
  bedId: '',
}

function statusTone(label: string): 'success' | 'warning' | 'critical' | 'info' {
  if (label === 'Emergency' || label.startsWith('SentTo')) return 'critical'
  if (label === 'Discharged') return 'success'
  if (label.startsWith('AdmittedTo')) return 'info'
  return 'warning'
}

export default function Admissions() {
  const { user } = useAuth()
  const admissions = useEntityList(() => admissionsApi.list())
  const patients = useEntityList(() => patientsApi.list())
  const beds = useEntityList(() => bedsApi.list())

  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const canManage = isDoctorRole(user?.role)

  const [transferTarget, setTransferTarget] = useState<AdmissionResponse | null>(null)
  const [transferForm, setTransferForm] = useState({ status: 1, changeBed: false, bedId: '' })
  const [transferSubmitting, setTransferSubmitting] = useState(false)
  const [transferError, setTransferError] = useState<string | null>(null)

  const patientNameById = useMemo(
    () => new Map(patients.data.map((p) => [p.id, `${p.firstName} ${p.lastName}`])),
    [patients.data],
  )
  const bedNumberById = useMemo(() => new Map(beds.data.map((b) => [b.id, b.bedNumber])), [beds.data])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      await admissionsApi.create(
        {
          patientId: form.patientId,
          admissionTime: form.admissionTime,
          status: Number(form.status),
          bedId: form.bedId || null,
        },
        user.userId,
      )
      setModalOpen(false)
      setForm(emptyForm)
      admissions.reload()
    } catch (err) {
      const status = (err as { response?: { status?: number } })?.response?.status
      setFormError(
        status === 403
          ? 'Only doctors (Consultant/Registrar/Resident) can admit or transfer patients between wards.'
          : 'Could not create the admission — check the patient and bed selected.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  function openTransfer(a: AdmissionResponse) {
    setTransferTarget(a)
    setTransferForm({ status: a.status, changeBed: false, bedId: a.bedId ?? '' })
    setTransferError(null)
  }

  async function handleTransferSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId || !transferTarget) return
    setTransferSubmitting(true)
    setTransferError(null)
    try {
      await transferAdmission(transferTarget.id, {
        actingUserId: user.userId,
        newStatus: transferForm.status,
        changeBed: transferForm.changeBed,
        newBedId: transferForm.changeBed ? transferForm.bedId || null : null,
      })
      setTransferTarget(null)
      admissions.reload()
      beds.reload()
    } catch (err) {
      const status = (err as { response?: { status?: number; data?: { message?: string } } })?.response?.status
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      if (status === 409) {
        setTransferError(message ?? 'That bed was just taken by another admission — pick a different one.')
      } else if (status === 403) {
        setTransferError('Only doctors (Consultant/Registrar/Resident) can transfer patients or change their status.')
      } else {
        setTransferError('Could not complete the transfer — please try again.')
      }
    } finally {
      setTransferSubmitting(false)
    }
  }

  // beds selectable in the transfer modal: vacant ones, plus whichever bed the
  // patient is already in (so it stays visible even though its own status is Occupied)
  const availableBedsForTransfer = beds.data.filter(
    (b) => BED_STATUS[b.status as keyof typeof BED_STATUS] === 'Vacant' || b.id === transferTarget?.bedId,
  )

  const columns: Column<AdmissionResponse>[] = [
    { header: 'Patient', render: (a) => patientNameById.get(a.patientId) ?? a.patientId.slice(0, 8) },
    { header: 'Bed', render: (a) => (a.bedId ? bedNumberById.get(a.bedId) ?? '—' : '—') },
    { header: 'Admitted', render: (a) => new Date(a.admissionTime).toLocaleString() },
    {
      header: 'Status',
      render: (a) => {
        const label = PATIENT_STATUS[a.status as keyof typeof PATIENT_STATUS] ?? 'Unknown'
        return <StatusPill label={label} tone={statusTone(label)} />
      },
    },
    {
      header: 'Stroke type',
      render: (a) => (a.strokeType ? STROKE_TYPE[a.strokeType as keyof typeof STROKE_TYPE] : '—'),
    },
    {
      header: 'Imaging',
      render: (a) => (
        <span className="text-text-secondary">
          {[a.ctDone && 'CT', a.mriDone && 'MRI', a.ctaDone && 'CTA'].filter(Boolean).join(', ') || '—'}
        </span>
      ),
    },
    {
      header: '',
      render: (a) =>
        canManage ? (
          <button
            type="button"
            onClick={() => openTransfer(a)}
            className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent hover:bg-accent/10"
          >
            Transfer / update
          </button>
        ) : null,
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Admissions"
        subtitle={`${admissions.data.length} admission${admissions.data.length === 1 ? '' : 's'} on record`}
        action={
          canManage ? (
            <PrimaryButton onClick={() => setModalOpen(true)} disabled={patients.data.length === 0}>
              + New admission
            </PrimaryButton>
          ) : (
            <span className="text-[12.5px] text-text-muted">Only doctors can admit or transfer patients</span>
          )
        }
      />

      <Card>
        <DataTable
          columns={columns}
          rows={admissions.data}
          rowKey={(a) => a.id}
          loading={admissions.loading || patients.loading}
          error={admissions.error}
          onRetry={admissions.reload}
          emptyMessage="No admissions yet."
        />
      </Card>

      <Modal open={modalOpen} title="New admission" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <Field label="Patient">
            <Select required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
              <option value="" disabled>
                Select a patient…
              </option>
              {patients.data.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.firstName} {p.lastName}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Admission time">
            <TextInput
              type="datetime-local"
              required
              value={form.admissionTime}
              onChange={(e) => setForm({ ...form, admissionTime: e.target.value })}
            />
          </Field>
          <Field label="Status">
            <Select value={form.status} onChange={(e) => setForm({ ...form, status: Number(e.target.value) })}>
              {PATIENT_STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Bed (optional)">
            <Select value={form.bedId} onChange={(e) => setForm({ ...form, bedId: e.target.value })}>
              <option value="">No bed assigned</option>
              {beds.data.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.bedNumber}
                </option>
              ))}
            </Select>
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save admission'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal
        open={transferTarget !== null}
        title={`Transfer / update status${
          transferTarget ? ` — ${patientNameById.get(transferTarget.patientId) ?? 'patient'}` : ''
        }`}
        onClose={() => setTransferTarget(null)}
      >
        <form onSubmit={handleTransferSubmit} className="flex flex-col gap-3.5">
          <Field label="New status">
            <Select
              value={transferForm.status}
              onChange={(e) => setTransferForm({ ...transferForm, status: Number(e.target.value) })}
            >
              {PATIENT_STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>

          <label className="flex items-center gap-2 text-[13.5px] text-text-secondary">
            <input
              type="checkbox"
              checked={transferForm.changeBed}
              onChange={(e) => setTransferForm({ ...transferForm, changeBed: e.target.checked })}
            />
            Move to a different bed
          </label>

          {transferForm.changeBed && (
            <Field label="New bed">
              <Select
                required
                value={transferForm.bedId}
                onChange={(e) => setTransferForm({ ...transferForm, bedId: e.target.value })}
              >
                <option value="" disabled>
                  Select a vacant bed…
                </option>
                {availableBedsForTransfer.map((b) => (
                  <option key={b.id} value={b.id}>
                    {b.bedNumber}
                  </option>
                ))}
              </Select>
            </Field>
          )}

          {transferError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">
              {transferError}
            </p>
          )}

          <PrimaryButton type="submit" disabled={transferSubmitting}>
            {transferSubmitting ? 'Saving…' : 'Save'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
