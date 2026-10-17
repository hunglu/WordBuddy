import { admin } from './admin'
import { auth } from './auth'
import { challenges } from './challenges'
import { common } from './common'
import { dashboard } from './dashboard'
import { errors } from './errors'
import { groups } from './groups'
import { lessons } from './lessons'
import { nav } from './nav'
import { profile } from './profile'
import { review } from './review'
import { support } from './support'
import { vocabulary } from './vocabulary'

/** Source of truth for translation keys. Every other language is typed against this. */
export const en = {
  common,
  nav,
  auth,
  profile,
  dashboard,
  lessons,
  vocabulary,
  review,
  challenges,
  groups,
  support,
  admin,
  errors,
} as const
