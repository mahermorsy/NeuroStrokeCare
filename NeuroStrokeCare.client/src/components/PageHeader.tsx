import type { ButtonHTMLAttributes, ReactNode } from 'react'

export default function PageHeader({
  title,
  subtitle,
  action,
}: {
  title: string
  subtitle?: string
  action?: ReactNode
}) {
  return (
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="font-display text-[20px] font-semibold text-text sm:text-[26px]">{title}</h1>
        {subtitle && <p className="mt-1.5 text-[14px] text-text-secondary">{subtitle}</p>}
      </div>
      {action}
    </div>
  )
}

export function PrimaryButton(props: ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      {...props}
      className={`rounded-lg bg-accent px-4 py-2.5 text-[13.5px] font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-60 ${props.className ?? ''}`}
    />
  )
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div
      className={`rounded-2xl border border-border bg-surface p-4 shadow-[0_1px_2px_rgba(16,40,45,0.04)] sm:p-6 ${className}`}
    >
      {children}
    </div>
  )
}
