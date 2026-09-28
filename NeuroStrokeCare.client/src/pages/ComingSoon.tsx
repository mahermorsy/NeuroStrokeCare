import { motion } from 'framer-motion'

export default function ComingSoon({ title }: { title: string }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      className="flex min-h-[60vh] flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-border bg-surface/60 text-center"
    >
      <h1 className="font-display text-[22px] font-semibold text-text">{title}</h1>
      <p className="max-w-sm text-[14px] text-text-secondary">
        This screen is next on the design list — the Dashboard came first for review. Let me know
        when to design {title} next.
      </p>
    </motion.div>
  )
}
