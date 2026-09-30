import { api } from '@/lib/api'

// PATCH /api/Admission/{id}/thrombolysis — records that the thrombolytic
// was actually administered, and when. This timestamp is what the 24h
// antithrombotic lockout alert (Alerts page) counts down from.
export function recordThrombolysis(admissionId: string, actingUserId: string, drug: string, doseMg: number) {
  return api.patch<void>(`/Admission/${admissionId}/thrombolysis`, null, {
    params: { actingUserId, drug, doseMg },
  })
}
