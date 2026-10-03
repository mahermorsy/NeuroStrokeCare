import { api } from '@/lib/api'
import type { AdmissionPatientSearchResultResponse } from '@/types/entities'

export function searchAdmissionPatients(query: string, signal?: AbortSignal) {
  return api
    .get<AdmissionPatientSearchResultResponse[]>('/Admission/patient-search', {
      params: { query },
      signal,
    })
    .then((r) => r.data)
}
