import { useEffect, useState } from 'react'

/**
 * Animates a number from 0 up to `target` over `durationMs`.
 * Used for the dashboard stat tiles so numbers settle in gently
 * instead of popping in — a calm, restrained nod to the kind of
 * motion used across modern marketing sites, kept subtle for a
 * clinical tool.
 */
export function useCountUp(target: number, durationMs = 900) {
  const [value, setValue] = useState(0)

  useEffect(() => {
    let frame: number
    const start = performance.now()

    const tick = (now: number) => {
      const progress = Math.min((now - start) / durationMs, 1)
      const eased = 1 - Math.pow(1 - progress, 3)
      setValue(Math.round(eased * target))
      if (progress < 1) {
        frame = requestAnimationFrame(tick)
      }
    }

    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [target, durationMs])

  return value
}
