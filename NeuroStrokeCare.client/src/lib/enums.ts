// Mirrors NeuroStrokeCare.Data.Enums exactly (numeric values as the API sends
// them in request bodies; display labels are for the UI only).

export const GENDER_OPTIONS = ['Male', 'Female'] as const

export const BED_STATUS = {
  1: 'Vacant',
  2: 'Occupied',
  3: 'PendingDischarge',
  4: 'Cleaning',
} as const
export const BED_STATUS_OPTIONS = Object.entries(BED_STATUS).map(([value, label]) => ({
  value: Number(value),
  label,
}))
export const BED_STATUS_TONE: Record<string, 'success' | 'critical' | 'warning' | 'info'> = {
  Vacant: 'success',
  Occupied: 'critical',
  PendingDischarge: 'warning',
  Cleaning: 'info',
}

export const PATIENT_STATUS = {
  1: 'Emergency',
  2: 'SentToImaging',
  3: 'ImagingCompleted',
  4: 'SentToNeedle',
  5: 'NeedleCompleted',
  6: 'SentToGroin',
  7: 'GroinCompleted',
  8: 'AdmittedToER',
  9: 'AdmittedToWard',
  10: 'AdmittedToNICU',
  11: 'AdmittedToIMC',
  12: 'Discharged',
  13: 'TransferredOut',
} as const
export const PATIENT_STATUS_OPTIONS = Object.entries(PATIENT_STATUS).map(([value, label]) => ({
  value: Number(value),
  label,
}))

export const STROKE_TYPE = {
  1: 'Ischemic',
  2: 'Hemorrhagic',
  3: 'TIA',
} as const
export const STROKE_TYPE_OPTIONS = Object.entries(STROKE_TYPE).map(([value, label]) => ({
  value: Number(value),
  label,
}))

export const BRADEN_RISK_LEVEL: Record<number, string> = {
  1: 'NoRisk',
  2: 'MildRisk',
  3: 'ModerateRisk',
  4: 'HighRisk',
  5: 'VeryHighRisk',
}

export const GCS_SEVERITY: Record<number, string> = {
  1: 'Mild',
  2: 'Moderate',
  3: 'Severe',
}

export const GUSS_SEVERITY: Record<number, string> = {
  1: 'Normal',
  2: 'Mild',
  3: 'Moderate',
  4: 'Severe',
}

export const MORSE_RISK_LEVEL: Record<number, string> = {
  1: 'LowRisk',
  2: 'ModerateRisk',
  3: 'HighRisk',
}

// Loose "does this label mean trouble" mapping shared by the risk/severity
// enums above, used to pick a pill color without hardcoding per enum.
export function riskTone(label: string): 'success' | 'warning' | 'critical' | 'info' {
  const s = label.toLowerCase()
  if (s.includes('high') || s.includes('severe')) return 'critical'
  if (s.includes('moderate') || s.includes('mild')) return 'warning'
  if (s.includes('no') || s.includes('normal') || s.includes('low')) return 'success'
  return 'info'
}
