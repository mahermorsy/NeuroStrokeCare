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

const wardsApi = entityApi<WardResponse>('Ward')
const bedsApi = entityApi<BedResponse>('Bed')

export default function WardsBeds() {
  const { user } = useAuth()
  const wards = useEntityList(() => wardsApi.list())
  const beds = useEntityList(() => bedsApi.list())

  const [wardModalOpen, setWardModalOpen] = useState(false)
  const [bedModalOpen, setBedModalOpen] = useState(false)
  const [wardForm, setWardForm] = useState({ code: '', name: '', totalBeds: '' })
  const [bedForm, setBedForm] = useState({ wardId: '', bedNumber: '', status: 1 })
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const wardNameById = useMemo(() => new Map(wards.data.map((w) => [w.id, w.name])), [wards.data])

  const occupancyByWard = useMemo(() => {
    const map = new Map<string, { occupied: number; total: number }>()
    for (const ward of wards.data) {
      const wardBeds = beds.data.filter((b) => b.wardId === ward.id)
      const occupied = wardBeds.filter((b) => b.status === 2).length
      map.set(ward.id, { occupied, total: ward.totalBeds })
    }
    return map
  }, [wards.data, beds.data])

  async function submitWard(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      await wardsApi.create(
        { code: wardForm.code, name: wardForm.name, totalBeds: Number(wardForm.totalBeds) },
        user.userId,
      )
      setWardModalOpen(false)
      setWardForm({ code: '', name: '', totalBeds: '' })
      wards.reload()
    } catch {
      setFormError('Could not create the ward — the code may already be in use.')
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
      await bedsApi.create(
        { wardId: bedForm.wardId, bedNumber: bedForm.bedNumber, status: Number(bedForm.status) },
        user.userId,
      )
      setBedModalOpen(false)
      setBedForm({ wardId: '', bedNumber: '', status: 1 })
      beds.reload()
    } catch {
      setFormError('Could not create the bed — the bed number may already be in use.')
    } finally {
      setSubmitting(false)
    }
  }

  const wardColumns: Column<WardResponse>[] = [
    { header: 'Code', render: (w) => <span className="font-semibold">{w.code}</span> },
    { header: 'Name', render: (w) => w.name },
    { header: 'Beds', render: (w) => `${occupancyByWard.get(w.id)?.occupied ?? 0} / ${w.totalBeds}` },
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
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="Wards & Beds" subtitle="Ward capacity and bed-level status across the unit" />

      <Card className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-[16px] font-semibold text-text">Wards</h2>
          <PrimaryButton onClick={() => setWardModalOpen(true)}>+ New ward</PrimaryButton>
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
          <PrimaryButton onClick={() => setBedModalOpen(true)} disabled={wards.data.length === 0}>
            + New bed
          </PrimaryButton>
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

      <Modal open={wardModalOpen} title="New ward" onClose={() => setWardModalOpen(false)}>
        <form onSubmit={submitWard} className="flex flex-col gap-3.5">
          <Field label="Code (e.g. FW, MW, ICU)">
            <TextInput required value={wardForm.code} onChange={(e) => setWardForm({ ...wardForm, code: e.target.value })} />
          </Field>
          <Field label="Name">
            <TextInput required value={wardForm.name} onChange={(e) => setWardForm({ ...wardForm, name: e.target.value })} />
          </Field>
          <Field label="Total beds">
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
            {submitting ? 'Saving…' : 'Save ward'}
          </PrimaryButton>
        </form>
      </Modal>

      <Modal open={bedModalOpen} title="New bed" onClose={() => setBedModalOpen(false)}>
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
            {submitting ? 'Saving…' : 'Save bed'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
