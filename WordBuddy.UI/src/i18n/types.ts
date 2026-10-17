/** Same shape as `T`, every string leaf widened to `string`. A `vi` module typed with it must match `en` key for key. */
export type TranslationOf<T> = {
  [K in keyof T]: T[K] extends string ? string : TranslationOf<T[K]>
}
