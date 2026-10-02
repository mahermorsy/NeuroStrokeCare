import { entityApi } from '@/lib/entityApi'
import type {
  AdmissionResponse,
  PatientResponse,
  LabResultsResponse,
  DoorTimingResponse,
  FollowUpNoteResponse,
  NIHSSAssessmentResponse,
  ASPECTSAssessmentResponse,
  ICHAssessmentResponse,
  CanadianTIAAssessmentResponse,
  BradenAssessmentResponse,
  GCSAssessmentResponse,
  GUSSAssessmentResponse,
  MorseAssessmentResponse,
} from '@/types/entities'
import { BRADEN_RISK_LEVEL, GCS_SEVERITY, GUSS_SEVERITY, MORSE_RISK_LEVEL, riskTone } from '@/lib/enums'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')
const labResultsApi = entityApi<LabResultsResponse>('LabResults')
const doorTimingApi = entityApi<DoorTimingResponse>('DoorTiming')
const followUpNotesApi = entityApi<FollowUpNoteResponse>('FollowUpNote')
const nihssApi = entityApi<NIHSSAssessmentResponse>('NIHSSAssessment')
const aspectsApi = entityApi<ASPECTSAssessmentResponse>('ASPECTSAssessment')
const ichApi = entityApi<ICHAssessmentResponse>('ICHAssessment')
const canadianTiaApi = entityApi<CanadianTIAAssessmentResponse>('CanadianTIAAssessment')
const bradenApi = entityApi<BradenAssessmentResponse>('BradenAssessment')
const gcsApi = entityApi<GCSAssessmentResponse>('GCSAssessment')
const gussApi = entityApi<GUSSAssessmentResponse>('GUSSAssessment')
const morseApi = entityApi<MorseAssessmentResponse>('MorseAssessment')

export interface AlertItem {
  id: string
  severity: 'critical' | 'warning'
  category: string
  patientName: string
  admissionId: string
  message: string
  when: string
}

// Every threshold here mirrors a rule that already exists elsewhere (the
// LabResults/DoorTiming alert flags are computed server-side; the assessment
// thresholds are the same ones Assessments.tsx uses to color each score) —
// this page's only job is to pull all of those already-true alerts into one
// place instead of leaving them scattered across eight tabs and three pages.
export async function loadAlerts(): Promise<AlertItem[]> {
  const [
    admissions,
    patients,
    labResults,
    doorTimings,
    followUpNotes,
    nihss,
    aspects,
    ich,
    canadianTia,
    braden,
    gcs,
    guss,
    morse,
  ] = await Promise.all([
    admissionsApi.list(),
    patientsApi.list(),
    labResultsApi.list(),
    doorTimingApi.list(),
    followUpNotesApi.list(),
    nihssApi.list(),
    aspectsApi.list(),
    ichApi.list(),
    canadianTiaApi.list(),
    bradenApi.list(),
    gcsApi.list(),
    gussApi.list(),
    morseApi.list(),
  ])

  const patientNameById = new Map(patients.map((p) => [p.id, `${p.firstName} ${p.lastName}`]))
  const admissionById = new Map(admissions.map((a) => [a.id, a]))
  const nameFor = (admissionId: string) => {
    const a = admissionById.get(admissionId)
    return a ? (patientNameById.get(a.patientId) ?? 'Unknown patient') : 'Unknown patient'
  }

  const alerts: AlertItem[] = []

  labResults.forEach((l) => {
    const flags = [l.inrAlert && 'INR', l.glucoseAlert && 'Glucose', l.plateletsAlert && 'Platelets'].filter(
      Boolean,
    ) as string[]
    if (flags.length > 0) {
      alerts.push({
        id: `lab-${l.id}`,
        severity: 'critical',
        category: 'Lab result',
        patientName: nameFor(l.admissionId),
        admissionId: l.admissionId,
        message: `Out-of-range: ${flags.join(', ')}`,
        when: l.recordedAt,
      })
    }
  })

  doorTimings.forEach((d) => {
    if (d.ctDelayed) {
      alerts.push({
        id: `door-ct-${d.id}`,
        severity: 'warning',
        category: 'Door timing',
        patientName: nameFor(d.admissionId),
        admissionId: d.admissionId,
        message: `Door-to-CT delayed (${Math.round(d.minutesToCT ?? 0)} min, target ≤ 25)`,
        when: d.doorToCT ?? d.er_StrokeArrival,
      })
    }
    if (d.needleDelayed) {
      alerts.push({
        id: `door-needle-${d.id}`,
        severity: 'warning',
        category: 'Door timing',
        patientName: nameFor(d.admissionId),
        admissionId: d.admissionId,
        message: `Door-to-needle delayed (${Math.round(d.minutesToNeedle ?? 0)} min, target ≤ 60)`,
        when: d.doorToNeedle ?? d.er_StrokeArrival,
      })
    }
  })

  // Stroke Code, live: once a clinician activates a stroke code (creating the
  // DoorTiming record is the activation itself), this watches the same two
  // windows the Delayed pills check — but *before* the milestone is ever
  // recorded, while the code is still open. It only becomes an alert once a
  // window is actually missed, not the instant the code opens, so the sidebar
  // badge stays quiet unless the team is genuinely behind. Standing a code
  // down (DoorTiming.tsx "Stand down") removes it from this list immediately,
  // since closed records drop out of the active DoorTiming query entirely.
  doorTimings.forEach((d) => {
    const elapsedMin = (Date.now() - new Date(d.er_StrokeArrival).getTime()) / 60000
    if (!d.doorToCT && elapsedMin > 25) {
      alerts.push({
        id: `code-ct-${d.id}`,
        severity: 'critical',
        category: 'Stroke Code',
        patientName: nameFor(d.admissionId),
        admissionId: d.admissionId,
        message: `CT not yet done — ${Math.round(elapsedMin)} min since stroke code activation (target ≤ 25)`,
        when: d.er_StrokeArrival,
      })
    }
    if (!d.doorToNeedle && elapsedMin > 60) {
      alerts.push({
        id: `code-needle-${d.id}`,
        severity: 'critical',
        category: 'Stroke Code',
        patientName: nameFor(d.admissionId),
        admissionId: d.admissionId,
        message: `Needle decision not yet made — ${Math.round(elapsedMin)} min since stroke code activation (target ≤ 60)`,
        when: d.er_StrokeArrival,
      })
    }
  })

  nihss.forEach((r) => {
    if (r.totalScore > 15) {
      alerts.push({
        id: `nihss-${r.id}`,
        severity: 'critical',
        category: 'NIHSS',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `NIHSS ${r.totalScore} (${r.severity})`,
        when: r.assessedAt,
      })
    }
  })

  aspects.forEach((r) => {
    if (r.totalScore < 6) {
      alerts.push({
        id: `aspects-${r.id}`,
        severity: 'critical',
        category: 'ASPECTS',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `ASPECTS ${r.totalScore} / 10`,
        when: r.assessedAt,
      })
    }
  })

  ich.forEach((r) => {
    if (r.totalScore > 3) {
      alerts.push({
        id: `ich-${r.id}`,
        severity: 'critical',
        category: 'ICH Score',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `ICH score ${r.totalScore}`,
        when: r.assessedAt,
      })
    }
  })

  canadianTia.forEach((r) => {
    if (riskTone(r.riskLevel) === 'critical') {
      alerts.push({
        id: `tia-${r.id}`,
        severity: 'critical',
        category: 'Canadian TIA',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `TIA risk: ${r.riskLevel}`,
        when: r.assessedAt,
      })
    }
  })

  braden.forEach((r) => {
    const label = BRADEN_RISK_LEVEL[r.riskLevel] ?? ''
    if (riskTone(label) === 'critical') {
      alerts.push({
        id: `braden-${r.id}`,
        severity: 'warning',
        category: 'Braden (pressure injury)',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `Braden: ${label}`,
        when: r.assessedAt,
      })
    }
  })

  gcs.forEach((r) => {
    const label = GCS_SEVERITY[r.severity] ?? ''
    if (riskTone(label) === 'critical') {
      alerts.push({
        id: `gcs-${r.id}`,
        severity: 'critical',
        category: 'GCS',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `GCS ${r.totalScore} (${label})`,
        when: r.assessedAt,
      })
    }
  })

  guss.forEach((r) => {
    const label = GUSS_SEVERITY[r.severity] ?? ''
    if (riskTone(label) === 'critical') {
      alerts.push({
        id: `guss-${r.id}`,
        severity: 'warning',
        category: 'GUSS (swallow)',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `Swallow risk: ${label}`,
        when: r.assessedAt,
      })
    }
  })

  morse.forEach((r) => {
    const label = MORSE_RISK_LEVEL[r.riskLevel] ?? ''
    if (riskTone(label) === 'critical') {
      alerts.push({
        id: `morse-${r.id}`,
        severity: 'warning',
        category: 'Morse (fall risk)',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `Fall risk: ${label}`,
        when: r.assessedAt,
      })
    }
  })

  followUpNotes
    .filter((n) => n.noteType === 2)
    .forEach((n) => {
      alerts.push({
        id: `note-${n.id}`,
        severity: 'warning',
        category: 'New finding',
        patientName: nameFor(n.admissionId),
        admissionId: n.admissionId,
        message: n.content,
        when: n.createdAt,
      })
    })

  // Neuro-deterioration trend: compare each admission's own consecutive assessments
  // (sorted by when they were taken) — a rising NIHSS or a falling GCS between two
  // checks on the SAME admission is the classic early sign of clinical worsening,
  // independent of whether either single reading crossed its own critical threshold.
  function byAdmission<T extends { admissionId: string; assessedAt: string }>(rows: T[]) {
    const map = new Map<string, T[]>()
    rows.forEach((r) => {
      const list = map.get(r.admissionId) ?? []
      list.push(r)
      map.set(r.admissionId, list)
    })
    map.forEach((list) => list.sort((a, b) => new Date(a.assessedAt).getTime() - new Date(b.assessedAt).getTime()))
    return map
  }

  byAdmission(nihss).forEach((list, admissionId) => {
    for (let i = 1; i < list.length; i++) {
      const rise = list[i].totalScore - list[i - 1].totalScore
      if (rise >= 2) {
        alerts.push({
          id: `nihss-trend-${list[i].id}`,
          severity: 'critical',
          category: 'Neuro-deterioration',
          patientName: nameFor(admissionId),
          admissionId,
          message: `NIHSS rose ${rise} pts (${list[i - 1].totalScore} → ${list[i].totalScore}) since the previous assessment`,
          when: list[i].assessedAt,
        })
      }
    }
  })

  byAdmission(gcs).forEach((list, admissionId) => {
    for (let i = 1; i < list.length; i++) {
      const drop = list[i - 1].totalScore - list[i].totalScore
      if (drop >= 2) {
        alerts.push({
          id: `gcs-trend-${list[i].id}`,
          severity: 'critical',
          category: 'Neuro-deterioration',
          patientName: nameFor(admissionId),
          admissionId,
          message: `GCS dropped ${drop} pts (${list[i - 1].totalScore} → ${list[i].totalScore}) since the previous assessment`,
          when: list[i].assessedAt,
        })
      }
    }
  })

  // GUSS-fail auto NPO: failing the indirect (bedside) swallow screen — any of the
  // 5 items scored 0 — means the direct/oral stages are never attempted, and the
  // patient should be kept nil-by-mouth with strict aspiration precautions.
  guss
    .filter((r) => r.indirectScore < 5)
    .forEach((r) => {
      alerts.push({
        id: `guss-npo-${r.id}`,
        severity: 'critical',
        category: 'NPO / Aspiration precaution',
        patientName: nameFor(r.admissionId),
        admissionId: r.admissionId,
        message: `GUSS indirect screen failed (${r.indirectScore}/5) — keep NPO, strict aspiration precautions`,
        when: r.assessedAt,
      })
    })

  // 24h antithrombotic lockout: no antiplatelet/anticoagulant should be given within
  // 24h of a thrombolytic dose. Surfaces as a live warning until that window closes.
  const LOCKOUT_HOURS = 24
  admissions
    .filter((a) => a.thrombolysisGivenAt)
    .forEach((a) => {
      const givenAt = new Date(a.thrombolysisGivenAt as string)
      const unlocksAt = new Date(givenAt.getTime() + LOCKOUT_HOURS * 3600 * 1000)
      if (unlocksAt.getTime() > Date.now()) {
        alerts.push({
          id: `thrombolysis-lockout-${a.id}`,
          severity: 'warning',
          category: 'Antithrombotic lockout',
          patientName: nameFor(a.id),
          admissionId: a.id,
          message: `${a.thrombolysisDrug ?? 'Thrombolytic'} given at ${givenAt.toLocaleTimeString()} — no antiplatelet/anticoagulant until ${unlocksAt.toLocaleString()}`,
          when: a.thrombolysisGivenAt as string,
        })
      }
    })

  return alerts.sort((a, b) => new Date(b.when).getTime() - new Date(a.when).getTime())
}
