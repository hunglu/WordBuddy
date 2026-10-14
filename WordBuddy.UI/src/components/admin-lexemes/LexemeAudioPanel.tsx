import type { ChangeEvent, ReactElement } from 'react'
import { useRef, useState } from 'react'
import { problemMessage } from '../../api/supportLinks'
import { useRemoveLexemeAudio, useUploadLexemeAudio } from '../../hooks/useAdminLexemes'
import type { AdminLexemeDetail, AudioLocale } from '../../types'
import { ConfirmDialog } from '../admin-lessons/ConfirmDialog'
import { cardClass, dangerButtonClass, softButtonClass } from '../support/supportUi'

const MAX_AUDIO_BYTES = 5 * 1024 * 1024

type LocaleRowProps = Readonly<{
  lexemeId: string
  locale: AudioLocale
  label: string
  /** Asset id of the stored file, or `null` when there is none. */
  assetId: string | null
}>

/** Play, upload and remove for one locale. */
function LocaleRow({ lexemeId, locale, label, assetId }: LocaleRowProps): ReactElement {
  const upload = useUploadLexemeAudio()
  const remove = useRemoveLexemeAudio()
  const fileInput = useRef<HTMLInputElement>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [fileError, setFileError] = useState<string | null>(null)
  // The file name stays the same after an upload, so the preview URL gets a new query value.
  const [version, setVersion] = useState(0)

  const onFile = (event: ChangeEvent<HTMLInputElement>): void => {
    const file: File | undefined = event.target.files?.[0]
    event.target.value = ''
    if (!file) return
    if (file.type !== 'audio/mpeg' && file.type !== 'audio/mp3') {
      setFileError('Choose an MP3 file.')
      return
    }
    if (file.size > MAX_AUDIO_BYTES) {
      setFileError('The file can be at most 5 MB.')
      return
    }
    setFileError(null)
    upload.mutate({ id: lexemeId, locale, file }, { onSuccess: () => setVersion(Date.now()) })
  }

  const play = (): void => {
    const suffix: string = version === 0 ? '' : `?v=${version}`
    void new Audio(`/api/media/${assetId}${suffix}`).play().catch(() => undefined)
  }

  const uploadId = `lexeme-audio-${locale}`

  return (
    <li className="flex flex-wrap items-center gap-3 border-t border-wb-border-subtle py-3 first:border-t-0">
      <span className="w-28 font-bold text-wb-ink">{label}</span>
      <span className="w-24 text-sm text-wb-ink-muted">{assetId ? 'Audio set' : 'No audio'}</span>
      {assetId && (
        <button type="button" onClick={play} className={softButtonClass} aria-label={`Play ${label} audio`}>
          ▶ Play
        </button>
      )}
      <input
        ref={fileInput}
        id={uploadId}
        type="file"
        accept="audio/mpeg,audio/mp3"
        className="sr-only"
        aria-label={`Upload ${label} audio`}
        onChange={onFile}
      />
      <button type="button" disabled={upload.isPending} onClick={() => fileInput.current?.click()} className={softButtonClass}>
        {upload.isPending ? 'Uploading…' : assetId ? 'Replace' : 'Upload MP3'}
      </button>
      {assetId && (
        <button type="button" onClick={() => setConfirmOpen(true)} className={dangerButtonClass}>
          Remove
        </button>
      )}
      {fileError && (
        <p role="alert" className="w-full text-sm font-semibold text-wb-danger">
          {fileError}
        </p>
      )}
      {upload.isError && (
        <p role="alert" className="w-full text-sm font-semibold text-wb-danger">
          {problemMessage(upload.error, 'Could not upload the audio.')}
        </p>
      )}
      <ConfirmDialog
        open={confirmOpen}
        title={`Remove the ${label} audio?`}
        message="The audio file is deleted unless another item still uses it."
        confirmLabel="Remove"
        pendingLabel="Removing…"
        isPending={remove.isPending}
        errorMessage={remove.isError ? problemMessage(remove.error, 'Could not remove the audio.') : null}
        onCancel={() => {
          remove.reset()
          setConfirmOpen(false)
        }}
        onConfirm={() => remove.mutate({ id: lexemeId, locale }, { onSuccess: () => setConfirmOpen(false) })}
      />
    </li>
  )
}

type LexemeAudioPanelProps = Readonly<{ lexeme: AdminLexemeDetail }>

/** Pronunciation audio of a lexeme, British and American. */
export function LexemeAudioPanel({ lexeme }: LexemeAudioPanelProps): ReactElement {
  return (
    <section className={`mt-6 ${cardClass}`}>
      <h2 className="text-xl font-bold text-wb-ink">Pronunciation audio</h2>
      <p className="mt-1 text-sm text-wb-ink-muted">One MP3 per accent, up to 5 MB. A new file replaces the old one.</p>
      <ul className="mt-3">
        <LocaleRow lexemeId={lexeme.id} locale="en-GB" label="British (UK)" assetId={lexeme.audioUkAssetId} />
        <LocaleRow lexemeId={lexeme.id} locale="en-US" label="American (US)" assetId={lexeme.audioUsAssetId} />
      </ul>
    </section>
  )
}
