import { usePatientSelection } from '@/context/PatientSelectionContext'
import AdmissionPicker from '@/components/AdmissionPicker'
import type { AdmissionSearchResultResponse } from '@/types/entities'

/**
 * PHASE 11 (area 5): wraps AdmissionPicker so clinical write forms (Assessments, Lab Results,
 * Follow-up Notes) "auto-use the selected admission and avoid an unnecessary admission dropdown"
 * when the shared Patient Selection Context already has one selected, while still falling back
 * to the full searchable picker when it doesn't — or once the user explicitly asks to change it
 * via the "Change" button this renders.
 *
 * `value` must already have been initialized from `usePatientSelection().admissionId` by the
 * caller (each page's own "open the New ⟨X⟩ modal" handler does this) — this component only
 * decides how to *display* the current value, it does not seed it.
 *
 * Door Timing deliberately does NOT use this: its own eligibility rule
 * (`admissionsWithoutOpenCode` in DoorTiming.tsx) must still be applied on top of whatever the
 * context points to, and it shows its own explanation rather than silently falling back to a
 * picker when the context's admission isn't eligible — see DoorTiming.tsx.
 */
export default function ContextAwareAdmissionField({
  value,
  onSelect,
  openOnly = true,
  label = 'Patient / admission',
}: {
  value: string
  onSelect: (admissionId: string, result: AdmissionSearchResultResponse | null) => void
  openOnly?: boolean
  label?: string
}) {
  const { snapshot } = usePatientSelection()

  if (snapshot && value === snapshot.admissionId) {
    return (
      <div className="flex items-center justify-between gap-2 rounded-lg border border-accent/40 bg-accent/5 px-3 py-2">
        <div className="flex min-w-0 flex-col leading-tight">
          <span className="truncate text-[13.5px] font-semibold text-text">{snapshot.patientName}</span>
          <span className="truncate text-[11.5px] text-text-muted">
            {snapshot.hospitalNumber ? `Hospital No. ${snapshot.hospitalNumber}` : 'No hospital number assigned'}
            {snapshot.wardCode && snapshot.bedNumber ? ` — ${snapshot.wardCode} ${snapshot.bedNumber}` : ''}
          </span>
        </div>
        <button
          type="button"
          onClick={() => onSelect('', null)}
          className="shrink-0 rounded-md px-2 py-1 text-[12px] font-medium text-text-muted hover:bg-border-soft hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
        >
          Change
        </button>
      </div>
    )
  }

  return <AdmissionPicker value={value} onSelect={onSelect} openOnly={openOnly} label={label} />
}
