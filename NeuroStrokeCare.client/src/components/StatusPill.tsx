type Tone = 'success' | 'warning' | 'critical' | 'info' | 'neutral'

const TONE_STYLES: Record<Tone, string> = {
  success: 'bg-success-bg text-success',
  warning: 'bg-warning-bg text-warning',
  critical: 'bg-critical-bg text-critical',
  info: 'bg-info-bg text-blue',
  neutral: 'bg-border-soft text-text-secondary',
}

export default function StatusPill({ label, tone = 'neutral' }: { label: string; tone?: Tone }) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-1 text-[12px] font-semibold ${TONE_STYLES[tone]}`}>
      {label}
    </span>
  )
}
