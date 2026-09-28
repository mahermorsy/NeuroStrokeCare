import { api } from '@/lib/api'

// PATCH /api/Admission/{id}/transfer — the one safe way to move a patient's
// clinical status forward and/or change their bed. Not a generic entityApi()
// call: it needs to distinguish "leave the bed alone" from "clear the bed",
// which a single nullable field can't express, so it takes an explicit
// changeBed flag (see AdmissionController.Transfer on the backend).
export interface TransferAdmissionParams {
  actingUserId: string
  newStatus: number
  changeBed?: boolean
  newBedId?: string | null
}

export function transferAdmission(admissionId: string, params: TransferAdmissionParams) {
  return api.patch<void>(`/Admission/${admissionId}/transfer`, null, {
    params: {
      actingUserId: params.actingUserId,
      newStatus: params.newStatus,
      changeBed: params.changeBed ?? false,
      newBedId: params.changeBed ? params.newBedId ?? undefined : undefined,
    },
  })
}
