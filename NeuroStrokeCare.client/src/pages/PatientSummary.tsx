import { useEffect, useMemo, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { loadAdmissionReport, type AdmissionReport } from '@/lib/reportApi'
import { usePatientSelection } from '@/context/PatientSelectionContext'
import { maskNationalId } from '@/lib/patients'
import { describeApiError } from '@/lib/apiError'
import PageHeader, { Card } from '@/components/PageHeader'
import AdmissionPicker from '@/components/AdmissionPicker'
import StatusPill from '@/components/StatusPill'
import { PATIENT_STATUS, STROKE_TYPE } from '@/lib/enums'
import type { AdmissionSearchResultResponse } from '@/types/entities'

function age(dateOfBirth: string) {
  const dob = new Date(dateOfBirth)
  if (Number.isNaN(dob.getTime())) return '—'
  const diff = Date.now() - dob.getTime()
  return Math.max(0, Math.floor(diff / (365.25 * 24 * 3600 * 1000)))
}

function fmt(dt: string | null | undefined) {
  return dt ? new Date(dt).toLocaleString() : '—'
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-4 text-[13.5px]">
      <span className="text-text-muted">{label}</span>
      <span className="text-right font-medium text-text">{value}</span>
    </div>
  )
}

// One row per assessment TYPE that has at least one entry, showing only the most recent —
// a "latest assessment summary" per PHASE 11 area 6, not the full history (Report.tsx already
// shows every entry for that).
function latestAssessmentRows(report: AdmissionReport) {
  const rows: { type: string; when: string; result: string }[] = []
  const pick = <T extends { assessedAt: string }>(list: T[]) =>
    list.length ? list.reduce((a, b) => (new Date(a.assessedAt) > new Date(b.assessedAt) ? a : b)) : null

  const nihss = pick(report.assessments.nihss)
  if (nihss) rows.push({ type: 'NIHSS', when: nihss.assessedAt, result: `${nihss.totalScore} (${nihss.severity})` })

  const aspects = pick(report.assessments.aspects)
  if (aspects) rows.push({ type: 'ASPECTS', when: aspects.assessedAt, result: `${aspects.totalScore} / 10` })

  const ich = pick(report.assessments.ich)
  if (ich) rows.push({ type: 'ICH Score', when: ich.assessedAt, result: String(ich.totalScore) })

  const canadianTia = pick(report.assessments.canadianTia)
  if (canadianTia) rows.push({ type: 'Canadian TIA', when: canadianTia.assessedAt, result: canadianTia.riskLevel })

  const gcs = pick(report.assessments.gcs)
  if (gcs) rows.push({ type: 'GCS', when: gcs.assessedAt, result: String(gcs.totalScore) })

  const braden = pick(report.assessments.braden)
  if (braden) rows.push({ type: 'Braden Scale', when: braden.assessedAt, result: String(braden.totalScore) })

  const guss = pick(report.assessments.guss)
  if (guss) rows.push({ type: 'GUSS', when: guss.assessedAt, result: String(guss.totalScore) })

  const morse = pick(report.assessments.morse)
  if (morse) rows.push({ type: 'Morse Fall Scale', when: morse.assessedAt, result: String(morse.totalScore) })

  return rows.sort((a, b) => new Date(b.when).getTime() - new Date(a.when).getTime())
}

/**
 * PHASE 11 (area 6): a compact clinical overview — demographics, HospitalNumber, masked
 * National ID, current admission/ward/bed, stroke type, thrombolysis status, the latest
 * assessment/labs summary, and current Stroke Code status, with links out to the detailed
 * modules. Deliberately NOT a new dashboard: it reuses `loadAdmissionReport` (the exact same
 * aggregation Report.tsx already uses) rather than re-implementing any backend business logic
 * — this page only decides what to *show* from data the backend already computed.
 *
 * Context-aware (area 5): with no :admissionId route param, it falls back to the shared
 * Patient Selection Context; with neither, it shows an AdmissionPicker. Visiting a specific
 * admission directly (e.g. from a Patients.tsx row action) also establishes that admission as
 * the shared context, so the rest of the context-aware pages pick it up without re-selecting.
 */
export default function PatientSummary() {
  const { admissionId: routeAdmissionId } = useParams<{ admissionId?: string }>()
  const navigate = useNavigate()
  const { admissionId: contextAdmissionId, snapshot: contextSnapshot, select } = usePatientSelection()
  const admissionId = routeAdmissionId ?? contextAdmissionId

  const [report, setReport] = useState<AdmissionReport | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!admissionId) {
      setReport(null)
      return
    }
    let cancelled = false
    setLoading(true)
    setError(null)
    loadAdmissionReport(admissionId)
      .then((r) => {
        if (!cancelled) setReport(r)
      })
      .catch((err) => {
        if (!cancelled) setError(describeApiError(err, { 404: 'That admission could not be found.' }))
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [admissionId])

  // Visiting /patient-summary/:admissionId directly establishes it as the shared context too,
  // so Assessments/Lab Results/Door Timing/Follow-up/Report can pick it up without re-selecting
  // (area 3). Only runs when the route explicitly named an admission, and only once that
  // admission's own report has loaded (so the snapshot we store is accurate, not guessed).
  useEffect(() => {
    if (!routeAdmissionId || !report) return
    if (contextSnapshot?.admissionId === routeAdmissionId) return
    const snapshot: AdmissionSearchResultResponse = {
      admissionId: report.admission.id,
      patientId: report.patient.id,
      patientName: `${report.patient.firstName} ${report.patient.lastName}`,
      hospitalNumber: report.patient.hospitalNumber,
      nationalIdMasked: report.patient.nationalId ? maskNationalId(report.patient.nationalId) : null,
      status: report.admission.status,
      isOpen: report.admission.dischargeTime == null,
      wardCode: report.ward?.code ?? null,
      wardName: report.ward?.name ?? null,
      bedNumber: report.bed?.bedNumber ?? null,
      admissionTime: report.admission.admissionTime,
    }
    select(snapshot)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [routeAdmissionId, report])

  const latestAssessments = useMemo(() => (report ? latestAssessmentRows(report) : []), [report])
  const latestLab = useMemo(() => {
    if (!report || report.labResults.length === 0) return null
    return report.labResults.reduce((a, b) => (new Date(a.recordedAt) > new Date(b.recordedAt) ? a : b))
  }, [report])
  const strokeCodeActive = Boolean(report && report.doorTimings.length > 0)

  if (!admissionId) {
    return (
      <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
        <PageHeader title="Patient summary" subtitle="Search for a patient or admission to view their summary." />
        <Card>
          <AdmissionPicker
            value=""
            onSelect={(id, result) => {
              if (result) select(result)
              if (id) navigate(`/patient-summary/${id}`)
            }}
          />
        </Card>
      </motion.div>
    )
  }

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title={report ? `${report.patient.firstName} ${report.patient.lastName}` : 'Patient summary'}
        subtitle={report?.patient.hospitalNumber ? `Hospital No. ${report.patient.hospitalNumber}` : 'No hospital number assigned'}
      />

      {loading && <Card>Loading…</Card>}
      {!loading && error && <Card className="text-critical">{error}</Card>}

      {!loading && !error && report && (
        <div className="grid grid-cols-1 gap-5 lg:grid-cols-2">
          <Card className="flex flex-col gap-2.5">
            <h2 className="text-[13.5px] font-semibold uppercase tracking-wide text-text-muted">Demographics</h2>
            <Row label="Full name" value={`${report.patient.firstName} ${report.patient.lastName}`} />
            <Row label="Hospital number" value={report.patient.hospitalNumber ?? 'Not assigned'} />
            <Row
              label="National ID"
              value={report.patient.nationalId ? maskNationalId(report.patient.nationalId) : '—'}
            />
            <Row label="Age / sex" value={`${age(report.patient.dateOfBirth)}y, ${report.patient.gender}`} />
            <Row label="Weight" value={`${report.patient.weightKg} kg`} />
            <Row label="Chief complaint" value={report.patient.chiefComplaint || '—'} />
          </Card>

          <Card className="flex flex-col gap-2.5">
            <h2 className="text-[13.5px] font-semibold uppercase tracking-wide text-text-muted">Current admission</h2>
            <Row label="Admitted" value={fmt(report.admission.admissionTime)} />
            <Row
              label="Status"
              value={
                <StatusPill
                  label={PATIENT_STATUS[report.admission.status as keyof typeof PATIENT_STATUS] ?? 'Unknown'}
                  tone={report.admission.dischargeTime ? 'neutral' : 'info'}
                />
              }
            />
            <Row
              label="Ward / bed"
              value={report.ward && report.bed ? `${report.ward.name} — Bed ${report.bed.bedNumber}` : 'Not assigned'}
            />
            <Row
              label="Stroke type"
              value={
                report.admission.strokeType != null
                  ? STROKE_TYPE[report.admission.strokeType as keyof typeof STROKE_TYPE] ?? '—'
                  : 'Not yet classified'
              }
            />
            <Row
              label="Thrombolysis"
              value={
                report.admission.thrombolysisGivenAt
                  ? `${report.admission.thrombolysisDrug ?? 'Given'} at ${fmt(report.admission.thrombolysisGivenAt)}`
                  : 'Not given'
              }
            />
            <Row
              label="Stroke code"
              value={
                strokeCodeActive ? (
                  <StatusPill label="Active" tone="critical" />
                ) : (
                  <StatusPill label="Not active" tone="neutral" />
                )
              }
            />
          </Card>

          <Card className="flex flex-col gap-2.5">
            <h2 className="text-[13.5px] font-semibold uppercase tracking-wide text-text-muted">
              Latest assessments
            </h2>
            {latestAssessments.length === 0 && <p className="text-[13px] text-text-muted">No assessments recorded yet.</p>}
            {latestAssessments.map((row) => (
              <Row key={row.type} label={row.type} value={`${row.result} — ${fmt(row.when)}`} />
            ))}
          </Card>

          <Card className="flex flex-col gap-2.5">
            <h2 className="text-[13.5px] font-semibold uppercase tracking-wide text-text-muted">Latest labs</h2>
            {!latestLab && <p className="text-[13px] text-text-muted">No lab results recorded yet.</p>}
            {latestLab && (
              <>
                <Row label="Recorded" value={fmt(latestLab.recordedAt)} />
                <Row label="Glucose" value={latestLab.glucoseMmol != null ? `${latestLab.glucoseMmol} mmol/L` : '—'} />
                <Row label="INR" value={latestLab.inr ?? '—'} />
                <Row label="Platelets" value={latestLab.platelets ?? '—'} />
              </>
            )}
          </Card>

          <Card className="flex flex-wrap gap-3 lg:col-span-2">
            <Link to="/assessments" className="rounded-lg border border-border px-3.5 py-2 text-[13px] font-medium text-accent hover:bg-accent/5">
              Open Assessments
            </Link>
            <Link to="/lab-results" className="rounded-lg border border-border px-3.5 py-2 text-[13px] font-medium text-accent hover:bg-accent/5">
              Open Lab Results
            </Link>
            <Link to="/door-timing" className="rounded-lg border border-border px-3.5 py-2 text-[13px] font-medium text-accent hover:bg-accent/5">
              Open Door Timing
            </Link>
            <Link to="/follow-up" className="rounded-lg border border-border px-3.5 py-2 text-[13px] font-medium text-accent hover:bg-accent/5">
              Open Follow-up notes
            </Link>
            <Link
              to={`/report/${admissionId}`}
              className="rounded-lg border border-border px-3.5 py-2 text-[13px] font-medium text-accent hover:bg-accent/5"
            >
              Open full report / timeline
            </Link>
          </Card>
        </div>
      )}
    </motion.div>
  )
}
