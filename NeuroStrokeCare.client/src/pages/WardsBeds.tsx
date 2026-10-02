import { useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import type { WardResponse, BedResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select } from '@/components/FormField'
import StatusPill from '@/components/StatusPill'
import { BED_STATUS, BED_STATUS_OPTIONS, BED_STATUS_TONE } from '@/lib/enums'
import { describeApiError } from '@/lib/apiError'

const wardsApi = entityApi<WardResponse>('Ward')
const bedsApi = entityApi<BedResponse>('Bed')

const emptyWardForm = { code: '', name: '', totalBeds: '' }
const emptyBedForm = { wardId: '', bedNumber: '', status: 1 }

export default function WardsBeds() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const wards = useEntityList(() => wardsApi.list())
  const beds = useEntityList(() => bedsApi.list())

  const [wardModalOpen, setWardModalOpen] = useState(false)
  const [bedModalOpen, setBedModalOpen] = useState(false)
  const [editingWardId, setEditingWardId] = useState<string | null>(null)
  const [editingBedId, setEditingBedId] = useState<string | null>(null)
  const [wardForm, setWardForm] = useState(emptyWardForm)
  const [bedForm, setBedForm] = useState(emptyBedForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const wardNameById = useMemo(() => new Map(wards.data.map((w) => [w.id, w.name])), [wards.data])

  // Live counts from the actual Bed rows in each ward — not the Ward's own
  // `totalBeds` field, which is just the planned capacity typed in once at
  // creation and never updates itself as beds are actually added or removed.
  // Using it as the denominator here is what made the number look "stuck".
  const countsByWard = useMemo(() => {
    const map = new Map<string, { occupied: number; actual: number }>()
    for (const ward of wards.data) {
      const wardBeds = beds.data.filter((b) => b.wardId === ward.id)
      map.set(ward.id, {
        occupied: wardBeds.filter((b) => BED_STATUS[b.status as keyof typeof BED_STATUS] === 'Occupied').length,
        actual: wardBeds.length,
      })
    }
    return map
  }, [wards.data, beds.data])

  function openNewWard() {
    setEditingWardId(null)
    setWardForm(emptyWardForm)
    setFormError(null)
    setWardModalOpen(true)
  }

  function openEditWard(w: WardResponse) {
    setEditingWardId(w.id)
    setWardForm({ code: w.code, name: w.name, totalBeds: String(w.totalBeds) })
    setFormError(null)
    setWardModalOpen(true)
  }

  function openNewBed() {
    setEditingBedId(null)
    setBedForm(emptyBedForm)
    setFormError(null)
    setBedModalOpen(true)
  }

  function openEditBed(b: BedResponse) {
    setEditingBedId(b.id)
    setBedForm({ wardId: b.wardId, bedNumber: b.bedNumber, status: b.status })
    setFormError(null)
    setBedModalOpen(true)
  }

  async function submitWard(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      const payload = { code: wardForm.code, name: wardForm.name, totalBeds: Number(wardForm.totalBeds) }
      if (editingWardId) {
        await wardsApi.update({ id: editingWardId, ...payload }, user.userId)
      } else {
        await wardsApi.create(payload, user.userId)
      }
      setWardModalOpen(false)
      wards.reload()
    } catch (err) {
      setFormError(
        describeApiError(err, {
          409: editingWardId
            ? 'Could not save these changes — the code may already be in use by another ward.'
            : 'Could not create the ward — the code may already be in use.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  async function submitBed(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      const payload = { wardId: bedForm.wardId, bedNumber: bedForm.bedNumber, status: Number(bedForm.status) }
      if (editingBedId) {
        await bedsApi.update({ id: editingBedId, ...payload }, user.userId)
      } else {
        await bedsApi.create(payload, user.userId)
      }
      setBedModalOpen(false)
      beds.reload()
    } catch (err) {
      setFormError(
        describeApiError(err, {
          409: editingBedId
            ? 'Could not save these changes — the bed number may already be in use by another bed.'
            : 'Could not create the bed — the bed number may already be in use.',
        }),
      )
    } finally {
      setSubmitting(false)
    }
  }

  const wardColumns: Column<WardResponse>[] = [
    { header: 'Code', render: (w) => <span className="font-semibold">{w.code}</span> },
    { header: 'Name', render: (w) => w.name },
    {
      header: 'Beds',
      render: (w) => {
        const c = countsByWard.get(w.id)
        return `${c?.occupied ?? 0} / ${c?.actual ?? 0}`
      },
    },
    ...(isAdmin
      ? [
          {
            header: '',
            render: (w: WardResponse) => (
              <button
                type="button"
                onClick={() => openEditWard(w)}
                className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent hover:bg-accent/10"
              >
                Edit
              </button>
            ),
          } as Column<WardResponse>,
        ]
      : []),
  ]

  const bedColumns: Column<BedResponse>[] = [
    { header: 'Bed number', render: (b) => <span className="font-semibold">{b.bedNumber}</span> },
    { header: 'Ward', render: (b) => wardNameById.get(b.wardId) ?? '—' },
    {
      header: 'Status',
      render: (b) => {
        const label = BED_STATUS[b.status as keyof typeof BED_STATUS] ?? 'Unknown'
        return <StatusPill label={label} tone={BED_STATUS_TONE[label] ?? 'neutral'} />
      },
    },
    ...(isAdmin
      ? [
          {
            header: '',
            render: (b: BedResponse) => (
              <button
                type="button"
                onClick={() => openEditBed(b)}
                className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent hover:bg-accent/10"
              >
                Edit
              </button>
            ),
          } as Column<BedResponse>,
        ]
      : []),
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="Wards & Beds" subtitle="Ward capacity and bed-level status across the unit" />

      {!isAdmin && (
        <p className="text-[12.5px] text-text-muted">
          Only Admin accounts can add or edit wards and beds — everyone can still see live status here.
        </p>
      )}

      <Card className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-[16px] font-semibold text-text">Wards</h2>
          {isAdmin && <PrimaryButton onClick={openNewWard}>+ New ward</PrimaryButton>}
        </div>
        <DataTable
          columns={wardColumns}
          rows={wards.data}
          rowKey={(w) => w.id}
          loading={wards.loading}
          error={wards.error}
          onRetry={wards.reload}
          emptyMessage="No wards yet — add the first one."
        />
      </Card>

      <Card className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-[16px] font-semibold text-text">Beds</h2>
          {isAdmin && (
            <PrimaryButton onClick={openNewBed} disabled={wards.data.length === 0}>
              + New bed
            </PrimaryButton>
          )}
        </div>
        <DataTable
          columns={bedColumns}
          rows={beds.data}
          rowKey={(b) => b.id}
          loading={beds.loading}
          error={beds.error}
          onRetry={beds.reload}
          emptyMessage="No beds yet — add a ward first, then add beds to it."
        />
      </Card>

      <Modal
        open={wardModalOpen}
        title={editingWardId ? 'Edit ward' : 'New ward'}
        onClose={() => setWardModalOpen(false)}
      >
        <form onSubmit={submitWard} className="flex flex-col gap-3.5">
          <Field label="Code (e.g. FW, MW, ICU)">
            <TextInput required value={wardForm.code} onChange={(e) => setWardForm({ ...wardForm, code: e.target.value })} />
          </Field>
          <Field label="Name">
            <TextInput required value={wardForm.name} onChange={(e) => setWardForm({ ...wardForm, name: e.target.value })} />
          </Field>
          <Field label="Planned capacity (reference only — the Beds count above always reflects actual beds added)">
            <TextInput
              type="number"
              min="1"
              required
              value={wardForm.totalBeds}
              onChange={(e) => setWardForm({ ...wardForm, totalBeds: e.target.value })}
            />
          </Field>
          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}
          <PrimaryButton type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : editingWardId ? 'Save changes' : 'Save ward'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal open={bedModalOpen} title={editingBedId ? 'Edit bed' : 'New bed'} onClose={() => setBedModalOpen(false)}>
        <form onSubmit={submitBed} className="flex flex-col gap-3.5">
          <Field label="Ward">
            <Select
              required
              value={bedForm.wardId}
              onChange={(e) => setBedForm({ ...bedForm, wardId: e.target.value })}
            >
              <option value="" disabled>
                Select a ward…
              </option>
              {wards.data.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Bed number">
            <TextInput
              required
              placeholder="e.g. FW-04"
              value={bedForm.bedNumber}
              onChange={(e) => setBedForm({ ...bedForm, bedNumber: e.target.value })}
            />
          </Field>
          <Field label="Status">
            <Select
              value={bedForm.status}
              onChange={(e) => setBedForm({ ...bedForm, status: Number(e.target.value) })}
            >
              {BED_STATUS_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </Select>
          </Field>
          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}
          <PrimaryButton type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : editingBedId ? 'Save changes' : 'Save bed'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
