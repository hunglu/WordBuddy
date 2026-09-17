import { animate, motion, useMotionValue, useTransform } from 'framer-motion'
import type { ReactElement } from 'react'
import { useEffect } from 'react'

/** Animates from 0 up to `value` on mount/update. */
export function CountUpStat({ value, suffix = '' }: { value: number; suffix?: string }): ReactElement {
  const motionValue = useMotionValue(0)
  const rounded = useTransform(motionValue, (latest) => `${Math.round(latest)}${suffix}`)

  useEffect(() => {
    const controls = animate(motionValue, value, { duration: 0.8, ease: 'easeOut' })
    return () => controls.stop()
  }, [motionValue, value])

  return <motion.span>{rounded}</motion.span>
}
