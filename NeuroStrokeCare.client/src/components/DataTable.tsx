import type { ReactNode } from 'react'

export interface Column<T> {
  header: string
  render: (row: T) => ReactNode
  width?: string
}

export default function DataTable<T>({
  columns,
  rows,
  rowKey,
  loading,
  error,
  emptyMessage = 'Nothing here yet.',
  onRetry,
}: {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  loading: boolean
  error: string | null
  emptyMessage?: string
  onRetry?: () => void
}) {
  if (loading) {
    return (
      <div className="flex flex-col gap-2 py-6">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="h-10 animate-pulse rounded-lg bg-border-soft" />
        ))}
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-start gap-2.5 rounded-xl bg-critical-bg px-4 py-4 text-[13.5px] text-critical">
        <span>{error}</span>
        {onRetry && (
          <button type="button" onClick={onRetry} className="font-semibold underline">
            Try again
          </button>
        )}
      </div>
    )
  }

  if (rows.length === 0) {
    return (
      <div className="rounded-xl border border-dashed border-border px-4 py-8 text-center text-[13.5px] text-text-muted">
        {emptyMessage}
      </div>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[640px] border-collapse">
        <thead>
          <tr>
            {columns.map((col) => (
              <th
                key={col.header}
                style={col.width ? { width: col.width } : undefined}
                className="border-b border-border px-2.5 py-2 text-left text-[11px] font-semibold uppercase tracking-wide text-text-muted"
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={rowKey(row)} className={i < rows.length - 1 ? 'border-b border-border-soft' : ''}>
              {columns.map((col) => (
                <td key={col.header} className="px-2.5 py-3 text-[13.5px] text-text">
                  {col.render(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
