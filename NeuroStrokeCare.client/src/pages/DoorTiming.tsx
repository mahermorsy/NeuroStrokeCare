import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import { useAdmissionContext } from '@/hooks/useAdmissionContext'
import type { DoorTimingResponse, AdmissionResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, Select, TextInput } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { isOpenAdmission } from '@/lib/admissions'
import { usePatientSelection } from '@/context/PatientSelectionContext'

const doorTimingApi = entityApi<DoorTimingResponse>('DoorTiming')
const admissionsApi = entityApi<AdmissionResponse>('Admission')

function minutes(v: number | null) {
  return v === null || v === undefined ? '—' : `${v} min`
}

// datetime-local wants "YYYY-MM-DDTHH:mm" in local time, not an ISO string.
function toLocalInputValue(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export default function DoorTiming() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useEntityList(() => doorTimingApi.list())
  const {
    patientNameByAdmissionId,
    hospitalNumberByAdmissionId,
    loading: contextLoading,
    error: contextError,
    reload: reloadContext,
  } = useAdmissionContext()
  // PHASE 11 (area 5): "Patient Selection Context" (not useAdmissionContext above, a
  // differently-named, unrelated id-lookup hook - see PatientSelectionContext.tsx's header
  // comment for why they're named apart).
  const { admissionId: contextAdmissionId } = usePatientSelection()
  const admissions = useEntityList(() => admissionsApi.list())

  const [activateOpen, setActivateOpen] = useState(false)
  const [activateAdmissionId, setActivateAdmissionId] = useState('')
  const [activateArrival, setActivateArrival] = useState(() => toLocalInputValue(new Date()))
  const [activateBusy, setActivateBusy] = useState(false)
  const [activateError, setActivateError] = useState<string | null>(null)
  // ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 6): an explicit confirmation step
  // before activation, on top of the duplicate-prevention already in admissionsWithoutOpenCode
  // below - this is the "require confirmation" requirement, kept as a plain checkbox rather
  // than a second modal/dialog so it doesn't add new UI infrastructure.
  const [activateConfirmed, setActivateConfirmed] = useState(false)

  const [busyKey, setBusyKey] = useState<string | null>(null)
  // Phase F1 - Frontend Hardening (Section 13/14): recordMilestone/standDown previously had no
  // catch at all, so a failed write just left the button stuck mid-spin with an unhandled
  // promise rejection in the console and zero feedback to the clinician - this surfaces it the
  // same way the activation form's own error banner does.
  const [actionError, setActionError] = useState<string | null>(null)

  // A live clock so "elapsed since activation" keeps counting up on screen
  // without the clinician having to refresh the page.
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 30000)
    return () => clearInterval(interval)
  }, [])

  // Only admissions that don't already have an open (active) stroke code —
  // stops the team from accidentally double-activating the same case.
  const admissionsWithoutOpenCode = useMemo(() => {
    const openAdmissionIds = new Set(data.map((d) => d.admissionId))
    return admissions.data
      .filter((a) => isOpenAdmission(a) && !openAdmissionIds.has(a.id))
      .sort((a, b) => new Date(b.admissionTime).getTime() - new Date(a.admissionTime).getTime())
  }, [admissions.data, data])

  // PHASE 11 (area 5): "if global context points to an ineligible admission, show a clear
  // explanation and do not allow an invalid write" - this never overrides
  // admissionsWithoutOpenCode (the one safety rule that decides what's selectable); it only
  // decides whether to pre-fill the activation form from context, and if it can't, why not.
  const contextEligible = useMemo(
    () => Boolean(contextAdmissionId) && admissionsWithoutOpenCode.some((a) => a.id === contextAdmissionId),
    [contextAdmissionId, admissionsWithoutOpenCode],
  )

  const contextIneligibleReason = useMemo(() => {
    if (!contextAdmissionId || contextEligible) return null
    const admission = admissions.data.find((a) => a.id === contextAdmissionId)
    if (!admission) return null // not loaded yet, or genuinely not found - say nothing rather than guess
    if (!isOpenAdmission(admission)) return 'that patient has already been discharged'
    if (data.some((d) => d.admissionId === contextAdmissionId)) return 'that patient already has an active stroke code'
    return 'that admission is not eligible for a new stroke code right now'
  }, [contextAdmissionId, contextEligible, admissions.data, data])

  function openActivate() {
    setActivateAdmissionId(contextEligible ? (contextAdmissionId as string) : '')
    setActivateArrival(toLocalInputValue(new Date()))
    setActivateError(null)
    setActivateConfirmed(false)
    setActivateOpen(true)
  }

  async function submitActivate(e: FormEvent) {
    e.preventDefault()
    // Re-entrancy guard (Phase F1 - Frontend Hardening, Section 16) - a double-click/double-
    // Enter could otherwise fire two concurrent activations for the same admission before the
    // disabled-button re-render lands.
    if (activateBusy) return
    if (!user?.userId || !activateAdmissionId || !activateConfirmed) return
    setActivateBusy(true)
    setActivateError(null)
    try {
      await doorTimingApi.create(
        {
          admissionId: activateAdmissionId,
          er_StrokeArrival: new Date(activateArrival).toISOString(),
        },
        user.userId,
      )
      setActivateOpen(false)
      reload()
    } catch {
      setActivateError('Could not activate the stroke code — try again.')
    } finally {
      setActivateBusy(false)
    }
  }

  async function recordMilestone(row: DoorTimingResponse, field: 'doorToCT' | 'doorToNeedle' | 'doorToGroin') {
    if (!user?.userId) return
    const key = `${row.id}-${field}`
    setBusyKey(key)
    setActionError(null)
    try {
      await doorTimingApi.update(
        {
          id: row.id,
          admissionId: row.admissionId,
          er_StrokeArrival: row.er_StrokeArrival,
          doorToCT: row.doorToCT,
          doorToNeedle: row.doorToNeedle,
          doorToGroin: row.doorToGroin,
          [field]: new Date().toISOString(),
        },
        user.userId,
      )
      reload()
    } catch {
      setActionError('Could not record that milestone — try again.')
    } finally {
      setBusyKey(null)
    }
  }

  async function standDown(row: DoorTimingResponse) {
    if (!user?.userId) return
    const key = `${row.id}-standdown`
    setBusyKey(key)
    setActionError(null)
    try {
      await doorTimingApi.changeStatus(row.id, 0, user.userId)
      reload()
    } catch {
      setActionError('Could not stand down this stroke code — try again.')
    } finally {
      setBusyKey(null)
    }
  }

  function milestoneCell(
    row: DoorTimingResponse,
    field: 'doorToCT' | 'doorToNeedle' | 'doorToGroin',
    recordedAt: string | null,
    minutesValue: number | null,
    delayed: boolean,
    targetMinutes: number | null,
    label: string,
  ) {
    if (recordedAt) {
      return (
        <div className="flex items-center gap-1.5">
          <span>{minutes(minutesValue)}</span>
          {delayed && <StatusPill label="Delayed" tone="critical" />}
        </div>
      )
    }
    const elapsed = Math.round((now - new Date(row.er_StrokeArrival).getTime()) / 60000)
    const overdue = targetMinutes !== null && elapsed > targetMinutes
    const busy = busyKey === `${row.id}-${field}`
    return (
      <div className="flex flex-col gap-1">
        <span className={`text-[12px] ${overdue ? 'font-semibold text-critical' : 'text-text-muted'}`}>
          {elapsed} min elapsed{overdue ? ' — overdue' : ''}
        </span>
        <button
          type="button"
          onClick={() => recordMilestone(row, field)}
          disabled={busy}
          className="w-fit rounded-lg border border-border-subtle px-2 py-1 text-[12px] font-medium text-accent hover:bg-accent/10 disabled:opacity-60"
        >
          {busy ? 'Recording…' : label}
        </button>
      </div>
    )
  }

  const columns: Column<DoorTimingResponse>[] = [
    {
      header: 'Patient',
      render: (r) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">{patientNameByAdmissionId.get(r.admissionId) ?? '—'}</span>
          <span className="text-[11.5px] text-text-muted">
            {hospitalNumberByAdmissionId.get(r.admissionId)
              ? `Hospital No. ${hospitalNumberByAdmissionId.get(r.admissionId)}`
              : 'No hospital number assigned'}
          </span>
        </div>
      ),
    },
    { header: 'ER arrival', render: (r) => new Date(r.er_StrokeArrival).toLocaleString() },
    {
      header: 'Door-to-CT',
      render: (r) => milestoneCell(r, 'doorToCT', r.doorToCT, r.minutesToCT, r.ctDelayed, 25, 'Record CT now'),
    },
    {
      header: 'Door-to-needle',
      render: (r) =>
        milestoneCell(r, 'doorToNeedle', r.doorToNeedle, r.minutesToNeedle, r.needleDelayed, 60, 'Record needle now'),
    },
    {
      header: 'Door-to-groin',
      render: (r) => milestoneCell(r, 'doorToGroin', r.doorToGroin, r.minutesToGroin, false, null, 'Record groin now'),
    },
    {
      header: '',
      render: (r) => (
        <button
          type="button"
          onClick={() => standDown(r)}
          disabled={busyKey === `${r.id}-standdown`}
          className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-text-secondary hover:bg-border-soft disabled:opacity-60"
        >
          {busyKey === `${r.id}-standdown` ? 'Standing down…' : 'Stand down'}
        </button>
      ),
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Door Timing"
        subtitle="Stroke code activation and response-time benchmarks from ER arrival"
        action={<PrimaryButton onClick={openActivate}>+ Activate stroke code</PrimaryButton>}
      />

      {actionError && (
        <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{actionError}</p>
      )}

      {contextError && !error && (
        <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
          Patient names couldn't be loaded, so rows below may show as "—".{' '}
          <button type="button" onClick={reloadContext} className="underline">
            Retry
          </button>
        </p>
      )}

      <Card>
        <DataTable
          columns={columns}
          rows={data}
          rowKey={(r) => r.id}
          loading={loading || contextLoading || admissions.loading}
          error={error}
          onRetry={reload}
          emptyMessage="No active stroke code — activate one the moment a stroke case arrives."
        />
      </Card>

      <Modal open={activateOpen} title="Activate stroke code" onClose={() => setActivateOpen(false)}>
        <form onSubmit={submitActivate} className="flex flex-col gap-3.5">
          <Field label="Admission">
            <Select
              required
              value={activateAdmissionId}
              onChange={(e) => setActivateAdmissionId(e.target.value)}
            >
              <option value="" disabled>
                Select the patient's admission…
              </option>
              {admissionsWithoutOpenCode.map((a) => (
                <option key={a.id} value={a.id}>
                  {patientNameByAdmissionId.get(a.id) ?? 'Unknown patient'}
                  {hospitalNumberByAdmissionId.get(a.id) ? ` (Hospital No. ${hospitalNumberByAdmissionId.get(a.id)})` : ''} —{' '}
                  {new Date(a.admissionTime).toLocaleString()}
                </option>
              ))}
            </Select>
          </Field>
          {admissionsWithoutOpenCode.length === 0 && (
            <p className="text-[12.5px] text-text-muted">
              No admitted patient is available to activate — every current admission already has an open stroke
              code, or there are no active admissions yet.
            </p>
          )}
          {contextIneligibleReason && !activateAdmissionId && (
            <p className="rounded-lg bg-warning-bg px-3 py-2 text-[12.5px] font-medium text-warning">
              The patient currently selected in your Patient Context can&apos;t be used for a new stroke code
              activation right now — {contextIneligibleReason}. Choose a different admission below instead.
            </p>
          )}
          <Field label="ER / stroke arrival time">
            <TextInput
              type="datetime-local"
              required
              value={activateArrival}
              onChange={(e) => setActivateArrival(e.target.value)}
            />
          </Field>
          <p className="text-[12.5px] text-text-secondary">
            This starts the Door-to-CT (target ≤ 25 min) and Door-to-needle (target ≤ 60 min) clocks for this case,
            and surfaces it as a live alert to the whole team if either window is missed.
          </p>
          <label className="flex items-start gap-2.5 rounded-lg border border-border-subtle bg-surface px-3 py-2.5 text-[12.5px] font-medium text-text-secondary">
            <input
              type="checkbox"
              checked={activateConfirmed}
              onChange={(e) => setActivateConfirmed(e.target.checked)}
              className="mt-0.5 h-4 w-4 shrink-0 accent-accent focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
            />
            <span>
              I confirm I want to activate a stroke code for{' '}
              {activateAdmissionId ? (
                <>
                  {patientNameByAdmissionId.get(activateAdmissionId) ?? 'this patient'}
                  {hospitalNumberByAdmissionId.get(activateAdmissionId)
                    ? ` (Hospital No. ${hospitalNumberByAdmissionId.get(activateAdmissionId)})`
                    : ''}
                </>
              ) : (
                'this admission'
              )}{' '}
              now.
            </span>
          </label>
          {activateError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">
              {activateError}
            </p>
          )}
          <PrimaryButton type="submit" disabled={activateBusy || !activateAdmissionId || !activateConfirmed}>
            {activateBusy ? 'Activating…' : 'Activate'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
