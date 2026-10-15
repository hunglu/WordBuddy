import type {
  AdminLexemeDetail,
  AdminLexemeFilters,
  AdminLexemeListItem,
  AudioLocale,
  PagedResult,
  UpdateLexemeValues,
} from '../types'
import { apiClient } from './client'

// ---- Admin lexeme screen (AdminOnly on the server)

export async function getAdminLexemes(filters: AdminLexemeFilters): Promise<PagedResult<AdminLexemeListItem>> {
  const { data } = await apiClient.get<PagedResult<AdminLexemeListItem>>('/vocabulary/admin/lexemes', { params: filters })
  return data
}

export async function getAdminLexeme(id: string): Promise<AdminLexemeDetail> {
  const { data } = await apiClient.get<AdminLexemeDetail>(`/vocabulary/admin/lexemes/${id}`)
  return data
}

export interface UpdateLexemePayload {
  id: string
  values: UpdateLexemeValues
}

export async function updateLexeme({ id, values }: UpdateLexemePayload): Promise<void> {
  await apiClient.put(`/vocabulary/admin/lexemes/${id}`, values)
}

export interface LexemeAudioPayload {
  id: string
  locale: AudioLocale
}

/** Sets the pronunciation from one MP3 file (multipart field `file`). */
export async function uploadLexemeAudio({ id, locale, file }: LexemeAudioPayload & { file: File }): Promise<void> {
  const form = new FormData()
  form.append('file', file)
  await apiClient.put(`/vocabulary/admin/lexemes/${id}/audio/${locale}`, form)
}

/** Removes a pronunciation. The server deletes the file unless something else still uses it. */
export async function removeLexemeAudio({ id, locale }: LexemeAudioPayload): Promise<void> {
  await apiClient.delete(`/vocabulary/admin/lexemes/${id}/audio/${locale}`)
}
