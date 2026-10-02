import { useMemo } from 'react'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import type { AdmissionResponse, PatientResponse } from '@/types/entities'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')

/**
 * Most clinical records (assessments, lab results, door timing) only carry
 * an AdmissionId — this resolves that back to a patient name for display,
 * shared by every page that needs it.
 */
export function useAdmissionContext() {
  const admissions = useEntityList(() => admissionsApi.list())
  const patients = useEntityList(() => patientsApi.list())

  const patientNameByAdmissionId = useMemo(() => {
    const patientNameById = new Map(patients.data.map((p) => [p.id, `${p.firstName} ${p.lastName}`]))
    return new Map(admissions.data.map((a) => [a.id, patientNameById.get(a.patientId) ?? 'Unknown patient']))
  }, [admissions.data, patients.data])

  // PHASE 11 (area 1): a HospitalNumber-first label for tables that previously fell back to
  // a truncated internal Guid (e.g. "Admission #A1B2C3D4") when a lookup was otherwise
  // unavailable — area 1 explicitly says not to expose the database id as a stand-in
  // identifier. null when the patient has no HospitalNumber assigned yet, so callers can
  // still fall back to something else (never the Guid) in that case.
  const hospitalNumberByAdmissionId = useMemo(() => {
    const hospitalNumberById = new Map(patients.data.map((p) => [p.id, p.hospitalNumber]))
    return new Map(admissions.data.map((a) => [a.id, hospitalNumberById.get(a.patientId) ?? null]))
  }, [admissions.data, patients.data])

  return {
    patientNameByAdmissionId,
    hospitalNumberByAdmissionId,
    loading: admissions.loading || patients.loading,
    // Phase F1 - Frontend Hardening (Section 11, loading/error states). Previously this hook
    // swallowed its own fetch failures entirely — a consumer had no way to know the admission
    // or patient lookups behind every "Unknown patient" fallback had actually failed vs. simply
    // being empty. Exposing both lets LabResults/Assessments/DoorTiming show a real error+retry
    // instead of silently mislabeling every row.
    error: admissions.error ?? patients.error ?? null,
    reload: () => {
      admissions.reload()
      patients.reload()
    },
  }
}
