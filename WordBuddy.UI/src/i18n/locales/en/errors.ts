/** `codes` mirrors backend problem codes (`User.AliasLength` is `codes.User.AliasLength`). */
export const errors = {
  codes: {
    Login: {
      InvalidCredentials: 'Email or password is incorrect.',
    },
    RegisterUser: {
      EmailTaken: 'This email is already registered.',
    },
    User: {
      NotFound: 'The user was not found.',
      AliasLength: 'The alias must be 3 to 20 characters.',
      AliasRevealsIdentity: 'A child alias must not be the display name or the email name.',
      AliasTaken: 'This alias is already taken.',
      UnknownAvatar: 'The avatar is not in the catalog.',
      UnknownTimeZone: 'The time zone is not known.',
      DateOfBirthInvalid: 'The date of birth is not valid.',
      DateOfBirthAgeGroupMismatch: 'The date of birth does not match the account age group.',
      DisplayNameLength: 'The display name must be 2 to 50 characters.',
      WrongCurrentPassword: 'The current password is incorrect.',
      PasswordTooWeak: 'The new password is not valid.',
      InvalidLanguage: 'The language is not valid.',
    },
    UpdatePreferredLanguage: {
      Validation: 'The language is not valid.',
    },
  },
  generic: 'Something went wrong. Please try again.',
  noAccess: 'You do not have access to this page.',
} as const
