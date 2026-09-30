import { api } from '@/lib/api'

// PATCH /api/Admission/{id}/stroke-type — dedicated command (not the blind
// PUT) so it records who set it and when, without risking other fields on
// the admission being reset by a full-entity overwrite.
export function setAdmissionStrokeType(admissionId: string, actingUserId: string, strokeType: number) {
  return api.patch<void>(`/Admission/${admissionId}/stroke-type`, null, {
    params: { actingUserId, strokeType },
  })
}
