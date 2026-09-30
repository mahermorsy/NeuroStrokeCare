import { api } from '@/lib/api'
import { entityApi } from '@/lib/entityApi'
import type {
  AdmissionResponse,
  PatientResponse,
  BedResponse,
  WardResponse,
  AdmissionTimelineEntryResponse,
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

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')
const bedsApi = entityApi<BedResponse>('Bed')
const wardsApi = entityApi<WardResponse>('Ward')
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

function getTimeline(admissionId: string) {
  return api.get<AdmissionTimelineEntryResponse[]>(`/Admission/${admissionId}/timeline`).then((r) => r.data)
}

// Every clinical record only carries an AdmissionId (no server-side filter on
// most GET-all endpoints), so — same as every other page in this app — we
// fetch each full list and filter to this one admission client-side.
function byAdmission<T extends { admissionId: string }>(rows: T[], admissionId: string) {
  return rows.filter((r) => r.admissionId === admissionId)
}

export async function loadAdmissionReport(admissionId: string) {
  const [
    admission,
    beds,
    wards,
    timeline,
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
    admissionsApi.getById(admissionId),
    bedsApi.list(),
    wardsApi.list(),
    getTimeline(admissionId),
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

  const patient = await patientsApi.getById(admission.patientId)

  const bed = admission.bedId ? (beds.find((b) => b.id === admission.bedId) ?? null) : null
  const ward = bed ? (wards.find((w) => w.id === bed.wardId) ?? null) : null

  return {
    admission,
    patient,
    bed,
    ward,
    timeline,
    labResults: byAdmission(labResults, admissionId),
    doorTimings: byAdmission(doorTimings, admissionId),
    followUpNotes: byAdmission(followUpNotes, admissionId),
    assessments: {
      nihss: byAdmission(nihss, admissionId),
      aspects: byAdmission(aspects, admissionId),
      ich: byAdmission(ich, admissionId),
      canadianTia: byAdmission(canadianTia, admissionId),
      braden: byAdmission(braden, admissionId),
      gcs: byAdmission(gcs, admissionId),
      guss: byAdmission(guss, admissionId),
      morse: byAdmission(morse, admissionId),
    },
  }
}

export type AdmissionReport = Awaited<ReturnType<typeof loadAdmissionReport>>
