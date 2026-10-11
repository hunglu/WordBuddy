/** The browser IANA time zone (for example `Asia/Ho_Chi_Minh`), or undefined when it cannot be read. */
export function browserTimeZone(): string | undefined {
  try {
    const zone: string = Intl.DateTimeFormat().resolvedOptions().timeZone
    return zone.length > 0 ? zone : undefined
  } catch {
    return undefined
  }
}

/** All IANA time zone ids the browser knows, sorted. Empty when the browser has no support. */
export function listTimeZones(): string[] {
  try {
    return Intl.supportedValuesOf('timeZone')
  } catch {
    return []
  }
}
