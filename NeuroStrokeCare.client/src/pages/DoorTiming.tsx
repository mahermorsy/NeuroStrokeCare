import { motion } from 'framer-motion'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import { useAdmissionContext } from '@/hooks/useAdmissionContext'
import type { DoorTimingResponse } from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card } from '@/components/PageHeader'
import StatusPill from '@/components/StatusPill'

const doorTimingApi = entityApi<DoorTimingResponse>('DoorTiming')

function minutes(v: number | null) {
  return v === null || v === undefined ? '—' : `${v} min`
}

export default function DoorTiming() {
  const { data, loading, error, reload } = useEntityList(() => doorTimingApi.list())
  const { patientNameByAdmissionId, loading: contextLoading } = useAdmissionContext()

  const columns: Column<DoorTimingResponse>[] = [
    { header: 'Patient', render: (r) => patientNameByAdmissionId.get(r.admissionId) ?? '—' },
    { header: 'ER arrival', render: (r) => new Date(r.er_StrokeArrival).toLocaleString() },
    {
      header: 'Door-to-CT',
      render: (r) => (
        <div className="flex items-center gap-1.5">
          <span>{minutes(r.minutesToCT)}</span>
          {r.ctDelayed && <StatusPill label="Delayed" tone="critical" />}
        </div>
      ),
    },
    {
      header: 'Door-to-needle',
      render: (r) => (
        <div className="flex items-center gap-1.5">
          <span>{minutes(r.minutesToNeedle)}</span>
          {r.needleDelayed && <StatusPill label="Delayed" tone="critical" />}
        </div>
      ),
    },
    { header: 'Door-to-groin', render: (r) => minutes(r.minutesToGroin) },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="Door Timing" subtitle="Stroke response time benchmarks from ER arrival" />

      <Card>
        <DataTable
          columns={columns}
          rows={data}
          rowKey={(r) => r.id}
          loading={loading || contextLoading}
          error={error}
          onRetry={reload}
          emptyMessage="No door-timing records yet."
        />
      </Card>
    </motion.div>
  )
}
