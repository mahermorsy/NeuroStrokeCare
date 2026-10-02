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

  return {
    patientNameByAdmissionId,
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
