import type { ReactElement } from 'react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { problemMessage } from '../../api/supportLinks'
import { useUpdateTimeZone } from '../../hooks/useSupportLinks'
import type { User } from '../../types'
import { browserTimeZone, listTimeZones } from '../../utils/timeZone'
import { cardClass, inputClass, primaryButtonClass } from '../support/supportUi'

type TimeZoneFormProps = Readonly<{ user: User }>

/** Time zone select. The zone sets the learner's local day for challenge daily caps and goals. */
export function TimeZoneForm({ user }: TimeZoneFormProps): ReactElement {
  const { t } = useTranslation(['profile', 'common'])
  const update = useUpdateTimeZone()
  const [zone, setZone] = useState<string>(user.timeZoneId ?? '')
  const zones = useMemo<string[]>(() => {
    const all = listTimeZones()
    return zone.length > 0 && !all.includes(zone) ? [zone, ...all] : all
  }, [zone])
  const detected = browserTimeZone()

  return (
    <form
      className={`mt-6 max-w-xl ${cardClass}`}
      onSubmit={(e) => {
        e.preventDefault()
        update.mutate(zone.length === 0 ? null : zone)
      }}
    >
      <label htmlFor="timeZone" className="block text-sm font-semibold text-wb-ink">
        {t('profile:timeZone.label')}
      </label>
      <p className="mt-1 text-sm text-wb-ink-muted">{t('profile:timeZone.hint')}</p>
      <select id="timeZone" value={zone} onChange={(e) => setZone(e.target.value)} className={`mt-2 ${inputClass}`}>
        <option value="">{t('profile:timeZone.notSet')}</option>
        {zones.map((id) => (
          <option key={id} value={id}>
            {id}
          </option>
        ))}
      </select>
      {detected !== undefined && detected !== zone && (
        <button type="button" onClick={() => setZone(detected)} className="mt-2 text-sm font-semibold text-wb-primary">
          {t('profile:timeZone.useBrowser', { zone: detected })}
        </button>
      )}

      <button type="submit" disabled={update.isPending} className={`mt-4 block ${primaryButtonClass}`}>
        {t('profile:timeZone.save')}
      </button>
      {update.isSuccess && <p className="mt-2 text-sm text-wb-ink">{t('common:saved')}</p>}
      {update.isError && (
        <p className="mt-2 text-sm text-wb-danger">{problemMessage(update.error, t('profile:timeZone.failed'))}</p>
      )}
    </form>
  )
}
