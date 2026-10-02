import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import { useAuth } from '@/context/AuthContext'
import type { AdmissionSearchResultResponse } from '@/types/entities'

/**
 * PHASE 11 (areas 3-5): the "Global Patient Context" the spec asks for — a lightweight,
 * app-wide record of the one admission a staff member is currently working on, so Patient
 * Summary / Assessments / Lab Results / Door Timing / Follow-up / Report can share it instead
 * of each re-prompting with its own AdmissionPicker.
 *
 * Named `PatientSelectionContext` / `usePatientSelection` (not "PatientContext") deliberately,
 * to avoid any confusion with the pre-existing `useAdmissionContext` hook
 * (hooks/useAdmissionContext.ts) — that hook is an unrelated id-to-display-label lookup map
 * used by data tables, not a selection/navigation context. The two are not interchangeable.
 *
 * What this stores, and why it's safe:
 * - Only `admissionId` plus the same small, already-public-to-this-role projection the
 *   AdmissionPicker/search endpoint already returns (AdmissionSearchResultResponse: patient
 *   name, HospitalNumber, masked National ID, ward/bed, status, admission time). This is
 *   exactly the "minimal identifying info" the spec asks for — never a full Patient or
 *   Admission record, and never anything the search endpoint wouldn't already have shown the
 *   same authorized user a moment earlier.
 * - Persisted to `sessionStorage`, not `localStorage` — it clears when the browser tab closes,
 *   rather than lingering on a shared clinical workstation indefinitely. It is also cleared
 *   immediately on logout (see the effect below), so a different staff member signing in on
 *   the same machine never inherits the previous user's selected patient.
 * - Pages that consume it (`usePatientSelection()`) are responsible for re-validating that the
 *   selected admission is still appropriate for whatever they're about to do (e.g. Door Timing
 *   must still apply its own `admissionsWithoutOpenCode` eligibility check) — this context is a
 *   convenience for *finding* the admission again, never a substitute for a page's own
 *   authorization or safety checks.
 */

const STORAGE_KEY = 'nsc_patient_selection'

export interface PatientSelectionContextValue {
  /** The selected admission's id, or null if nothing is selected. */
  admissionId: string | null
  /** The small display snapshot captured at selection time (see file header). May go stale if
   *  the admission changes elsewhere (e.g. a discharge) — consumers that need up-to-date
   *  clinical state should still fetch the admission by id rather than trusting this blindly
   *  for anything beyond display. */
  snapshot: AdmissionSearchResultResponse | null
  /** Establishes (or replaces) the selected patient/admission. */
  select: (snapshot: AdmissionSearchResultResponse) => void
  /** Clears the selected patient/admission. Call this from an explicit "Clear patient" /
   *  "Change patient" action — never automatically from a page just because it didn't find a
   *  use for the context, since another page may still be relying on it. */
  clear: () => void
}

const PatientSelectionContext = createContext<PatientSelectionContextValue | undefined>(undefined)

function readStoredSnapshot(): AdmissionSearchResultResponse | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    return raw ? (JSON.parse(raw) as AdmissionSearchResultResponse) : null
  } catch {
    return null
  }
}

export function PatientSelectionProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth()
  const [snapshot, setSnapshot] = useState<AdmissionSearchResultResponse | null>(() => readStoredSnapshot())

  const select = useCallback((next: AdmissionSearchResultResponse) => {
    setSnapshot(next)
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next))
    } catch {
      // sessionStorage can throw in a private/locked-down browsing context. The in-memory
      // state above still works for the rest of this tab's session — it just won't survive a
      // reload. Not worth surfacing to the user over.
    }
  }, [])

  const clear = useCallback(() => {
    setSnapshot(null)
    try {
      sessionStorage.removeItem(STORAGE_KEY)
    } catch {
      // see select()
    }
  }, [])

  // Clinical privacy (area 3): never let a selected patient outlive the session that picked
  // it. A different staff member signing in on a shared workstation must not inherit it.
  useEffect(() => {
    if (!isAuthenticated) clear()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated])

  const value: PatientSelectionContextValue = {
    admissionId: snapshot?.admissionId ?? null,
    snapshot,
    select,
    clear,
  }

  return <PatientSelectionContext.Provider value={value}>{children}</PatientSelectionContext.Provider>
}

export function usePatientSelection() {
  const ctx = useContext(PatientSelectionContext)
  if (!ctx) throw new Error('usePatientSelection must be used inside a PatientSelectionProvider')
  return ctx
}
