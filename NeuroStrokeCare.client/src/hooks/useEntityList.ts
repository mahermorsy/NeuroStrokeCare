import { useCallback, useEffect, useState } from 'react'

export function useEntityList<T>(fetcher: () => Promise<T[]>, deps: unknown[] = []) {
  const [data, setData] = useState<T[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await fetcher()
      setData(result)
    } catch (err) {
      const message =
        (err as { response?: { data?: { message?: string }; status?: number } })?.response?.status === 500
          ? 'The server could not load this list (500). If this is one of the assessment types, its database table may not exist yet.'
          : 'Could not load this list — check that the API is running and you are signed in.'
      setError(message)
    } finally {
      setLoading(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  useEffect(() => {
    load()
  }, [load])

  return { data, loading, error, reload: load }
}
