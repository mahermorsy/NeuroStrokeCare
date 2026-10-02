import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { usePatientSelection } from '@/context/PatientSelectionContext'
import { entityApi } from '@/lib/entityApi'
import { api } from '@/lib/api'
import { useAuth } from '@/context/AuthContext'
import { isClinicalRole } from '@/lib/roles'
import type { AdmissionResponse, PatientResponse, DoorTimingResponse, AdmissionSearchResultResponse } from '@/types/entities'
import { PATIENT_STATUS, STROKE_TYPE } from '@/lib/enums'
import Modal from '@/components/Modal'
import AdmissionPicker from '@/components/AdmissionPicker'
import { AlertIcon } from '@/components/icons'

const admissionsApi = entityApi<AdmissionResponse>('Admission')
const patientsApi = entityApi<PatientResponse>('Patient')

interface PagedResult<T> {
  items: T[]
  totalCount: number
}

function calculateAge(dateOfBirth: string): number | null {
  const dob = new Date(dateOfBirth)
  if (Number.isNaN(dob.getTime())) return null
  const now = new Date()
  let age = now.getFullYear() - dob.getFullYear()
  const hadBirthdayThisYear =
    now.getMonth() > dob.getMonth() || (now.getMonth() === dob.getMonth() && now.getDate() >= dob.getDate())
  if (!hadBirthdayThisYear) age -= 1
  return age >= 0 ? age : null
}

/**
 * PHASE 11 (area 4 + 9 + 10 + 18): the compact "Patient Context Header" — mounted once in
 * Layout.tsx, just above <Outlet/>, so it's visible on every clinical page without that page
 * needing to know anything about it. Renders nothing when no patient/admission is selected
 * (see usePatientSelection / PatientSelectionContext).
 *
 * The name, HospitalNumber, masked National ID, ward/bed, status and admission time come
 * straight from the selection snapshot (already fetched once, at selection time, by whichever
 * AdmissionPicker established it) — no extra request needed for those. Age/sex and stroke type
 * are "if already modeled" / "where available" per the spec, and they're each a single extra
 * by-id fetch (Admission, Patient) that only re-runs when the selected admission actually
 * changes, not on every navigation (Layout itself only mounts once per sign-in — see the
 * comment on its alert-badge effect). These enrichment fields live in this component's own
 * local state, not in the shared context, so the global context stays the minimal snapshot the
 * spec asks for.
 *
 * The Stroke Code indicator (area 9) reuses the exact same "an active (CurrentState=Active)
 * DoorTiming record exists for this admission" rule DoorTiming.tsx's own
 * `admissionsWithoutOpenCode` filter is built on — see that file. The model has no separate
 * "resolved" state distinct from "never activated" once a stroke code is stood down (its
 * DoorTiming record simply stops being Active), so this indicator can only show
 * active / not-active, not a three-way active/resolved/never state — documented in the
 * deliverable report rather than invented here.
 */
export default function PatientContextHeader() {
  const { user } = useAuth()
  const { admissionId, snapshot, select, clear } = usePatientSelection()
  const [pickerOpen, setPickerOpen] = useState(false)
  const [enrichment, setEnrichment] = useState<{
    strokeType: number | null
    age: number | null
    gender: string | null
    strokeCodeActive: boolean
  } | null>(null)

  useEffect(() => {
    if (!admissionId) {
      setEnrichment(null)
      return
    }
    let cancelled = false
    async function load() {
      try {
        const [admission, patient, doorTimingPage] = await Promise.all([
          admissionsApi.getById(admissionId as string),
          snapshot?.patientId ? patientsApi.getById(snapshot.patientId) : Promise.resolve(null),
          api
            .get<PagedResult<DoorTimingResponse>>('/DoorTiming/paged', {
              params: { admissionId, pageNumber: 1, pageSize: 1 },
            })
            .then((r) => r.data)
            .catch(() => null),
        ])
        if (cancelled) return
        setEnrichment({
          strokeType: admission.strokeType,
          age: patient ? calculateAge(patient.dateOfBirth) : null,
          gender: patient?.gender ?? null,
          strokeCodeActive: Boolean(doorTimingPage && doorTimingPage.items.length > 0),
        })
      } catch {
        if (!cancelled) setEnrichment(null)
      }
    }
    load()
    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [admissionId, snapshot?.patientId])

  if (!admissionId || !snapshot) return null

  function handlePicked(_id: string, result: AdmissionSearchResultResponse | null) {
    if (result) select(result)
    setPickerOpen(false)
  }

  return (
    <>
      <div className="mb-5 flex flex-wrap items-center gap-x-5 gap-y-2.5 rounded-xl border border-accent/30 bg-accent/5 px-4 py-3 text-[13px] print:hidden">
        <div className="flex min-w-0 flex-col leading-tight">
          <span className="truncate text-[14.5px] font-semibold text-text">{snapshot.patientName}</span>
          <span className="truncate text-[11.5px] text-text-muted">
            {snapshot.hospitalNumber ? `Hospital No. ${snapshot.hospitalNumber}` : 'No hospital number assigned'}
            {enrichment?.age != null ? ` · ${enrichment.age}y` : ''}
            {enrichment?.gender ? ` ${enrichment.gender}` : ''}
          </span>
        </div>

        <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-[12px] text-text-secondary">
          <span>Admitted {new Date(snapshot.admissionTime).toLocaleDateString()}</span>
          <span>
            {snapshot.wardCode && snapshot.bedNumber ? `${snapshot.wardCode} · Bed ${snapshot.bedNumber}` : 'No bed assigned'}
          </span>
          {enrichment?.strokeType != null && (
            <span>{STROKE_TYPE[enrichment.strokeType as keyof typeof STROKE_TYPE] ?? 'Stroke type recorded'}</span>
          )}
          <span className="rounded-full bg-border-soft px-2 py-0.5 font-medium text-text-secondary">
            {PATIENT_STATUS[snapshot.status as keyof typeof PATIENT_STATUS] ?? 'Unknown status'}
            {!snapshot.isOpen ? ' — discharged' : ''}
          </span>
          {enrichment?.strokeCodeActive && (
            <Link
              to="/door-timing"
              className="flex items-center gap-1 rounded-full bg-critical-bg px-2.5 py-0.5 font-semibold text-critical hover:underline"
            >
              <AlertIcon className="h-3.5 w-3.5" />
              Stroke code active
            </Link>
          )}
        </div>

        <div className="ml-auto flex items-center gap-2">
          {isClinicalRole(user?.role) && (
            <button
              type="button"
              onClick={() => setPickerOpen(true)}
              className="rounded-md px-2.5 py-1.5 text-[12.5px] font-medium text-accent hover:bg-accent/10"
            >
              Change patient
            </button>
          )}
          <button
            type="button"
            onClick={clear}
            className="rounded-md px-2.5 py-1.5 text-[12.5px] font-medium text-text-muted hover:bg-border-soft hover:text-text"
          >
            Clear patient
          </button>
        </div>
      </div>

      <Modal open={pickerOpen} title="Change patient" onClose={() => setPickerOpen(false)}>
        <div className="flex flex-col gap-3">
          <p className="text-[12.5px] text-text-muted">
            Search by Hospital Number, National ID, or name to switch the patient this screen and the other clinical
            pages are using.
          </p>
          <AdmissionPicker value={admissionId} onSelect={handlePicked} />
        </div>
      </Modal>
    </>
  )
}
