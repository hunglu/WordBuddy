import { motion } from 'framer-motion'
import type { ReactElement, ReactNode } from 'react'
import styles from './SenseCard.module.css'
import { SenseDetails, type SenseDetailsData } from './SenseDetails'

type SenseCardProps = Readonly<{
  sense: SenseDetailsData
  /** Position in the list, for the staggered entrance. */
  index?: number
  /** Action buttons (for example "Add"). */
  children?: ReactNode
}>

/** One sense as a card: POS, IPA, UK/US audio, definition, examples, translation, then actions. */
export function SenseCard({ sense, index = 0, children }: SenseCardProps): ReactElement {
  return (
    <motion.div
      className={styles.card}
      initial={{ opacity: 0, y: 10 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25, delay: index * 0.06 }}
    >
      <SenseDetails sense={sense} />
      {children && <div className="mt-2 flex gap-2">{children}</div>}
    </motion.div>
  )
}
