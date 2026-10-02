import { useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import { useAdmissionContext } from '@/hooks/useAdmissionContext'
import type {
  AdmissionResponse,
  PatientResponse,
  NIHSSAssessmentResponse,
  ASPECTSAssessmentResponse,
  ICHAssessmentResponse,
  CanadianTIAAssessmentResponse,
  BradenAssessmentResponse,
  GCSAssessmentResponse,
  GUSSAssessmentResponse,
  MorseAssessmentResponse,
} from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { AlertIcon } from '@/components/icons'
import { BRADEN_RISK_LEVEL, GCS_SEVERITY, GUSS_SEVERITY, MORSE_RISK_LEVEL, riskTone, STROKE_TYPE } from '@/lib/enums'
import { isDoctorRole, isNurseRole } from '@/lib/roles'

interface Row {
  id: string
  admissionId: string
  assessedAt: string
  totalScore: number
}

// Every score/severity/risk-level field is computed server-side from these
// raw sub-items (see e.g. NIHSSAssessment.TotalScore on the entity) — the
// Create endpoints only accept the raw items below, never a pre-computed
// total, so these forms never have to reimplement the scoring formulas.
type FieldDef =
  | { type: 'select'; key: string; label: string; options: number[]; optionLabels?: Record<number, string> }
  | { type: 'checkbox'; key: string; label: string; boolAsInt?: boolean; defaultChecked?: boolean }
  | { type: 'number'; key: string; label: string; min?: number; max?: number; step?: number }
  | { type: 'optionalSelect'; key: string; label: string; options: number[] }

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')

const nihss = entityApi<NIHSSAssessmentResponse>('NIHSSAssessment')
const aspects = entityApi<ASPECTSAssessmentResponse>('ASPECTSAssessment')
const ich = entityApi<ICHAssessmentResponse>('ICHAssessment')
const canadianTia = entityApi<CanadianTIAAssessmentResponse>('CanadianTIAAssessment')
const braden = entityApi<BradenAssessmentResponse>('BradenAssessment')
const gcs = entityApi<GCSAssessmentResponse>('GCSAssessment')
const guss = entityApi<GUSSAssessmentResponse>('GUSSAssessment')
const morse = entityApi<MorseAssessmentResponse>('MorseAssessment')

function nihssTone(score: number): 'success' | 'warning' | 'critical' | 'info' {
  if (score === 0) return 'success'
  if (score <= 4) return 'info'
  if (score <= 15) return 'warning'
  return 'critical'
}

function range(from: number, to: number) {
  const out: number[] = []
  for (let i = from; i <= to; i++) out.push(i)
  return out
}

const NIHSS_FIELDS: FieldDef[] = [
  { type: 'select', key: 'LOC', label: 'Level of consciousness', options: range(0, 3) },
  { type: 'select', key: 'LOCQuestions', label: 'LOC questions', options: range(0, 2) },
  { type: 'select', key: 'LOCCommands', label: 'LOC commands', options: range(0, 2) },
  { type: 'select', key: 'BestGaze', label: 'Best gaze', options: range(0, 2) },
  { type: 'select', key: 'Visual', label: 'Visual fields', options: range(0, 3) },
  { type: 'select', key: 'FacialPalsy', label: 'Facial palsy', options: range(0, 3) },
  { type: 'select', key: 'MotorArmLeft', label: 'Motor arm — left', options: range(0, 4) },
  { type: 'select', key: 'MotorArmRight', label: 'Motor arm — right', options: range(0, 4) },
  { type: 'select', key: 'MotorLegLeft', label: 'Motor leg — left', options: range(0, 4) },
  { type: 'select', key: 'MotorLegRight', label: 'Motor leg — right', options: range(0, 4) },
  { type: 'select', key: 'LimbAtaxia', label: 'Limb ataxia', options: range(0, 2) },
  { type: 'select', key: 'Sensory', label: 'Sensory', options: range(0, 2) },
  { type: 'select', key: 'BestLanguage', label: 'Best language', options: range(0, 3) },
  { type: 'select', key: 'Dysarthria', label: 'Dysarthria', options: range(0, 2) },
  { type: 'select', key: 'Extinction', label: 'Extinction / inattention', options: range(0, 2) },
]

const ASPECTS_FIELDS: FieldDef[] = (['C', 'P', 'IC', 'I', 'M1', 'M2', 'M3', 'M4', 'M5', 'M6'] as const).map((k) => ({
  type: 'checkbox',
  key: k,
  label: `${k} — normal (unchecked = affected)`,
  boolAsInt: true,
  defaultChecked: true,
}))

const ICH_FIELDS: FieldDef[] = [
  { type: 'number', key: 'GCSScore', label: 'GCS score', min: 3, max: 15 },
  { type: 'number', key: 'ICHVolumeMl', label: 'ICH volume (mL)', min: 0, step: 0.1 },
  { type: 'checkbox', key: 'InfratentorialOrigin', label: 'Infratentorial origin' },
  { type: 'checkbox', key: 'IVHPresent', label: 'IVH present' },
  {
    type: 'select',
    key: 'AgeScore',
    label: 'Age',
    options: [0, 1],
    optionLabels: { 0: 'Under 80', 1: '80 or older' },
  },
]

const TIA_FIELDS: FieldDef[] = [
  { type: 'checkbox', key: 'FirstTIAInLifetime', label: 'First TIA in lifetime' },
  { type: 'checkbox', key: 'SymptomsOver10Min', label: 'Symptoms lasted over 10 minutes' },
  { type: 'checkbox', key: 'HistoryCarotidStenosis', label: 'History of carotid stenosis' },
  { type: 'checkbox', key: 'OnAntiplateletTherapy', label: 'Currently on antiplatelet therapy' },
  { type: 'checkbox', key: 'GaitDisturbance', label: 'Gait disturbance' },
  { type: 'checkbox', key: 'UnilateralWeakness', label: 'Unilateral weakness' },
  { type: 'checkbox', key: 'HistoryOfVertigo', label: 'History of vertigo' },
  { type: 'checkbox', key: 'DiastolicBPOver110', label: 'Diastolic BP over 110 mmHg' },
  { type: 'checkbox', key: 'DysarthriaOrAphasia', label: 'Dysarthria or aphasia' },
  { type: 'checkbox', key: 'AtrialFibrillationOnECG', label: 'Atrial fibrillation on ECG' },
  { type: 'checkbox', key: 'InfarctionOnCT', label: 'Infarction on CT' },
  { type: 'checkbox', key: 'PlateletOver400', label: 'Platelet count over 400×10⁹/L' },
  { type: 'checkbox', key: 'GlucoseOver15', label: 'Glucose over 15 mmol/L' },
]

const BRADEN_FIELDS: FieldDef[] = [
  { type: 'select', key: 'SensoryPerception', label: 'Sensory perception', options: range(1, 4) },
  { type: 'select', key: 'Moisture', label: 'Moisture', options: range(1, 4) },
  { type: 'select', key: 'Activity', label: 'Activity', options: range(1, 4) },
  { type: 'select', key: 'Mobility', label: 'Mobility', options: range(1, 4) },
  { type: 'select', key: 'Nutrition', label: 'Nutrition', options: range(1, 4) },
  { type: 'select', key: 'FrictionShear', label: 'Friction & shear', options: range(1, 3) },
]

const GCS_FIELDS: FieldDef[] = [
  { type: 'select', key: 'EyeResponse', label: 'Eye response', options: range(1, 4) },
  { type: 'select', key: 'VerbalResponse', label: 'Verbal response', options: range(1, 5) },
  { type: 'select', key: 'MotorResponse', label: 'Motor response', options: range(1, 6) },
]

const GUSS_FIELDS: FieldDef[] = [
  { type: 'checkbox', key: 'Vigilance', label: 'Vigilance (alert ≥ 15 min)' },
  { type: 'checkbox', key: 'VoluntaryCough', label: 'Voluntary cough / throat clearing' },
  { type: 'checkbox', key: 'SalivaSwallowSuccessful', label: 'Saliva swallow successful' },
  { type: 'checkbox', key: 'NoDrooling', label: 'No drooling' },
  { type: 'checkbox', key: 'NoVoiceChange', label: 'No voice change' },
  { type: 'optionalSelect', key: 'SemisolidScore', label: 'Semisolid test score (0–5, if performed)', options: range(0, 5) },
  { type: 'optionalSelect', key: 'LiquidScore', label: 'Liquid test score (0–5, if performed)', options: range(0, 5) },
  { type: 'optionalSelect', key: 'SolidScore', label: 'Solid test score (0–5, if performed)', options: range(0, 5) },
]

const MORSE_FIELDS: FieldDef[] = [
  { type: 'checkbox', key: 'HistoryOfFalling', label: 'History of falling' },
  { type: 'checkbox', key: 'SecondaryDiagnosis', label: 'Secondary diagnosis' },
  {
    type: 'select',
    key: 'AmbulatoryAid',
    label: 'Ambulatory aid',
    options: [0, 15, 30],
    optionLabels: { 0: 'None / bedrest / wheelchair / nurse assist', 15: 'Crutches / cane / walker', 30: 'Furniture' },
  },
  { type: 'checkbox', key: 'IVOrHeparinLock', label: 'IV / heparin lock in place' },
  {
    type: 'select',
    key: 'Gait',
    label: 'Gait',
    options: [0, 10, 20],
    optionLabels: { 0: 'Normal / bedrest / wheelchair', 10: 'Weak', 20: 'Impaired' },
  },
  { type: 'checkbox', key: 'MentalStatus', label: 'Overestimates ability / forgets limitations' },
]

interface TabDef {
  key: string
  label: string
  fetch: () => Promise<Row[]>
  scoreTone: (row: Row) => 'success' | 'warning' | 'critical' | 'info'
  scoreLabel: (row: Row) => string
  fields: FieldDef[]
  canWrite: (role?: string | null) => boolean
  create: (payload: Record<string, unknown>, actingUserId: string) => Promise<string>
  // Which stroke types this assessment is typically used for (per the team's
  // agreed protocol: Ischemic → NIHSS/ASPECTS, Hemorrhagic → NIHSS/ICH,
  // TIA → Canadian TIA). null = relevant regardless of stroke type (the
  // general nursing/neuro scales). This only drives a warning, never blocks
  // recording — a clinician can always have a real reason to use it anyway.
  relevantStrokeTypes: number[] | null
}

const TABS: TabDef[] = [
  {
    key: 'nihss',
    label: 'NIHSS',
    fetch: () => nihss.list(),
    scoreTone: (r) => nihssTone(r.totalScore),
    scoreLabel: (r) => (r as unknown as NIHSSAssessmentResponse).severity,
    fields: NIHSS_FIELDS,
    canWrite: isDoctorRole,
    create: (p, u) => nihss.create(p, u),
    relevantStrokeTypes: [1, 2],
  },
  {
    key: 'aspects',
    label: 'ASPECTS',
    fetch: () => aspects.list(),
    scoreTone: (r) => (r.totalScore >= 8 ? 'success' : r.totalScore >= 6 ? 'warning' : 'critical'),
    scoreLabel: (r) => `${r.totalScore} / 10`,
    fields: ASPECTS_FIELDS,
    canWrite: isDoctorRole,
    create: (p, u) => aspects.create(p, u),
    relevantStrokeTypes: [1],
  },
  {
    key: 'ich',
    label: 'ICH Score',
    fetch: () => ich.list(),
    scoreTone: (r) => (r.totalScore <= 1 ? 'success' : r.totalScore <= 3 ? 'warning' : 'critical'),
    scoreLabel: (r) => String(r.totalScore),
    fields: ICH_FIELDS,
    canWrite: isDoctorRole,
    create: (p, u) => ich.create(p, u),
    relevantStrokeTypes: [2],
  },
  {
    key: 'tia',
    label: 'Canadian TIA',
    fetch: () => canadianTia.list(),
    scoreTone: (r) => riskTone((r as unknown as CanadianTIAAssessmentResponse).riskLevel),
    scoreLabel: (r) => (r as unknown as CanadianTIAAssessmentResponse).riskLevel,
    fields: TIA_FIELDS,
    canWrite: isDoctorRole,
    create: (p, u) => canadianTia.create(p, u),
    relevantStrokeTypes: [3],
  },
  {
    key: 'braden',
    label: 'Braden Scale',
    fetch: () => braden.list(),
    scoreTone: (r) => riskTone(BRADEN_RISK_LEVEL[(r as unknown as BradenAssessmentResponse).riskLevel] ?? ''),
    scoreLabel: (r) => BRADEN_RISK_LEVEL[(r as unknown as BradenAssessmentResponse).riskLevel] ?? '—',
    fields: BRADEN_FIELDS,
    canWrite: isNurseRole,
    create: (p, u) => braden.create(p, u),
    relevantStrokeTypes: null,
  },
  {
    key: 'gcs',
    label: 'GCS',
    fetch: () => gcs.list(),
    scoreTone: (r) => riskTone(GCS_SEVERITY[(r as unknown as GCSAssessmentResponse).severity] ?? ''),
    scoreLabel: (r) => GCS_SEVERITY[(r as unknown as GCSAssessmentResponse).severity] ?? '—',
    fields: GCS_FIELDS,
    canWrite: isDoctorRole,
    create: (p, u) => gcs.create(p, u),
    relevantStrokeTypes: null,
  },
  {
    key: 'guss',
    label: 'GUSS (Swallow)',
    fetch: () => guss.list(),
    scoreTone: (r) => riskTone(GUSS_SEVERITY[(r as unknown as GUSSAssessmentResponse).severity] ?? ''),
    scoreLabel: (r) => GUSS_SEVERITY[(r as unknown as GUSSAssessmentResponse).severity] ?? '—',
    fields: GUSS_FIELDS,
    canWrite: isNurseRole,
    create: (p, u) => guss.create(p, u),
    relevantStrokeTypes: null,
  },
  {
    key: 'morse',
    label: 'Morse Fall Scale',
    fetch: () => morse.list(),
    scoreTone: (r) => riskTone(MORSE_RISK_LEVEL[(r as unknown as MorseAssessmentResponse).riskLevel] ?? ''),
    scoreLabel: (r) => MORSE_RISK_LEVEL[(r as unknown as MorseAssessmentResponse).riskLevel] ?? '—',
    fields: MORSE_FIELDS,
    canWrite: isNurseRole,
    create: (p, u) => morse.create(p, u),
    relevantStrokeTypes: null,
  },
]

function defaultsFor(fields: FieldDef[]): Record<string, number | boolean | null> {
  const obj: Record<string, number | boolean | null> = {}
  fields.forEach((f) => {
    if (f.type === 'select') obj[f.key] = f.options[0]
    else if (f.type === 'checkbox') obj[f.key] = f.defaultChecked ?? false
    else if (f.type === 'number') obj[f.key] = f.min ?? 0
    else obj[f.key] = null
  })
  return obj
}

function buildPayload(fields: FieldDef[], values: Record<string, number | boolean | null>) {
  const payload: Record<string, unknown> = {}
  fields.forEach((f) => {
    if (f.type === 'checkbox' && f.boolAsInt) payload[f.key] = values[f.key] ? 1 : 0
    else payload[f.key] = values[f.key]
  })
  return payload
}

function FieldInput({
  field,
  value,
  onChange,
}: {
  field: FieldDef
  value: number | boolean | null
  onChange: (v: number | boolean | null) => void
}) {
  if (field.type === 'select') {
    return (
      <Field label={field.label} required>
        <Select value={value as number} onChange={(e) => onChange(Number(e.target.value))}>
          {field.options.map((o) => (
            <option key={o} value={o}>
              {field.optionLabels?.[o] ?? o}
            </option>
          ))}
        </Select>
      </Field>
    )
  }
  if (field.type === 'checkbox') {
    return (
      <label className="flex items-center gap-2.5 rounded-lg border border-border-subtle bg-surface px-3 py-2.5 text-[13px] text-text-secondary transition-colors duration-150 hover:bg-border-soft/60">
        <input
          type="checkbox"
          checked={!!value}
          onChange={(e) => onChange(e.target.checked)}
          className="h-4 w-4 shrink-0 accent-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
        />
        <span>{field.label}</span>
      </label>
    )
  }
  if (field.type === 'number') {
    return (
      <Field label={field.label} required>
        <TextInput
          type="number"
          min={field.min}
          max={field.max}
          step={field.step ?? 1}
          value={value as number}
          onChange={(e) => onChange(Number(e.target.value))}
        />
      </Field>
    )
  }
  return (
    <Field label={field.label}>
      <Select
        value={value === null ? '' : (value as number)}
        onChange={(e) => onChange(e.target.value === '' ? null : Number(e.target.value))}
      >
        <option value="">Not tested</option>
        {field.options.map((o) => (
          <option key={o} value={o}>
            {o}
          </option>
        ))}
      </Select>
    </Field>
  )
}

function AssessmentTable({ tab }: { tab: TabDef }) {
  const { data, loading, error, reload } = useEntityList<Row>(() => tab.fetch() as Promise<Row[]>, [tab.key])
  const { patientNameByAdmissionId, loading: contextLoading, error: contextError, reload: reloadContext } = useAdmissionContext()

  const columns: Column<Row>[] = [
    {
      header: 'Patient',
      render: (r) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">{patientNameByAdmissionId.get(r.admissionId) ?? '—'}</span>
          <span className="text-[11.5px] text-text-muted">Admission #{r.admissionId.slice(0, 8).toUpperCase()}</span>
        </div>
      ),
    },
    {
      header: 'Assessed at',
      render: (r) => (
        <div className="flex flex-col gap-0.5">
          <span>
            {new Date(r.assessedAt).toLocaleDateString([], { month: 'short', day: 'numeric', year: 'numeric' })}
          </span>
          <span className="text-[11.5px] text-text-muted">
            {new Date(r.assessedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
          </span>
        </div>
      ),
    },
    {
      header: 'Total score',
      render: (r) => <span className="text-[15px] font-semibold text-text">{r.totalScore}</span>,
    },
    { header: 'Result', render: (r) => <StatusPill label={tab.scoreLabel(r)} tone={tab.scoreTone(r)} /> },
  ]

  return (
    <div className="flex flex-col gap-3">
      {contextError && !error && (
        <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
          Patient names couldn't be loaded, so rows below may show as "—".{' '}
          <button type="button" onClick={reloadContext} className="underline">
            Retry
          </button>
        </p>
      )}
      <DataTable
        columns={columns}
        rows={data}
        rowKey={(r) => r.id}
        loading={loading || contextLoading}
        error={error}
        onRetry={reload}
        emptyMessage={`No ${tab.label} assessments recorded yet.`}
      />
    </div>
  )
}

export default function Assessments() {
  const { user } = useAuth()
  const [active, setActive] = useState(TABS[0].key)
  const activeTab = TABS.find((t) => t.key === active) ?? TABS[0]
  const [reloadKey, setReloadKey] = useState(0)

  const admissions = useEntityList(() => admissionsApi.list())
  const patients = useEntityList(() => patientsApi.list())
  const patientNameByAdmissionId = useMemo(() => {
    const patientNameById = new Map(patients.data.map((p) => [p.id, `${p.firstName} ${p.lastName}`]))
    return new Map(admissions.data.map((a) => [a.id, patientNameById.get(a.patientId) ?? 'Unknown patient']))
  }, [admissions.data, patients.data])

  const [modalOpen, setModalOpen] = useState(false)
  const [admissionId, setAdmissionId] = useState('')
  const [assessedAt, setAssessedAt] = useState('')
  const [values, setValues] = useState<Record<string, number | boolean | null>>(() => defaultsFor(TABS[0].fields))
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const canWrite = activeTab.canWrite(user?.role)

  const selectedAdmission = admissions.data.find((a) => a.id === admissionId)
  const strokeTypeMismatch =
    activeTab.relevantStrokeTypes !== null &&
    selectedAdmission?.strokeType != null &&
    !activeTab.relevantStrokeTypes.includes(selectedAdmission.strokeType)

  function openNew() {
    setAdmissionId('')
    setAssessedAt('')
    setValues(defaultsFor(activeTab.fields))
    setFormError(null)
    setModalOpen(true)
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      const payload = {
        admissionId,
        assessedAt,
        ...buildPayload(activeTab.fields, values),
      }
      await activeTab.create(payload, user.userId)
      setModalOpen(false)
      setReloadKey((k) => k + 1)
    } catch (err) {
      const status = (err as { response?: { status?: number } })?.response?.status
      setFormError(
        status === 403
          ? 'Your role cannot record this assessment type.'
          : 'Could not save the assessment — check the values entered.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Assessments"
        subtitle="Clinical scoring tools, grouped by type"
        action={
          canWrite ? (
            <PrimaryButton
              onClick={openNew}
              disabled={admissions.data.length === 0}
              className="focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40 focus-visible:ring-offset-2"
            >
              + New {activeTab.label}
            </PrimaryButton>
          ) : (
            <span className="text-[12.5px] text-text-muted">
              {activeTab.key === 'braden' || activeTab.key === 'guss' || activeTab.key === 'morse'
                ? 'Only nursing staff can record this scale'
                : 'Only doctors can record this assessment'}
            </span>
          )
        }
      />

      <Card className="flex flex-col gap-5">
        <div className="flex flex-wrap gap-2 border-b border-border pb-3">
          {TABS.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActive(tab.key)}
              className={`rounded-full px-3.5 py-1.5 text-[13px] font-semibold transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40 ${
                tab.key === active ? 'bg-accent text-white' : 'bg-border-soft text-text-secondary hover:bg-border'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
        <AssessmentTable key={activeTab.key + reloadKey} tab={activeTab} />
      </Card>

      <Modal
        open={modalOpen}
        title={`New ${activeTab.label} assessment`}
        onClose={() => setModalOpen(false)}
        size="wide"
      >
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-3 rounded-xl border border-border-subtle bg-border-soft/40 p-3.5">
            <span className="text-[11px] font-semibold uppercase tracking-wide text-text-muted">
              Assessment context
            </span>
            <Field label="Patient / admission" required>
              <Select required value={admissionId} onChange={(e) => setAdmissionId(e.target.value)}>
                <option value="" disabled>
                  Select an admission…
                </option>
                {admissions.data.map((a) => (
                  <option key={a.id} value={a.id}>
                    {patientNameByAdmissionId.get(a.id) ?? a.id.slice(0, 8)} —{' '}
                    {new Date(a.admissionTime).toLocaleDateString()}
                  </option>
                ))}
              </Select>
            </Field>

            <Field label="Assessed at" required>
              <TextInput
                type="datetime-local"
                required
                value={assessedAt}
                onChange={(e) => setAssessedAt(e.target.value)}
              />
            </Field>
          </div>

          {strokeTypeMismatch && selectedAdmission?.strokeType != null && (
            <p className="flex items-start gap-1.5 rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
              <AlertIcon className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              <span>
                {activeTab.label} isn't typically used for a{' '}
                {STROKE_TYPE[selectedAdmission.strokeType as keyof typeof STROKE_TYPE]} stroke — you can still
                record it if there's a clinical reason to.
              </span>
            </p>
          )}

          <fieldset className="flex flex-col gap-3 rounded-xl border border-border-subtle p-3.5">
            <legend className="px-1 text-[11px] font-semibold uppercase tracking-wide text-text-muted">
              {activeTab.label} items
            </legend>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {activeTab.fields.map((f) => (
                <FieldInput
                  key={f.key}
                  field={f}
                  value={values[f.key]}
                  onChange={(v) => setValues((prev) => ({ ...prev, [f.key]: v }))}
                />
              ))}
            </div>
          </fieldset>

          {formError && (
            <p className="flex items-start gap-1.5 rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">
              <AlertIcon className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              <span>{formError}</span>
            </p>
          )}

          <PrimaryButton
            type="submit"
            disabled={submitting}
            className="focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40 focus-visible:ring-offset-2"
          >
            {submitting ? 'Saving…' : 'Save assessment'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
