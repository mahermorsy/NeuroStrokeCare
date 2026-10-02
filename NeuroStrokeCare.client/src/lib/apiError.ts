// Phase F1 - Frontend Hardening. A single, shared way to turn an axios error into a safe,
// status-aware message, instead of every page hand-rolling its own `(err as {...}).response
// ?.status` narrowing (which had drifted inconsistent - some pages handled 409/404, some only
// 403, some collapsed everything into one generic string). This does NOT replace backend
// authorization or validation - it only decides what text a user sees after the backend has
// already decided the outcome.
//
// Never invents detail the backend didn't send: for 400/404/409 it prefers the backend's own
// `message`/`errors` (the backend's global exception handler already keeps real exception
// text/stack traces out of these bodies outside Development - see Phase 10), falling back to a
// generic, specific-to-that-status message only when the backend didn't include one. For
// 403/500/network errors it never uses backend-sent text at all, even if present, since those
// cases are the ones most likely to carry internal detail that isn't meant for an end user.

export interface ApiErrorFallbacks {
  400?: string
  403?: string
  404?: string
  409?: string
  500?: string
  network?: string
}

interface BackendErrorBody {
  message?: string
  errors?: string[] | Record<string, string[]>
  title?: string
}

const DEFAULT_FALLBACKS: Required<ApiErrorFallbacks> = {
  400: 'Some of the information entered is not valid. Please check the form and try again.',
  403: "You don't have permission to do that.",
  404: 'The record you were looking for could not be found. It may have been removed.',
  409: 'This could not be saved because something about it changed since the page loaded. Please refresh and try again.',
  500: 'Something went wrong on our end. Please try again in a moment.',
  network: 'Could not reach the server. Check your connection and try again.',
}

function extractBackendMessage(data: unknown): string | null {
  if (!data || typeof data !== 'object') return null
  const body = data as BackendErrorBody
  if (typeof body.message === 'string' && body.message.trim()) return body.message
  if (Array.isArray(body.errors) && body.errors.length > 0) {
    return body.errors.filter((e) => typeof e === 'string' && e.trim()).join(' ') || null
  }
  if (body.errors && typeof body.errors === 'object' && !Array.isArray(body.errors)) {
    const flat = Object.values(body.errors).flat()
    const text = flat.filter((e): e is string => typeof e === 'string' && e.trim().length > 0).join(' ')
    return text || null
  }
  return null
}

/**
 * @param err the caught value from an axios call (typed `unknown` at the call site)
 * @param fallbacks optional per-status overrides for THIS call site's wording - e.g. a create
 *   form's 409 means something more specific ("that bed was just taken") than the generic
 *   default. Only change the statuses that need call-site-specific wording; leave the rest to
 *   the shared defaults above.
 */
export function describeApiError(err: unknown, fallbacks: ApiErrorFallbacks = {}): string {
  const merged = { ...DEFAULT_FALLBACKS, ...fallbacks }
  const axiosLike = err as { response?: { status?: number; data?: unknown }; request?: unknown }

  if (!axiosLike?.response) {
    // No response at all - a network failure, CORS issue, or the request never reached the
    // server (axios sets `request` but not `response` in that case).
    return merged.network
  }

  const status = axiosLike.response.status
  const backendMessage = extractBackendMessage(axiosLike.response.data)

  switch (status) {
    case 400:
      return backendMessage ?? merged[400]
    case 401:
      // The global interceptor in src/lib/api.ts already clears the token and redirects to
      // /login on 401 before most callers ever see this - this fallback only covers a caller
      // that reads the error before that redirect takes effect.
      return 'Your session has expired. Please sign in again.'
    case 403:
      return merged[403]
    case 404:
      return backendMessage ?? merged[404]
    case 409:
      return backendMessage ?? merged[409]
    default:
      return merged[500]
  }
}
