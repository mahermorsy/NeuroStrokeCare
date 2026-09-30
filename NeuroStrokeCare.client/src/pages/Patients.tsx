import { useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import type { PatientResponse, AdmissionResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select, TextArea } from '@/components/FormField'
import { GENDER_OPTIONS } from '@/lib/enums'
import { calculateThrombolyticDose, type ThrombolyticDrug } from '@/lib/doseCalculator'
import { recordThrombolysis } from '@/lib/admissionThrombolysisApi'
import { isDoctorRole } from '@/lib/roles'

const patientsApi = entityApi<PatientResponse>('Patient')
const admissionsApi = entityApi<AdmissionResponse>('Admission')

function age(dateOfBirth: string) {
  const dob = new Date(dateOfBirth)
  if (Number.isNaN(dob.getTime())) return '—'
  const diff = Date.now() - dob.getTime()
  return Math.max(0, Math.floor(diff / (365.25 * 24 * 3600 * 1000)))
}

const emptyForm = {
  nationalId: '',
  firstName: '',
  middleName: '',
  lastName: '',
  dateOfBirth: '',
  gender: 'Male',
  weightKg: '',
  chiefComplaint: '',
}

export default function Patients() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useEntityList(() => patientsApi.list())
  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [search, setSearch] = useState('')
  const [doseTarget, setDoseTarget] = useState<PatientResponse | null>(null)
  const [doseDrug, setDoseDrug] = useState<ThrombolyticDrug>('Alteplase')
  const admissions = useEntityList(() => admissionsApi.list())
  const [recordBusy, setRecordBusy] = useState(false)
  const [recordMessage, setRecordMessage] = useState<string | null>(null)

  function activeAdmissionFor(patientId: string) {
    return admissions.data
      .filter((a) => a.patientId === patientId && !a.dischargeTime)
      .sort((a, b) => new Date(b.admissionTime).getTime() - new Date(a.admissionTime).getTime())[0]
  }

  async function handleRecordThrombolysis() {
    if (!doseTarget || !user?.userId || !dose) return
    const admission = activeAdmissionFor(doseTarget.id)
    if (!admission) {
      setRecordMessage('No active admission found for this patient — record it from the Admissions page instead.')
      return
    }
    setRecordBusy(true)
    setRecordMessage(null)
    try {
      await recordThrombolysis(admission.id, user.userId, doseDrug, dose.totalDoseMg)
      setRecordMessage('Recorded — the 24h antithrombotic lockout now applies to this admission.')
      admissions.reload()
    } catch {
      setRecordMessage('Could not record this — try again.')
    } finally {
      setRecordBusy(false)
    }
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return data
    return data.filter((p) =>
      `${p.firstName} ${p.middleName} ${p.lastName} ${p.nationalId ?? ''}`.toLowerCase().includes(q),
    )
  }, [data, search])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      await patientsApi.create(
        {
          nationalId: form.nationalId || null,
          firstName: form.firstName,
          middleName: form.middleName,
          lastName: form.lastName,
          dateOfBirth: form.dateOfBirth,
          gender: form.gender,
          weightKg: Number(form.weightKg),
          chiefComplaint: form.chiefComplaint,
        },
        user.userId,
      )
      setModalOpen(false)
      setForm(emptyForm)
      reload()
    } catch (err) {
      const message =
        (err as { response?: { data?: { errors?: string[]; title?: string } } })?.response?.data
      setFormError(
        (Array.isArray(message?.errors) && message.errors.join(', ')) ||
          message?.title ||
          'Could not create the patient — check the fields and try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  const columns: Column<PatientResponse>[] = [
    { header: 'Name', render: (p) => `${p.firstName} ${p.middleName} ${p.lastName}` },
    { header: 'National ID', render: (p) => p.nationalId ?? '—' },
    { header: 'Age', render: (p) => age(p.dateOfBirth) },
    { header: 'Gender', render: (p) => p.gender },
    { header: 'Weight (kg)', render: (p) => p.weightKg },
    { header: 'Chief Complaint', render: (p) => <span className="text-text-secondary">{p.chiefComplaint}</span> },
    {
      header: '',
      render: (p) => (
        <button
          type="button"
          onClick={() => {
            setDoseDrug('Alteplase')
            setRecordMessage(null)
            setDoseTarget(p)
          }}
          className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent hover:bg-accent/10"
        >
          Thrombolytic dose
        </button>
      ),
    },
  ]

  const dose = doseTarget ? calculateThrombolyticDose(doseTarget.weightKg, doseDrug) : null

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Patients"
        subtitle={`${data.length} patient${data.length === 1 ? '' : 's'} on record`}
        action={<PrimaryButton onClick={() => setModalOpen(true)}>+ New patient</PrimaryButton>}
      />

      <Card className="flex flex-col gap-4">
        <TextInput
          placeholder="Search by name or national ID…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-[320px]"
        />
        <DataTable
          columns={columns}
          rows={filtered}
          rowKey={(p) => p.id}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyMessage="No patients yet — add the first one."
        />
      </Card>

      <Modal open={modalOpen} title="New patient" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="First name">
              <TextInput
                required
                value={form.firstName}
                onChange={(e) => setForm({ ...form, firstName: e.target.value })}
              />
            </Field>
            <Field label="Middle name">
              <TextInput
                required
                value={form.middleName}
                onChange={(e) => setForm({ ...form, middleName: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Last name">
            <TextInput
              required
              value={form.lastName}
              onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            />
          </Field>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Date of birth">
              <TextInput
                type="date"
                required
                value={form.dateOfBirth}
                onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
              />
            </Field>
            <Field label="Gender">
              <Select value={form.gender} onChange={(e) => setForm({ ...form, gender: e.target.value })}>
                {GENDER_OPTIONS.map((g) => (
                  <option key={g} value={g}>
                    {g}
                  </option>
                ))}
              </Select>
            </Field>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Weight (kg)">
              <TextInput
                type="number"
                step="0.1"
                min="0.5"
                required
                value={form.weightKg}
                onChange={(e) => setForm({ ...form, weightKg: e.target.value })}
              />
            </Field>
            <Field label="National ID (optional)">
              <TextInput
                value={form.nationalId}
                onChange={(e) => setForm({ ...form, nationalId: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Chief complaint">
            <TextArea
              required
              value={form.chiefComplaint}
              onChange={(e) => setForm({ ...form, chiefComplaint: e.target.value })}
            />
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting} className="mt-1">
            {submitting ? 'Saving…' : 'Save patient'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal
        open={doseTarget !== null}
        title={`Thrombolytic dose${doseTarget ? ` — ${doseTarget.firstName} ${doseTarget.lastName}` : ''}`}
        onClose={() => setDoseTarget(null)}
      >
        {doseTarget && dose && (
          <div className="flex flex-col gap-3.5">
            <Field label="Drug">
              <Select value={doseDrug} onChange={(e) => setDoseDrug(e.target.value as ThrombolyticDrug)}>
                <option value="Alteplase">Alteplase (Actilyse) — 0.9 mg/kg, max 90 mg</option>
                <option value="Tenecteplase">Tenecteplase (Metalyse) — 0.25 mg/kg, max 25 mg</option>
              </Select>
            </Field>

            <p className="text-[13px] text-text-secondary">{dose.administration}</p>

            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-lg border border-border-subtle px-3 py-2.5">
                <div className="text-[11.5px] font-medium text-text-muted">Weight</div>
                <div className="text-[16px] font-semibold text-text">{doseTarget.weightKg} kg</div>
              </div>
              <div className="rounded-lg border border-border-subtle px-3 py-2.5">
                <div className="text-[11.5px] font-medium text-text-muted">Total dose</div>
                <div className="text-[16px] font-semibold text-text">
                  {dose.totalDoseMg} mg
                  {dose.cappedByMax && (
                    <span className="ml-1 text-[11px] text-warning">
                      (capped at {doseDrug === 'Alteplase' ? '90' : '25'} mg)
                    </span>
                  )}
                </div>
              </div>
              {doseDrug === 'Alteplase' ? (
                <>
                  <div className="rounded-lg border border-border-subtle px-3 py-2.5">
                    <div className="text-[11.5px] font-medium text-text-muted">Bolus (10%, IV over 1 min)</div>
                    <div className="text-[16px] font-semibold text-text">{dose.bolusMg} mg</div>
                  </div>
                  <div className="rounded-lg border border-border-subtle px-3 py-2.5">
                    <div className="text-[11.5px] font-medium text-text-muted">Infusion (90%, over 60 min)</div>
                    <div className="text-[16px] font-semibold text-text">
                      {dose.infusionMg} mg (~{dose.infusionRateMgPerHour} mg/hr)
                    </div>
                  </div>
                </>
              ) : (
                <div className="rounded-lg border border-border-subtle px-3 py-2.5 col-span-2">
                  <div className="text-[11.5px] font-medium text-text-muted">Bolus (single dose)</div>
                  <div className="text-[16px] font-semibold text-text">{dose.bolusMg} mg — over 5–10 seconds</div>
                </div>
              )}
            </div>

            <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
              Calculated value only — the treating clinician must verify this against the patient's chart and
              contraindications before administration.
            </p>

            {isDoctorRole(user?.role) && (
              <button
                type="button"
                onClick={handleRecordThrombolysis}
                disabled={recordBusy}
                className="rounded-lg border border-accent px-3.5 py-2 text-[13px] font-semibold text-accent transition-opacity hover:opacity-90 disabled:opacity-60"
              >
                {recordBusy ? 'Recording…' : 'Record as given now (starts the 24h lockout)'}
              </button>
            )}
            {recordMessage && <p className="text-[12.5px] text-text-secondary">{recordMessage}</p>}
          </div>
        )}
      </Modal>
    </motion.div>
  )
}
