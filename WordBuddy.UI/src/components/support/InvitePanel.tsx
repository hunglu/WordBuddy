import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../../api/supportLinks'
import { useAcceptInvitation, useCreateInvitation } from '../../hooks/useSupportLinks'
import type { InvitationSide, SupportRelationship } from '../../types'
import { RELATIONSHIPS, cardClass, formatDate, inputClass, primaryButtonClass } from './supportUi'

type InvitePanelProps = Readonly<{
  /** Child accounts may only invite a supporter (never offer to support). */
  isChild: boolean
}>

/** Create an invitation (code + link) and accept one by code. */
export function InvitePanel({ isChild }: InvitePanelProps): ReactElement {
  const { t } = useTranslation('support')
  const createInvitation = useCreateInvitation()
  const acceptInvitation = useAcceptInvitation()
  const [inviteAs, setInviteAs] = useState<InvitationSide>('Learner')
  const [relationship, setRelationship] = useState<SupportRelationship | ''>('')
  const [code, setCode] = useState('')

  const created = createInvitation.data
  const acceptLink = created ? `${window.location.origin}/support/accept/${created.token}` : null

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <div className={cardClass}>
        <h2 className="text-xl font-bold text-wb-ink">{isChild ? t('invite.inviteSupporter') : t('invite.createInvitation')}</h2>

        {!isChild && (
          <fieldset className="mt-3 flex flex-col gap-2 text-sm font-semibold text-wb-ink">
            <legend className="sr-only">{t('invite.type')}</legend>
            <label className="flex items-center gap-2">
              <input
                type="radio"
                name="inviteAs"
                checked={inviteAs === 'Learner'}
                onChange={() => setInviteAs('Learner')}
              />
              {t('invite.wantSupporter')}
            </label>
            <label className="flex items-center gap-2">
              <input
                type="radio"
                name="inviteAs"
                checked={inviteAs === 'Supporter'}
                onChange={() => setInviteAs('Supporter')}
              />
              {t('invite.wantToSupport')}
            </label>
          </fieldset>
        )}

        <label htmlFor="relationship" className="mt-3 block text-sm font-semibold text-wb-ink">
          {t('invite.relationship')}
        </label>
        <select
          id="relationship"
          value={relationship}
          onChange={(e) => setRelationship(RELATIONSHIPS.find((r) => r === e.target.value) ?? '')}
          className={inputClass}
        >
          <option value="">{t('invite.noLabel')}</option>
          {RELATIONSHIPS.map((r) => (
            <option key={r} value={r}>
              {t(`relationship.${r}`)}
            </option>
          ))}
        </select>

        <button
          type="button"
          disabled={createInvitation.isPending}
          onClick={() =>
            createInvitation.mutate({
              inviteAs: isChild ? 'Learner' : inviteAs,
              relationship: relationship === '' ? null : relationship,
            })
          }
          className={`mt-4 ${primaryButtonClass}`}
        >
          {t('invite.createCode')}
        </button>

        {created && acceptLink && (
          <div className="mt-4 rounded-wb-md bg-wb-primary-soft p-4">
            <p className="text-sm text-wb-ink-muted">{t('invite.share', { date: formatDate(created.expiresAtUtc) })}</p>
            <p className="mt-1 font-mono text-3xl font-extrabold tracking-widest text-wb-ink">{created.code}</p>
            <p className="mt-1 break-all text-sm text-wb-ink">{acceptLink}</p>
          </div>
        )}
        {createInvitation.isError && (
          <p className="mt-2 text-sm text-wb-danger">{problemMessage(createInvitation.error, t('invite.createFailed'))}</p>
        )}
      </div>

      <form
        className={cardClass}
        onSubmit={(e) => {
          e.preventDefault()
          acceptInvitation.mutate({ code: code.trim() }, { onSuccess: () => setCode('') })
        }}
      >
        <h2 className="text-xl font-bold text-wb-ink">{t('invite.enterCode')}</h2>
        <label htmlFor="invitationCode" className="mt-3 block text-sm font-semibold text-wb-ink">
          {t('invite.codeLabel')}
        </label>
        <input
          id="invitationCode"
          value={code}
          maxLength={9}
          autoComplete="off"
          onChange={(e) => setCode(e.target.value.toUpperCase())}
          className={`${inputClass} font-mono tracking-widest`}
        />
        <button type="submit" disabled={acceptInvitation.isPending || code.trim().length < 8} className={`mt-4 ${primaryButtonClass}`}>
          {t('invite.accept')}
        </button>
        {acceptInvitation.isSuccess && <p className="mt-2 text-sm text-wb-ink">{t('invite.linked')}</p>}
        {acceptInvitation.isError && (
          <p className="mt-2 text-sm text-wb-danger">{problemMessage(acceptInvitation.error, t('invite.acceptFailed'))}</p>
        )}
      </form>
    </div>
  )
}
