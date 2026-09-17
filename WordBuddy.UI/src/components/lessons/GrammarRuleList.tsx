import type { ReactElement } from 'react'
import type { GrammarRule } from '../../types'

export function GrammarRuleList({ rules }: { rules: GrammarRule[] }): ReactElement {
  return (
    <ul className="flex flex-col gap-4">
      {rules.map((rule) => (
        <li key={rule.id} className="rounded-2xl bg-emerald-50 p-4">
          <p className="text-xl font-bold text-emerald-900">{rule.title}</p>
          <p className="mt-1 text-emerald-800">{rule.explanation}</p>
          <div className="mt-2 flex flex-wrap gap-2">
            {rule.examples.map((example) => (
              <span
                key={example}
                className="rounded-full bg-emerald-100 px-3 py-1 text-sm text-emerald-800"
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
