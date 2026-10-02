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
import StatusPill from '@/components/StatusPill'
import { GENDER_OPTIONS, PATIENT_STATUS } from '@/lib/enums'
import { calculateThrombolyticDose, type ThrombolyticDrug, calculateSeizureDose, type SeizureDrug } from '@/lib/doseCalculator'
import { recordThrombolysis } from '@/lib/admissionThrombolysisApi'
import { isDoctorRole, isClinicalRole } from '@/lib/roles'
import { isOpenAdmission } from '@/lib/admissions'
import { maskNationalId } from '@/lib/patients'
import { describeApiError } from '@/lib/apiError'

const patientsApi = entityApi<PatientResponse>('Patient')
const admissionsApi = entityApi<AdmissionResponse>('Admission')

function age(dateOfBirth: string) {
  const dob = new Date(dateOfBirth)
  if (Number.isNaN(dob.getTime())) return '—'
  const diff = Date.now() - dob.getTime()
  return Math.max(0, Math.floor(diff / (365.25 * 24 * 3600 * 1000)))
}

// Mirrors the tone mapping used on the Admissions page so an admission status
// shown here reads the same way it does there.
function statusTone(label: string): 'success' | 'warning' | 'critical' | 'info' {
  if (label === 'Emergency' || label.startsWith('SentTo')) return 'critical'
  if (label === 'Discharged') return 'success'
  if (label.startsWith('AdmittedTo')) return 'info'
  return 'warning'
}

const emptyForm = {
  nationalId: '',
  hospitalNumber: '',
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
  const [seizureTarget, setSeizureTarget] = useState<PatientResponse | null>(null)
  const [seizureDrug, setSeizureDrug] = useState<SeizureDrug>('Lorazepam')
  const admissions = useEntityList(() => admissionsApi.list())
  const [recordBusy, setRecordBusy] = useState(false)
  const [recordMessage, setRecordMessage] = useState<string | null>(null)
  const [recordConfirmed, setRecordConfirmed] = useState(false)

  // PHASE 11 (area 7/8): a real "Edit patient" action, kept entirely separate from the
  // Admission/Stroke Code/assessment actions above and below — this only ever calls
  // PUT /api/patient, never touches an Admission, DoorTiming, or assessment record. Gated on
  // isClinicalRole to match the backend's new [Authorize(Roles = Roles.AnyClinical)] on
  // PatientController.Update — the button simply isn't rendered for a role that would get a
  // 403 from the API anyway, but the backend attribute remains the real boundary.
  const [editTarget, setEditTarget] = useState<PatientResponse | null>(null)
  const [editForm, setEditForm] = useState(emptyForm)
  const [editSubmitting, setEditSubmitting] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  function openEdit(p: PatientResponse) {
    setEditForm({
      nationalId: p.nationalId ?? '',
      hospitalNumber: p.hospitalNumber ?? '',
      firstName: p.firstName,
      middleName: p.middleName,
      lastName: p.lastName,
      dateOfBirth: p.dateOfBirth.slice(0, 10),
      gender: p.gender,
      weightKg: String(p.weightKg),
      chiefComplaint: p.chiefComplaint,
    })
    setEditError(null)
    setEditTarget(p)
  }

  async function handleEditSubmit(e: FormEvent) {
    e.preventDefault()
    if (!editTarget || !user?.userId) return
    setEditSubmitting(true)
    setEditError(null)
    try {
      await patientsApi.update(
        {
          id: editTarget.id,
          nationalId: editForm.nationalId || null,
          hospitalNumber: editForm.hospitalNumber || null,
          firstName: editForm.firstName,
          middleName: editForm.middleName,
          lastName: editForm.lastName,
          dateOfBirth: editForm.dateOfBirth,
          gender: editForm.gender,
          weightKg: Number(editForm.weightKg),
          chiefComplaint: editForm.chiefComplaint,
        },
        user.userId,
      )
      setEditTarget(null)
      reload()
    } catch (err) {
      setEditError(
        describeApiError(err, {
          400: 'Could not save these changes — check the fields and try again.',
          403: 'Your role cannot edit patient demographics.',
          409: 'Could not save — this hospital number is already assigned to another patient.',
        }),
      )
    } finally {
      setEditSubmitting(false)
    }
  }

  function activeAdmissionFor(patientId: string) {
    return admissions.data
      .filter((a) => a.patientId === patientId && isOpenAdmission(a))
      .sort((a, b) => new Date(b.admissionTime).getTime() - new Date(a.admissionTime).getTime())[0]
  }

  async function handleRecordThrombolysis() {
    if (!doseTarget || !user?.userId || !dose) return
    // Section 17 (confirmation UX): this starts an irreversible 24h antithrombotic lockout on
    // the admission, so it must not fire from a single click without an explicit confirmation.
    if (!recordConfirmed) return
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
      setRecordConfirmed(false)
      admissions.reload()
    } catch (err) {
      setRecordMessage(
        describeApiError(err, {
          409: 'Could not record this — the admission may have changed since this page loaded. Please refresh and try again.',
        }),
      )
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
          hospitalNumber: form.hospitalNumber || null,
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
      setFormError(
        describeApiError(err, {
          400: 'Could not create the patient — check the fields and try again.',
          409: 'Could not create the patient — this hospital number is already assigned to another patient.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const columns: Column<PatientResponse>[] = [
    {
      header: 'Patient',
      render: (p) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">
            {p.firstName} {p.middleName} {p.lastName}
          </span>
          {/* HospitalNumber first — it's the preferred operational identifier (area 14) —
              National ID stays masked in this ordinary list view. */}
          <span className="text-[11.5px] text-text-muted">
            {p.hospitalNumber ? `Hospital No. ${p.hospitalNumber}` : 'No hospital number assigned'}
          </span>
          <span className="text-[11.5px] text-text-muted">
            {p.nationalId ? `National ID ${maskNationalId(p.nationalId)}` : 'No national ID on file'}
          </span>
        </div>
      ),
    },
    {
      header: 'Age',
      render: (p) => {
        const a = age(p.dateOfBirth)
        return typeof a === 'number' ? `${a} yrs` : a
      },
    },
    { header: 'Gender', render: (p) => p.gender },
    { header: 'Weight', render: (p) => `${p.weightKg} kg` },
    {
      header: 'Chief Complaint',
      render: (p) => (
        <span className="line-clamp-2 max-w-[220px] text-text-secondary" title={p.chiefComplaint}>
          {p.chiefComplaint}
        </span>
      ),
    },
    {
      header: 'Admission status',
      render: (p) => {
        const active = activeAdmissionFor(p.id)
        if (!active) return <span className="text-text-muted">Not admitted</span>
        const label = PATIENT_STATUS[active.status as keyof typeof PATIENT_STATUS] ?? 'Unknown'
        return <StatusPill label={label} tone={statusTone(label)} />
      },
    },
    {
      header: 'Actions',
      render: (p) => (
        <div className="flex flex-wrap items-center gap-1.5">
          {isClinicalRole(user?.role) && (
            <button
              type="button"
              onClick={() => openEdit(p)}
              className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-text-secondary transition-colors duration-150 hover:bg-border-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
            >
              Edit
            </button>
          )}
          <button
            type="button"
            onClick={() => {
              setDoseDrug('Alteplase')
              setRecordMessage(null)
              setRecordConfirmed(false)
              setDoseTarget(p)
            }}
            className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent transition-colors duration-150 hover:bg-accent/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
          >
            Thrombolytic dose
          </button>
          <button
            type="button"
            onClick={() => {
              setSeizureDrug('Lorazepam')
              setSeizureTarget(p)
            }}
            className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent transition-colors duration-150 hover:bg-accent/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
          >
            Seizure dose
          </button>
        </div>
      ),
    },
  ]

  const dose = doseTarget ? calculateThrombolyticDose(doseTarget.weightKg, doseDrug) : null
  const seizureDose = seizureTarget ? calculateSeizureDose(seizureTarget.weightKg, seizureDrug) : null

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Patients"
        subtitle={`${data.length} patient${data.length === 1 ? '' : 's'} on record`}
        action={
          // PHASE 11 (area 7): previously shown to every logged-in user regardless of role —
          // the backend had no [Authorize] at all on Patient Create. Now matches it.
          isClinicalRole(user?.role) ? (
            <PrimaryButton
              onClick={() => setModalOpen(true)}
              className="focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40 focus-visible:ring-offset-2"
            >
              + New patient
            </PrimaryButton>
          ) : undefined
        }
      />

      <Card className="flex flex-col gap-4">
        <TextInput
          placeholder="Search by name or national ID…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-[320px]"
        />
        {admissions.error && !error && (
          <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
            Admission status couldn't be loaded, so the "Admission status" column below may be out of date.{' '}
            <button type="button" onClick={admissions.reload} className="underline">
              Retry
            </button>
          </p>
        )}
        <DataTable
          columns={columns}
          rows={filtered}
          rowKey={(p) => p.id}
          loading={loading || admissions.loading}
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
          {/* PHASE 11 (area 1): optional at creation — new-patient workflow can leave this
              blank and an Admin/clinical user can assign it later from the Edit action below. */}
          <Field label="Hospital number (optional)">
            <TextInput
              placeholder="e.g. a physical card/wristband number, once issued"
              value={form.hospitalNumber}
              onChange={(e) => setForm({ ...form, hospitalNumber: e.target.value })}
            />
          </Field>
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

      {/* PHASE 11 (area 7/8): a separate modal, deliberately not sharing state with the
          "New patient" modal above or any Admission/Stroke Code/assessment modal below. */}
      <Modal
        open={editTarget !== null}
        title={`Edit patient${editTarget ? ` — ${editTarget.firstName} ${editTarget.lastName}` : ''}`}
        onClose={() => setEditTarget(null)}
      >
        <form onSubmit={handleEditSubmit} className="flex flex-col gap-3.5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="First name">
              <TextInput
                required
                value={editForm.firstName}
                onChange={(e) => setEditForm({ ...editForm, firstName: e.target.value })}
              />
            </Field>
            <Field label="Middle name">
              <TextInput
                required
                value={editForm.middleName}
                onChange={(e) => setEditForm({ ...editForm, middleName: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Last name">
            <TextInput
              required
              value={editForm.lastName}
              onChange={(e) => setEditForm({ ...editForm, lastName: e.target.value })}
            />
          </Field>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Date of birth">
              <TextInput
                type="date"
                required
                value={editForm.dateOfBirth}
                onChange={(e) => setEditForm({ ...editForm, dateOfBirth: e.target.value })}
              />
            </Field>
            <Field label="Gender">
              <Select value={editForm.gender} onChange={(e) => setEditForm({ ...editForm, gender: e.target.value })}>
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
                value={editForm.weightKg}
                onChange={(e) => setEditForm({ ...editForm, weightKg: e.target.value })}
              />
            </Field>
            <Field label="National ID (optional)">
              <TextInput
                value={editForm.nationalId}
                onChange={(e) => setEditForm({ ...editForm, nationalId: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Hospital number (optional)">
            <TextInput
              value={editForm.hospitalNumber}
              onChange={(e) => setEditForm({ ...editForm, hospitalNumber: e.target.value })}
            />
          </Field>
          <Field label="Chief complaint">
            <TextArea
              required
              value={editForm.chiefComplaint}
              onChange={(e) => setEditForm({ ...editForm, chiefComplaint: e.target.value })}
            />
          </Field>

          {editError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{editError}</p>
          )}

          <PrimaryButton type="submit" disabled={editSubmitting} className="mt-1">
            {editSubmitting ? 'Saving…' : 'Save changes'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal
        open={doseTarget !== null}
        title={`Thrombolytic dose${doseTarget ? ` — ${doseTarget.firstName} ${doseTarget.lastName}` : ''}`}
        onClose={() => {
          setDoseTarget(null)
          setRecordConfirmed(false)
        }}
      >
        {doseTarget && dose && (
          <div className="flex flex-col gap-3.5">
            <Field label="Drug">
              <Select
                value={doseDrug}
                onChange={(e) => {
                  setDoseDrug(e.target.value as ThrombolyticDrug)
                  setRecordConfirmed(false)
                }}
              >
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
              <div className="flex flex-col gap-2 rounded-lg border border-border-subtle px-3 py-2.5">
                <label className="flex items-start gap-2 text-[12.5px] font-medium text-text-secondary">
                  <input
                    type="checkbox"
                    checked={recordConfirmed}
                    onChange={(e) => setRecordConfirmed(e.target.checked)}
                    className="mt-0.5"
                  />
                  I confirm {doseTarget.firstName} {doseTarget.lastName}
                  {doseTarget.hospitalNumber ? ` (Hospital No. ${doseTarget.hospitalNumber})` : ''} has been given
                  this dose now — this will start the 24h antithrombotic lockout on their admission and cannot be
                  undone from here.
                </label>
                <button
                  type="button"
                  onClick={handleRecordThrombolysis}
                  disabled={recordBusy || !recordConfirmed}
                  className="self-start rounded-lg border border-accent px-3.5 py-2 text-[13px] font-semibold text-accent transition-opacity hover:opacity-90 disabled:opacity-60"
                >
                  {recordBusy ? 'Recording…' : 'Record as given now (starts the 24h lockout)'}
                </button>
              </div>
            )}
            {recordMessage && <p className="text-[12.5px] text-text-secondary">{recordMessage}</p>}
          </div>
        )}
      </Modal>

      <Modal
        open={seizureTarget !== null}
        title={`Status epilepticus dose${seizureTarget ? ` — ${seizureTarget.firstName} ${seizureTarget.lastName}` : ''}`}
        onClose={() => setSeizureTarget(null)}
      >
        {seizureTarget && seizureDose && (
          <div className="flex flex-col gap-3.5">
            <Field label="Drug">
              <Select value={seizureDrug} onChange={(e) => setSeizureDrug(e.target.value as SeizureDrug)}>
                <option value="Lorazepam">Lorazepam — first-line, 0.1 mg/kg, max 4 mg</option>
                <option value="Phenytoin">Phenytoin — second-line loading dose, 20 mg/kg, max 1500 mg</option>
              </Select>
            </Field>

            <p className="text-[13px] text-text-secondary">{seizureDose.administration}</p>

            <div className="grid grid-cols-2 gap-3">
              <div className="rounded-lg border border-border-subtle px-3 py-2.5">
                <div className="text-[11.5px] font-medium text-text-muted">Weight</div>
                <div className="text-[16px] font-semibold text-text">{seizureTarget.weightKg} kg</div>
              </div>
              <div className="rounded-lg border border-border-subtle px-3 py-2.5">
                <div className="text-[11.5px] font-medium text-text-muted">
                  {seizureDrug === 'Lorazepam' ? 'Dose' : 'Loading dose'}
                </div>
                <div className="text-[16px] font-semibold text-text">
                  {seizureDose.totalDoseMg} mg
                  {seizureDose.cappedByMax && (
                    <span className="ml-1 text-[11px] text-warning">
                      (capped at {seizureDrug === 'Lorazepam' ? '4' : '1500'} mg)
                    </span>
                  )}
                </div>
              </div>
              <div className="rounded-lg border border-border-subtle px-3 py-2.5 col-span-2">
                <div className="text-[11.5px] font-medium text-text-muted">Rate</div>
                <div className="text-[14px] font-semibold text-text">{seizureDose.maxRateNote}</div>
              </div>
            </div>

            {seizureDose.repeatNote && (
              <p className="text-[12.5px] text-text-secondary">{seizureDose.repeatNote}</p>
            )}

            <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
              Calculated value only — the treating clinician must verify this against the patient's chart,
              airway/cardiac status, and contraindications before administration.
            </p>
          </div>
        )}
      </Modal>
    </motion.div>
  )
}
