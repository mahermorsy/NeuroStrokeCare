import { motion } from 'framer-motion'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import { useAdmissionContext } from '@/hooks/useAdmissionContext'
import type { LabResultsResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card } from '@/components/PageHeader'
import StatusPill from '@/components/StatusPill'

const labResultsApi = entityApi<LabResultsResponse>('LabResults')

function value(v: number | null, unit = '') {
  return v === null || v === undefined ? '—' : `${v}${unit}`
}

export default function LabResults() {
  const { data, loading, error, reload } = useEntityList(() => labResultsApi.list())
  const { patientNameByAdmissionId, loading: contextLoading } = useAdmissionContext()

  const columns: Column<LabResultsResponse>[] = [
    { header: 'Patient', render: (r) => patientNameByAdmissionId.get(r.admissionId) ?? '—' },
    { header: 'Recorded at', render: (r) => new Date(r.recordedAt).toLocaleString() },
    { header: 'Glucose', render: (r) => value(r.glucoseMmol, ' mmol/L') },
    { header: 'INR', render: (r) => value(r.inr) },
    { header: 'PT', render: (r) => value(r.pt) },
    { header: 'Platelets', render: (r) => value(r.platelets) },
    { header: 'Sodium', render: (r) => value(r.sodium) },
    { header: 'Potassium', render: (r) => value(r.potassium) },
    { header: 'Creatinine', render: (r) => value(r.creatinine) },
    { header: 'Hemoglobin', render: (r) => value(r.hemoglobin) },
    { header: 'LDL', render: (r) => value(r.ldl) },
    { header: 'HbA1c', render: (r) => value(r.hbA1c) },
    { header: 'aPTT', render: (r) => value(r.aPTT) },
    { header: 'ALT', render: (r) => value(r.alt) },
    { header: 'AST', render: (r) => value(r.ast) },
    {
      header: 'AFib (ECG)',
      render: (r) => (r.ecgAtrialFibrillation === null ? '—' : r.ecgAtrialFibrillation ? 'Yes' : 'No'),
    },
    {
      header: 'Alerts',
      render: (r) => {
        const alerts = [
          r.inrAlert && 'INR',
          r.glucoseAlert && 'Glucose',
          r.plateletsAlert && 'Platelets',
        ].filter(Boolean) as string[]
        if (alerts.length === 0) return <StatusPill label="None" tone="success" />
        return (
          <div className="flex flex-wrap gap-1">
            {alerts.map((a) => (
              <StatusPill key={a} label={a} tone="critical" />
            ))}
          </div>
        )
      },
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="Lab Results" subtitle="Bloodwork and coagulation panels recorded per admission" />

      <Card>
        <DataTable
          columns={columns}
          rows={data}
          rowKey={(r) => r.id}
          loading={loading || contextLoading}
          error={error}
          onRetry={reload}
          emptyMessage="No lab results recorded yet."
        />
      </Card>
    </motion.div>
  )
}
