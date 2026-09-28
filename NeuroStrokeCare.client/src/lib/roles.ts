// Mirrors NeuroStrokeCare.Data.Constants.Roles on the backend. Doctor ranks
// (Consultant/Registrar/Resident) own admissions, ward transfers, and the
// physician-administered scales (NIHSS/ASPECTS/ICH/Canadian TIA/GCS). Nursing
// roles (Nurse/NursingSupervisor) own the nursing-administered scales
// (Braden/Morse/GUSS). Admin can do everything (superuser, for setup/testing).

export const ALL_ROLES = ['Admin', 'Consultant', 'Registrar', 'Resident', 'Nurse', 'NursingSupervisor'] as const
export type Role = (typeof ALL_ROLES)[number]

export const ROLE_LABELS: Record<string, string> = {
  Admin: 'Admin',
  Consultant: 'Consultant',
  Registrar: 'Registrar',
  Resident: 'Resident',
  Nurse: 'Nurse',
  NursingSupervisor: 'Nursing Supervisor',
}

const DOCTOR_ROLES = ['Consultant', 'Registrar', 'Resident']
const NURSE_ROLES = ['Nurse', 'NursingSupervisor']

export function isDoctorRole(role?: string | null) {
  return role === 'Admin' || DOCTOR_ROLES.includes(role ?? '')
}

export function isNurseRole(role?: string | null) {
  return role === 'Admin' || NURSE_ROLES.includes(role ?? '')
}

export function roleLabel(role?: string | null) {
  if (!role) return 'Unknown'
  return ROLE_LABELS[role] ?? role
}
