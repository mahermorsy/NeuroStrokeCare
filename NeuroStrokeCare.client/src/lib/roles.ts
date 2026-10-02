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

// PHASE 11 (area 7): mirrors the backend's Roles.AnyClinical (AnyDoctor + AnyNurse) - every
// role that currently exists is clinical, but this stays a named helper (rather than
// inlining isDoctorRole(...) || isNurseRole(...) at each call site) so a future
// non-clinical role (e.g. a pure Administrator/receptionist account) only needs updating
// here, not at every page that gates on it.
export function isClinicalRole(role?: string | null) {
  return isDoctorRole(role) || isNurseRole(role)
}

export function roleLabel(role?: string | null) {
  if (!role) return 'Unknown'
  return ROLE_LABELS[role] ?? role
}
