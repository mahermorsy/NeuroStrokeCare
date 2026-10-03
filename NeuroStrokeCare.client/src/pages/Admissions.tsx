import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { Link } from 'react-router-dom'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import type {
  AdmissionPatientSearchResultResponse,
  AdmissionResponse,
  PatientResponse,
  BedResponse,
  WardResponse,
} from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { BED_STATUS, PATIENT_STATUS, PATIENT_STATUS_OPTIONS, STROKE_TYPE, STROKE_TYPE_OPTIONS } from '@/lib/enums'
import { isDoctorRole } from '@/lib/roles'
import { isOpenAdmission } from '@/lib/admissions'
import { describeApiError } from '@/lib/apiError'
import { transferAdmission } from '@/lib/admissionTransferApi'
import { setAdmissionStrokeType } from '@/lib/admissionStrokeTypeApi'
import { searchAdmissionPatients } from '@/lib/admissionPatientSearchApi'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')
const bedsApi = entityApi<BedResponse>('Bed')
const wardsApi = entityApi<WardResponse>('Ward')

// Phase F1 - Frontend Hardening (Section 6/17): PatientStatus values 12 (Discharged) and 13
// (TransferredOut) are the two terminal states - setting either via the Transfer modal ends the
// admission for good (mirrors isOpenAdmission's own dischargeTime-based definition) and deserves
// the same explicit confirmation step Patients.tsx now has for recording thrombolysis.
const TERMINAL_STATUS_VALUES = [12, 13]

const emptyForm = {
  patientId: '',
  admissionTime: '',
  status: 1,
  bedId: '',
}

function toDateTimeLocalValue(date = new Date()) {
  const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000)
  return localDate.toISOString().slice(0, 16)
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
  const wards = useEntityList(() => wardsApi.list())

  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [patientQuery, setPatientQuery] = useState('')
  const [patientResults, setPatientResults] = useState<AdmissionPatientSearchResultResponse[]>([])
  const [patientSearchLoading, setPatientSearchLoading] = useState(false)
  const [patientSearchError, setPatientSearchError] = useState<string | null>(null)
  const [selectedPatient, setSelectedPatient] = useState<AdmissionPatientSearchResultResponse | null>(null)
  const canManage = isDoctorRole(user?.role)

  const [transferTarget, setTransferTarget] = useState<AdmissionResponse | null>(null)
  const [transferForm, setTransferForm] = useState({ status: 1, changeBed: false, bedId: '' })
  const [transferSubmitting, setTransferSubmitting] = useState(false)
  const [transferError, setTransferError] = useState<string | null>(null)
  const [dischargeConfirmed, setDischargeConfirmed] = useState(false)
  const isTerminalTransfer = TERMINAL_STATUS_VALUES.includes(transferForm.status)

  const [strokeTarget, setStrokeTarget] = useState<AdmissionResponse | null>(null)
  const [strokeValue, setStrokeValue] = useState(1)
  const [strokeSubmitting, setStrokeSubmitting] = useState(false)
  const [strokeError, setStrokeError] = useState<string | null>(null)

  const patientNameById = useMemo(
    () => new Map(patients.data.map((p) => [p.id, `${p.firstName} ${p.lastName}`])),
    [patients.data],
  )
  // PHASE 11 (area 13): wrong-patient safety - high-impact confirmations (discharge/transfer)
  // must show patient name + HospitalNumber, not just name.
  const hospitalNumberByPatientId = useMemo(
    () => new Map(patients.data.map((p) => [p.id, p.hospitalNumber])),
    [patients.data],
  )
  const bedNumberById = useMemo(() => new Map(beds.data.map((b) => [b.id, b.bedNumber])), [beds.data])
  const wardNameById = useMemo(() => new Map(wards.data.map((w) => [w.id, w.name])), [wards.data])
  const wardNameByBedId = useMemo(() => {
    const map = new Map<string, string>()
    for (const b of beds.data) {
      const name = wardNameById.get(b.wardId)
      if (name) map.set(b.id, name)
    }
    return map
  }, [beds.data, wardNameById])

  useEffect(() => {
    const query = patientQuery.trim()

    if (!modalOpen || selectedPatient || query.length < 2) {
      setPatientResults([])
      setPatientSearchLoading(false)
      setPatientSearchError(null)
      return
    }

    const controller = new AbortController()
    const timeout = window.setTimeout(async () => {
      setPatientSearchLoading(true)
      setPatientSearchError(null)
      try {
        const results = await searchAdmissionPatients(query, controller.signal)
        setPatientResults(results)
      } catch (err) {
        if (!controller.signal.aborted) {
          setPatientResults([])
          setPatientSearchError(
            describeApiError(err, {
              409: 'This patient already has an active admission.',
              403: 'Only doctors can search eligible patients for a new admission.',
            }),
          )
        }
      } finally {
        if (!controller.signal.aborted) {
          setPatientSearchLoading(false)
        }
      }
    }, 350)

    return () => {
      controller.abort()
      window.clearTimeout(timeout)
    }
  }, [modalOpen, patientQuery, selectedPatient])

  function openNewAdmissionModal() {
    setForm({ ...emptyForm, admissionTime: toDateTimeLocalValue() })
    setFormError(null)
    setPatientQuery('')
    setPatientResults([])
    setPatientSearchError(null)
    setSelectedPatient(null)
    setModalOpen(true)
  }

  function choosePatient(patient: AdmissionPatientSearchResultResponse) {
    setSelectedPatient(patient)
    setForm((current) => ({ ...current, patientId: patient.patientId }))
    setPatientQuery('')
    setPatientResults([])
    setPatientSearchError(null)
  }

  function clearSelectedPatient() {
    setSelectedPatient(null)
    setForm((current) => ({ ...current, patientId: '' }))
    setPatientQuery('')
    setPatientResults([])
    setPatientSearchError(null)
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    if (submitting) return
    if (!form.patientId || !selectedPatient) {
      setFormError('Select an eligible patient before saving the admission.')
      return
    }

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
      clearSelectedPatient()
      admissions.reload()
      beds.reload()
    } catch (err) {
      setFormError(
        describeApiError(err, {
          409: 'This patient already has an open admission, or that bed was just taken — please check and try again.',
          403: 'Only doctors (Consultant/Registrar/Resident) can admit or transfer patients between wards.',
          404: 'Selected bed could not be found.',
          400: 'Could not create the admission — check the patient and bed selected.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  function openTransfer(a: AdmissionResponse) {
    setTransferTarget(a)
    setTransferForm({ status: a.status, changeBed: false, bedId: a.bedId ?? '' })
    setTransferError(null)
    setDischargeConfirmed(false)
  }

  async function handleTransferSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId || !transferTarget) return
    // Section 17: a terminal status change (Discharged/TransferredOut) must be explicitly
    // confirmed before it fires - it ends the admission for good.
    if (isTerminalTransfer && !dischargeConfirmed) return
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
      setTransferError(
        describeApiError(err, {
          409: 'That bed was just taken by another admission — pick a different one.',
          403: 'Only doctors (Consultant/Registrar/Resident) can transfer patients or change their status.',
          400: 'Could not complete the transfer — please try again.',
        }),
      )
    } finally {
      setTransferSubmitting(false)
    }
  }

  function openStrokeType(a: AdmissionResponse) {
    setStrokeTarget(a)
    setStrokeValue(a.strokeType ?? 1)
    setStrokeError(null)
  }

  async function handleStrokeTypeSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId || !strokeTarget) return
    setStrokeSubmitting(true)
    setStrokeError(null)
    try {
      await setAdmissionStrokeType(strokeTarget.id, user.userId, strokeValue)
      setStrokeTarget(null)
      admissions.reload()
    } catch (err) {
      setStrokeError(
        describeApiError(err, {
          403: 'Only doctors (Consultant/Registrar/Resident) can set the stroke type.',
          400: 'Could not save the stroke type — please try again.',
        }),
      )
    } finally {
      setStrokeSubmitting(false)
    }
  }

  // beds selectable in the transfer modal: vacant ones, plus whichever bed the
  // patient is already in (so it stays visible even though its own status is Occupied)
  const availableBedsForTransfer = beds.data.filter(
    (b) => BED_STATUS[b.status as keyof typeof BED_STATUS] === 'Vacant' || b.id === transferTarget?.bedId,
  )

  // beds selectable when creating a brand-new admission: vacant only — there's no
  // "current bed" exception here since the admission doesn't exist yet. The backend
  // re-validates this on submit (409 if the bed was taken in the meantime), this is
  // just to stop the doctor from picking an occupied bed in the first place.
  const vacantBedsForCreate = beds.data.filter((b) => BED_STATUS[b.status as keyof typeof BED_STATUS] === 'Vacant')

  const columns: Column<AdmissionResponse>[] = [
    {
      header: 'Patient',
      render: (a) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">{patientNameById.get(a.patientId) ?? 'Unknown patient'}</span>
          {!isOpenAdmission(a) && <span className="text-[11.5px] text-text-muted">Discharged — read only</span>}
        </div>
      ),
    },
    {
      header: 'Bed / Ward',
      render: (a) =>
        a.bedId ? (
          <div className="flex flex-col gap-0.5">
            <span>{bedNumberById.get(a.bedId) ?? '—'}</span>
            <span className="text-[11.5px] text-text-muted">{wardNameByBedId.get(a.bedId) ?? '—'}</span>
          </div>
        ) : (
          <span className="text-text-muted">Unassigned</span>
        ),
    },
    {
      header: 'Admitted',
      render: (a) => (
        <div className="flex flex-col gap-0.5">
          <span>{new Date(a.admissionTime).toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })}</span>
          <span className="text-[11.5px] text-text-muted">
            {new Date(a.admissionTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </span>
        </div>
      ),
    },
    {
      header: 'Status',
      render: (a) => {
        const label = PATIENT_STATUS[a.status as keyof typeof PATIENT_STATUS] ?? 'Unknown'
        return <StatusPill label={label} tone={statusTone(label)} />
      },
    },
    {
      header: 'Stroke type',
      render: (a) =>
        canManage && isOpenAdmission(a) ? (
          <button
            type="button"
            onClick={() => openStrokeType(a)}
            className="rounded-md px-1.5 py-0.5 text-[12.5px] font-medium text-text-primary transition-colors duration-150 hover:bg-accent/10 hover:text-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
          >
            {a.strokeType ? STROKE_TYPE[a.strokeType as keyof typeof STROKE_TYPE] : 'Set stroke type…'}
          </button>
        ) : a.strokeType ? (
          STROKE_TYPE[a.strokeType as keyof typeof STROKE_TYPE]
        ) : (
          <span className="text-text-muted">—</span>
        ),
    },
    {
      header: 'Imaging',
      render: (a) => (
        <span className="text-text-secondary">
          {[a.ctDone && 'CT', a.mriDone && 'MRI', a.ctaDone && 'CTA'].filter(Boolean).join(', ') || (
            <span className="text-text-muted">—</span>
          )}
        </span>
      ),
    },
    {
      header: 'Actions',
      render: (a) => (
        <div className="flex flex-wrap items-center gap-1.5">
          {canManage && isOpenAdmission(a) && (
            <button
              type="button"
              onClick={() => openTransfer(a)}
              className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent transition-colors duration-150 hover:bg-accent/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
            >
              Transfer / update
            </button>
          )}
          <Link
            to={`/patient-summary/${a.id}`}
            className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-text-secondary transition-colors duration-150 hover:bg-border-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
          >
            Summary
          </Link>
          <Link
            to={`/report/${a.id}`}
            className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-text-secondary transition-colors duration-150 hover:bg-border-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
          >
            Report
          </Link>
        </div>
      ),
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Admissions"
        subtitle={`${admissions.data.length} admission${admissions.data.length === 1 ? '' : 's'} on record`}
        action={
          canManage ? (
            <PrimaryButton
              onClick={openNewAdmissionModal}
              className="focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40 focus-visible:ring-offset-2"
            >
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
          loading={admissions.loading || patients.loading || beds.loading || wards.loading}
          error={admissions.error}
          onRetry={admissions.reload}
          emptyMessage={
            canManage ? 'No admissions yet — use "+ New admission" above to add one.' : 'No admissions yet.'
          }
        />
      </Card>

      <Modal open={modalOpen} title="New admission" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <div className="flex flex-col gap-1.5">
            <span className="text-[13px] font-semibold text-text-secondary">Patient</span>
            {selectedPatient ? (
              <div className="flex flex-col gap-2 rounded-xl border border-accent/35 bg-accent/5 p-3 sm:flex-row sm:items-center sm:justify-between">
                <div className="min-w-0">
                  <p className="truncate text-[14px] font-semibold text-text">{selectedPatient.fullName}</p>
                  <p className="mt-0.5 text-[12.5px] text-text-muted">
                    Hospital No. {selectedPatient.hospitalNumber ?? '—'}
                    {selectedPatient.nationalIdMasked ? ` · National ID ${selectedPatient.nationalIdMasked}` : ''}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={clearSelectedPatient}
                  className="self-start rounded-lg border border-border-subtle px-3 py-1.5 text-[12.5px] font-medium text-text-secondary transition-colors duration-150 hover:bg-border-soft focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40 sm:self-auto"
                >
                  Change
                </button>
              </div>
            ) : (
              <>
                <TextInput
                  type="search"
                  value={patientQuery}
                  onChange={(e) => setPatientQuery(e.target.value)}
                  placeholder="Search by Hospital No. or National ID"
                  autoComplete="off"
                />
                <span className="text-[12px] text-text-muted">Type at least 2 characters. Patients with an active admission are excluded.</span>

                {patientSearchLoading && (
                  <p className="rounded-lg bg-border-soft px-3 py-2 text-[12.5px] font-medium text-text-secondary">Searching…</p>
                )}

                {patientSearchError && (
                  <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{patientSearchError}</p>
                )}

                {!patientSearchLoading && patientQuery.trim().length >= 2 && !patientSearchError && patientResults.length === 0 && (
                  <p className="rounded-lg border border-dashed border-border-subtle px-3 py-2 text-[12.5px] text-text-muted">
                    No eligible patient found.
                  </p>
                )}

                {patientResults.length > 0 && (
                  <div className="max-h-56 overflow-y-auto rounded-xl border border-border-subtle bg-white">
                    {patientResults.map((patient) => (
                      <button
                        key={patient.patientId}
                        type="button"
                        onClick={() => choosePatient(patient)}
                        className="flex w-full flex-col items-start gap-1 border-b border-border-soft px-3 py-2.5 text-left last:border-b-0 transition-colors duration-150 hover:bg-accent/5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-accent/40"
                      >
                        <span className="text-[14px] font-semibold text-text">{patient.fullName}</span>
                        <span className="text-[12.5px] text-text-muted">
                          Hospital No. {patient.hospitalNumber ?? '—'}
                          {patient.nationalIdMasked ? ` · National ID ${patient.nationalIdMasked}` : ''}
                        </span>
                      </button>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
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
              {vacantBedsForCreate.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.bedNumber}
                </option>
              ))}
            </Select>
            {beds.data.length > 0 && vacantBedsForCreate.length === 0 && (
              <span className="text-[12px] text-text-muted">No vacant beds right now — this admission can still be created without one.</span>
            )}
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting || !selectedPatient}>
            {submitting ? 'Saving…' : 'Save admission'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal
        open={transferTarget !== null}
        title={`Transfer / update status${
          transferTarget
            ? ` — ${patientNameById.get(transferTarget.patientId) ?? 'patient'}${
                hospitalNumberByPatientId.get(transferTarget.patientId)
                  ? ` (Hospital No. ${hospitalNumberByPatientId.get(transferTarget.patientId)})`
                  : ''
              }`
            : ''
        }`}
        onClose={() => setTransferTarget(null)}
      >
        <form onSubmit={handleTransferSubmit} className="flex flex-col gap-3.5">
          <Field label="New status">
            <Select
              value={transferForm.status}
              onChange={(e) => {
                setTransferForm({ ...transferForm, status: Number(e.target.value) })
                setDischargeConfirmed(false)
              }}
            >
              {PATIENT_STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>

          {isTerminalTransfer && (
            <label className="flex items-start gap-2 rounded-lg border border-warning/40 bg-warning-bg px-3 py-2.5 text-[12.5px] font-medium text-warning">
              <input
                type="checkbox"
                checked={dischargeConfirmed}
                onChange={(e) => setDischargeConfirmed(e.target.checked)}
                className="mt-0.5"
              />
              I confirm this ends the admission for{' '}
              {transferTarget ? patientNameById.get(transferTarget.patientId) ?? 'this patient' : 'this patient'}
              {transferTarget && hospitalNumberByPatientId.get(transferTarget.patientId)
                ? ` (Hospital No. ${hospitalNumberByPatientId.get(transferTarget.patientId)})`
                : ''}
              {' '}— they will show as discharged/transferred out and this cannot be undone from here.
            </label>
          )}

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

          <PrimaryButton type="submit" disabled={transferSubmitting || (isTerminalTransfer && !dischargeConfirmed)}>
            {transferSubmitting ? 'Saving…' : 'Save'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal
        open={strokeTarget !== null}
        title={`Stroke type${strokeTarget ? ` — ${patientNameById.get(strokeTarget.patientId) ?? 'patient'}` : ''}`}
        onClose={() => setStrokeTarget(null)}
      >
        <form onSubmit={handleStrokeTypeSubmit} className="flex flex-col gap-3.5">
          <Field label="Stroke type">
            <Select value={strokeValue} onChange={(e) => setStrokeValue(Number(e.target.value))}>
              {STROKE_TYPE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>

          {strokeError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{strokeError}</p>
          )}

          <PrimaryButton type="submit" disabled={strokeSubmitting}>
            {strokeSubmitting ? 'Saving…' : 'Save'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
