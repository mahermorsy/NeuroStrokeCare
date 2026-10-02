import { useEffect, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { roleLabel } from '@/lib/roles'
import { ChevronDownIcon, EditIcon, LockIcon, LogoutIcon, UserCircleIcon } from '@/components/icons'

function initials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return '?'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
}

interface MenuAction {
  key: string
  label: string
  icon: typeof UserCircleIcon
  onSelect: () => void
  tone?: 'default' | 'danger'
}

/**
 * The account block pinned to the bottom of the sidebar — a compact
 * [avatar][name+role][chevron] trigger that opens a small popover menu
 * upward (there's no room below it). Self-contained: it manages its own
 * open/close state, closes on an outside click, Escape, or a route change,
 * and supports arrow-key navigation between items like a native menu.
 *
 * User/profile data comes straight from AuthContext — it's loaded centrally
 * there once per sign-in, so this component never fetches anything itself.
 */
export default function UserMenu() {
  const { user, profile, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [open, setOpen] = useState(false)

  const rootRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const itemRefs = useRef<Array<HTMLButtonElement | null>>([])

  const displayName = profile ? `${profile.firstName} ${profile.lastName}`.trim() : user?.userName ?? 'Signed-in clinician'
  const displayRole = roleLabel(profile?.role ?? user?.role)
  const photoUrl = profile?.profilePhotoUrl ?? null

  // Outside click + Escape close the menu; a route change (picking an item
  // that navigates) closes it too, same as the mobile drawer's own effect.
  useEffect(() => {
    if (!open) return
    function onPointerDown(e: MouseEvent) {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) setOpen(false)
    }
    function onKeyDown(e: globalThis.KeyboardEvent) {
      if (e.key === 'Escape') {
        setOpen(false)
        triggerRef.current?.focus()
      }
    }
    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  useEffect(() => {
    setOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (open) {
      const frame = requestAnimationFrame(() => itemRefs.current[0]?.focus())
      return () => cancelAnimationFrame(frame)
    }
  }, [open])

  function go(path: string) {
    setOpen(false)
    navigate(path)
  }

  const actions: MenuAction[] = [
    { key: 'profile', label: 'My Profile', icon: UserCircleIcon, onSelect: () => go('/id-card') },
    { key: 'edit', label: 'Edit Profile', icon: EditIcon, onSelect: () => go('/id-card') },
    { key: 'password', label: 'Change Password', icon: LockIcon, onSelect: () => go('/account/change-password') },
  ]

  function handleMenuKeyDown(e: ReactKeyboardEvent) {
    const items = itemRefs.current.filter((el): el is HTMLButtonElement => el !== null)
    const currentIndex = items.findIndex((el) => el === document.activeElement)
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      items[(currentIndex + 1 + items.length) % items.length]?.focus()
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      items[(currentIndex - 1 + items.length) % items.length]?.focus()
    } else if (e.key === 'Home') {
      e.preventDefault()
      items[0]?.focus()
    } else if (e.key === 'End') {
      e.preventDefault()
      items[items.length - 1]?.focus()
    }
  }

  function handleTriggerKeyDown(e: ReactKeyboardEvent) {
    if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
      e.preventDefault()
      setOpen(true)
    }
  }

  return (
    <div ref={rootRef} className="relative">
      <button
        ref={triggerRef}
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen((o) => !o)}
        onKeyDown={handleTriggerKeyDown}
        className="flex w-full items-center gap-2.5 rounded-xl px-2.5 py-2 text-left transition-colors duration-150 hover:bg-white/[0.06] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gold/50 active:bg-white/[0.1]"
      >
        <span className="flex h-9 w-9 flex-shrink-0 items-center justify-center overflow-hidden rounded-full border-[1.5px] border-gold/50 bg-accent">
          {photoUrl ? (
            <img src={photoUrl} alt="" className="h-full w-full object-cover" />
          ) : (
            <span className="text-[12.5px] font-semibold text-gold">{initials(displayName)}</span>
          )}
        </span>
        <span className="flex min-w-0 flex-grow flex-col leading-tight">
          <span className="truncate text-[13px] font-semibold text-white">{displayName}</span>
          <span className="truncate text-[11px] font-normal text-sidebar-muted">{displayRole}</span>
        </span>
        <ChevronDownIcon
          className={`h-4 w-4 flex-shrink-0 text-sidebar-muted transition-transform duration-200 ${open ? 'rotate-180' : ''}`}
        />
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            initial={{ opacity: 0, y: 6, scale: 0.98 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: 6, scale: 0.98 }}
            transition={{ duration: 0.16, ease: 'easeOut' }}
            role="menu"
            aria-label="Account menu"
            onKeyDown={handleMenuKeyDown}
            className="absolute inset-x-0 bottom-full z-50 mb-2 overflow-hidden rounded-xl border border-white/10 bg-[#102444] py-1.5 shadow-[0_10px_28px_rgba(0,0,0,0.4)]"
          >
            {actions.map((action, i) => (
              <button
                key={action.key}
                ref={(el) => {
                  itemRefs.current[i] = el
                }}
                type="button"
                role="menuitem"
                tabIndex={-1}
                onClick={action.onSelect}
                className="flex w-full items-center gap-2.5 px-3.5 py-2.5 text-left text-[13px] font-medium text-sidebar-text outline-none transition-colors duration-150 hover:bg-white/[0.07] hover:text-white focus-visible:bg-white/[0.07] focus-visible:text-white"
              >
                <action.icon className="h-[16px] w-[16px] flex-shrink-0" />
                {action.label}
              </button>
            ))}

            <div className="my-1 border-t border-white/10" />

            <button
              ref={(el) => {
                itemRefs.current[actions.length] = el
              }}
              type="button"
              role="menuitem"
              tabIndex={-1}
              onClick={() => {
                setOpen(false)
                logout()
              }}
              className="flex w-full items-center gap-2.5 px-3.5 py-2.5 text-left text-[13px] font-medium text-[#e2948f] outline-none transition-colors duration-150 hover:bg-[rgba(176,58,58,0.15)] focus-visible:bg-[rgba(176,58,58,0.15)]"
            >
              <LogoutIcon className="h-[16px] w-[16px] flex-shrink-0" />
              Sign out
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
