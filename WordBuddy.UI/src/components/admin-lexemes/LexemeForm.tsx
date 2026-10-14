import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactElement } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { problemMessage } from '../../api/supportLinks'
import { useUpdateLexeme } from '../../hooks/useAdminLexemes'
import type { AdminLexemeDetail, CefrLevel, PartOfSpeech, UpdateLexemeValues } from '../../types'
import { inputClass, primaryButtonClass } from '../support/supportUi'

const PARTS_OF_SPEECH = [
  'Noun',
  'Verb',
  'Adjective',
  'Adverb',
  'Pronoun',
  'Preposition',
  'Conjunction',
  'Determiner',
  'Interjection',
  'Phrase',
] as const satisfies readonly PartOfSpeech[]

const CEFR_LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'] as const satisfies readonly CefrLevel[]

// Same limits as the backend validator (`UpdateLexemeCommandValidator`).
const IPA_MAX = 100
const SYLLABLES_MAX = 200
const WORD_FORMS_MAX = 20
const WORD_FORM_MAX_LENGTH = 100
const RANK_MAX = 1_000_000

/** Splits the textarea into trimmed, non-empty lines. */
function toLines(text: string): string[] {
  return text
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
}

const lexemeSchema = z.object({
  // '' is the honest "nothing chosen" state of the select; the transform rejects it, so the
  // submitted value is always a real PartOfSpeech.
  partOfSpeech: z.union([z.enum(PARTS_OF_SPEECH), z.literal('')]).transform((value, ctx) => {
    if (value === '') {
      ctx.addIssue({ code: 'custom', message: 'Choose a part of speech' })
      return z.NEVER
    }
    return value
  }),
  ipaUk: z.string().trim().max(IPA_MAX, `IPA can have up to ${IPA_MAX} characters`),
  ipaUs: z.string().trim().max(IPA_MAX, `IPA can have up to ${IPA_MAX} characters`),
  syllables: z.string().trim().max(SYLLABLES_MAX, `Syllables can have up to ${SYLLABLES_MAX} characters`),
  wordFormsText: z
    .string()
    .refine((text) => toLines(text).length <= WORD_FORMS_MAX, `Use at most ${WORD_FORMS_MAX} word forms`)
    .refine(
      (text) => toLines(text).every((line) => line.length <= WORD_FORM_MAX_LENGTH),
      `Each word form can have up to ${WORD_FORM_MAX_LENGTH} characters`,
    ),
  cefrLevel: z.union([z.enum(CEFR_LEVELS), z.literal('')]),
  frequencyRank: z
    .string()
    .trim()
    .refine(
      (value) => value === '' || (/^\d+$/.test(value) && Number(value) >= 1 && Number(value) <= RANK_MAX),
      `Rank must be a whole number from 1 to ${RANK_MAX}`,
    ),
})

type LexemeFormInput = z.input<typeof lexemeSchema>
type LexemeFormValues = z.output<typeof lexemeSchema>

/** Form values for a loaded lexeme. */
function toFormValues(lexeme: AdminLexemeDetail): LexemeFormInput {
  return {
    // A lexeme can still be without a part of speech; the form asks the admin to choose one.
    partOfSpeech: lexeme.partOfSpeech ?? '',
    ipaUk: lexeme.ipaUk ?? '',
    ipaUs: lexeme.ipaUs ?? '',
    syllables: lexeme.syllables ?? '',
    wordFormsText: lexeme.wordForms.join('\n'),
    cefrLevel: lexeme.cefrLevel ?? '',
    frequencyRank: lexeme.frequencyRank === null ? '' : String(lexeme.frequencyRank),
  }
}

/** API body for the form values. Empty text becomes `null`. */
function toRequest(values: LexemeFormValues): UpdateLexemeValues {
  return {
    partOfSpeech: values.partOfSpeech,
    ipaUk: values.ipaUk === '' ? null : values.ipaUk,
    ipaUs: values.ipaUs === '' ? null : values.ipaUs,
    syllables: values.syllables === '' ? null : values.syllables,
    wordForms: toLines(values.wordFormsText),
    cefrLevel: values.cefrLevel === '' ? null : values.cefrLevel,
    frequencyRank: values.frequencyRank === '' ? null : Number(values.frequencyRank),
  }
}

type LexemeFormProps = Readonly<{ lexeme: AdminLexemeDetail }>

/** Edit form for the dictionary fields of one lexeme. The word itself cannot change. */
export function LexemeForm({ lexeme }: LexemeFormProps): ReactElement {
  const update = useUpdateLexeme()
  const {
    register,
    handleSubmit,
    formState: { errors, isDirty },
  } = useForm<LexemeFormInput, unknown, LexemeFormValues>({
    resolver: zodResolver(lexemeSchema),
    // Fills the form when the lexeme loads and resets it to the saved values after every save.
    values: toFormValues(lexeme),
  })

  const onSubmit = (values: LexemeFormValues): void => {
    update.mutate({ id: lexeme.id, values: toRequest(values) })
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="mt-6 grid gap-4 rounded-wb-card bg-wb-surface-card p-5 shadow-wb-card md:grid-cols-2">
      <div>
        <label htmlFor="lexeme-pos" className="mb-1 block text-sm font-semibold text-wb-ink">
          Part of speech
        </label>
        <select id="lexeme-pos" className={inputClass} {...register('partOfSpeech')}>
          {lexeme.partOfSpeech === null && <option value="">Choose…</option>}
          {PARTS_OF_SPEECH.map((p) => (
            <option key={p} value={p}>
              {p}
            </option>
          ))}
        </select>
        {errors.partOfSpeech && <p className="mt-1 text-sm text-wb-danger">{errors.partOfSpeech.message}</p>}
      </div>

      <div>
        <label htmlFor="lexeme-cefr" className="mb-1 block text-sm font-semibold text-wb-ink">
          CEFR level
        </label>
        <select id="lexeme-cefr" className={inputClass} {...register('cefrLevel')}>
          <option value="">Not set</option>
          {CEFR_LEVELS.map((l) => (
            <option key={l} value={l}>
              {l}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label htmlFor="lexeme-ipa-uk" className="mb-1 block text-sm font-semibold text-wb-ink">
          IPA (UK)
        </label>
        <input id="lexeme-ipa-uk" type="text" className={inputClass} {...register('ipaUk')} />
        {errors.ipaUk && <p className="mt-1 text-sm text-wb-danger">{errors.ipaUk.message}</p>}
      </div>

      <div>
        <label htmlFor="lexeme-ipa-us" className="mb-1 block text-sm font-semibold text-wb-ink">
          IPA (US)
        </label>
        <input id="lexeme-ipa-us" type="text" className={inputClass} {...register('ipaUs')} />
        {errors.ipaUs && <p className="mt-1 text-sm text-wb-danger">{errors.ipaUs.message}</p>}
      </div>

      <div>
        <label htmlFor="lexeme-syllables" className="mb-1 block text-sm font-semibold text-wb-ink">
          Syllables
        </label>
        <input id="lexeme-syllables" type="text" className={inputClass} {...register('syllables')} />
        {errors.syllables && <p className="mt-1 text-sm text-wb-danger">{errors.syllables.message}</p>}
      </div>

      <div>
        <label htmlFor="lexeme-rank" className="mb-1 block text-sm font-semibold text-wb-ink">
          Frequency rank
        </label>
        <input id="lexeme-rank" type="text" inputMode="numeric" className={inputClass} {...register('frequencyRank')} />
        {errors.frequencyRank && <p className="mt-1 text-sm text-wb-danger">{errors.frequencyRank.message}</p>}
      </div>

      <div className="md:col-span-2">
        <label htmlFor="lexeme-forms" className="mb-1 block text-sm font-semibold text-wb-ink">
          Word forms
        </label>
        <textarea id="lexeme-forms" rows={4} className={inputClass} {...register('wordFormsText')} />
        <p className="mt-1 text-xs text-wb-ink-muted">One form per line, for example the plural or the past tense.</p>
        {errors.wordFormsText && <p className="mt-1 text-sm text-wb-danger">{errors.wordFormsText.message}</p>}
      </div>

      <div className="flex flex-wrap items-center gap-3 md:col-span-2">
        <button type="submit" disabled={update.isPending || !isDirty} className={primaryButtonClass}>
          {update.isPending ? 'Saving…' : 'Save changes'}
        </button>
        {update.isSuccess && !isDirty && <span className="text-sm font-semibold text-wb-success">Saved</span>}
        {update.isError && (
          <span role="alert" className="text-sm font-semibold text-wb-danger">
            {problemMessage(update.error, 'Could not save the lexeme.')}
          </span>
        )}
      </div>
    </form>
  )
}
