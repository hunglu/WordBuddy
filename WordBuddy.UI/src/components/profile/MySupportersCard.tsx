import type { ReactElement } from 'react'
import { Link } from 'react-router-dom'
import { useSupportLinks } from '../../hooks/useSupportLinks'
import type { SupportLink, User } from '../../types'
import { avatarEmoji, cardClass } from '../support/supportUi'

type MySupportersCardProps = Readonly<{ user: User }>

/** Active supporters of the caller, Primary (guardian) first. A child always sees the card; an adult only with links. */
export function MySupportersCard({ user }: MySupportersCardProps): ReactElement | null {
  const links = useSupportLinks()
  const isChild = user.ageGroup === 'Child'

  if (links.isLoading) {
    return null
  }

  if (links.isError) {
    return (
      <section className={`mt-6 max-w-xl ${cardClass}`}>
        <h2 className="text-xl font-bold text-wb-ink">My supporters</h2>
        <p className="mt-2 text-sm text-wb-danger">Couldn't load your supporters right now.</p>
      </section>
    )
  }

  const active: SupportLink[] = (links.data?.asLearner ?? [])
    .filter((link) => link.status === 'Active')
    .sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary))

  if (!isChild && active.length === 0) {
    return null
  }

  return (
    <section className={`mt-6 max-w-xl ${cardClass}`} aria-labelledby="supporters-heading">
      <h2 id="supporters-heading" className="text-xl font-bold text-wb-ink">
        My supporters
      </h2>
      {active.length === 0 ? (
        <p className="mt-2 text-sm text-wb-ink-muted">No supporters yet.</p>
      ) : (
        <ul className="mt-3 flex flex-col gap-2">
          {active.map((link) => (
            <li key={link.id} className="flex flex-wrap items-center gap-2">
              <span className="text-lg text-wb-ink">
                {avatarEmoji(link.supporterAvatarId)} {link.supporterName}
              </span>
              {link.isPrimary && (
                <span className="rounded-wb-md bg-wb-primary-soft px-2 py-1 text-xs font-bold text-wb-ink">Guardian</span>
              )}
              {link.relationship && <span className="text-sm text-wb-ink-muted">{link.relationship}</span>}
            </li>
          ))}
        </ul>
      )}
      <Link to="/support-links" className="mt-3 inline-block text-sm font-semibold text-wb-primary">
        Manage supporters
      </Link>
    </section>
  )
}
