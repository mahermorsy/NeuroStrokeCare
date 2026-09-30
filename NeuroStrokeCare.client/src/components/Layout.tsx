import { useEffect, useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { AnimatePresence, motion } from 'framer-motion'
import { useAuth } from '@/context/AuthContext'
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
  LogoMark,
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
]

const ADMIN_NAV_ITEMS: NavItem[] = [{ to: '/users', label: 'Staff', icon: UsersIcon }]

export default function Layout() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const [sidebarOpen, setSidebarOpen] = useState(false)

  // Close the drawer automatically whenever the route changes (link tap on
  // mobile/tablet) so it never stays open covering the new page.
  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

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
        <span className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-[9px] bg-accent">
          <LogoMark className="h-[18px] w-[18px]" />
        </span>
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
          className={`fixed inset-y-0 left-0 z-40 flex w-[264px] max-w-[80vw] flex-shrink-0 flex-col gap-7 overflow-y-auto bg-sidebar px-[18px] py-6 text-sidebar-text shadow-2xl transition-transform duration-300 ease-out print:hidden lg:sticky lg:top-0 lg:z-0 lg:h-screen lg:w-[248px] lg:max-w-none lg:translate-x-0 lg:py-7 lg:shadow-none ${
            sidebarOpen ? 'translate-x-0' : '-translate-x-full'
          }`}
        >
          <div className="flex items-center justify-between gap-2 px-[10px] lg:justify-start">
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-[10px]">
                <span className="flex h-[38px] w-[38px] flex-shrink-0 items-center justify-center rounded-[11px] bg-accent">
                  <LogoMark className="h-[22px] w-[22px]" />
                </span>
                <span className="font-display text-[18px] font-semibold leading-tight text-white">
                  NeuroStrokeCare
                </span>
              </div>
              <span className="-mt-1 pl-[50px] text-[11px] text-sidebar-muted">
                Mansoura University Hospital
              </span>
            </div>
            <button
              type="button"
              onClick={() => setSidebarOpen(false)}
              aria-label="Close menu"
              className="rounded-lg p-1.5 text-sidebar-muted hover:bg-white/10 hover:text-white lg:hidden"
            >
              <CloseIcon className="h-5 w-5" />
            </button>
          </div>

          <ul className="flex flex-col gap-[3px]">
            {navItems.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  className={({ isActive }) =>
                    `flex items-center gap-3 rounded-lg border-l-[3px] px-3 py-[10px] text-sm font-medium transition-colors ${
                      isActive
                        ? 'border-accent bg-white/10 font-semibold text-white'
                        : 'border-transparent text-sidebar-text hover:bg-white/5 hover:text-white'
                    }`
                  }
                >
                  <Icon className="h-[18px] w-[18px] flex-shrink-0" />
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>

          <div className="mt-auto flex flex-col gap-[10px] border-t border-white/10 pt-4">
            <div className="flex items-center gap-[10px] rounded-lg px-3 py-2">
              <UserCircleIcon className="h-[26px] w-[26px] flex-shrink-0 text-[#5FC8CE]" />
              <span className="flex flex-col text-[13px] font-semibold text-white">
                {user?.userName ?? 'Signed-in clinician'}
                <span className="text-[11px] font-normal text-sidebar-muted">
                  {user?.role ?? 'Stroke Unit'}
                </span>
              </span>
            </div>
            <button
              type="button"
              onClick={logout}
              className="px-3 text-left text-xs text-sidebar-muted hover:text-white"
            >
              Sign out
            </button>
          </div>
        </nav>

        <main className="min-w-0 flex-grow px-4 py-6 sm:px-6 sm:py-8 print:w-full print:p-0 lg:px-10 lg:py-9 lg:pb-12">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
