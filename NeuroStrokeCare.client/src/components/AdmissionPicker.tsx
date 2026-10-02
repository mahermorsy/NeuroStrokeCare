import { useEffect, useRef, useState } from 'react'
import { admissionSearchApi } from '@/lib/admissionSearchApi'
import type { AdmissionSearchResultResponse } from '@/types/entities'
import { PATIENT_STATUS } from '@/lib/enums'

/**
 * ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 1), extended in PHASE 11 (areas 2, 13, 17):
 * a searchable combobox that replaces the old "load every active admission into a giant
 * <select>" pattern on the clinical forms (Assessments, Lab Results, Door Timing activation,
 * Follow-up Notes). It calls the GET /api/admission/search endpoint, debounced, and never
 * fetches more than `take` rows at once — it does not preload the full admission list.
 *
 * Search priority: Hospital Number, then National ID, then name (see AdmissionController.Search,
 * which orders matches in that order). Hospital Number is shown first in both the result list and
 * the "selected" chip, per PHASE 11 area 14 ("HospitalNumber becomes the preferred operational
 * identifier in ordinary workflows"); National ID is shown masked, as returned by the backend.
 *
 * Accessibility (PHASE 11 area 17): implements the WAI-ARIA combobox pattern by hand (no extra
 * UI library, to stay consistent with this codebase's other hand-written form components —
 * FormField.tsx, Modal.tsx, etc). ArrowDown/ArrowUp move a visible "active" option,
 * Enter selects the active option, Escape closes the list without selecting, and the input
 * exposes aria-expanded/aria-activedescendant/aria-controls so screen readers can track state.
 * Loading/empty/error states are each rendered as a single non-interactive row with role="status".
 */
export default function AdmissionPicker({
  value,
  onSelect,
  openOnly = true,
  placeholder = 'Search by Hospital No., National ID, or name…',
  disabled = false,
  label = 'Patient / admission',
}: {
  /** The currently selected admission's id, or '' if none. Used only to show a "selected"
   *  state — this component does not resolve an existing id back into a label on mount. */
  value: string
  onSelect: (admissionId: string, result: AdmissionSearchResultResponse | null) => void
  /** Restricts results to admissions with no discharge time yet (default true — matches the
   *  "only active/open admissions selectable for new clinical writes" requirement). */
  openOnly?: boolean
  placeholder?: string
  disabled?: boolean
  /** Accessible name for the combobox input, used for aria-label. Not rendered visually —
   *  callers that already show a visible <Field label="…"> wrapper can leave this as-is. */
  label?: string
}) {
  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [results, setResults] = useState<AdmissionSearchResultResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<AdmissionSearchResultResponse | null>(null)
  const [activeIndex, setActiveIndex] = useState(-1)
  const containerRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const abortRef = useRef<AbortController | null>(null)
  const listboxId = useRef(`admission-picker-listbox-${Math.random().toString(36).slice(2)}`).current

  // Clears the picker's own local selection if the parent form resets admissionId elsewhere
  // (e.g. after a successful submit).
  useEffect(() => {
    if (value === '' && selected !== null) setSelected(null)
  }, [value, selected])

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Keep the active (highlighted) option in range whenever the result set changes, and clear it
  // when the list closes so re-opening doesn't start mid-list.
  useEffect(() => {
    if (!open) {
      setActiveIndex(-1)
      return
    }
    setActiveIndex((i) => (results.length === 0 ? -1 : Math.min(Math.max(i, 0), results.length - 1)))
  }, [open, results])

  function runSearch(term: string) {
    abortRef.current?.abort()
    const controller = new AbortController()
    abortRef.current = controller
    setLoading(true)
    setError(null)
    admissionSearchApi
      .search(term, { openOnly, take: 20, signal: controller.signal })
      .then((r) => setResults(r))
      .catch((err) => {
        if (err?.code === 'ERR_CANCELED' || err?.name === 'CanceledError') return
        setError('Could not load matching admissions — try again.')
        setResults([])
      })
      .finally(() => setLoading(false))
  }

  function handleFocus() {
    setOpen(true)
    if (results.length === 0 && !loading) runSearch(query)
  }

  function handleQueryChange(next: string) {
    setQuery(next)
    setOpen(true)
    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => runSearch(next), 300)
  }

  function handlePick(r: AdmissionSearchResultResponse) {
    setSelected(r)
    setQuery('')
    setOpen(false)
    setActiveIndex(-1)
    onSelect(r.admissionId, r)
  }

  function handleClear() {
    setSelected(null)
    setQuery('')
    onSelect('', null)
    // Give focus back to the search input so a sighted or keyboard/screen-reader user can
    // immediately search for a replacement without hunting for the field again.
    requestAnimationFrame(() => inputRef.current?.focus())
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (disabled) return
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      if (!open) {
        setOpen(true)
        if (results.length === 0 && !loading) runSearch(query)
        return
      }
      if (results.length > 0) setActiveIndex((i) => (i + 1) % results.length)
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      if (open && results.length > 0) setActiveIndex((i) => (i <= 0 ? results.length - 1 : i - 1))
    } else if (e.key === 'Enter') {
      if (open && activeIndex >= 0 && activeIndex < results.length) {
        e.preventDefault()
        handlePick(results[activeIndex])
      }
    } else if (e.key === 'Escape') {
      if (open) {
        e.preventDefault()
        setOpen(false)
        setActiveIndex(-1)
      }
    }
  }

  const activeOptionId = activeIndex >= 0 ? `${listboxId}-option-${activeIndex}` : undefined

  function resultSubtitle(r: AdmissionSearchResultResponse) {
    const idPart = r.hospitalNumber
      ? `Hospital No. ${r.hospitalNumber}`
      : r.nationalIdMasked
        ? `ID ${r.nationalIdMasked}`
        : 'No hospital number or National ID on file'
    const wardPart = r.wardCode && r.bedNumber ? ` — ${r.wardCode} ${r.bedNumber}` : ''
    return idPart + wardPart
  }

  return (
    <div ref={containerRef} className="relative flex flex-col gap-1.5">
      {selected ? (
        <div className="flex items-center justify-between gap-2 rounded-lg border border-accent/40 bg-accent/5 px-3 py-2">
          <div className="flex min-w-0 flex-col leading-tight">
            <span className="truncate text-[13.5px] font-semibold text-text">{selected.patientName}</span>
            <span className="truncate text-[11.5px] text-text-muted">
              {resultSubtitle(selected)}
              {' — '}
              {PATIENT_STATUS[selected.status as keyof typeof PATIENT_STATUS] ?? 'Unknown status'}
              {!selected.isOpen ? ' (discharged)' : ''}
            </span>
          </div>
          {!disabled && (
            <button
              type="button"
              onClick={handleClear}
              className="shrink-0 rounded-md px-2 py-1 text-[12px] font-medium text-text-muted hover:bg-border-soft hover:text-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent/40"
            >
              Change
            </button>
          )}
        </div>
      ) : (
        <input
          ref={inputRef}
          type="text"
          role="combobox"
          aria-label={label}
          aria-expanded={open}
          aria-controls={listboxId}
          aria-activedescendant={activeOptionId}
          aria-autocomplete="list"
          disabled={disabled}
          value={query}
          placeholder={placeholder}
          onFocus={handleFocus}
          onChange={(e) => handleQueryChange(e.target.value)}
          onKeyDown={handleKeyDown}
          className="rounded-lg border border-border px-3 py-2 text-[13.5px] text-text outline-none focus:border-accent disabled:opacity-60"
          autoComplete="off"
        />
      )}

      {open && !selected && (
        <ul
          id={listboxId}
          role="listbox"
          aria-label={label}
          className="absolute top-full z-20 mt-1 max-h-64 w-full overflow-y-auto rounded-lg border border-border bg-surface shadow-lg"
        >
          {loading && (
            <li role="status" className="px-3 py-2.5 text-[12.5px] text-text-muted">
              Searching…
            </li>
          )}
          {!loading && error && (
            <li role="status" className="px-3 py-2.5 text-[12.5px] text-critical">
              {error}
            </li>
          )}
          {!loading && !error && results.length === 0 && (
            <li role="status" className="px-3 py-2.5 text-[12.5px] text-text-muted">
              {query ? 'No matching admissions.' : 'No admissions available.'}
            </li>
          )}
          {!loading &&
            !error &&
            results.map((r, i) => (
              <li key={r.admissionId} role="presentation">
                <button
                  id={`${listboxId}-option-${i}`}
                  role="option"
                  aria-selected={i === activeIndex}
                  type="button"
                  onMouseEnter={() => setActiveIndex(i)}
                  onClick={() => handlePick(r)}
                  className={`flex w-full flex-col items-start gap-0.5 border-b border-border-subtle px-3 py-2 text-left last:border-b-0 focus-visible:outline-none ${
                    i === activeIndex ? 'bg-border-soft/60' : 'hover:bg-border-soft/60'
                  }`}
                >
                  <span className="text-[13px] font-semibold text-text">{r.patientName}</span>
                  <span className="text-[11.5px] text-text-muted">
                    {resultSubtitle(r)}
                    {' — '}
                    {PATIENT_STATUS[r.status as keyof typeof PATIENT_STATUS] ?? 'Unknown status'}
                    {!r.isOpen ? ' (discharged)' : ''}
                  </span>
                </button>
              </li>
            ))}
        </ul>
      )}
    </div>
  )
}
