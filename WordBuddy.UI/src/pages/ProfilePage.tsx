import { motion } from 'framer-motion'
import type { ReactElement } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../api/supportLinks'
import { avatarEmoji, cardClass, inputClass, primaryButtonClass } from '../components/support/supportUi'
import { AccountInfoCard } from '../components/profile/AccountInfoCard'
import { ChangePasswordForm } from '../components/profile/ChangePasswordForm'
import { DateOfBirthForm } from '../components/profile/DateOfBirthForm'
import { MySupportersCard } from '../components/profile/MySupportersCard'
import { TimeZoneForm } from '../components/profile/TimeZoneForm'
import { useAvatarCatalog, useCurrentUser, useUpdateProfile } from '../hooks/useSupportLinks'
import type { User } from '../types'

/** `/profile` — account info, alias and avatar, date of birth, time zone, password, supporters. The alias is what linked users see instead of the name. */
export function ProfilePage(): ReactElement {
  const { t } = useTranslation(['profile', 'common'])
  const currentUser = useCurrentUser()

  return (
    <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} transition={{ duration: 0.3 }}>
      <h1 className="text-3xl font-extrabold text-wb-ink">{t('profile:title')}</h1>
      {currentUser.isLoading && <p className="mt-6 text-lg text-wb-ink-muted">{t('common:loading')}</p>}
      {currentUser.isError && <p className="mt-6 text-lg text-wb-danger">{t('profile:loadFailed')}</p>}
      {/* key resets the form state when the saved profile changes */}
      {currentUser.data && (
        <ProfileForm key={`${currentUser.data.alias ?? ''}|${currentUser.data.avatarId ?? ''}`} user={currentUser.data} />
      )}
      {currentUser.data && <AccountInfoCard key={currentUser.data.displayName} user={currentUser.data} />}
      {currentUser.data && <DateOfBirthForm key={currentUser.data.dateOfBirth ?? ''} user={currentUser.data} />}
      {currentUser.data && <TimeZoneForm key={currentUser.data.timeZoneId ?? ''} user={currentUser.data} />}
      {currentUser.data && <ChangePasswordForm />}
      {currentUser.data && <MySupportersCard user={currentUser.data} />}
    </motion.div>
  )
}

type ProfileFormProps = Readonly<{ user: User }>

function ProfileForm({ user }: ProfileFormProps): ReactElement {
  const { t } = useTranslation(['profile', 'common'])
  const avatars = useAvatarCatalog()
  const update = useUpdateProfile()
  const [alias, setAlias] = useState(user.alias ?? '')
  const [avatarId, setAvatarId] = useState<string | null>(user.avatarId ?? null)
  const isChild = user.ageGroup === 'Child'

  return (
    <form
      className={`mt-6 max-w-xl ${cardClass}`}
      onSubmit={(e) => {
        e.preventDefault()
        update.mutate({ alias: alias.trim().length === 0 ? null : alias.trim(), avatarId })
      }}
    >
      <p className="text-5xl">{avatarEmoji(user.avatarId)}</p>
      <p className="mt-2 text-lg font-bold text-wb-ink">{user.alias ?? t('profile:form.noAlias')}</p>

      <label htmlFor="alias" className="mt-4 block text-sm font-semibold text-wb-ink">
        {t('profile:form.aliasLabel')}
      </label>
      <input id="alias" value={alias} maxLength={20} onChange={(e) => setAlias(e.target.value)} className={inputClass} />
      {isChild && <p className="mt-1 text-sm text-wb-ink-muted">{t('profile:form.childHint')}</p>}

      <fieldset className="mt-4">
        <legend className="text-sm font-semibold text-wb-ink">{t('profile:form.avatar')}</legend>
        {avatars.isError && <p className="text-sm text-wb-danger">{t('profile:form.avatarsFailed')}</p>}
        <div className="mt-2 flex flex-wrap gap-2">
          {avatars.data?.map((id) => (
            <button
              key={id}
              type="button"
              aria-label={id}
              aria-pressed={avatarId === id}
              onClick={() => setAvatarId(id)}
              className={`rounded-wb-md px-3 py-2 text-3xl ${
                avatarId === id ? 'bg-wb-primary-soft ring-2 ring-wb-primary' : 'bg-wb-hover-tint'
              }`}
            >
              {avatarEmoji(id)}
            </button>
          ))}
        </div>
      </fieldset>

      <button type="submit" disabled={update.isPending} className={`mt-6 ${primaryButtonClass}`}>
        {t('common:save')}
      </button>
      {update.isSuccess && <p className="mt-2 text-sm text-wb-ink">{t('common:saved')}</p>}
      {update.isError && <p className="mt-2 text-sm text-wb-danger">{problemMessage(update.error, t('profile:form.saveFailed'))}</p>}
    </form>
  )
}
