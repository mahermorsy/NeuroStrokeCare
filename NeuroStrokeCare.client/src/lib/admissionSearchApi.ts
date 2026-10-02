import { api } from '@/lib/api'
import type { AdmissionSearchResultResponse } from '@/types/entities'

// ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 1): thin client for the new read-only
// GET /api/admission/search endpoint - a typeahead, not a listing, so it is never called
// without a `take` cap and is always debounced by its caller (see AdmissionPicker).
export const admissionSearchApi = {
  search: (q: string, opts?: { openOnly?: boolean; take?: number; signal?: AbortSignal }) =>
    api
      .get<AdmissionSearchResultResponse[]>('/Admission/search', {
        params: { q: q || undefined, openOnly: opts?.openOnly ?? true, take: opts?.take ?? 20 },
        signal: opts?.signal,
      })
      .then((r) => r.data),
}
