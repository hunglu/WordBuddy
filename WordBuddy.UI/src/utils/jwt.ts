/** Decodes a JWT's payload without verifying its signature — for client-side expiry checks only. */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split('.')
  if (parts.length !== 3) {
    return null
  }

  try {
    const base64Url = parts[1]
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
        .join(''),
    )
    return JSON.parse(json) as Record<string, unknown>
  } catch {
    return null
  }
}

/** Returns true if the JWT's `exp` claim (seconds since epoch) is in the past, or the token is unreadable. */
export function isJwtExpired(token: string): boolean {
  const payload = decodeJwtPayload(token)
  const exp = payload?.['exp']
  if (typeof exp !== 'number') {
    return true
  }
  return exp * 1000 <= Date.now()
}
