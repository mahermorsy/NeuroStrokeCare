import { useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import { useAdmissionContext } from '@/hooks/useAdmissionContext'
import type { LabResultsResponse, AdmissionResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import ContextAwareAdmissionField from '@/components/ContextAwareAdmissionField'
import { usePatientSelection } from '@/context/PatientSelectionContext'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { isDoctorRole, isNurseRole } from '@/lib/roles'
import { describeApiError } from '@/lib/apiError'

const labResultsApi = entityApi<LabResultsResponse>('LabResults')
const admissionsApi = entityApi<AdmissionResponse>('Admission')

function value(v: number | null, unit = '') {
  return v === null || v === undefined ? '—' : `${v}${unit}`
}

// Phase F1 - Frontend Hardening (Section 9): every one of these is a physical lab quantity that
// cannot be negative in reality, so `min="0"` is a basic input sanity floor, not a new clinical
// threshold — it stops an obviously-invalid negative value from ever reaching the backend and
// surfacing there as a generic validation error. No upper bound is imposed here since "what
// counts as a dangerously high/low result" is a clinical judgment this phase must not encode.
const NUMERIC_FIELDS: { key: keyof typeof emptyForm; label: string; step?: string; min?: string }[] = [
  { key: 'glucoseMmol', label: 'Glucose (mmol/L)', step: '0.1', min: '0' },
  { key: 'inr', label: 'INR', step: '0.01', min: '0' },
  { key: 'pt', label: 'PT', step: '0.1', min: '0' },
  { key: 'platelets', label: 'Platelets', step: '1', min: '0' },
  { key: 'sodium', label: 'Sodium', step: '0.1', min: '0' },
  { key: 'potassium', label: 'Potassium', step: '0.1', min: '0' },
  { key: 'creatinine', label: 'Creatinine', step: '0.01', min: '0' },
  { key: 'hemoglobin', label: 'Hemoglobin', step: '0.1', min: '0' },
  { key: 'ldl', label: 'LDL', step: '0.1', min: '0' },
  { key: 'hbA1c', label: 'HbA1c', step: '0.1', min: '0' },
  { key: 'aPTT', label: 'aPTT', step: '0.1', min: '0' },
  { key: 'alt', label: 'ALT', step: '1', min: '0' },
  { key: 'ast', label: 'AST', step: '1', min: '0' },
]

const emptyForm = {
  admissionId: '',
  recordedAt: '',
  glucoseMmol: '',
  inr: '',
  pt: '',
  platelets: '',
  sodium: '',
  potassium: '',
  creatinine: '',
  hemoglobin: '',
  ldl: '',
  hbA1c: '',
  aPTT: '',
  alt: '',
  ast: '',
  ecgAtrialFibrillation: '',
}

export default function LabResults() {
  const { user } = useAuth()
  const { admissionId: contextAdmissionId } = usePatientSelection()
  const { data, loading, error, reload } = useEntityList(() => labResultsApi.list())
  const {
    patientNameByAdmissionId,
    hospitalNumberByAdmissionId,
    loading: contextLoading,
    error: contextError,
    reload: reloadContext,
  } = useAdmissionContext()
  const admissions = useEntityList(() => admissionsApi.list())

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

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    // Guards against a rapid double-click/double-submit firing a second request before the
    // button's disabled={submitting} re-render takes effect (Phase F1 - Frontend Hardening,
    // Section 16).
    if (submitting) return
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      const num = (v: string) => (v === '' ? null : Number(v))
      await labResultsApi.create(
        {
          admissionId: form.admissionId,
          recordedAt: form.recordedAt,
          glucoseMmol: num(form.glucoseMmol),
          inr: num(form.inr),
          pt: num(form.pt),
          platelets: num(form.platelets),
          sodium: num(form.sodium),
          potassium: num(form.potassium),
          creatinine: num(form.creatinine),
          hemoglobin: num(form.hemoglobin),
          ldl: num(form.ldl),
          hbA1c: num(form.hbA1c),
          aPTT: num(form.aPTT),
          alt: num(form.alt),
          ast: num(form.ast),
          ecgAtrialFibrillation: form.ecgAtrialFibrillation === '' ? null : form.ecgAtrialFibrillation === 'true',
        },
        user.userId,
      )
      setModalOpen(false)
      reload()
    } catch (err) {
      setFormError(
        describeApiError(err, {
          403: 'Only doctors or nursing staff can record lab results.',
          400: 'Could not save the lab result — check the admission and values entered.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const columns: Column<LabResultsResponse>[] = [
    {
      header: 'Patient',
      render: (r) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">{patientNameByAdmissionId.get(r.admissionId) ?? '—'}</span>
          <span className="text-[11.5px] text-text-muted">
            {hospitalNumberByAdmissionId.get(r.admissionId)
              ? `Hospital No. ${hospitalNumberByAdmissionId.get(r.admissionId)}`
              : 'No hospital number assigned'}
          </span>
        </div>
      ),
    },
    { header: 'Recorded at', render: (r) => new Date(r.recordedAt).toLocaleString() },
    { header: 'Glucose', render: (r) => value(r.glucoseMmol, ' mmol/L') },
    { header: 'INR', render: (r) => value(r.inr) },
    { header: 'PT', render: (r) => value(r.pt) },
    { header: 'Platelets', render: (r) => value(r.platelets) },
    { header: 'Sodium', render: (r) => value(r.sodium) },
    { header: 'Potassium', render: (r) => value(r.potassium) },
    { header: 'Creatinine', render: (r) => value(r.creatinine) },
    { header: 'Hemoglobin', render: (r) => value(r.hemoglobin) },
    { header: 'LDL', render: (r) => value(r.ldl) },
    { header: 'HbA1c', render: (r) => value(r.hbA1c) },
    { header: 'aPTT', render: (r) => value(r.aPTT) },
    { header: 'ALT', render: (r) => value(r.alt) },
    { header: 'AST', render: (r) => value(r.ast) },
    {
      header: 'AFib (ECG)',
      render: (r) => (r.ecgAtrialFibrillation === null ? '—' : r.ecgAtrialFibrillation ? 'Yes' : 'No'),
    },
    {
      header: 'Alerts',
      render: (r) => {
        const alerts = [
          r.inrAlert && 'INR',
          r.glucoseAlert && 'Glucose',
          r.plateletsAlert && 'Platelets',
        ].filter(Boolean) as string[]
        if (alerts.length === 0) return <StatusPill label="None" tone="success" />
        return (
          <div className="flex flex-wrap gap-1">
            {alerts.map((a) => (
              <StatusPill key={a} label={a} tone="critical" />
            ))}
          </div>
        )
      },
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Lab Results"
        subtitle="Bloodwork and coagulation panels recorded per admission"
        action={
          canWrite ? (
            <PrimaryButton onClick={openNew} disabled={admissions.data.length === 0}>
              + New lab result
            </PrimaryButton>
          ) : (
            <span className="text-[12.5px] text-text-muted">Only doctors and nursing staff can record labs</span>
          )
        }
      />

      {contextError && !error && (
        <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
          Patient names couldn't be loaded, so rows below may show as "—".{' '}
          <button type="button" onClick={reloadContext} className="underline">
            Retry
          </button>
        </p>
      )}

      <Card>
        <DataTable
          columns={columns}
          rows={data}
          rowKey={(r) => r.id}
          loading={loading || contextLoading}
          error={error}
          onRetry={reload}
          emptyMessage="No lab results recorded yet."
        />
      </Card>

      <Modal open={modalOpen} title="New lab result" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <Field label="Patient / admission">
            <ContextAwareAdmissionField
              value={form.admissionId}
              onSelect={(id) => setForm({ ...form, admissionId: id })}
            />
          </Field>

          <Field label="Recorded at">
            <TextInput
              type="datetime-local"
              required
              value={form.recordedAt}
              onChange={(e) => setForm({ ...form, recordedAt: e.target.value })}
            />
          </Field>

          <p className="text-[12px] text-text-muted">
            Every value below is optional — leave anything not yet available blank.
          </p>

          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
            {NUMERIC_FIELDS.map((f) => (
              <Field key={f.key} label={f.label}>
                <TextInput
                  type="number"
                  step={f.step}
                  min={f.min}
                  value={form[f.key]}
                  onChange={(e) => setForm({ ...form, [f.key]: e.target.value })}
                />
              </Field>
            ))}
          </div>

          <Field label="AFib on ECG">
            <Select
              value={form.ecgAtrialFibrillation}
              onChange={(e) => setForm({ ...form, ecgAtrialFibrillation: e.target.value })}
            >
              <option value="">Not assessed</option>
              <option value="true">Yes</option>
              <option value="false">No</option>
            </Select>
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save lab result'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
