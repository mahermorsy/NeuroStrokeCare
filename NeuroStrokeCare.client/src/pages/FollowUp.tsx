import { useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { followUpNotesApi } from '@/lib/followUpNotesApi'
import { useEntityList } from '@/hooks/useEntityList'
import type { AdmissionResponse, PatientResponse, FollowUpNoteResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import ContextAwareAdmissionField from '@/components/ContextAwareAdmissionField'
import { usePatientSelection } from '@/context/PatientSelectionContext'
import { Field, TextArea, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { isDoctorRole, isNurseRole, roleLabel } from '@/lib/roles'
import { describeApiError } from '@/lib/apiError'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')

const NOTE_TYPE_OPTIONS = [
  { value: 1, label: 'General follow-up' },
  { value: 2, label: 'New finding / condition' },
]

const emptyForm = { admissionId: '', content: '', noteType: 1 }

export default function FollowUp() {
  const { admissionId: contextAdmissionId } = usePatientSelection()
  const { user } = useAuth()
  const notes = useEntityList(() => followUpNotesApi.list())
  const admissions = useEntityList(() => admissionsApi.list())
  const patients = useEntityList(() => patientsApi.list())

  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const canWrite = isDoctorRole(user?.role) || isNurseRole(user?.role)

  function openNew() {
    // PHASE 11 (area 5): auto-use the shared Patient Selection Context's admission when one is
    // set, instead of always starting from an empty picker.
    setForm({ ...emptyForm, admissionId: contextAdmissionId ?? '' })
    setFormError(null)
    setModalOpen(true)
  }

  const patientNameByAdmissionId = useMemo(() => {
    const patientNameById = new Map(patients.data.map((p) => [p.id, `${p.firstName} ${p.lastName}`]))
    return new Map(admissions.data.map((a) => [a.id, patientNameById.get(a.patientId) ?? 'Unknown patient']))
  }, [admissions.data, patients.data])

  // PHASE 11 (area 1): resolve HospitalNumber per admission so the Patient column never has to
  // fall back to an internal database GUID. See useAdmissionContext.ts for the shared pattern.
  const hospitalNumberByAdmissionId = useMemo(() => {
    const hospitalNumberById = new Map(patients.data.map((p) => [p.id, p.hospitalNumber]))
    return new Map(admissions.data.map((a) => [a.id, hospitalNumberById.get(a.patientId) ?? null]))
  }, [admissions.data, patients.data])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    // Guard against a double-click/double-Enter firing two concurrent creates before the
    // disabled-button re-render lands (Phase F1 - Frontend Hardening, Section 16).
    if (submitting) return
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      await followUpNotesApi.create(
        {
          admissionId: form.admissionId,
          content: form.content,
          noteType: Number(form.noteType),
          authorRole: user.role,
        },
        user.userId,
      )
      setModalOpen(false)
      setForm(emptyForm)
      notes.reload()
    } catch (err) {
      setFormError(
        describeApiError(err, {
          403: 'Only doctors or nursing staff can add follow-up notes.',
          400: 'Could not save the note — check the admission selected.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const sortedNotes = useMemo(
    () => [...notes.data].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()),
    [notes.data],
  )

  const columns: Column<FollowUpNoteResponse>[] = [
    {
      header: 'Patient',
      render: (n) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">{patientNameByAdmissionId.get(n.admissionId) ?? 'Unknown admission'}</span>
          <span className="text-[11.5px] text-text-muted">
            {hospitalNumberByAdmissionId.get(n.admissionId)
              ? `Hospital No. ${hospitalNumberByAdmissionId.get(n.admissionId)}`
              : 'No hospital number assigned'}
          </span>
        </div>
      ),
    },
    { header: 'When', render: (n) => new Date(n.createdAt).toLocaleString() },
    { header: 'By', render: (n) => roleLabel(n.authorRole) },
    {
      header: 'Type',
      render: (n) => (
        <StatusPill
          label={n.noteType === 2 ? 'New finding' : 'General'}
          tone={n.noteType === 2 ? 'warning' : 'info'}
        />
      ),
    },
    { header: 'Note', render: (n) => <span className="whitespace-pre-wrap text-text-secondary">{n.content}</span> },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Follow-up notes"
        subtitle={`${notes.data.length} note${notes.data.length === 1 ? '' : 's'} across all admissions`}
        action={
          canWrite ? (
            <PrimaryButton onClick={openNew} disabled={admissions.data.length === 0}>
              + Add note
            </PrimaryButton>
          ) : (
            <span className="text-[12.5px] text-text-muted">Only doctors and nursing staff can add notes</span>
          )
        }
      />

      <Card>
        <DataTable
          columns={columns}
          rows={sortedNotes}
          rowKey={(n) => n.id}
          loading={notes.loading || admissions.loading || patients.loading}
          error={notes.error}
          onRetry={notes.reload}
          emptyMessage="No follow-up notes recorded yet."
        />
      </Card>

      <Modal open={modalOpen} title="New follow-up note" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <Field label="Patient / admission">
            <ContextAwareAdmissionField
              value={form.admissionId}
              onSelect={(id) => setForm({ ...form, admissionId: id })}
            />
          </Field>

          <Field label="Type">
            <Select value={form.noteType} onChange={(e) => setForm({ ...form, noteType: Number(e.target.value) })}>
              {NOTE_TYPE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>

          <Field label="Note">
            <TextArea
              rows={4}
              required
              maxLength={2000}
              value={form.content}
              onChange={(e) => setForm({ ...form, content: e.target.value })}
            />
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save note'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
