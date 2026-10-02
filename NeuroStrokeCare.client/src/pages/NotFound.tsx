import { motion } from 'framer-motion'
import { Link } from 'react-router-dom'

// Phase F1 - Frontend Hardening (Section 21, navigation). Previously there was no catch-all
// route at all, so any unmatched URL (a stale bookmark, a typo, a link to a since-removed page)
// rendered a blank page inside the app shell with no indication of what happened. This gives
// that case a clear, on-brand message instead — it is purely a navigation convenience, not a
// security boundary (ProtectedRoute/RequireRole already handle access).
export default function NotFound() {
  return (
    <motion.div
      initial={{ opacity: 0, y: 12 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35, ease: 'easeOut' }}
      className="flex min-h-[60vh] flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-border bg-surface/60 text-center"
    >
      <h1 className="font-display text-[22px] font-semibold text-text">Page not found</h1>
      <p className="max-w-sm text-[14px] text-text-secondary">
        The page you're looking for doesn't exist, or may have moved.
      </p>
      <Link
        to="/"
        className="mt-2 rounded-lg border border-border-subtle px-3.5 py-1.5 text-[13px] font-medium text-accent transition-colors duration-150 hover:bg-accent/10"
      >
        Back to Dashboard
      </Link>
    </motion.div>
  )
}
