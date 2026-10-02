import type { ReactNode } from 'react'
import { motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'

// Phase F1 - Frontend Hardening (Section 4/21). The Staff/Users page already has no nav entry
// for non-Admin roles, but nothing stopped a non-Admin who typed/bookmarked /users directly from
// landing on a blank-feeling page while the actual data calls behind it failed with 403s. This
// gives that case a clear, immediate message instead.
//
// This is a UX helper only, exactly like the rest of the frontend's role handling - it never
// replaces backend authorization. The real enforcement is the backend's own role checks on the
// underlying endpoints (Phase 8); a user who reached this past some future bug in `roles` would
// still be rejected by the API itself.
export default function RequireRole({ roles, children }: { roles: string[]; children: ReactNode }) {
  const { user } = useAuth()

  if (!user?.role || !roles.includes(user.role)) {
    return (
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: 'easeOut' }}
        className="flex min-h-[60vh] flex-col items-center justify-center gap-2 rounded-2xl border border-dashed border-border bg-surface/60 text-center"
      >
        <h1 className="font-display text-[22px] font-semibold text-text">Access restricted</h1>
        <p className="max-w-sm text-[14px] text-text-secondary">
          This page is only available to Admin accounts. If you believe you should have access, contact your
          system administrator.
        </p>
      </motion.div>
    )
  }

  return <>{children}</>
}
