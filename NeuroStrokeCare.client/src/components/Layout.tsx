import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
import { loadAlerts } from '@/lib/alertsApi'
import UserMenu from '@/components/UserMenu'
import PatientContextHeader from '@/components/PatientContextHeader'
import type { ReactElement, SVGProps } from 'react'
import {
  GridIcon,
  AlertIcon,
  UserIcon,
  ClipboardPlusIcon,
  BedIcon,
  CheckSquareIcon,
  FlaskIcon,
  ClockIcon,
  NoteIcon,
  UserCircleIcon,
  UsersIcon,
  MenuIcon,
  CloseIcon,
} from '@/components/icons'

interface NavItem {
  to: string
  label: string
  icon: (props: SVGProps<SVGSVGElement>) => ReactElement
  end?: boolean
}

const NAV_ITEMS: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: GridIcon, end: true },
  { to: '/alerts', label: 'Alerts', icon: AlertIcon },
  { to: '/patients', label: 'Patients', icon: UserIcon },
  { to: '/admissions', label: 'Admissions', icon: ClipboardPlusIcon },
  { to: '/wards', label: 'Wards & Beds', icon: BedIcon },
  { to: '/assessments', label: 'Assessments', icon: CheckSquareIcon },
  { to: '/lab-results', label: 'Lab Results', icon: FlaskIcon },
  { to: '/door-timing', label: 'Door Timing', icon: ClockIcon },
  { to: '/follow-up', label: 'Follow-up notes', icon: NoteIcon },
  { to: '/id-card', label: 'My ID Card', icon: UserCircleIcon },
]

const ADMIN_NAV_ITEMS: NavItem[] = [{ to: '/users', label: 'Staff', icon: UsersIcon }]

export default function Layout() {
  const { user } = useAuth()
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const [criticalAlertCount, setCriticalAlertCount] = useState(0)

  // Close the drawer automatically whenever the route changes (link tap on
  // mobile/tablet) so it never stays open covering the new page.
  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

  // Polls the same alert rules the Alerts page shows, just to count how many
  // are currently critical — a quiet badge on the nav item instead of a
  // popup, so it never interrupts, but it's there the moment it matters.
  //
  // Phase F1 - Frontend Hardening (Section 24, performance sanity): this used to depend on
  // [location.pathname], which meant every single in-app navigation (including to completely
  // unrelated pages like /id-card) tore down and recreated this effect — firing an immediate
  // extra loadAlerts() call (13 parallel full-table GET requests) and restarting the 60s
  // interval from zero. A user clicking around the app for a minute could trigger this many
  // times more often than the intended "once a minute" cadence. Mount-once + a steady 60s
  // interval for the lifetime of the Layout (which itself only mounts/unmounts on
  // login/logout, not on navigation) gives the same "stays reasonably fresh" badge behavior
  // without re-fetching on every click.
  useEffect(() => {
    let cancelled = false
    async function check() {
      try {
        const alerts = await loadAlerts()
        if (!cancelled) setCriticalAlertCount(alerts.filter((a) => a.severity === 'critical').length)
      } catch {
        // Silent — this is a background badge, not a page the user is waiting on.
      }
    }
    check()
    const interval = setInterval(check, 60000)
    return () => {
      cancelled = true
      clearInterval(interval)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const navItems = user?.role === 'Admin' ? [...NAV_ITEMS, ...ADMIN_NAV_ITEMS] : NAV_ITEMS

  return (
    <div className="min-h-screen w-full bg-bg">
      {/* Mobile / tablet top bar — hidden once the sidebar is always-on at lg */}
      <header className="sticky top-0 z-20 flex items-center gap-3 border-b border-border bg-surface px-4 py-3 lg:hidden print:hidden">
        <button
          type="button"
          onClick={() => setSidebarOpen(true)}
          aria-label="Open menu"
          className="rounded-lg p-2 text-text hover:bg-border-soft"
        >
          <MenuIcon className="h-5 w-5" />
        </button>
        <img
          src="/brand/WindowIcon.svg"
          alt="NeuroStrokeCare"
          className="h-8 w-8 flex-shrink-0 rounded-[9px] object-cover ring-1 ring-gold/30"
        />
        <span className="font-display text-[15px] font-semibold text-text">NeuroStrokeCare</span>
      </header>

      {/* Backdrop behind the drawer on mobile/tablet only */}
      <AnimatePresence>
        {sidebarOpen && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.15 }}
            onClick={() => setSidebarOpen(false)}
            className="fixed inset-0 z-30 bg-black/40 lg:hidden"
          />
        )}
      </AnimatePresence>

      <div className="flex min-h-screen w-full items-stretch">
        {/*
          Sidebar: an overlay drawer that slides in on top of the page below
          the `lg` breakpoint (phones/tablets), and a normal static column
          that's always visible from `lg` up (desktop) — same nav, same look.
        */}
        <nav
          style={{ paddingBottom: 'max(1.5rem, calc(env(safe-area-inset-bottom, 0px) + 1rem))' }}
          className={`fixed inset-y-0 left-0 z-40 flex w-[272px] max-w-[82vw] flex-shrink-0 flex-col gap-7 overflow-y-auto border-r border-white/[0.06] bg-sidebar px-[18px] pt-6 text-sidebar-text shadow-2xl transition-transform duration-[220ms] ease-[cubic-bezier(0.22,1,0.36,1)] print:hidden lg:sticky lg:top-0 lg:z-0 lg:h-screen lg:w-[252px] lg:max-w-none lg:translate-x-0 lg:pt-7 lg:shadow-none ${
            sidebarOpen ? 'translate-x-0' : '-translate-x-full'
          }`}
        >
          <div className="flex items-center justify-between gap-2 px-[10px] lg:justify-start">
            <div className="flex flex-col gap-[7px]">
              <div className="flex items-center gap-[11px]">
                <img
                  src="/brand/WindowIcon.svg"
                  alt="NeuroStrokeCare"
                  className="h-[38px] w-[38px] flex-shrink-0 rounded-[11px] object-cover shadow-[0_1px_3px_rgba(0,0,0,0.35)] ring-1 ring-gold/40"
                />
                <span className="font-display text-[18px] font-semibold leading-tight text-white">
                  NeuroStrokeCare
                </span>
              </div>
              <span className="pl-[49px] text-[10.5px] font-medium uppercase tracking-wider text-sidebar-muted">
                Mansoura University Hospital
              </span>
            </div>
            <button
              type="button"
              onClick={() => setSidebarOpen(false)}
              aria-label="Close menu"
              className="rounded-full p-2 text-sidebar-muted transition-colors duration-150 hover:bg-white/10 hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-gold/50 lg:hidden"
            >
              <CloseIcon className="h-[18px] w-[18px]" />
            </button>
          </div>

          <ul className="flex flex-col gap-1">
            {navItems.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    `group flex items-center gap-3 rounded-[10px] border-l-[3px] px-3.5 py-[9px] text-[13.5px] font-medium outline-none transition-colors duration-150 focus-visible:ring-2 focus-visible:ring-gold/50 focus-visible:ring-inset ${
                      isActive
                        ? 'border-gold bg-white/[0.08] font-semibold text-white'
                        : 'border-transparent text-sidebar-text hover:bg-white/[0.045] hover:text-white active:bg-white/[0.08]'
                    }`
                  }
                >
                  {({ isActive }) => (
                    <>
                      <Icon
                        className={`h-[18px] w-[18px] flex-shrink-0 transition-colors duration-150 ${
                          isActive ? 'text-gold' : 'text-sidebar-muted group-hover:text-sidebar-text'
                        }`}
                      />
                      <span className="flex-grow">{label}</span>
                      {to === '/alerts' && criticalAlertCount > 0 && (
                        <span className="flex h-[19px] min-w-[19px] items-center justify-center rounded-full bg-gold px-1 text-[10.5px] font-bold text-sidebar">
                          {criticalAlertCount > 99 ? '99+' : criticalAlertCount}
                        </span>
                      )}
                    </>
                  )}
                </NavLink>
              </li>
            ))}
          </ul>

          <div className="mt-auto border-t border-white/10 pt-3">
            <UserMenu />
          </div>
        </nav>

        <main className="min-w-0 flex-grow px-4 py-6 sm:px-6 sm:py-8 print:w-full print:p-0 lg:px-10 lg:py-9 lg:pb-12">
          {/* PHASE 11 (area 4): mounted once here (Layout mounts once per sign-in, not per
              navigation) so every clinical page gets it for free without importing anything. */}
          <PatientContextHeader />
          <Outlet />
        </main>
      </div>
    </div>
  )
}
