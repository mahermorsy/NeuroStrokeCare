// Mirrors the API's *Response DTOs field-for-field (camelCase, as ASP.NET
// Core's default JSON serialization sends them). Keep these in sync with the
// C# classes under NeuroStrokeCare.Core/Features/*/Dtos.

export interface WardResponse {
  id: string
  code: string
  name: string
  totalBeds: number
}

export interface BedResponse {
  id: string
  wardId: string
  bedNumber: string
  status: number // BedStatus enum -> serialized as its numeric value
}

export interface PatientResponse {
  id: string
  nationalId: string | null
  // PHASE 11 (area 1): the real hospital/medical-record identifier - independent from
  // NationalId, unique when present, null until an Admin/clinical user assigns one.
  hospitalNumber: string | null
  firstName: string
  middleName: string
  lastName: string
  dateOfBirth: string
  gender: string // plain string on the entity ("Male" / "Female"), not an enum
  weightKg: number
  chiefComplaint: string
}

export interface AdmissionResponse {
  id: string
  patientId: string
  patientName: string
  patientHospitalNumber: string | null
  admissionTime: string
  status: number
  strokeType: number | null
  strokeTypeSetAt: string | null
  bedId: string | null
  dischargeTime: string | null
  dischargeNotes: string | null
  admittedById: string
  strokeTypeSetById: string | null
  ctDone: boolean
  mriDone: boolean
  ctaDone: boolean
  ctFindings: string | null
  mriFindings: string | null
  ctaFindings: string | null
  thrombolysisGivenAt: string | null
  thrombolysisDrug: string | null
  thrombolysisDoseMg: number | null
  thrombolysisRecordedById: string | null
}

export interface NIHSSAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  totalScore: number
  severity: string
}

export interface ASPECTSAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  totalScore: number
}

export interface ICHAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  gcsScore: number
  ichVolumeMl: number
  infratentorialOrigin: boolean
  ivhPresent: boolean
  ageScore: number
  totalScore: number
}

export interface CanadianTIAAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  totalScore: number
  riskLevel: string
}

export interface BradenAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  totalScore: number
  riskLevel: number
}

export interface GCSAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  eyeResponse: number
  verbalResponse: number
  motorResponse: number
  totalScore: number
  severity: number
}

export interface GUSSAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  indirectScore: number
  totalScore: number
  severity: number
}

export interface MorseAssessmentResponse {
  id: string
  admissionId: string
  assessedById: string
  assessedAt: string
  totalScore: number
  riskLevel: number
}

export interface LabResultsResponse {
  id: string
  admissionId: string
  recordedById: string
  recordedAt: string
  glucoseMmol: number | null
  inr: number | null
  pt: number | null
  platelets: number | null
  sodium: number | null
  potassium: number | null
  creatinine: number | null
  hemoglobin: number | null
  ldl: number | null
  hbA1c: number | null
  aPTT: number | null
  alt: number | null
  ast: number | null
  ecgAtrialFibrillation: boolean | null
  inrAlert: boolean
  glucoseAlert: boolean
  plateletsAlert: boolean
}

export interface UserSummaryResponse {
  id: string
  userName: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  role: string
  isRootSuperAdmin: boolean
  createdAt: string
  employeeId: string | null
  profilePhotoUrl: string | null
  profession: string | null
  jobTitle: string | null
  academicDegree: string | null
  department: string | null
}

export interface AdmissionSearchResultResponse {
  admissionId: string
  patientId: string
  patientName: string
  hospitalNumber: string | null
  nationalIdMasked: string | null
  status: number
  isOpen: boolean
  wardCode: string | null
  wardName: string | null
  bedNumber: string | null
  admissionTime: string
}

export interface AdmissionPatientSearchResultResponse {
  patientId: string
  fullName: string
  hospitalNumber: string | null
  nationalIdMasked: string | null
}

export interface PendingUserResponse {
  id: string
  userName: string
  email: string
  firstName: string
  lastName: string
  phoneNumber: string | null
  requestedRole: string | null
  createdAt: string
}

export interface AdmissionTimelineEntryResponse {
  timestamp: string
  statusLabel: string
  status: number | null
  changedById: string | null
  minutesSincePrevious: number | null
  minutesSinceArrival: number
}

export interface FollowUpNoteResponse {
  id: string
  admissionId: string
  content: string
  noteType: number // FollowUpNoteType: 1=General, 2=NewFinding
  authorRole: string | null
  createdBy: string
  createdAt: string
}

export interface DoorTimingResponse {
  id: string
  admissionId: string
  er_StrokeArrival: string
  doorToCT: string | null
  doorToNeedle: string | null
  doorToGroin: string | null
  minutesToCT: number | null
  minutesToNeedle: number | null
  minutesToGroin: number | null
  ctDelayed: boolean
  needleDelayed: boolean
}
