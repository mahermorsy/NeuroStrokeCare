import type { AdmissionResponse } from '@/types/entities'

/**
 * The single definition of "this admission is still open" (i.e. the patient
 * hasn't been discharged or transferred out yet). Mirrors the backend's own
 * definition in AdmissionController (DischargeTime == null) so the frontend
 * and backend never disagree about what counts as an active admission.
 *
 * Previously this `!a.dischargeTime` check was duplicated independently in
 * Patients.tsx, Dashboard.tsx, and DoorTiming.tsx — now they all import this.
 */
export function isOpenAdmission(admission: Pick<AdmissionResponse, 'dischargeTime'>): boolean {
  return admission.dischargeTime == null
}
