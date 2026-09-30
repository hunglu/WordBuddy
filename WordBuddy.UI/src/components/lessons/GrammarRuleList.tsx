import type { ReactElement } from 'react'
import type { GrammarRule } from '../../types'

export function GrammarRuleList({ rules }: { rules: GrammarRule[] }): ReactElement {
  return (
    <ul className="flex flex-col gap-4">
      {rules.map((rule) => (
        <li key={rule.id} className="rounded-wb-lg bg-wb-grammar-surface p-4">
          <p className="text-xl font-bold text-wb-grammar-ink">{rule.title}</p>
          <p className="mt-1 text-wb-grammar-ink">{rule.explanation}</p>
          <div className="mt-2 flex flex-wrap gap-2">
            {rule.examples.map((example) => (
              <span
                key={example}
                className="rounded-wb-pill bg-wb-grammar-bg px-3 py-1 text-sm text-wb-grammar-ink"
              >
                {example}
              </span>
            ))}
          </div>
        </li>
      ))}
    </ul>
  )
}
