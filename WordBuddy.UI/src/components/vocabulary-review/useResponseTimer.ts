import { useEffect, useRef } from 'react'

/** Measures ms from the exercise being shown (mount) to `elapsed()` being called. */
export function useResponseTimer(): () => number {
  const shownAt = useRef<number>(0)

  useEffect(() => {
    shownAt.current = performance.now()
  }, [])

  return () => Math.min(600_000, Math.max(0, Math.round(performance.now() - shownAt.current)))
}
