import { useEffect, useRef, type ReactNode } from 'react'
import { motion, AnimatePresence } from 'framer-motion'

export default function Modal({
  open,
  title,
  onClose,
  children,
  size = 'default',
}: {
  open: boolean
  title: string
  onClose: () => void
  children: ReactNode
  /** 'wide' is for forms with many fields (e.g. clinical scoring scales) that feel cramped at the
   *  default width. Omitting it keeps every existing modal exactly as wide as it was before. */
  size?: 'default' | 'wide'
}) {
  const dialogRef = useRef<HTMLDivElement>(null)
  const previouslyFocusedRef = useRef<HTMLElement | null>(null)
  // Phase F1 - Frontend Hardening (Section 20, accessibility). Modal is used by nearly every
  // page in the app, so this one fix (role/aria, Escape-to-close, and basic focus management)
  // has an unusually wide blast radius. `onClose` is kept out of the effect's dependency array
  // on purpose: most call sites pass a fresh inline arrow function on every render, and
  // depending on it here would tear down and re-run this effect (re-stealing focus into the
  // dialog) on every keystroke inside a form the modal contains. A ref always gives the Escape
  // handler the latest `onClose` without that problem.
  const onCloseRef = useRef(onClose)
  useEffect(() => {
    onCloseRef.current = onClose
  }, [onClose])

  useEffect(() => {
    if (!open) return
    previouslyFocusedRef.current = document.activeElement as HTMLElement | null
    const frame = requestAnimationFrame(() => dialogRef.current?.focus())
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') onCloseRef.current()
    }
    document.addEventListener('keydown', handleKeyDown)
    return () => {
      cancelAnimationFrame(frame)
      document.removeEventListener('keydown', handleKeyDown)
      previouslyFocusedRef.current?.focus?.()
    }
  }, [open])

  return (
    <AnimatePresence>
      {open && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          transition={{ duration: 0.15 }}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/30 px-4"
          onClick={onClose}
        >
          <motion.div
            ref={dialogRef}
            role="dialog"
            aria-modal="true"
            aria-label={title}
            tabIndex={-1}
            initial={{ opacity: 0, y: 10, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 10, scale: 0.98 }}
            transition={{ duration: 0.18, ease: 'easeOut' }}
            onClick={(e) => e.stopPropagation()}
            className={`max-h-[85vh] w-full overflow-y-auto rounded-2xl bg-surface p-4 shadow-lg outline-none sm:p-6 ${
              size === 'wide' ? 'max-w-[720px]' : 'max-w-[520px]'
            }`}
          >
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-[17px] font-semibold text-text">{title}</h2>
              <button
                type="button"
                onClick={onClose}
                aria-label="Close"
                className="rounded-md px-2 py-1 text-text-muted hover:bg-border-soft hover:text-text"
              >
                ✕
              </button>
            </div>
            {children}
          </motion.div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
