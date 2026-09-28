import { useState } from 'react'
import { motion } from 'framer-motion'
import { entityApi } from '@/lib/entityApi'
import { useEntityList } from '@/hooks/useEntityList'
import { useAdmissionContext } from '@/hooks/useAdmissionContext'
import type {
  NIHSSAssessmentResponse,
  ASPECTSAssessmentResponse,
  ICHAssessmentResponse,
  CanadianTIAAssessmentResponse,
  BradenAssessmentResponse,
  GCSAssessmentResponse,
  GUSSAssessmentResponse,
  MorseAssessmentResponse,
} from '@/types/entities'
import DataTable, { type Column } from '@/components/DataTable'
import PageHeader, { Card } from '@/components/PageHeader'
import StatusPill from '@/components/StatusPill'
import { BRADEN_RISK_LEVEL, GCS_SEVERITY, GUSS_SEVERITY, MORSE_RISK_LEVEL, riskTone } from '@/lib/enums'

interface Row {
  id: string
  admissionId: string
  assessedAt: string
  totalScore: number
}

interface TabDef {
  key: string
  label: string
  fetch: () => Promise<Row[]>
  scoreTone: (row: Row) => 'success' | 'warning' | 'critical' | 'info'
  scoreLabel: (row: Row) => string
}

const nihss = entityApi<NIHSSAssessmentResponse>('NIHSSAssessment')
const aspects = entityApi<ASPECTSAssessmentResponse>('ASPECTSAssessment')
const ich = entityApi<ICHAssessmentResponse>('ICHAssessment')
const canadianTia = entityApi<CanadianTIAAssessmentResponse>('CanadianTIAAssessment')
const braden = entityApi<BradenAssessmentResponse>('BradenAssessment')
const gcs = entityApi<GCSAssessmentResponse>('GCSAssessment')
const guss = entityApi<GUSSAssessmentResponse>('GUSSAssessment')
const morse = entityApi<MorseAssessmentResponse>('MorseAssessment')

function nihssTone(score: number): 'success' | 'warning' | 'critical' | 'info' {
  if (score === 0) return 'success'
  if (score <= 4) return 'info'
  if (score <= 15) return 'warning'
  return 'critical'
}

const TABS: TabDef[] = [
  {
    key: 'nihss',
    label: 'NIHSS',
    fetch: () => nihss.list(),
    scoreTone: (r) => nihssTone(r.totalScore),
    scoreLabel: (r) => (r as unknown as NIHSSAssessmentResponse).severity,
  },
  {
    key: 'aspects',
    label: 'ASPECTS',
    fetch: () => aspects.list(),
    scoreTone: (r) => (r.totalScore >= 8 ? 'success' : r.totalScore >= 6 ? 'warning' : 'critical'),
    scoreLabel: (r) => `${r.totalScore} / 10`,
  },
  {
    key: 'ich',
    label: 'ICH Score',
    fetch: () => ich.list(),
    scoreTone: (r) => (r.totalScore <= 1 ? 'success' : r.totalScore <= 3 ? 'warning' : 'critical'),
    scoreLabel: (r) => String(r.totalScore),
  },
  {
    key: 'tia',
    label: 'Canadian TIA',
    fetch: () => canadianTia.list(),
    scoreTone: (r) => riskTone((r as unknown as CanadianTIAAssessmentResponse).riskLevel),
    scoreLabel: (r) => (r as unknown as CanadianTIAAssessmentResponse).riskLevel,
  },
  {
    key: 'braden',
    label: 'Braden Scale',
    fetch: () => braden.list(),
    scoreTone: (r) => riskTone(BRADEN_RISK_LEVEL[(r as unknown as BradenAssessmentResponse).riskLevel] ?? ''),
    scoreLabel: (r) => BRADEN_RISK_LEVEL[(r as unknown as BradenAssessmentResponse).riskLevel] ?? '—',
  },
  {
    key: 'gcs',
    label: 'GCS',
    fetch: () => gcs.list(),
    scoreTone: (r) => riskTone(GCS_SEVERITY[(r as unknown as GCSAssessmentResponse).severity] ?? ''),
    scoreLabel: (r) => GCS_SEVERITY[(r as unknown as GCSAssessmentResponse).severity] ?? '—',
  },
  {
    key: 'guss',
    label: 'GUSS (Swallow)',
    fetch: () => guss.list(),
    scoreTone: (r) => riskTone(GUSS_SEVERITY[(r as unknown as GUSSAssessmentResponse).severity] ?? ''),
    scoreLabel: (r) => GUSS_SEVERITY[(r as unknown as GUSSAssessmentResponse).severity] ?? '—',
  },
  {
    key: 'morse',
    label: 'Morse Fall Scale',
    fetch: () => morse.list(),
    scoreTone: (r) => riskTone(MORSE_RISK_LEVEL[(r as unknown as MorseAssessmentResponse).riskLevel] ?? ''),
    scoreLabel: (r) => MORSE_RISK_LEVEL[(r as unknown as MorseAssessmentResponse).riskLevel] ?? '—',
  },
]

function AssessmentTable({ tab }: { tab: TabDef }) {
  const { data, loading, error, reload } = useEntityList<Row>(() => tab.fetch() as Promise<Row[]>, [tab.key])
  const { patientNameByAdmissionId, loading: contextLoading } = useAdmissionContext()

  const columns: Column<Row>[] = [
    { header: 'Patient', render: (r) => patientNameByAdmissionId.get(r.admissionId) ?? '—' },
    { header: 'Assessed at', render: (r) => new Date(r.assessedAt).toLocaleString() },
    { header: 'Total score', render: (r) => r.totalScore },
    { header: 'Result', render: (r) => <StatusPill label={tab.scoreLabel(r)} tone={tab.scoreTone(r)} /> },
  ]

  return (
    <DataTable
      columns={columns}
      rows={data}
      rowKey={(r) => r.id}
      loading={loading || contextLoading}
      error={error}
      onRetry={reload}
      emptyMessage={`No ${tab.label} assessments recorded yet.`}
    />
  )
}

export default function Assessments() {
  const [active, setActive] = useState(TABS[0].key)
  const activeTab = TABS.find((t) => t.key === active) ?? TABS[0]

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="Assessments" subtitle="Clinical scoring tools, grouped by type" />

      <Card className="flex flex-col gap-5">
        <div className="flex flex-wrap gap-2 border-b border-border pb-3">
          {TABS.map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActive(tab.key)}
              className={`rounded-full px-3.5 py-1.5 text-[13px] font-semibold transition-colors ${
                tab.key === active ? 'bg-accent text-white' : 'bg-border-soft text-text-secondary hover:bg-border'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
        <AssessmentTable key={activeTab.key} tab={activeTab} />
      </Card>
    </motion.div>
  )
}
