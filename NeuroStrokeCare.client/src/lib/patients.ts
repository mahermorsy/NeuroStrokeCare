/**
 * The single definition of "mask a National ID for ordinary UI display" (list rows, search
 * results, the Patient Context Header, Patient Summary) — keeps only the last 4 characters
 * visible, everything before that becomes asterisks. Presentation only: the stored value and
 * API response are untouched.
 *
 * PHASE 11 (area 14): "ordinary UI/search/list/context uses masked NationalId where practical;
 * a formal clinical report may show it unmasked" — Report.tsx deliberately does NOT use this
 * helper, since it is the one place an unmasked National ID is intentionally shown.
 *
 * Previously defined independently inside Patients.tsx; extracted here so Patient Summary can
 * use the exact same masking rule rather than a second, possibly-drifting copy.
 */
export function maskNationalId(value: string) {
  const trimmed = value.trim()
  if (trimmed.length <= 4) return '*'.repeat(trimmed.length)
  return '*'.repeat(trimmed.length - 4) + trimmed.slice(-4)
}
