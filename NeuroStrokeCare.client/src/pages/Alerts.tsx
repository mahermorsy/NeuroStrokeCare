import { useMemo, useState } from 'react'
import { motion } from 'framer-motion'
import { Link } from 'react-router-dom'
import { loadAlerts, type AlertItem } from '@/lib/alertsApi'
import { useEntityList } from '@/hooks/useEntityList'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card } from '@/components/PageHeader'
import StatusPill from '@/components/StatusPill'

export default function Alerts() {
  const { data, loading, error, reload } = useEntityList(() => loadAlerts())
  const [category, setCategory] = useState('All')

  const categories = useMemo(() => ['All', ...Array.from(new Set(data.map((a) => a.category)))], [data])
  const filtered = category === 'All' ? data : data.filter((a) => a.category === category)
  const criticalCount = data.filter((a) => a.severity === 'critical').length

  const columns: Column<AlertItem>[] = [
    {
      header: 'Severity',
      render: (a) => <StatusPill label={a.severity === 'critical' ? 'Critical' : 'Warning'} tone={a.severity} />,
    },
    { header: 'Category', render: (a) => a.category },
    {
      header: 'Patient',
      render: (a) => (
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-text">{a.patientName}</span>
          <span className="text-[11.5px] text-text-muted">
            {a.hospitalNumber ? `Hospital No. ${a.hospitalNumber}` : 'No hospital number assigned'}
          </span>
        </div>
      ),
    },
    { header: 'Alert', render: (a) => <span className="text-text-secondary">{a.message}</span> },
    { header: 'When', render: (a) => new Date(a.when).toLocaleString() },
    {
      header: '',
      render: (a) => (
        <Link to={`/report/${a.admissionId}`} className="text-[12.5px] font-medium text-accent hover:underline">
          View report
        </Link>
      ),
    },
  ]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader
        title="Alerts"
        subtitle={`${data.length} active alert${data.length === 1 ? '' : 's'} across all admissions${criticalCount ? ` — ${criticalCount} critical` : ''}`}
      />

      <Card className="flex flex-col gap-4">
        <div className="flex flex-wrap gap-2 border-b border-border pb-3">
          {categories.map((c) => (
            <button
              key={c}
              type="button"
              onClick={() => setCategory(c)}
              className={`rounded-full px-3.5 py-1.5 text-[13px] font-semibold transition-colors ${
                c === category ? 'bg-accent text-white' : 'bg-border-soft text-text-secondary hover:bg-border'
              }`}
            >
              {c}
            </button>
          ))}
        </div>
        <DataTable
          columns={columns}
          rows={filtered}
          rowKey={(a) => a.id}
          loading={loading}
          error={error}
          onRetry={reload}
          emptyMessage="No active alerts — everything's within range."
        />
      </Card>
    </motion.div>
  )
}
