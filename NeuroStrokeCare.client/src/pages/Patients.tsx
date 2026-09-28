import { useMemo, useState, type FormEvent } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import type { PatientResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import Modal from '@/components/Modal'
import { Field, TextInput, Select, TextArea } from '@/components/FormField'
import { GENDER_OPTIONS } from '@/lib/enums'

const patientsApi = entityApi<PatientResponse>('Patient')

function age(dateOfBirth: string) {
  const dob = new Date(dateOfBirth)
  if (Number.isNaN(dob.getTime())) return '—'
  const diff = Date.now() - dob.getTime()
  return Math.max(0, Math.floor(diff / (365.25 * 24 * 3600 * 1000)))
}

const emptyForm = {
  nationalId: '',
  firstName: '',
  middleName: '',
  lastName: '',
  dateOfBirth: '',
  gender: 'Male',
  weightKg: '',
  chiefComplaint: '',
}

export default function Patients() {
  const { user } = useAuth()
  const { data, loading, error, reload } = useEntityList(() => patientsApi.list())
  const [modalOpen, setModalOpen] = useState(false)
  const [form, setForm] = useState(emptyForm)
  const [submitting, setSubmitting] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)
  const [search, setSearch] = useState('')

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return data
    return data.filter((p) =>
      `${p.firstName} ${p.middleName} ${p.lastName} ${p.nationalId ?? ''}`.toLowerCase().includes(q),
    )
  }, [data, search])

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    if (!user?.userId) return
    setSubmitting(true)
    setFormError(null)
    try {
      await patientsApi.create(
        {
          nationalId: form.nationalId || null,
          firstName: form.firstName,
          middleName: form.middleName,
          lastName: form.lastName,
          dateOfBirth: form.dateOfBirth,
          gender: form.gender,
          weightKg: Number(form.weightKg),
          chiefComplaint: form.chiefComplaint,
        },
        user.userId,
      )
      setModalOpen(false)
      setForm(emptyForm)
      reload()
    } catch (err) {
      const message =
        (err as { response?: { data?: { errors?: string[]; title?: string } } })?.response?.data
      setFormError(
        (Array.isArray(message?.errors) && message.errors.join(', ')) ||
          message?.title ||
          'Could not create the patient — check the fields and try again.',
      )
    } finally {
      setSubmitting(false)
    }
  }

  const columns: Column<PatientResponse>[] = [
    { header: 'Name', render: (p) => `${p.firstName} ${p.middleName} ${p.lastName}` },
    { header: 'National ID', render: (p) => p.nationalId ?? '—' },
    { header: 'Age', render: (p) => age(p.dateOfBirth) },
    { header: 'Gender', render: (p) => p.gender },
    { header: 'Weight (kg)', render: (p) => p.weightKg },
    { header: 'Chief Complaint', render: (p) => <span className="text-text-secondary">{p.chiefComplaint}</span> },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Patients"
        subtitle={`${data.length} patient${data.length === 1 ? '' : 's'} on record`}
        action={<PrimaryButton onClick={() => setModalOpen(true)}>+ New patient</PrimaryButton>}
      />

      <Card className="flex flex-col gap-4">
        <TextInput
          placeholder="Search by name or national ID…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-[320px]"
        />
        <DataTable
          columns={columns}
          rows={filtered}
          rowKey={(p) => p.id}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyMessage="No patients yet — add the first one."
        />
      </Card>

      <Modal open={modalOpen} title="New patient" onClose={() => setModalOpen(false)}>
        <form onSubmit={handleSubmit} className="flex flex-col gap-3.5">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="First name">
              <TextInput
                required
                value={form.firstName}
                onChange={(e) => setForm({ ...form, firstName: e.target.value })}
              />
            </Field>
            <Field label="Middle name">
              <TextInput
                required
                value={form.middleName}
                onChange={(e) => setForm({ ...form, middleName: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Last name">
            <TextInput
              required
              value={form.lastName}
              onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            />
          </Field>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Date of birth">
              <TextInput
                type="date"
                required
                value={form.dateOfBirth}
                onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
              />
            </Field>
            <Field label="Gender">
              <Select value={form.gender} onChange={(e) => setForm({ ...form, gender: e.target.value })}>
                {GENDER_OPTIONS.map((g) => (
                  <option key={g} value={g}>
                    {g}
                  </option>
                ))}
              </Select>
            </Field>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Field label="Weight (kg)">
              <TextInput
                type="number"
                step="0.1"
                min="0.5"
                required
                value={form.weightKg}
                onChange={(e) => setForm({ ...form, weightKg: e.target.value })}
              />
            </Field>
            <Field label="National ID (optional)">
              <TextInput
                value={form.nationalId}
                onChange={(e) => setForm({ ...form, nationalId: e.target.value })}
              />
            </Field>
          </div>
          <Field label="Chief complaint">
            <TextArea
              required
              value={form.chiefComplaint}
              onChange={(e) => setForm({ ...form, chiefComplaint: e.target.value })}
            />
          </Field>

          {formError && (
            <p className="rounded-lg bg-critical-bg px-3 py-2 text-[13px] font-medium text-critical">{formError}</p>
          )}

          <PrimaryButton type="submit" disabled={submitting} className="mt-1">
            {submitting ? 'Saving…' : 'Save patient'}
          </PrimaryButton>
        </form>
      </Modal>
    </motion.div>
  )
}
