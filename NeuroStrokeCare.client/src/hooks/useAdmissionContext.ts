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
  }
}
