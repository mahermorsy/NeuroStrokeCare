import { useMemo, type ReactElement, type SVGProps } from 'react'
import { motion } from 'framer-motion'
import { Link } from 'react-router-dom'
import { useCountUp } from '@/hooks/useCountUp'
import { useEntityList } from '@/hooks/useEntityList'
import { entityApi } from '@/lib/entityApi'
import { ClipboardPlusIcon, BedIcon, CheckSquareIcon, ClockIcon } from '@/components/icons'
import { PATIENT_STATUS, riskTone } from '@/lib/enums'
import type {
  AdmissionResponse,
  PatientResponse,
  WardResponse,
  BedResponse,
  DoorTimingResponse,
  NIHSSAssessmentResponse,
  ASPECTSAssessmentResponse,
  ICHAssessmentResponse,
  CanadianTIAAssessmentResponse,
  BradenAssessmentResponse,
  GCSAssessmentResponse,
  GUSSAssessmentResponse,
  MorseAssessmentResponse,
} from '@/types/entities'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')
const wardsApi = entityApi<WardResponse>('Ward')
const bedsApi = entityApi<BedResponse>('Bed')
const doorTimingApi = entityApi<DoorTimingResponse>('DoorTiming')
const nihssApi = entityApi<NIHSSAssessmentResponse>('NIHSSAssessment')
const aspectsApi = entityApi<ASPECTSAssessmentResponse>('ASPECTSAssessment')
const ichApi = entityApi<ICHAssessmentResponse>('ICHAssessment')
const tiaApi = entityApi<CanadianTIAAssessmentResponse>('CanadianTIAAssessment')
const bradenApi = entityApi<BradenAssessmentResponse>('BradenAssessment')
const gcsApi = entityApi<GCSAssessmentResponse>('GCSAssessment')
const gussApi = entityApi<GUSSAssessmentResponse>('GUSSAssessment')
const morseApi = entityApi<MorseAssessmentResponse>('MorseAssessment')

// Mirrors Assessments.tsx's clinical relevance mapping: null = every admission
// needs it regardless of stroke type, an array = only admissions with that
// StrokeType (1 Ischemic / 2 Hemorrhagic / 3 TIA) need it.
const REQUIRED_ASSESSMENTS: { key: string; label: string; relevantStrokeTypes: number[] | null }[] = [
  { key: 'nihss', label: 'NIHSS reassessment', relevantStrokeTypes: [1, 2] },
  { key: 'aspects', label: 'ASPECTS', relevantStrokeTypes: [1] },
  { key: 'ich', label: 'ICH Score', relevantStrokeTypes: [2] },
  { key: 'tia', label: 'Canadian TIA', relevantStrokeTypes: [3] },
  { key: 'braden', label: 'Braden Scale', relevantStrokeTypes: null },
  { key: 'gcs', label: 'GCS check', relevantStrokeTypes: null },
  { key: 'guss', label: 'GUSS (Swallow)', relevantStrokeTypes: null },
  { key: 'morse', label: 'Morse Fall Scale', relevantStrokeTypes: null },
]

const STATUS_STYLES: Record<string, string> = {
  success: 'bg-success-bg text-success',
  critical: 'bg-critical-bg text-critical',
  warning: 'bg-warning-bg text-warning',
  info: 'bg-info-bg text-blue',
}

const ASSESSMENT_TONE_STYLES: Record<string, { bg: string; text: string }> = {
  critical: { bg: 'bg-critical-bg', text: 'text-critical' },
  warning: { bg: 'bg-warning-bg', text: 'text-warning' },
}

function occupancyColor(pct: number) {
  if (pct >= 95) return 'bg-critical'
  if (pct >= 70) return 'bg-warning'
  return 'bg-success'
}

const fadeUp = {
  hidden: { opacity: 0, y: 14 },
  show: { opacity: 1, y: 0 },
}

const stagger = {
  hidden: {},
  show: {
    transition: { staggerChildren: 0.06 },
  },
}

function StatTile({
  label,
  value,
  suffix,
  icon: Icon,
  iconBg,
  iconColor,
  note,
  noteColor,
}: {
  label: string
  value: number
  suffix: string
  icon: (props: SVGProps<SVGSVGElement>) => ReactElement
  iconBg: string
  iconColor: string
  note: string
  noteColor: string
}) {
  const count = useCountUp(value)
  return (
    <motion.div
      variants={fadeUp}
      transition={{ duration: 0.4, ease: 'easeOut' }}
      className="flex flex-col gap-3.5 rounded-2xl border border-border bg-surface p-5 shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
    >
      <div className="flex items-center gap-2.5">
        <span className={`flex h-[38px] w-[38px] items-center justify-center rounded-[10px] ${iconBg}`}>
          <Icon className={`h-[19px] w-[19px] ${iconColor}`} />
        </span>
        <span className="text-[12px] font-semibold uppercase tracking-wide text-text-secondary">{label}</span>
      </div>
      <span className="text-[32px] font-bold leading-none text-text">
        {count}
        <span className="text-[18px] font-medium text-text-muted">{suffix}</span>
      </span>
      <span className={`text-[12.5px] font-semibold ${noteColor}`}>{note}</span>
    </motion.div>
  )
}

export default function Dashboard() {
  const admissions = useEntityList(() => admissionsApi.list())
  const patients = useEntityList(() => patientsApi.list())
  const wards = useEntityList(() => wardsApi.list())
  const beds = useEntityList(() => bedsApi.list())
  const doorTimings = useEntityList(() => doorTimingApi.list())
  const nihss = useEntityList(() => nihssApi.list())
  const aspects = useEntityList(() => aspectsApi.list())
  const ich = useEntityList(() => ichApi.list())
  const tia = useEntityList(() => tiaApi.list())
  const braden = useEntityList(() => bradenApi.list())
  const gcs = useEntityList(() => gcsApi.list())
  const guss = useEntityList(() => gussApi.list())
  const morse = useEntityList(() => morseApi.list())

  const loading =
    admissions.loading || patients.loading || wards.loading || beds.loading || doorTimings.loading || nihss.loading

  const view = useMemo(() => {
    const patientById = new Map(patients.data.map((p) => [p.id, p]))
    const wardById = new Map(wards.data.map((w) => [w.id, w]))
    const bedById = new Map(beds.data.map((b) => [b.id, b]))

    const assessedByType: Record<string, Set<string>> = {
      nihss: new Set(nihss.data.map((r) => r.admissionId)),
      aspects: new Set(aspects.data.map((r) => r.admissionId)),
      ich: new Set(ich.data.map((r) => r.admissionId)),
      tia: new Set(tia.data.map((r) => r.admissionId)),
      braden: new Set(braden.data.map((r) => r.admissionId)),
      gcs: new Set(gcs.data.map((r) => r.admissionId)),
      guss: new Set(guss.data.map((r) => r.admissionId)),
      morse: new Set(morse.data.map((r) => r.admissionId)),
    }
    const latestNihssByAdmission = new Map<string, number>()
    nihss.data.forEach((r) => {
      const prev = latestNihssByAdmission.get(r.admissionId)
      if (prev === undefined) latestNihssByAdmission.set(r.admissionId, r.totalScore)
    })

    const activeAdmissions = admissions.data
      .filter((a) => !a.dischargeTime)
      .sort((a, b) => new Date(b.admissionTime).getTime() - new Date(a.admissionTime).getTime())

    const admittedLast24h = activeAdmissions.filter(
      (a) => Date.now() - new Date(a.admissionTime).getTime() < 24 * 3600 * 1000,
    ).length

    const vacantBeds = beds.data.filter((b) => b.status === 1)
    const wardsWithVacancy = new Set(vacantBeds.map((b) => b.wardId)).size

    // Every active admission × every clinically-required assessment it hasn't
    // had recorded yet — pending items sorted with the longest-waiting first.
    type PendingItem = { admissionId: string; patientName: string; label: string; minutesSince: number }
    const pendingItems: PendingItem[] = []
    activeAdmissions.forEach((a) => {
      const patient = patientById.get(a.patientId)
      const patientName = patient ? `${patient.firstName} ${patient.lastName}` : 'Unknown patient'
      const minutesSince = (Date.now() - new Date(a.admissionTime).getTime()) / 60000
      REQUIRED_ASSESSMENTS.forEach((req) => {
        if (req.relevantStrokeTypes !== null && (!a.strokeType || !req.relevantStrokeTypes.includes(a.strokeType)))
          return
        if (!assessedByType[req.key].has(a.id)) {
          pendingItems.push({ admissionId: a.id, patientName, label: req.label, minutesSince })
        }
      })
    })
    pendingItems.sort((a, b) => b.minutesSince - a.minutesSince)
    const admissionsWithPending = new Set(pendingItems.map((p) => p.admissionId)).size
    const urgentCount = pendingItems.filter((p) => p.minutesSince > 30).length

    const needleMinutes = doorTimings.data.map((d) => d.minutesToNeedle).filter((m): m is number => m !== null)
    const avgDoorToNeedle =
      needleMinutes.length > 0 ? Math.round(needleMinutes.reduce((s, m) => s + m, 0) / needleMinutes.length) : null

    const admissionRows = activeAdmissions.slice(0, 5).map((a) => {
      const patient = patientById.get(a.patientId)
      const bed = a.bedId ? bedById.get(a.bedId) : undefined
      const ward = bed ? wardById.get(bed.wardId) : undefined
      const statusLabel = PATIENT_STATUS[a.status as keyof typeof PATIENT_STATUS] ?? 'Unknown'
      return {
        id: a.id,
        name: patient ? `${patient.firstName} ${patient.lastName}` : 'Unknown patient',
        location: bed && ward ? `${ward.name} · ${bed.bedNumber}` : 'Unassigned',
        time: new Date(a.admissionTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        nihss: latestNihssByAdmission.get(a.id) ?? '—',
        statusLabel,
        tone: riskTone(statusLabel),
      }
    })

    const wardOccupancy = wards.data.map((w) => {
      const wardBeds = beds.data.filter((b) => b.wardId === w.id)
      const occupied = wardBeds.filter((b) => b.status !== 1).length
      const total = wardBeds.length || w.totalBeds || 1
      return { name: w.name, occupied, total, pct: Math.round((occupied / total) * 100) }
    })

    return {
      activeCount: activeAdmissions.length,
      admittedLast24h,
      vacantBeds: vacantBeds.length,
      totalBeds: beds.data.length,
      wardsWithVacancy,
      admissionsWithPending,
      urgentCount,
      avgDoorToNeedle,
      admissionRows,
      pendingItems: pendingItems.slice(0, 4),
      wardOccupancy,
    }
  }, [
    admissions.data,
    patients.data,
    wards.data,
    beds.data,
    doorTimings.data,
    nihss.data,
    aspects.data,
    ich.data,
    tia.data,
    braden.data,
    gcs.data,
    guss.data,
    morse.data,
  ])

  const stats = [
    {
      key: 'admissions',
      label: 'Active Admissions',
      value: view.activeCount,
      suffix: '',
      icon: ClipboardPlusIcon,
      iconBg: 'bg-accent/10',
      iconColor: 'text-accent',
      note: `${view.admittedLast24h} admitted in the last 24h`,
      noteColor: 'text-success',
    },
    {
      key: 'beds',
      label: 'Bed Availability',
      value: view.vacantBeds,
      suffix: ` / ${view.totalBeds}`,
      icon: BedIcon,
      iconBg: 'bg-blue/10',
      iconColor: 'text-blue',
      note: `open across ${view.wardsWithVacancy} ward${view.wardsWithVacancy === 1 ? '' : 's'}`,
      noteColor: 'text-text-muted',
    },
    {
      key: 'assessment',
      label: 'In Assessment',
      value: view.admissionsWithPending,
      suffix: '',
      icon: CheckSquareIcon,
      iconBg: 'bg-warning/15',
      iconColor: 'text-warning',
      note: `${view.urgentCount} pending > 30 min`,
      noteColor: 'text-warning',
    },
    {
      key: 'dtn',
      label: 'Avg Door-to-Needle',
      value: view.avgDoorToNeedle ?? 0,
      suffix: view.avgDoorToNeedle === null ? '' : ' min',
      icon: ClockIcon,
      iconBg: 'bg-success/10',
      iconColor: 'text-success',
      note: view.avgDoorToNeedle === null ? 'No needle-time data yet' : 'Target: ≤ 60 min',
      noteColor: 'text-success',
    },
  ]

  return (
    <motion.div variants={stagger} initial="hidden" animate="show" className="flex flex-col gap-6">
      <motion.div
        variants={fadeUp}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        className="flex flex-wrap items-start justify-between gap-5"
      >
        <div>
          <h1 className="font-display text-[22px] font-semibold text-text sm:text-[28px]">Dashboard</h1>
          <p className="mt-1.5 text-[14px] text-text-secondary">
            Stroke unit overview ·{' '}
            {new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1.5 rounded-full border border-border bg-surface px-3.5 py-2 text-[13px] text-text-secondary">
            <span className="h-2 w-2 rounded-full bg-success" />
            Live data
          </span>
          <Link
            to="/admissions"
            className="rounded-lg border border-border bg-surface px-4 py-2.5 text-[13.5px] font-semibold text-text"
          >
            View reports
          </Link>
          <Link
            to="/admissions"
            className="rounded-lg bg-accent px-4 py-2.5 text-[13.5px] font-semibold text-white"
          >
            + New admission
          </Link>
        </div>
      </motion.div>

      <div className="grid grid-cols-1 gap-[18px] sm:grid-cols-2 xl:grid-cols-4">
        {stats.map(({ key, ...stat }) => (
          <StatTile key={key} {...stat} />
        ))}
      </div>

      <div className="grid grid-cols-1 gap-[18px] xl:grid-cols-[1.7fr_1fr]">
        <motion.div
          variants={fadeUp}
          transition={{ duration: 0.4, ease: 'easeOut' }}
          className="flex flex-col gap-4 rounded-2xl border border-border bg-surface p-6 shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
        >
          <div className="flex items-center justify-between">
            <h2 className="text-[16px] font-semibold text-text">Active Admissions</h2>
            <Link to="/admissions" className="text-[13px] font-semibold text-accent hover:underline">
              View all
            </Link>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[560px] border-collapse">
              <thead>
                <tr>
                  {['Patient', 'Ward / Bed', 'Admitted', 'NIHSS', 'Status'].map((h) => (
                    <th
                      key={h}
                      className="border-b border-border px-2.5 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-text-muted"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {view.admissionRows.length === 0 && !loading && (
                  <tr>
                    <td colSpan={5} className="px-2.5 py-6 text-center text-[13px] text-text-muted">
                      No active admissions.
                    </td>
                  </tr>
                )}
                {view.admissionRows.map((row, i) => (
                  <tr key={row.id} className={i < view.admissionRows.length - 1 ? 'border-b border-border-soft' : ''}>
                    <td className="px-2.5 py-3.5 text-[14px] font-semibold text-text">{row.name}</td>
                    <td className="px-2.5 py-3.5 text-[13.5px] text-text-secondary">{row.location}</td>
                    <td className="px-2.5 py-3.5 text-[13.5px] text-text-secondary">{row.time}</td>
                    <td className="px-2.5 py-3.5 text-[13.5px] text-text-secondary">{row.nihss}</td>
                    <td className="px-2.5 py-3.5">
                      <span
                        className={`rounded-full px-2.5 py-1 text-[12px] font-semibold ${STATUS_STYLES[row.tone]}`}
                      >
                        {row.statusLabel}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </motion.div>

        <motion.div
          variants={fadeUp}
          transition={{ duration: 0.4, ease: 'easeOut' }}
          className="flex flex-col gap-3.5 rounded-2xl border border-border bg-surface p-[22px] shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
        >
          <div className="flex items-center justify-between">
            <h2 className="text-[16px] font-semibold text-text">Pending Assessments</h2>
            <span className="rounded-full bg-critical-bg px-2.5 py-1 text-[11.5px] font-bold text-critical">
              {view.urgentCount} urgent
            </span>
          </div>
          <div className="flex flex-col gap-2.5">
            {view.pendingItems.length === 0 && !loading && (
              <p className="px-1 text-[13px] text-text-muted">Nothing pending — all active admissions are caught up.</p>
            )}
            {view.pendingItems.map((item, i) => {
              const tone = item.minutesSince > 30 ? ASSESSMENT_TONE_STYLES.critical : ASSESSMENT_TONE_STYLES.warning
              return (
                <Link
                  key={`${item.admissionId}-${item.label}-${i}`}
                  to={`/report/${item.admissionId}`}
                  className={`flex items-center justify-between gap-2.5 rounded-[10px] px-3.5 py-3 ${tone.bg}`}
                >
                  <div className="flex flex-col gap-0.5">
                    <span className="text-[13.5px] font-semibold text-text">{item.patientName}</span>
                    <span className="text-[12px] text-text-secondary">{item.label}</span>
                  </div>
                  <span className={`whitespace-nowrap text-[12px] font-bold ${tone.text}`}>
                    {Math.round(item.minutesSince)} min since admission
                  </span>
                </Link>
              )
            })}
          </div>
        </motion.div>
      </div>

      <motion.div
        variants={fadeUp}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        className="flex flex-col gap-4 rounded-2xl border border-border bg-surface p-6 shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[16px] font-semibold text-text">Wards & Beds Occupancy</h2>
          <Link to="/wards" className="text-[13px] font-semibold text-accent hover:underline">
            Manage wards
          </Link>
        </div>
        <div className="grid grid-cols-2 gap-3.5 sm:grid-cols-3 xl:grid-cols-6">
          {view.wardOccupancy.length === 0 && !loading && (
            <p className="col-span-full text-[13px] text-text-muted">No wards configured yet.</p>
          )}
          {view.wardOccupancy.map((ward) => (
            <div key={ward.name} className="flex flex-col gap-2.5 rounded-xl border border-border p-4">
              <div className="flex items-baseline justify-between">
                <span className="text-[13.5px] font-semibold text-text">{ward.name}</span>
                <span className="text-[12px] text-text-muted">
                  {ward.occupied}/{ward.total}
                </span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-border-soft">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${ward.pct}%` }}
                  transition={{ duration: 0.7, ease: 'easeOut', delay: 0.15 }}
                  className={`h-full rounded-full ${occupancyColor(ward.pct)}`}
                />
              </div>
              <span className="text-[11.5px] text-text-muted">{ward.pct}% occupied</span>
            </div>
          ))}
        </div>
      </motion.div>
    </motion.div>
  )
}
