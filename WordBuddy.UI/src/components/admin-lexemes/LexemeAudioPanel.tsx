import type { ChangeEvent, ReactElement } from 'react'
import { useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
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
  const { t } = useTranslation('admin')
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
      setFileError(t('audio.chooseMp3'))
      return
    }
    if (file.size > MAX_AUDIO_BYTES) {
      setFileError(t('audio.tooBig'))
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
      <span className="w-24 text-sm text-wb-ink-muted">{assetId ? t('audio.set') : t('audio.none')}</span>
      {assetId && (
        <button type="button" onClick={play} className={softButtonClass} aria-label={t('audio.playAria', { label })}>
          {t('audio.play')}
        </button>
      )}
      <input
        ref={fileInput}
        id={uploadId}
        type="file"
        accept="audio/mpeg,audio/mp3"
        className="sr-only"
        aria-label={t('audio.uploadAria', { label })}
        onChange={onFile}
      />
      <button type="button" disabled={upload.isPending} onClick={() => fileInput.current?.click()} className={softButtonClass}>
        {upload.isPending ? t('audio.uploading') : assetId ? t('audio.replace') : t('audio.upload')}
      </button>
      {assetId && (
        <button type="button" onClick={() => setConfirmOpen(true)} className={dangerButtonClass}>
          {t('audio.remove')}
        </button>
      )}
      {fileError && (
        <p role="alert" className="w-full text-sm font-semibold text-wb-danger">
          {fileError}
        </p>
      )}
      {upload.isError && (
        <p role="alert" className="w-full text-sm font-semibold text-wb-danger">
          {problemMessage(upload.error, t('audio.uploadFailed'))}
        </p>
      )}
      <ConfirmDialog
        open={confirmOpen}
        title={t('audio.removeTitle', { label })}
        message={t('audio.removeMessage')}
        confirmLabel={t('audio.remove')}
        pendingLabel={t('confirm.removing')}
        isPending={remove.isPending}
        errorMessage={remove.isError ? problemMessage(remove.error, t('audio.removeFailed')) : null}
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
  const { t } = useTranslation('admin')
  return (
    <section className={`mt-6 ${cardClass}`}>
      <h2 className="text-xl font-bold text-wb-ink">{t('audio.heading')}</h2>
      <p className="mt-1 text-sm text-wb-ink-muted">{t('audio.hint')}</p>
      <ul className="mt-3">
        <LocaleRow lexemeId={lexeme.id} locale="en-GB" label={t('audio.uk')} assetId={lexeme.audioUkAssetId} />
        <LocaleRow lexemeId={lexeme.id} locale="en-US" label={t('audio.us')} assetId={lexeme.audioUsAssetId} />
      </ul>
    </section>
  )
}
