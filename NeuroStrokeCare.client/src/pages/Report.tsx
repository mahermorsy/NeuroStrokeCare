import { useEffect, useState } from 'react'
import { useParams, Link, useNavigate } from 'react-router-dom'
import { motion } from 'framer-motion'
import { loadAdmissionReport, type AdmissionReport } from '@/lib/reportApi'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import AdmissionPicker from '@/components/AdmissionPicker'
import { usePatientSelection } from '@/context/PatientSelectionContext'
import { describeApiError } from '@/lib/apiError'
import { maskNationalId } from '@/lib/patients'
import {
  PATIENT_STATUS,
  STROKE_TYPE,
  BRADEN_RISK_LEVEL,
  GCS_SEVERITY,
  GUSS_SEVERITY,
  MORSE_RISK_LEVEL,
} from '@/lib/enums'

function age(dateOfBirth: string) {
  const dob = new Date(dateOfBirth)
  if (Number.isNaN(dob.getTime())) return '—'
  const diff = Date.now() - dob.getTime()
  return Math.max(0, Math.floor(diff / (365.25 * 24 * 3600 * 1000)))
}

function fmt(dt: string | null | undefined) {
  return dt ? new Date(dt).toLocaleString() : '—'
}

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-2.5 border-t border-border pt-4 first:border-t-0 first:pt-0">
      <h2 className="text-[13.5px] font-semibold uppercase tracking-wide text-text-muted">{title}</h2>
      {children}
    </section>
  )
}

function Row({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between gap-4 text-[13.5px]">
      <span className="text-text-muted">{label}</span>
      <span className="text-right font-medium text-text">{value}</span>
    </div>
  )
}

// Simple named-type + formatted-result rows for whichever of the 8 assessment
// types actually have an entry on this admission — kept generic on purpose
// since each type's DTO shape differs (Assessments.tsx does the same cast).
function assessmentRows(report: AdmissionReport) {
  const rows: { type: string; when: string; result: string }[] = []
  const { assessments } = report

  assessments.nihss.forEach((r) => rows.push({ type: 'NIHSS', when: r.assessedAt, result: `${r.totalScore} (${r.severity})` }))
  assessments.aspects.forEach((r) => rows.push({ type: 'ASPECTS', when: r.assessedAt, result: `${r.totalScore} / 10` }))
  assessments.ich.forEach((r) => rows.push({ type: 'ICH Score', when: r.assessedAt, result: String(r.totalScore) }))
  assessments.canadianTia.forEach((r) => rows.push({ type: 'Canadian TIA', when: r.assessedAt, result: r.riskLevel }))
  assessments.braden.forEach((r) =>
    rows.push({ type: 'Braden Scale', when: r.assessedAt, result: BRADEN_RISK_LEVEL[r.riskLevel] ?? String(r.riskLevel) }),
  )
  assessments.gcs.forEach((r) =>
    rows.push({ type: 'GCS', when: r.assessedAt, result: `${r.totalScore} (${GCS_SEVERITY[r.severity] ?? r.severity})` }),
  )
  assessments.guss.forEach((r) =>
    rows.push({ type: 'GUSS (Swallow)', when: r.assessedAt, result: GUSS_SEVERITY[r.severity] ?? String(r.severity) }),
  )
  assessments.morse.forEach((r) =>
    rows.push({ type: 'Morse Fall Scale', when: r.assessedAt, result: MORSE_RISK_LEVEL[r.riskLevel] ?? String(r.riskLevel) }),
  )

  return rows.sort((a, b) => new Date(a.when).getTime() - new Date(b.when).getTime())
}

export default function Report() {
  // PHASE 11 (area 5, 12): "Report" is also what the spec calls "Timeline" - this page already
  // renders the timeline (see getTimeline() in reportApi.ts) rather than that being a separate
  // route (confirmed by grep - there is no standalone Timeline page in this codebase). Falls
  // back to the shared Patient Selection Context when the route didn't name an admission, and
  // to a plain AdmissionPicker when neither is available - same pattern as PatientSummary.tsx.
  const { admissionId: routeAdmissionId } = useParams<{ admissionId?: string }>()
  const navigate = useNavigate()
  const { admissionId: contextAdmissionId, select } = usePatientSelection()
  const admissionId = routeAdmissionId ?? contextAdmissionId ?? undefined
  const [report, setReport] = useState<AdmissionReport | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!admissionId) return
    setReport(null)
    setError(null)
    loadAdmissionReport(admissionId)
      .then(setReport)
      .catch((err) =>
        setError(
          describeApiError(err, {
            404: 'This admission could not be found. It may have been removed.',
            500: 'Could not load this admission’s report. Please try again in a moment.',
          }),
        ),
      )
  }, [admissionId])

  // Reached directly via /report/:admissionId (e.g. from Admissions.tsx) rather than through
  // the Patient Selection Context - establish it as the shared context too, same reasoning as
  // PatientSummary.tsx, so Assessments/Lab Results/Door Timing/Follow-up pick it up from here.
  useEffect(() => {
    if (!routeAdmissionId || !report) return
    select({
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
    })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [routeAdmissionId, report])

  if (!admissionId) {
    return (
      <div className="flex flex-col gap-4">
        <PageHeader title="Patient report" subtitle="Search for a patient or admission to view their report." />
        <Card>
          {/* FINAL RELEASE-CANDIDATE PASS (section 11, "Search review"): CONFIRMED BUG, FIXED.
              AdmissionController.Search's own comment claims "Historical/read-only callers
              (e.g. Report, Timeline) pass openOnly=false to also reach discharged admissions" -
              but this call was relying on AdmissionPicker's default (openOnly=true), so a
              report for an already-discharged admission could never be found through this
              picker at all, contradicting that documented intent. Explicit openOnly={false}
              closes the gap; write-workflow pickers elsewhere (Assessments/LabResults/
              DoorTiming/FollowUp) are intentionally left on the default (true). */}
          <AdmissionPicker
            value=""
            openOnly={false}
            onSelect={(id, result) => {
              if (result) select(result)
              if (id) navigate(`/report/${id}`)
            }}
          />
        </Card>
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col gap-4">
        <p className="rounded-lg bg-critical-bg px-3 py-2.5 text-[13.5px] font-medium text-critical">{error}</p>
        <Link to="/admissions" className="text-[13.5px] font-medium text-accent hover:underline">
          Back to admissions
        </Link>
      </div>
    )
  }

  if (!report) {
    return <p className="text-[13.5px] text-text-secondary">Loading report…</p>
  }

  const { admission, patient, bed, ward, timeline, labResults, doorTimings, followUpNotes } = report
  const statusLabel = PATIENT_STATUS[admission.status as keyof typeof PATIENT_STATUS] ?? 'Unknown'
  const strokeLabel = admission.strokeType ? STROKE_TYPE[admission.strokeType as keyof typeof STROKE_TYPE] : null
  const rows = assessmentRows(report)

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <div className="print:hidden">
        <PageHeader
          title="Patient report"
          subtitle={`${patient.firstName} ${patient.lastName} — admitted ${fmt(admission.admissionTime)}`}
          action={<PrimaryButton onClick={() => window.print()}>Export PDF</PrimaryButton>}
        />
      </div>

      <Card className="flex flex-col gap-5 print:border-none print:p-0 print:shadow-none">
        <div className="hidden print:block">
          <img
            src="/brand/NeuroStrokeCare_Logo.svg"
            alt="NeuroStrokeCare — Mansoura University Hospital"
            className="mb-2 h-auto w-full max-w-[260px] rounded-lg object-contain"
          />
          <h1 className="text-[20px] font-semibold text-text">Patient Report</h1>
          <p className="text-[12.5px] text-text-muted">generated {new Date().toLocaleString()}</p>
        </div>

        <Section title="Patient">
          <Row label="Name" value={`${patient.firstName} ${patient.middleName} ${patient.lastName}`} />
          <Row label="Hospital number" value={patient.hospitalNumber ?? 'Not assigned'} />
          {/* PHASE 11 (area 14): a formal clinical report is explicitly allowed to show the
              unmasked National ID - preserved as-is, do not mask this one. */}
          <Row label="National ID" value={patient.nationalId ?? '—'} />
          <Row label="Age / Gender" value={`${age(patient.dateOfBirth)} / ${patient.gender}`} />
          <Row label="Weight" value={`${patient.weightKg} kg`} />
          <Row label="Chief complaint" value={patient.chiefComplaint} />
        </Section>

        <Section title="Admission">
          <Row label="Admission time" value={fmt(admission.admissionTime)} />
          <Row label="Current status" value={statusLabel} />
          <Row label="Stroke type" value={strokeLabel ?? 'Not yet determined'} />
          <Row label="Ward / Bed" value={bed ? `${ward?.name ?? 'Ward'} / ${bed.bedNumber}` : 'Not assigned'} />
          <Row
            label="Imaging"
            value={[admission.ctDone && 'CT', admission.mriDone && 'MRI', admission.ctaDone && 'CTA'].filter(Boolean).join(', ') || '—'}
          />
          {admission.ctFindings && <Row label="CT findings" value={admission.ctFindings} />}
          {admission.mriFindings && <Row label="MRI findings" value={admission.mriFindings} />}
          {admission.ctaFindings && <Row label="CTA findings" value={admission.ctaFindings} />}
          {admission.dischargeTime && <Row label="Discharge time" value={fmt(admission.dischargeTime)} />}
          {admission.dischargeNotes && <Row label="Discharge notes" value={admission.dischargeNotes} />}
        </Section>

        {doorTimings.length > 0 && (
          <Section title="Door timing (ER arrival → treatment)">
            {doorTimings.map((d) => (
              <div key={d.id} className="flex flex-col gap-1 text-[13.5px]">
                <Row label="ER arrival" value={fmt(d.er_StrokeArrival)} />
                <Row
                  label="Door-to-CT"
                  value={d.doorToCT ? `${fmt(d.doorToCT)} (${Math.round(d.minutesToCT ?? 0)} min)` : '—'}
                />
                <Row
                  label="Door-to-needle"
                  value={d.doorToNeedle ? `${fmt(d.doorToNeedle)} (${Math.round(d.minutesToNeedle ?? 0)} min)` : '—'}
                />
                <Row
                  label="Door-to-groin"
                  value={d.doorToGroin ? `${fmt(d.doorToGroin)} (${Math.round(d.minutesToGroin ?? 0)} min)` : '—'}
                />
              </div>
            ))}
          </Section>
        )}

        {timeline.length > 0 && (
          <Section title="Patient journey">
            <table className="w-full text-left text-[13px]">
              <thead>
                <tr className="text-text-muted">
                  <th className="pb-1.5 font-medium">Status</th>
                  <th className="pb-1.5 font-medium">Time</th>
                  <th className="pb-1.5 font-medium">Since arrival</th>
                </tr>
              </thead>
              <tbody>
                {timeline.map((t, i) => (
                  <tr key={i} className="border-t border-border-soft">
                    <td className="py-1.5 font-medium text-text">{t.statusLabel}</td>
                    <td className="py-1.5 text-text-secondary">{fmt(t.timestamp)}</td>
                    <td className="py-1.5 text-text-secondary">{t.minutesSinceArrival} min</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>
        )}

        {rows.length > 0 && (
          <Section title="Assessments">
            <table className="w-full text-left text-[13px]">
              <thead>
                <tr className="text-text-muted">
                  <th className="pb-1.5 font-medium">Type</th>
                  <th className="pb-1.5 font-medium">Assessed at</th>
                  <th className="pb-1.5 font-medium">Result</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r, i) => (
                  <tr key={i} className="border-t border-border-soft">
                    <td className="py-1.5 font-medium text-text">{r.type}</td>
                    <td className="py-1.5 text-text-secondary">{fmt(r.when)}</td>
                    <td className="py-1.5 text-text-secondary">{r.result}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>
        )}

        {labResults.length > 0 && (
          <Section title="Lab results">
            {labResults.map((l) => (
              <div key={l.id} className="mb-2 text-[13px] text-text-secondary">
                <span className="font-medium text-text">{fmt(l.recordedAt)}</span> — Glucose {l.glucoseMmol ?? '—'},
                INR {l.inr ?? '—'}, Platelets {l.platelets ?? '—'}
                {(l.inrAlert || l.glucoseAlert || l.plateletsAlert) && (
                  <span className="ml-1.5 font-semibold text-critical">(alert flagged)</span>
                )}
              </div>
            ))}
          </Section>
        )}

        {followUpNotes.length > 0 && (
          <Section title="Follow-up notes">
            {followUpNotes.map((n) => (
              <div key={n.id} className="mb-2 text-[13px]">
                <span className="font-medium text-text">
                  {fmt(n.createdAt)} — {n.authorRole ?? 'Staff'}
                  {n.noteType === 2 ? ' (new finding)' : ''}:
                </span>{' '}
                <span className="text-text-secondary">{n.content}</span>
              </div>
            ))}
          </Section>
        )}
      </Card>
    </motion.div>
  )
}
