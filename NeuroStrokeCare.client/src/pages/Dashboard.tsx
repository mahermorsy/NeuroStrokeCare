import { motion } from 'framer-motion'
import { useCountUp } from '@/hooks/useCountUp'
import { ClipboardPlusIcon, BedIcon, CheckSquareIcon, ClockIcon } from '@/components/icons'

const STATS = [
  {
    key: 'admissions',
    label: 'Active Admissions',
    value: 34,
    suffix: '',
    icon: ClipboardPlusIcon,
    iconBg: 'bg-accent/10',
    iconColor: 'text-accent',
    note: '↑ 4 since yesterday',
    noteColor: 'text-success',
  },
  {
    key: 'beds',
    label: 'Bed Availability',
    value: 18,
    suffix: ' / 96',
    icon: BedIcon,
    iconBg: 'bg-blue/10',
    iconColor: 'text-blue',
    note: 'open across 6 wards',
    noteColor: 'text-text-muted',
  },
  {
    key: 'assessment',
    label: 'In Assessment',
    value: 9,
    suffix: '',
    icon: CheckSquareIcon,
    iconBg: 'bg-warning/15',
    iconColor: 'text-warning',
    note: '3 pending > 30 min',
    noteColor: 'text-warning',
  },
  {
    key: 'dtn',
    label: 'Avg Door-to-Needle',
    value: 42,
    suffix: ' min',
    icon: ClockIcon,
    iconBg: 'bg-success/10',
    iconColor: 'text-success',
    note: 'Target: ≤ 60 min',
    noteColor: 'text-success',
  },
]

const ADMISSIONS = [
  { name: 'Ahmed El-Sayed', location: 'Ward A · Bed 3', time: '07:12 AM', nihss: 14, status: 'Critical' },
  { name: 'Mona Abdel Rahman', location: 'Ward B · Bed 7', time: '08:45 AM', nihss: 6, status: 'Stable' },
  { name: 'Youssef Mahmoud', location: 'ICU · Bed 2', time: '09:20 AM', nihss: 20, status: 'Critical' },
  { name: 'Sara Ibrahim', location: 'Ward A · Bed 9', time: '10:05 AM', nihss: 4, status: 'Stable' },
  { name: 'Khaled Fathy', location: 'Ward C · Bed 1', time: '11:30 AM', nihss: 9, status: 'Observation' },
] as const

const STATUS_STYLES: Record<string, string> = {
  Critical: 'bg-critical-bg text-critical',
  Stable: 'bg-success-bg text-success',
  Observation: 'bg-warning-bg text-warning',
}

const ASSESSMENTS = [
  { name: 'Youssef Mahmoud', type: 'NIHSS reassessment', due: '38 min overdue', tone: 'critical' },
  { name: 'Sara Ibrahim', type: 'Braden Scale', due: '12 min waiting', tone: 'warning' },
  { name: 'Ahmed El-Sayed', type: 'GCS check', due: '5 min waiting', tone: 'warning' },
  { name: 'Mona Abdel Rahman', type: 'Morse Fall Scale', due: 'Due in 20 min', tone: 'info' },
] as const

const ASSESSMENT_TONE_STYLES: Record<string, { bg: string; text: string }> = {
  critical: { bg: 'bg-critical-bg', text: 'text-critical' },
  warning: { bg: 'bg-warning-bg', text: 'text-warning' },
  info: { bg: 'bg-info-bg', text: 'text-blue' },
}

const WARDS = [
  { name: 'Ward A', occupied: 24, total: 28, pct: 86 },
  { name: 'Ward B', occupied: 19, total: 24, pct: 79 },
  { name: 'Ward C', occupied: 12, total: 20, pct: 60 },
  { name: 'ICU', occupied: 8, total: 10, pct: 80 },
  { name: 'Stroke Unit', occupied: 16, total: 16, pct: 100 },
  { name: 'Rehab', occupied: 9, total: 18, pct: 50 },
] as const

function occupancyColor(pct: number) {
  if (pct >= 95) return 'bg-critical'
  if (pct >= 70) return 'bg-warning'
  return 'bg-success'
}

const fadeUp = {
  hidden: { opacity: 0, y: 14 },
  show: { opacity: 1, y: 0 },
}

const stagger = {
  hidden: {},
  show: {
    transition: { staggerChildren: 0.06 },
  },
}

function StatTile({ stat }: { stat: (typeof STATS)[number] }) {
  const Icon = stat.icon
  const count = useCountUp(stat.value)
  return (
    <motion.div
      variants={fadeUp}
      transition={{ duration: 0.4, ease: 'easeOut' }}
      className="flex flex-col gap-3.5 rounded-2xl border border-border bg-surface p-5 shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
    >
      <div className="flex items-center gap-2.5">
        <span className={`flex h-[38px] w-[38px] items-center justify-center rounded-[10px] ${stat.iconBg}`}>
          <Icon className={`h-[19px] w-[19px] ${stat.iconColor}`} />
        </span>
        <span className="text-[12px] font-semibold uppercase tracking-wide text-text-secondary">
          {stat.label}
        </span>
      </div>
      <span className="text-[32px] font-bold leading-none text-text">
        {count}
        <span className="text-[18px] font-medium text-text-muted">{stat.suffix}</span>
      </span>
      <span className={`text-[12.5px] font-semibold ${stat.noteColor}`}>{stat.note}</span>
    </motion.div>
  )
}

export default function Dashboard() {
  return (
    <motion.div
      variants={stagger}
      initial="hidden"
      animate="show"
      className="flex flex-col gap-6"
    >
      <motion.div
        variants={fadeUp}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        className="flex flex-wrap items-start justify-between gap-5"
      >
        <div>
          <h1 className="font-display text-[22px] font-semibold text-text sm:text-[28px]">Dashboard</h1>
          <p className="mt-1.5 text-[14px] text-text-secondary">
            Stroke unit overview ·{' '}
            {new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' })}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <span className="flex items-center gap-1.5 rounded-full border border-border bg-surface px-3.5 py-2 text-[13px] text-text-secondary">
            <span className="h-2 w-2 rounded-full bg-success" />
            Live data
          </span>
          <button
            type="button"
            className="rounded-lg border border-border bg-surface px-4 py-2.5 text-[13.5px] font-semibold text-text"
          >
            Export report
          </button>
          <button
            type="button"
            className="rounded-lg bg-accent px-4 py-2.5 text-[13.5px] font-semibold text-white"
          >
            + New admission
          </button>
        </div>
      </motion.div>

      <div className="grid grid-cols-1 gap-[18px] sm:grid-cols-2 xl:grid-cols-4">
        {STATS.map((stat) => (
          <StatTile key={stat.key} stat={stat} />
        ))}
      </div>

      <div className="grid grid-cols-1 gap-[18px] xl:grid-cols-[1.7fr_1fr]">
        <motion.div
          variants={fadeUp}
          transition={{ duration: 0.4, ease: 'easeOut' }}
          className="flex flex-col gap-4 rounded-2xl border border-border bg-surface p-6 shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
        >
          <div className="flex items-center justify-between">
            <h2 className="text-[16px] font-semibold text-text">Active Admissions</h2>
            <a href="#" className="text-[13px] font-semibold text-accent hover:underline">
              View all
            </a>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full min-w-[560px] border-collapse">
              <thead>
                <tr>
                  {['Patient', 'Ward / Bed', 'Admitted', 'NIHSS', 'Status'].map((h) => (
                    <th
                      key={h}
                      className="border-b border-border px-2.5 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-text-muted"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {ADMISSIONS.map((row, i) => (
                  <tr key={row.name} className={i < ADMISSIONS.length - 1 ? 'border-b border-border-soft' : ''}>
                    <td className="px-2.5 py-3.5 text-[14px] font-semibold text-text">{row.name}</td>
                    <td className="px-2.5 py-3.5 text-[13.5px] text-text-secondary">{row.location}</td>
                    <td className="px-2.5 py-3.5 text-[13.5px] text-text-secondary">{row.time}</td>
                    <td className="px-2.5 py-3.5 text-[13.5px] text-text-secondary">{row.nihss}</td>
                    <td className="px-2.5 py-3.5">
                      <span
                        className={`rounded-full px-2.5 py-1 text-[12px] font-semibold ${STATUS_STYLES[row.status]}`}
                      >
                        {row.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </motion.div>

        <motion.div
          variants={fadeUp}
          transition={{ duration: 0.4, ease: 'easeOut' }}
          className="flex flex-col gap-3.5 rounded-2xl border border-border bg-surface p-[22px] shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
        >
          <div className="flex items-center justify-between">
            <h2 className="text-[16px] font-semibold text-text">Pending Assessments</h2>
            <span className="rounded-full bg-critical-bg px-2.5 py-1 text-[11.5px] font-bold text-critical">
              3 urgent
            </span>
          </div>
          <div className="flex flex-col gap-2.5">
            {ASSESSMENTS.map((item) => {
              const tone = ASSESSMENT_TONE_STYLES[item.tone]
              return (
                <div
                  key={item.name}
                  className={`flex items-center justify-between gap-2.5 rounded-[10px] px-3.5 py-3 ${tone.bg}`}
                >
                  <div className="flex flex-col gap-0.5">
                    <span className="text-[13.5px] font-semibold text-text">{item.name}</span>
                    <span className="text-[12px] text-text-secondary">{item.type}</span>
                  </div>
                  <span className={`whitespace-nowrap text-[12px] font-bold ${tone.text}`}>{item.due}</span>
                </div>
              )
            })}
          </div>
        </motion.div>
      </div>

      <motion.div
        variants={fadeUp}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        className="flex flex-col gap-4 rounded-2xl border border-border bg-surface p-6 shadow-[0_1px_2px_rgba(16,40,45,0.04)]"
      >
        <div className="flex items-center justify-between">
          <h2 className="text-[16px] font-semibold text-text">Wards & Beds Occupancy</h2>
          <a href="#" className="text-[13px] font-semibold text-accent hover:underline">
            Manage wards
          </a>
        </div>
        <div className="grid grid-cols-2 gap-3.5 sm:grid-cols-3 xl:grid-cols-6">
          {WARDS.map((ward) => (
            <div key={ward.name} className="flex flex-col gap-2.5 rounded-xl border border-border p-4">
              <div className="flex items-baseline justify-between">
                <span className="text-[13.5px] font-semibold text-text">{ward.name}</span>
                <span className="text-[12px] text-text-muted">
                  {ward.occupied}/{ward.total}
                </span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-border-soft">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${ward.pct}%` }}
                  transition={{ duration: 0.7, ease: 'easeOut', delay: 0.15 }}
                  className={`h-full rounded-full ${occupancyColor(ward.pct)}`}
                />
              </div>
              <span className="text-[11.5px] text-text-muted">{ward.pct}% occupied</span>
            </div>
          ))}
        </div>
      </motion.div>
    </motion.div>
  )
}
